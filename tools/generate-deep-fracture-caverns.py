"""Rebuild the twenty Deep Fracture districts as enclosed cavern chambers.

    blender --background --factory-startup --python tools/generate-deep-fracture-caverns.py -- [DF-01 ...]

The districts shipped as flat 88x88 slabs ringed by four corner boxes and four wall
slabs, with no ceiling at all: every part was a twelve-triangle cube, so a 96 m district
carried 144-988 triangles and read as a walled courtyard rather than anything
underground. This builds a real cavern volume instead:

  * an undulating, terraced floor with walkable slopes and a central basin;
  * a soaring vault that closes the space overhead and drops to low eaves at the rim;
  * perimeter rock joining floor to vault, cut by passage mouths on all four edges so
    the districts still chain into a network;
  * stalactites, stalagmites and floor-to-ceiling columns for interior silhouette;
  * each district's authored signature features re-seated on the new floor, so the
    Cathedral, Thermal, Rime, Grotto and Heart identities survive the rebuild.

Surface normals face into the cavity because the player stands inside the shell. The
shell materials are also double-sided: an interior that is invisible from the inside is
the exact failure this project has already shipped once, and the cost of the guard is
far lower than the cost of that bug reaching a player again.

Regeneration is deterministic per district id, so re-running reproduces byte-identical
geometry. The previous sources are recoverable from Git history.
"""
import bpy
import json
import math
import random
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
RUNTIME = ROOT / 'assets/models/runtime'

DISTRICT_IDS = ['DF-%02d' % n for n in range(1, 21)]

HALF = 46.0             # shell half-extent; districts sit on a 96 m pitch
GRID = 44               # samples per axis across the footprint
RIM_EAVES = 3.6         # vault height above the floor at the rim
MIN_HEADROOM = 4.6      # guaranteed standing clearance over the walkable interior
MOUTH_HALF = 13.0       # half-width of each passage mouth
MOUTH_CLEAR = 9.0       # guaranteed clear height inside a mouth

# Some chambers reach close enough to the surface that the roof has broken open. The
# fissure is a hole in the vault, walled by a shaft that rises to a pale cap, with a
# runtime light beneath it so daylight actually spills onto the floor. Not every district
# gets one: they read as colossal partly because breaking the surface is rare.
SHAFT_HEIGHT = 16.0
FISSURE_LIGHT_RANGE = 96.0
FISSURE_LIGHT_INTENSITY = 6.4
FISSURE_LIGHT_COLOUR = (0.62, 0.71, 0.86)   # cool skylight against warm cavern rock
# Lattice spacing is about 2.14 m, so a 1.45 m rise is roughly a 34 degree slope. Valheim
# players slide above the mid-thirties, so this is the ceiling for a floor that can be
# walked rather than looked at.
MAX_STEP = 1.45
SLOPE_PASSES = 200      # relaxation passes; the field asserts convergence afterwards

SKELETON = {
    'DistrictFloor', 'BoundaryMass0', 'BoundaryMass1', 'BoundaryMass2', 'BoundaryMass3',
    'BoundaryNorthWest', 'BoundaryNorthEast', 'BoundarySouthWest', 'BoundarySouthEast',
}


# ---------------------------------------------------------------- noise & shaping

def make_noise(seed, lattice=24):
    """Seeded, wrapping value noise. Deterministic for a given district."""
    rnd = random.Random(seed)
    grid = [[rnd.random() * 2.0 - 1.0 for _ in range(lattice)] for _ in range(lattice)]

    def smooth(t):
        return t * t * (3.0 - 2.0 * t)

    def sample(x, y, frequency):
        fx, fy = x * frequency, y * frequency
        x0, y0 = math.floor(fx), math.floor(fy)
        tx, ty = smooth(fx - x0), smooth(fy - y0)
        x0 %= lattice
        y0 %= lattice
        x1, y1 = (x0 + 1) % lattice, (y0 + 1) % lattice
        top = grid[x0][y0] * (1 - tx) + grid[x1][y0] * tx
        bottom = grid[x0][y1] * (1 - tx) + grid[x1][y1] * tx
        return top * (1 - ty) + bottom * ty

    def octaves(x, y, frequency, count=3):
        total, amplitude, weight = 0.0, 1.0, 0.0
        for _ in range(count):
            total += sample(x, y, frequency) * amplitude
            weight += amplitude
            amplitude *= 0.5
            frequency *= 2.05
        return total / weight

    return octaves


