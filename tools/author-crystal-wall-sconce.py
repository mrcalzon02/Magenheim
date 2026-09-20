"""Re-author the Crystal Wall Sconce at sconce scale.

    tools/blender.ps1 author-crystal-wall-sconce

The piece describes itself as "a stone and bronze wall bracket supporting a luminous faceted
crystal". What shipped through 0.0.85 was six axis-aligned boxes at the wrong scale: the wall plate
alone was 0.38 x 0.72 m, so at a glance the whole sconce read as a blank slab leaning on a wall with
a chip on top, and the lamp crystal was a 0.52 m column. Live play called it a Euclidean nightmare,
which is fair -- nothing in it was smaller than a doorframe fitting and nothing had an edge that
was not a right angle.

The part breakdown was never the problem, so this keeps all six objects and, critically, each
object's existing material: the geometry is rebuilt into the mesh datablock that is already bound to
that material, so the committed textures carry over untouched and no new texture has to be
generated or verified. What changes is shape and scale:

  wall-plate     0.17 x 0.34 m, chamfered face, reads as a mounting plate rather than a slab
  vertical-band  a narrow iron strap crossing the plate
  bracket        a tapering arm that rises as it projects, instead of a horizontal box
  socket         an octagonal cup at the arm's end that the crystal actually sits in
  lamp-crystal   a faceted 0.17 m point, the size of a lamp rather than a pillar
  lamp-tip       the bright terminal facet

Overall the sconce becomes 0.17 m wide, 0.52 m tall and projects 0.24 m from the wall, against
0.38 x 0.77 x 0.57 before.

Authoring convention matches tools/generate-crystal-tier-models.py: forms are built in game space,
Y up, and mapped (x, y, z) -> (x, -z, y) on the way into Blender so the exporter's own
(x, y, z) -> (x, z, -y) returns them unchanged. The wall face sits at z = 0 and the sconce projects
toward -z, which is the orientation the shipped model already used.
"""
import bpy
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source/decor-crystal-wall-sconce.blend'


class MeshBuilder:
    def __init__(self):
        self.vertices, self.faces, self.uvs = [], [], []

    def tri(self, points, uvs):
        base = len(self.vertices)
        self.vertices.extend(points)
        self.uvs.extend(uvs)
        self.faces.append((base, base + 1, base + 2))

    def signed_volume(self):
        total = 0.0
        for face in self.faces:
            a, b, c = (self.vertices[i] for i in face)
            total += (a[0] * (b[1] * c[2] - b[2] * c[1])
                      - a[1] * (b[0] * c[2] - b[2] * c[0])
                      + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6.0
        return total


def append_solid(target, solid):
    """Append a closed solid, reversing it once if it was built inside-out."""
    reverse = solid.signed_volume() < 0.0
    for face in solid.faces:
        points = [solid.vertices[i] for i in face]
        uvs = [solid.uvs[i] for i in face]
        if reverse:
            points = [points[0], points[2], points[1]]
            uvs = [uvs[0], uvs[2], uvs[1]]
        target.tri(points, uvs)


def quad(solid, a, b, c, d, uvs):
    solid.tri([a, b, c], [uvs[0], uvs[1], uvs[2]])
    solid.tri([a, c, d], [uvs[0], uvs[2], uvs[3]])


UV = [(0.06, 0.06), (0.94, 0.06), (0.94, 0.94), (0.06, 0.94)]


def ring_prism(target, levels, sides, phase=0.0):
    """A closed prism through a list of (centre, radius_x, radius_y) rings, capped at both ends."""
    rings = []
    for centre, rx, ry in levels:
        ring = []
        for k in range(sides):
            angle = math.tau * k / sides + phase
            ring.append((centre[0] + math.cos(angle) * rx,
                         centre[1],
                         centre[2] + math.sin(angle) * ry))
        rings.append(ring)

    solid = MeshBuilder()
    for s in range(len(rings) - 1):
        lo, hi = rings[s], rings[s + 1]
        for k in range(sides):
            n = (k + 1) % sides
            quad(solid, lo[k], lo[n], hi[n], hi[k], UV)
    # Opposite winding on the two caps: the far cap faces along the run and the near cap against it.
    for ring, flip in ((rings[0], True), (rings[-1], False)):
        for k in range(1, sides - 1):
            points = [ring[0], ring[k], ring[k + 1]]
            if flip:
                points.reverse()
            solid.tri(points, [UV[0], UV[1], UV[2]])
    append_solid(target, solid)


def chamfered_box(target, lo, hi, chamfer):
    """A box whose face is inset at front and back, so its silhouette is not a bare rectangle."""
    (x0, y0, z0), (x1, y1, z1) = lo, hi
    c = chamfer
    # An octagonal outline in the wall plane, extruded through the plate's depth. ring_prism's
    # circular rings run along the piece rather than across it, so the outline is built directly.
    outer = [(x0 + c, y0), (x1 - c, y0), (x1, y0 + c), (x1, y1 - c),
             (x1 - c, y1), (x0 + c, y1), (x0, y1 - c), (x0, y0 + c)]
    solid = MeshBuilder()
    count = len(outer)
    front = [(x, y, z1) for x, y in outer]
    back = [(x, y, z0) for x, y in outer]
    for k in range(count):
        n = (k + 1) % count
        quad(solid, back[k], back[n], front[n], front[k], UV)
    for ring, flip in ((back, True), (front, False)):
        for k in range(1, count - 1):
            points = [ring[0], ring[k], ring[k + 1]]
            if flip:
                points.reverse()
            solid.tri(points, [UV[0], UV[1], UV[2]])
    append_solid(target, solid)


def crystal_point(target, base, tip, radius, sides=6):
    """A faceted point: a tapering prism closed by an apex."""
    solid = MeshBuilder()
    rings = []
    for t, scale in ((0.0, 1.0), (0.55, 0.86), (0.82, 0.52)):
        centre = tuple(base[i] + (tip[i] - base[i]) * t for i in range(3))
        ring = []
        for k in range(sides):
            angle = math.tau * k / sides
            ring.append((centre[0] + math.cos(angle) * radius * scale,
                         centre[1],
                         centre[2] + math.sin(angle) * radius * scale))
        rings.append(ring)
    for s in range(len(rings) - 1):
        lo, hi = rings[s], rings[s + 1]
        for k in range(sides):
            n = (k + 1) % sides
            quad(solid, lo[k], lo[n], hi[n], hi[k], UV)
    crown = rings[-1]
    for k in range(sides):
        n = (k + 1) % sides
        solid.tri([crown[k], crown[n], tip], [UV[0], UV[1], UV[2]])
    root = rings[0]
    for k in range(1, sides - 1):
        solid.tri([root[0], root[k + 1], root[k]], [UV[0], UV[1], UV[2]])
    append_solid(target, solid)


def build():
    parts = {}

    plate = MeshBuilder()
    chamfered_box(plate, (-0.085, 0.30, -0.035), (0.085, 0.64, 0.015), 0.035)
    parts['wall-plate'] = plate

    band = MeshBuilder()
    chamfered_box(band, (-0.024, 0.33, -0.052), (0.024, 0.61, -0.030), 0.008)
    parts['vertical-band'] = band

    # A tapering arm that lifts as it reaches out, rather than a horizontal box.
    bracket = MeshBuilder()
    ring_prism(bracket, [
        ((0.0, 0.44, -0.030), 0.028, 0.026),
        ((0.0, 0.47, -0.110), 0.022, 0.021),
        ((0.0, 0.50, -0.190), 0.017, 0.016),
    ], sides=6)
    parts['bracket'] = bracket

    socket = MeshBuilder()
    ring_prism(socket, [
        ((0.0, 0.465, -0.196), 0.030, 0.030),
        ((0.0, 0.505, -0.196), 0.055, 0.055),
        ((0.0, 0.545, -0.196), 0.049, 0.049),
    ], sides=8, phase=math.tau / 16.0)
    parts['socket'] = socket

    lamp = MeshBuilder()
    crystal_point(lamp, (0.0, 0.525, -0.196), (0.0, 0.695, -0.196), 0.042, sides=6)
    parts['lamp-crystal'] = lamp

    tip = MeshBuilder()
    crystal_point(tip, (0.0, 0.660, -0.196), (0.0, 0.745, -0.196), 0.020, sides=6)
    parts['lamp-tip'] = tip

    return parts


def main():
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    if bpy.context.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')

    parts = build()
    scene = bpy.context.scene
    seen = set()
    for obj in list(scene.objects):
        if obj.type != 'MESH':
            continue
        # game_node_path is the full runtime path; the part is its last segment.
        name = str(obj.get('game_node_path', obj.name)).rsplit('/', 1)[-1]
        if name not in parts:
            raise SystemExit(f'unexpected sconce part {name!r}; expected {sorted(parts)}')
        mesh = parts[name]
        seen.add(name)

        # Replace the geometry inside the datablock that already carries this part's material, so
        # the committed textures and material settings survive untouched.
        materials = [m for m in obj.data.materials]
        if len(materials) != 1:
            raise SystemExit(f'{name}: expected exactly one material, found {len(materials)}')
        data = bpy.data.meshes.new(name)
        data.from_pydata([(v[0], -v[2], v[1]) for v in mesh.vertices], [], mesh.faces)
        data.update()
        uv_layer = data.uv_layers.new(name='UVMap')
        for loop in data.loops:
            uv_layer.data[loop.index].uv = mesh.uvs[loop.vertex_index]
        data.materials.append(materials[0])
        old = obj.data
        obj.data = data
        bpy.data.meshes.remove(old)
        obj.matrix_world.identity()

        xs = [v[0] for v in mesh.vertices]
        ys = [v[1] for v in mesh.vertices]
        zs = [v[2] for v in mesh.vertices]
        print('SCONCE %-14s tris=%-4d size=(%.3f, %.3f, %.3f)'
              % (name, len(mesh.faces), max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)))

    missing = set(parts) - seen
    if missing:
        raise SystemExit(f'sconce source is missing parts: {sorted(missing)}')

    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE), compress=True)
    print('AUTHORED decor-crystal-wall-sconce')


main()