def smoothstep(edge0, edge1, value):
    if edge1 <= edge0:
        return 0.0
    t = min(1.0, max(0.0, (value - edge0) / (edge1 - edge0)))
    return t * t * (3.0 - 2.0 * t)


def mouth_proximity(x, y):
    """1.0 deep inside a passage mouth, falling to 0 away from every mouth."""
    best = 0.0
    for along, across in ((x, y), (y, x)):
        near_rim = smoothstep(HALF - 16.0, HALF - 2.0, abs(across))
        in_window = 1.0 - smoothstep(MOUTH_HALF * 0.55, MOUTH_HALF, abs(along))
        best = max(best, near_rim * in_window)
    return best


class CavernField:
    """Floor and vault height fields for one district.

    The raw shaping functions can produce a terrace lip steeper than a player can climb,
    so the floor is baked onto the sample lattice once and relaxed until no neighbouring
    pair exceeds MAX_STEP. Every consumer - surfaces, walls, formations, signature
    features - then reads that single relaxed lattice, so the walkable floor the verifier
    checks is the same floor the geometry is built from.
    """

    def __init__(self, district_id, profile):
        seed = int(district_id[3:])
        self.profile = profile
        self.floor_noise = make_noise(seed * 977 + 13)
        self.vault_noise = make_noise(seed * 5417 + 91)
        self.terrace_axis = profile['terrace_axis']
        self.terrace_steps = profile['terrace_steps']
        self.terrace_drop = profile['terrace_drop']
        self.basin = profile['basin']
        self.peak = profile['vault_peak']

        self.step = (HALF * 2.0) / (GRID - 1)
        self.floor_lattice = [
            [self._raw_floor(-HALF + i * self.step, -HALF + j * self.step) for j in range(GRID)]
            for i in range(GRID)
        ]
        self._relax_slopes()
        self.vault_lattice = [
            [self._raw_vault(-HALF + i * self.step, -HALF + j * self.step, self.floor_lattice[i][j])
             for j in range(GRID)]
            for i in range(GRID)
        ]

    def _relax_slopes(self):
        """Pull neighbouring samples together until every step is climbable."""
        lattice = self.floor_lattice
        for _ in range(SLOPE_PASSES):
            worst = 0.0
            for i in range(GRID):
                for j in range(GRID):
                    for di, dj in ((1, 0), (0, 1)):
                        ni, nj = i + di, j + dj
                        if ni >= GRID or nj >= GRID:
                            continue
                        delta = lattice[ni][nj] - lattice[i][j]
                        magnitude = abs(delta)
                        if magnitude <= MAX_STEP:
                            continue
                        worst = max(worst, magnitude)
                        correction = (magnitude - MAX_STEP) * 0.5
                        if delta > 0:
                            lattice[ni][nj] -= correction
                            lattice[i][j] += correction
                        else:
                            lattice[ni][nj] += correction
                            lattice[i][j] -= correction
            if worst == 0.0:
                return
        residual = self._worst_step()
        if residual > MAX_STEP + 1e-6:
            raise RuntimeError('Floor slope relaxation did not converge: %.3f m step remains' % residual)

    def _worst_step(self):
        lattice = self.floor_lattice
        worst = 0.0
        for i in range(GRID):
            for j in range(GRID):
                for di, dj in ((1, 0), (0, 1)):
                    ni, nj = i + di, j + dj
                    if ni < GRID and nj < GRID:
                        worst = max(worst, abs(lattice[ni][nj] - lattice[i][j]))
        return worst

    def _sample(self, lattice, x, y):
        fx = (x + HALF) / self.step
        fy = (y + HALF) / self.step
        i = min(GRID - 2, max(0, int(fx)))
        j = min(GRID - 2, max(0, int(fy)))
        tx, ty = min(1.0, max(0.0, fx - i)), min(1.0, max(0.0, fy - j))
        top = lattice[i][j] * (1 - tx) + lattice[i + 1][j] * tx
        bottom = lattice[i][j + 1] * (1 - tx) + lattice[i + 1][j + 1] * tx
        return top * (1 - ty) + bottom * ty

    def floor(self, x, y):
        return self._sample(self.floor_lattice, x, y)

    def vault(self, x, y):
        return self._sample(self.vault_lattice, x, y)

    def _raw_floor(self, x, y):
        u, v = x / HALF, y / HALF
        radial = max(abs(u), abs(v))
        distance = min(1.0, math.hypot(u, v))

        height = self.floor_noise(u * 0.5 + 0.5, v * 0.5 + 0.5, 3.1) * 2.3

        # Terraced descent: the authored DescentStep idea, expressed as ground rather
        # than as a stack of boxes.
        along = (v if self.terrace_axis else u) * 0.5 + 0.5
        tier = math.floor(along * self.terrace_steps)
        height -= (tier - self.terrace_steps * 0.5) * self.terrace_drop
        # Round the tier lip so it reads as worn rock and stays walkable.
        lip = along * self.terrace_steps - tier
        height += self.terrace_drop * 0.5 * smoothstep(0.0, 0.35, lip)

        # Central basin, so the middle of the chamber sits below its approaches.
        height -= self.basin * (1.0 - smoothstep(0.0, 0.78, distance))

        # Rock climbs as it meets the wall, except where a passage must stay level.
        height += 7.5 * smoothstep(0.70, 0.99, radial) * (1.0 - mouth_proximity(x, y))

        # Mouths sit on a flat threshold so a corridor can dock against them.
        height *= 1.0 - 0.85 * mouth_proximity(x, y)
        return height

    def _raw_vault(self, x, y, floor):
        u, v = x / HALF, y / HALF
        radial = max(abs(u), abs(v))
        dome = self.peak * math.pow(max(0.0, 1.0 - radial * radial), 0.62)
        dome += self.vault_noise(u * 0.5 + 0.5, v * 0.5 + 0.5, 2.4) * 2.6

        eaves = floor + RIM_EAVES
        height = max(eaves, dome)

        # Guarantee standing room everywhere the player can actually walk, and a
        # generous opening at every passage mouth.
        interior = 1.0 - smoothstep(0.80, 0.99, radial)
        height = max(height, floor + MIN_HEADROOM * interior)
        mouth = mouth_proximity(x, y)
        if mouth > 0.0:
            height = max(height, floor + MOUTH_CLEAR * mouth)
        return height


def district_profile(district_id):
    """Per-district shaping, seeded so every regeneration is identical."""
    seed = int(district_id[3:])
    rnd = random.Random(seed * 31337)
    return {
        'terrace_axis': seed % 2,
        'terrace_steps': rnd.choice([4, 5, 6, 7]),
        'terrace_drop': rnd.uniform(1.6, 2.9),
        'basin': rnd.uniform(2.5, 6.5),
        'vault_peak': rnd.uniform(21.0, 29.5),
        'stalactites': rnd.randint(16, 26),
        'stalagmites': rnd.randint(9, 16),
        'columns': rnd.randint(2, 5),
        'clusters': [(rnd.uniform(-30.0, 30.0), rnd.uniform(-30.0, 30.0), rnd.uniform(7.0, 15.0))
                     for _ in range(rnd.randint(3, 5))],
        # Roughly half the districts break the surface, and never more than two fissures,
        # so a shaft of daylight stays an event rather than ambient lighting.
        'fissures': [
            {
                'x': rnd.uniform(-24.0, 24.0),
                'y': rnd.uniform(-24.0, 24.0),
                'major': rnd.uniform(7.0, 13.0),
                'minor': rnd.uniform(2.6, 5.2),
                'angle': rnd.uniform(0.0, math.pi),
                'wobble': rnd.uniform(0.18, 0.42),
            }
            for _ in range(rnd.choice([0, 0, 1, 1, 1, 2]))
        ],
    }


# ---------------------------------------------------------------- mesh assembly

class MeshBuilder:
    def __init__(self):
        self.vertices = []
        self.faces = []
        self.uvs = []

    def add_polygon(self, points, uvs, reference=None):
        """Append one polygon. If `reference` is given the winding is flipped when the
        face normal points away from it, which is how interior surfaces are kept
        facing the cavity."""
        if reference is not None and _normal_faces_away(points, reference):
            points = list(reversed(points))
            uvs = list(reversed(uvs))
        base = len(self.vertices)
        self.vertices.extend(points)
        self.uvs.extend(uvs)
        self.faces.append(tuple(range(base, base + len(points))))

    def add_raw(self, points, uvs):
        """Append without reorienting. Used where the caller guarantees a consistent
        topological winding and a single volume test settles the orientation afterwards."""
        base = len(self.vertices)
        self.vertices.extend(points)
        self.uvs.extend(uvs)
        self.faces.append(tuple(range(base, base + len(points))))

    def is_empty(self):
        return not self.faces

    def signed_volume(self):
        """Positive when a closed solid is wound outward."""
        total = 0.0
        for face in self.faces:
            a = self.vertices[face[0]]
            for k in range(1, len(face) - 1):
                b = self.vertices[face[k]]
                c = self.vertices[face[k + 1]]
                total += (a[0] * (b[1] * c[2] - b[2] * c[1])
                          - a[1] * (b[0] * c[2] - b[2] * c[0])
                          + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6.0
        return total


def append_solid(target, solid):
    """Append a closed solid, reversing every face if it came out inside-out.

    Orienting each face against a reference point is correct for an upright cone and wrong
    for one that hangs: with a negative span the natural ring winding reverses, and all
    eighteen stalactites in a district shipped inverted before this was measured rather than
    reasoned about. Building the solid, measuring its signed volume and correcting once is
    not subject to that mistake.
    """
    reverse = solid.signed_volume() < 0.0
    for face in solid.faces:
        points = [solid.vertices[i] for i in face]
        uvs = [solid.uvs[i] for i in face]
        if reverse:
            points, uvs = list(reversed(points)), list(reversed(uvs))
        base = len(target.vertices)
        target.vertices.extend(points)
        target.uvs.extend(uvs)
        target.faces.append(tuple(range(base, base + len(points))))


def _normal_faces_away(points, reference):
    ax, ay, az = points[0]
    bx, by, bz = points[1]
    cx, cy, cz = points[2]
    ux, uy, uz = bx - ax, by - ay, bz - az
    vx, vy, vz = cx - ax, cy - ay, cz - az
    nx = uy * vz - uz * vy
    ny = uz * vx - ux * vz
    nz = ux * vy - uy * vx
    rx, ry, rz = reference[0] - ax, reference[1] - ay, reference[2] - az
    return (nx * rx + ny * ry + nz * rz) < 0.0


def in_fissure(fissures, x, y):
    """Elliptical opening with a wobbling rim, so the break reads as torn rock."""
    for fissure in fissures:
        dx, dy = x - fissure['x'], y - fissure['y']
        ca, sa = math.cos(-fissure['angle']), math.sin(-fissure['angle'])
        u = (dx * ca - dy * sa) / fissure['major']
        v = (dx * sa + dy * ca) / fissure['minor']
        radius = math.hypot(u, v)
        wobble = 1.0 + fissure['wobble'] * math.sin(math.atan2(v, u) * 3.0 + fissure['angle'] * 5.0)
        if radius < wobble:
            return True
    return False


def build_surface(field, upward, fissures=()):
    """Floor (upward) or vault (downward) as a grid surface facing the cavity.

    Vault cells inside a fissure are omitted, and the omitted cells are returned so the
    shaft can be built against exactly the same lattice edges, leaving no seam.
    """
    builder = MeshBuilder()
    holes = set()
    step = (HALF * 2.0) / (GRID - 1)
    height = field.floor if upward else field.vault
    for i in range(GRID - 1):
        for j in range(GRID - 1):
            x0, y0 = -HALF + i * step, -HALF + j * step
            x1, y1 = x0 + step, y0 + step
            if fissures and in_fissure(fissures, x0 + step * 0.5, y0 + step * 0.5):
                holes.add((i, j))
                continue
            corners = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
            points = [(cx, cy, height(cx, cy)) for cx, cy in corners]
            uvs = [((cx + HALF) / 12.0, (cy + HALF) / 12.0) for cx, cy in corners]
            centre = sum(p[2] for p in points) / 4.0
            reference = (points[0][0], points[0][1], centre + (40.0 if upward else -40.0))
            builder.add_polygon(points, uvs, reference)
    return builder, holes


def build_shafts(field, holes, fissures=()):
    """Walls rising from each vault opening to a pale cap that stands in for the sky."""
    walls, caps = MeshBuilder(), MeshBuilder()
    if not holes:
        return walls, caps, []
    step = (HALF * 2.0) / (GRID - 1)

    def corner(i, j):
        x, y = -HALF + i * step, -HALF + j * step
        return x, y, field.vault(x, y)

    centroid_x = sum(-HALF + (i + 0.5) * step for i, _ in holes) / len(holes)
    centroid_y = sum(-HALF + (j + 0.5) * step for _, j in holes) / len(holes)
    cap_height = max(field.vault(-HALF + (i + 0.5) * step, -HALF + (j + 0.5) * step)
                     for i, j in holes) + SHAFT_HEIGHT

    for i, j in sorted(holes):
        # Any side facing a cell that is not part of the opening is a rim edge.
        sides = (
            ((i, j), (i + 1, j), (i, j - 1)),
            ((i + 1, j + 1), (i, j + 1), (i, j + 1)),
            ((i, j + 1), (i, j), (i - 1, j)),
            ((i + 1, j), (i + 1, j + 1), (i + 1, j)),
        )
        for a, b, neighbour in sides:
            if neighbour in holes:
                continue
            ax, ay, az = corner(*a)
            bx, by, bz = corner(*b)
            points = [(ax, ay, az), (bx, by, bz), (bx, by, cap_height), (ax, ay, cap_height)]
            uvs = [(0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0)]
            # Shaft walls are seen from inside the shaft looking up.
            walls.add_polygon(points, uvs, reference=(centroid_x, centroid_y, cap_height))

        x0, y0 = -HALF + i * step, -HALF + j * step
        x1, y1 = x0 + step, y0 + step
        cap = [(x0, y0, cap_height), (x1, y0, cap_height), (x1, y1, cap_height), (x0, y1, cap_height)]
        caps.add_polygon(cap, [(0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0)],
                         reference=(centroid_x, centroid_y, cap_height - 60.0))

    # One light per fissure, hung just below the roof line so each shaft throws its own
    # beam. Averaging several openings into a single anchor would light the rock between
    # them instead of the floor beneath each break.
    anchors = []
    for fissure in fissures:
        fx = min(HALF - 4.0, max(-HALF + 4.0, fissure['x']))
        fy = min(HALF - 4.0, max(-HALF + 4.0, fissure['y']))
        # Sit in the throat of the shaft, not under the roof: from here the light picks
        # out the shaft walls and pours down through the opening onto the floor.
        anchors.append((fx, fy, field.vault(fx, fy) + SHAFT_HEIGHT * 0.45))
    return walls, caps, anchors


def build_walls(field):
    """Perimeter rock from floor to vault, cut by a mouth on each edge."""
    builder = MeshBuilder()
    step = (HALF * 2.0) / (GRID - 1)
    edges = [
        (lambda t: (t, -HALF)), (lambda t: (t, HALF)),
        (lambda t: (-HALF, t)), (lambda t: (HALF, t)),
    ]
    for edge in edges:
        for i in range(GRID - 1):
            t0 = -HALF + i * step
            t1 = t0 + step
            a, b = edge(t0), edge(t1)
            if mouth_proximity(*a) > 0.30 or mouth_proximity(*b) > 0.30:
                continue  # leave the passage mouth open
            fa, fb = field.floor(*a), field.floor(*b)
            ca, cb = field.vault(*a), field.vault(*b)
            points = [(a[0], a[1], fa), (b[0], b[1], fb), (b[0], b[1], cb), (a[0], a[1], ca)]
            uvs = [(0.0, fa / 10.0), (step / 10.0, fb / 10.0),
                   (step / 10.0, cb / 10.0), (0.0, ca / 10.0)]
            builder.add_polygon(points, uvs, reference=(0.0, 0.0, (fa + ca) * 0.5))
    return builder


def add_cone(target, x, y, base_z, tip_z, radius, sides=7, seed_rnd=None, rings=4):
    """Build one formation in isolation, then append it with its winding corrected."""
    builder = MeshBuilder()
    _build_cone(builder, x, y, base_z, tip_z, radius, sides, seed_rnd, rings)
    append_solid(target, builder)


def _build_cone(builder, x, y, base_z, tip_z, radius, sides=7, seed_rnd=None, rings=4):
    """A dripstone formation: a stack of jittered rings that leans and bulges as it tapers.

    A single smooth taper reads as a traffic cone rather than rock, so the profile is built
    from several rings whose radius, angle and height are all perturbed, with a lateral
    drift that accumulates toward the tip.
    """
    rnd = seed_rnd or random.Random(0)
    span = tip_z - base_z
    lean_x = rnd.uniform(-0.16, 0.16) * abs(span)
    lean_y = rnd.uniform(-0.16, 0.16) * abs(span)
    twist = rnd.uniform(0.0, math.tau)
    bulge = rnd.uniform(0.7, 1.45)

    levels = []
    for r in range(rings):
        t = r / float(rings)
        # A bulged taper: fatter low down, pinching unevenly toward the tip.
        profile = math.pow(max(0.0, 1.0 - t), bulge) * rnd.uniform(0.84, 1.16)
        level_radius = max(0.12, radius * profile)
        cx = x + lean_x * t * t
        cy = y + lean_y * t * t
        cz = base_z + span * t
        points = []
        for k in range(sides):
            angle = math.tau * k / sides + twist + rnd.uniform(-0.16, 0.16)
            spoke = level_radius * rnd.uniform(0.68, 1.34)
            points.append((cx + math.cos(angle) * spoke,
                           cy + math.sin(angle) * spoke,
                           cz + rnd.uniform(-0.05, 0.05) * abs(span)))
        levels.append(points)

    tip = (x + lean_x, y + lean_y, tip_z)
    axis_at = lambda t: (x + lean_x * t * t, y + lean_y * t * t, base_z + span * t)

    # Skin between successive rings.
    for r in range(rings - 1):
        lower, upper = levels[r], levels[r + 1]
        centre = axis_at((r + 0.5) / float(rings))
        for k in range(sides):
            a, b = lower[k], lower[(k + 1) % sides]
            c, d = upper[(k + 1) % sides], upper[k]
            away = (centre[0] + (a[0] - centre[0]) * 8.0,
                    centre[1] + (a[1] - centre[1]) * 8.0,
                    centre[2])
            # Emit two triangles rather than a quad. Ring vertices are jittered in radius
            # and height, so a band quad is often badly non-planar; the exporter then
            # triangulates it and one half can face opposite its neighbours. That is what
            # verify-model-assets found in DF-01/DistrictSignature. Triangles are planar by
            # construction, so the ambiguity cannot arise.
            lo, hi = r / float(rings), (r + 1) / float(rings)
            builder.add_raw([a, b, c], [(0.0, lo), (1.0, lo), (1.0, hi)])
            builder.add_raw([a, c, d], [(0.0, lo), (1.0, hi), (0.0, hi)])

    # Close the tip.
    crown = levels[-1]
    centre = axis_at(1.0)
    for k in range(sides):
        a, b = crown[k], crown[(k + 1) % sides]
        away = (centre[0] + (a[0] - centre[0]) * 8.0,
                centre[1] + (a[1] - centre[1]) * 8.0,
                centre[2])
        builder.add_raw([a, b, tip], [(0.0, 0.8), (1.0, 0.8), (0.5, 1.0)])

    # Cap the base so the formation is a closed solid.
    outside = (x, y, base_z + span * 4.0)
    builder.add_raw(list(reversed(levels[0])),
                    [(math.cos(math.tau * k / sides) * 0.5 + 0.5,
                      math.sin(math.tau * k / sides) * 0.5 + 0.5)
                     for k in range(sides)])


def build_formations(field, profile, district_id):
    """Stalactites, stalagmites and columns, placed where they do not block movement."""
    rnd = random.Random(int(district_id[3:]) * 7717 + 5)
    tites, mites, columns = MeshBuilder(), MeshBuilder(), MeshBuilder()

    clusters = profile['clusters']

    def candidate():
        """Formations gather around a handful of centres, leaving open floor between them:
        an even scatter reads as a spike field and clutters every walking line."""
        if rnd.random() < 0.78 and clusters:
            cx, cy, spread = rnd.choice(clusters)
            x = cx + rnd.gauss(0.0, spread)
            y = cy + rnd.gauss(0.0, spread)
        else:
            x = rnd.uniform(-HALF + 6.0, HALF - 6.0)
            y = rnd.uniform(-HALF + 6.0, HALF - 6.0)
        limit = HALF - 6.0
        return min(limit, max(-limit, x)), min(limit, max(-limit, y))

    def scale_draw():
        """Mostly modest formations with an occasional large one, rather than one size."""
        roll = rnd.random()
        if roll > 0.88:
            return rnd.uniform(2.6, 4.6)
        if roll > 0.55:
            return rnd.uniform(1.3, 2.5)
        return rnd.uniform(0.6, 1.3)

    for _ in range(profile['stalactites']):
        for _attempt in range(12):
            x, y = candidate()
            if mouth_proximity(x, y) > 0.05:
                continue
            floor, vault = field.floor(x, y), field.vault(x, y)
            if vault - floor < MIN_HEADROOM + 3.0:
                continue
            drop = min((vault - floor) * rnd.uniform(0.18, 0.52), 11.0)
            add_cone(tites, x, y, vault, vault - drop, scale_draw(), 7, rnd, rings=rnd.randint(3, 5))
            break

    for _ in range(profile['stalagmites']):
        for _attempt in range(12):
            x, y = candidate()
            if mouth_proximity(x, y) > 0.05:
                continue
            floor, vault = field.floor(x, y), field.vault(x, y)
            if vault - floor < MIN_HEADROOM:
                continue
            rise = min((vault - floor) * rnd.uniform(0.15, 0.44), 8.5)
            add_cone(mites, x, y, floor, floor + rise, scale_draw(), 7, rnd, rings=rnd.randint(3, 5))
            break

    for _ in range(profile['columns']):
        for _attempt in range(16):
            x, y = candidate()
            if mouth_proximity(x, y) > 0.02:
                continue
            floor, vault = field.floor(x, y), field.vault(x, y)
            if vault - floor < 9.0:
                continue
            radius = rnd.uniform(1.6, 3.4)
            waist = (floor + vault) * 0.5
            add_cone(columns, x, y, floor, waist, radius, 9, rnd)
            add_cone(columns, x, y, vault, waist, radius, 9, rnd)
            break

    return tites, mites, columns


def build_signature(field, district_id):
    """Re-seat the district's authored features on the new floor."""
    runtime = RUNTIME / (district_id + '.model.json')
    builder = MeshBuilder()
    if not runtime.exists():
        return builder
    document = json.loads(runtime.read_text(encoding='utf-8'))
    rnd = random.Random(int(district_id[3:]) * 104729)
    for part in document.get('parts', []):
        if part['name'] in SKELETON:
            continue
        vertices = part['vertices']
        xs = [v[0] for v in vertices]
        zs = [v[2] for v in vertices]
        ys = [v[1] for v in vertices]
        # Game space is Y-up; Blender is Z-up.
        cx = (min(xs) + max(xs)) * 0.5
        cy = (min(zs) + max(zs)) * 0.5
        extent = max(max(xs) - min(xs), max(zs) - min(zs)) * 0.5
        height = max(1.5, max(ys) - min(ys))
        if abs(cx) > HALF - 8.0 or abs(cy) > HALF - 8.0:
            continue
        if mouth_proximity(cx, cy) > 0.05:
            continue
        floor = field.floor(cx, cy)
        vault = field.vault(cx, cy)
        top = min(floor + height, vault - 1.0)
        if top <= floor + 0.8:
            continue
        add_cone(builder, cx, cy, floor, top, max(1.2, min(extent, 6.0)), 9, rnd)
    return builder


# ---------------------------------------------------------------- Blender output

TEXTURE_SIZE = 256


def surface_map(name, seed, kind):
    """Greyscale albedo for a cavern surface.

    The districts shipped with flat Principled base colours and no map at all, which is the
    same reason the rest of the library reads as clay. These are the largest surfaces the
    player ever stands on, so a flat colour is most obvious here.
    """
    noise = make_noise(seed)

    def sample(u, v):
        n = noise(u, v, 5.0) * 0.55 + noise(u, v, 13.0) * 0.30 + noise(u, v, 31.0) * 0.15
        n = n * 0.5 + 0.5
        if kind == 'rock':
            return 0.30 + 0.40 * n
        if kind == 'vault':
            # Darker overhead, with broader banding so the roof does not read as a dome.
            band = 0.5 + 0.5 * math.sin(v * math.tau * 2.0 + n * 3.0)
            return 0.22 + 0.30 * n + 0.08 * band
        if kind == 'wall':
            strata = 0.5 + 0.5 * math.sin(v * math.tau * 7.0 + n * 2.0)
            return 0.26 + 0.30 * n + 0.12 * strata
        if kind == 'formation':
            return 0.34 + 0.44 * n
        if kind == 'daylight':
            return 0.86 + 0.14 * n
        facet = 0.5 + 0.5 * math.sin(u * math.tau * 6.0) * math.cos(v * math.tau * 4.0)
        return 0.46 + 0.34 * facet + 0.12 * n

    image = bpy.data.images.new(name, TEXTURE_SIZE, TEXTURE_SIZE)
    pixels = [0.0] * (TEXTURE_SIZE * TEXTURE_SIZE * 4)
    for y in range(TEXTURE_SIZE):
        vv = (y + 0.5) / TEXTURE_SIZE
        for x in range(TEXTURE_SIZE):
            value = min(1.0, max(0.0, sample((x + 0.5) / TEXTURE_SIZE, vv)))
            i = (y * TEXTURE_SIZE + x) * 4
            pixels[i] = pixels[i + 1] = pixels[i + 2] = value
            pixels[i + 3] = 1.0
    image.pixels = pixels
    # A generated image stores only its settings in the .blend, not its buffer, so without
    # packing here the export reopens the file and reads the default generated colour -
    # black. Every texture produced in this session shipped black before this line existed.
    image.update()
    image.pack()
    return image


def material(name, colour, roughness, metallic=0.0, kind='rock', seed=1):
    existing = bpy.data.materials.get(name)
    if existing:
        return existing
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    # Interior shells are double-sided on purpose: see the module docstring.
    mat.use_backface_culling = False
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = colour
    shader.inputs['Roughness'].default_value = roughness
    shader.inputs['Metallic'].default_value = metallic
    node = mat.node_tree.nodes.new('ShaderNodeTexImage')
    node.image = surface_map(name + '.albedo', seed, kind)
    mat.node_tree.links.new(node.outputs['Color'], shader.inputs['Base Color'])
    return mat


def emit(name, builder, mat, collision=True):
    if builder.is_empty():
        return None
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(builder.vertices, [], builder.faces)
    mesh.update()
    uv_layer = mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:
        uv_layer.data[loop.index].uv = builder.uvs[loop.vertex_index]
    mesh.materials.append(mat)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj['game_node_path'] = name
    obj['game_collision'] = collision
    obj['game_crystal'] = 'null'
    return obj


def build_district(district_id):
    profile = district_profile(district_id)
    field = CavernField(district_id, profile)

    bpy.ops.wm.read_factory_settings(use_empty=True)

    rock = material('df-cavern-rock', (0.62, 0.58, 0.54, 1.0), 0.88, kind='rock', seed=11)
    vault_rock = material('df-cavern-vault', (0.52, 0.50, 0.49, 1.0), 0.92, kind='vault', seed=23)
    wall_rock = material('df-cavern-wall', (0.58, 0.55, 0.52, 1.0), 0.9, kind='wall', seed=37)
    formation = material('df-cavern-formation', (0.68, 0.65, 0.62, 1.0), 0.8, kind='formation', seed=53)
    crystal = material('df-cavern-crystal', (0.66, 0.74, 0.86, 1.0), 0.42, 0.18, kind='crystal', seed=71)
    # Stands in for open sky at the top of a fissure; the runtime light does the work.
    daylight = material('df-cavern-daylight', (0.86, 0.90, 0.98, 1.0), 1.0, kind='daylight', seed=89)

    fissures = profile['fissures']
    floor_surface, _ = build_surface(field, True)
    vault_surface, holes = build_surface(field, False, fissures)
    shaft_walls, shaft_caps, light_anchors = build_shafts(field, holes, fissures)

    emit('CavernFloor', floor_surface, rock)
    emit('CavernVault', vault_surface, vault_rock)
    emit('CavernWalls', build_walls(field), wall_rock)
    emit('CavernShaft', shaft_walls, wall_rock)
    emit('CavernSkyCap', shaft_caps, daylight, collision=False)

    for index, (lx, ly, lz) in enumerate(light_anchors):
        lamp_data = bpy.data.lights.new('FissureLight%d' % index, type='POINT')
        lamp_data.color = FISSURE_LIGHT_COLOUR
        lamp_data.energy = FISSURE_LIGHT_INTENSITY * 100.0   # export divides by 100
        lamp_data.cutoff_distance = FISSURE_LIGHT_RANGE
        lamp = bpy.data.objects.new('FissureLight%d' % index, lamp_data)
        lamp.location = (lx, ly, lz)
        bpy.context.collection.objects.link(lamp)

    tites, mites, columns = build_formations(field, profile, district_id)
    emit('CavernStalactites', tites, formation)
    emit('CavernStalagmites', mites, formation)
    emit('CavernColumns', columns, formation)
    emit('DistrictSignature', build_signature(field, district_id), crystal)

    scene = bpy.context.scene
    scene['model_id'] = district_id
    scene['runtime_lights'] = '[]'
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / (district_id + '.blend')), compress=True)

    triangles = sum(len(o.data.polygons) for o in scene.objects if o.type == 'MESH')
    print('CAVERN %s objects=%d polygons=%d vault_peak=%.1f fissures=%d lights=%d' % (
        district_id, len([o for o in scene.objects if o.type == 'MESH']),
        triangles, profile['vault_peak'], len(fissures), len(light_anchors)))


def main():
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    targets = args or DISTRICT_IDS
    for district_id in targets:
        if district_id not in DISTRICT_IDS:
            raise SystemExit('Not a Deep Fracture district: ' + district_id)
        build_district(district_id)


main()
