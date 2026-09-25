"""Author the Fungal Forest's own flora and resources, replacing its vanilla donor stand-ins.

    tools/blender.ps1 author-underworld-fungal-forest [model-id ...]
    tools/blender.ps1 export-model-assets <the same ids>

Design authority: docs/UNDERWORLD_FLORA_TERRAIN_PLAN.md sections 4.1 and 5. The ecology preview
assembled this biome from ten vanilla prefabs -- Yggdrasil shoots, Magecap, Jotun Puffs and blue
mushrooms blown up five to twelve times, a Plains fern, a heath shrub, a rock and a root. This is
the plan's custom pass for that biome: the fungal tree species it names, the ground layer it
describes, and the four raw resources the biome yields.

Everything is authored at real size in meters, standing on the origin, Z up. Only tree stalks
collide (`game_collision`), so ground cover stays walk-through. Light comes from emissive gills,
caps and spore bulbs rather than per-plant point lights: the canopy is dense and a light per tree
would be the most expensive thing in the cavern. Regrowth and the Motherbloom are out of scope
(plan section 10 decisions 1 and 6); nothing here assumes either.
"""
import bpy
import bmesh
import math
import random
import sys
from functools import partial
from pathlib import Path

from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from magenheim_blender_kit import (  # noqa: E402
    blob, curve, lathe, merge, spec_painter, spike, transformed, unwrap, uv_overlap)
from magenheim_flora_kit import Model, buttresses, cap, gills, small_mushrooms, tube, bake_flora_atlas  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/models/source'
REVISION = 'fungal-forest-canopy-variants-3'
Z = Vector((0.0, 0.0, 1.0))

# ------------------------------------------------------------------------------------------------
# Surfaces
# ------------------------------------------------------------------------------------------------

SURFACES = {
    # Fibrous stalk flesh: grain runs up the stalk.
    'stalk': dict(low=(0.30, 0.27, 0.32), high=(0.58, 0.53, 0.56), scale=3, stretch=(9.0, 9.0, 0.5),
                  edge=((0.86, 0.84, 0.90), 0.35), occlusion=0.9, metallic=0.0, rough=0.82),
    'cap-teal': dict(low=(0.04, 0.17, 0.21), high=(0.12, 0.40, 0.42), scale=4,
                     veins=((0.36, 0.74, 0.70), 0.018, 3.0), edge=((0.50, 0.86, 0.80), 0.4), occlusion=0.85,
                     metallic=0.0, rough=0.55),
    'cap-violet': dict(low=(0.16, 0.08, 0.26), high=(0.40, 0.22, 0.54), scale=4,
                       veins=((0.72, 0.52, 0.90), 0.018, 3.0), edge=((0.80, 0.66, 0.95), 0.4), occlusion=0.85,
                       metallic=0.0, rough=0.55),
    'gill': dict(low=(0.30, 0.82, 0.76), high=(0.72, 1.00, 0.94), scale=12, edge=((0.9, 1.0, 1.0), 0.3),
                 occlusion=0.3, metallic=0.0, rough=0.5, emission=(0.08, 0.42, 0.38)),
    'gill-violet': dict(low=(0.60, 0.40, 0.90), high=(0.90, 0.78, 1.00), scale=12, edge=((1.0, 0.95, 1.0), 0.3),
                        occlusion=0.3, metallic=0.0, rough=0.5, emission=(0.30, 0.14, 0.46)),
    'amber': dict(low=(0.50, 0.26, 0.05), high=(0.94, 0.64, 0.20), scale=6,
                  veins=((1.00, 0.86, 0.46), 0.030, 7.0), edge=((1.0, 0.88, 0.55), 0.4), occlusion=0.8,
                  metallic=0.0, rough=0.6, emission=(0.18, 0.08, 0.01)),
    'worldroot': dict(low=(0.13, 0.19, 0.13), high=(0.34, 0.44, 0.30), scale=3, stretch=(8.0, 8.0, 0.6),
                      edge=((0.56, 0.66, 0.50), 0.4), occlusion=0.95, metallic=0.0, rough=0.85),
    'fern': dict(low=(0.10, 0.36, 0.27), high=(0.36, 0.72, 0.56), scale=9, edge=((0.70, 0.95, 0.80), 0.5),
                 occlusion=0.7, metallic=0.0, rough=0.7),
    'brush': dict(low=(0.28, 0.16, 0.38), high=(0.58, 0.40, 0.70), scale=10, edge=((0.80, 0.66, 0.90), 0.4),
                  occlusion=0.7, metallic=0.0, rough=0.7),
    'spore': dict(low=(0.80, 0.42, 0.78), high=(1.00, 0.80, 0.96), scale=10, edge=((1.0, 0.95, 1.0), 0.3),
                  occlusion=0.2, metallic=0.0, rough=0.4, emission=(0.40, 0.14, 0.36)),
    'lantern': dict(low=(0.20, 0.50, 0.72), high=(0.58, 0.88, 0.98), scale=7, edge=((0.9, 1.0, 1.0), 0.4),
                    occlusion=0.3, metallic=0.0, rough=0.4, emission=(0.06, 0.24, 0.34)),
    'nursery': dict(low=(0.18, 0.52, 0.48), high=(0.48, 0.88, 0.80), scale=7, edge=((0.8, 1.0, 0.96), 0.4),
                    occlusion=0.5, metallic=0.0, rough=0.5, emission=(0.03, 0.12, 0.10)),
    'stone': dict(low=(0.20, 0.22, 0.22), high=(0.44, 0.46, 0.44), scale=5,
                  veins=((0.14, 0.15, 0.15), 0.02, 4.0), edge=((0.62, 0.64, 0.60), 0.8), occlusion=1.0,
                  metallic=0.0, rough=0.92),
    'moss': dict(low=(0.14, 0.30, 0.18), high=(0.38, 0.60, 0.34), scale=18, edge=((0.55, 0.78, 0.50), 0.3),
                 occlusion=0.9, metallic=0.0, rough=0.95),
    'root': dict(low=(0.28, 0.18, 0.09), high=(0.58, 0.42, 0.24), scale=3, stretch=(8.0, 8.0, 0.6),
                 edge=((0.78, 0.62, 0.40), 0.4), occlusion=0.95, metallic=0.0, rough=0.85),
    'understone': dict(low=(0.22, 0.24, 0.28), high=(0.46, 0.48, 0.52), scale=6,
                       veins=((0.62, 0.52, 0.80), 0.015, 5.0), edge=((0.70, 0.72, 0.76), 0.9), occlusion=1.0,
                       metallic=0.0, rough=0.9),
}
# Occlusion radius scaled for trees and rocks rather than hand-held objects.
paint = spec_painter(SURFACES, ao_distance=0.25, edge_radius=0.012)


# ------------------------------------------------------------------------------------------------
# Shared forms
# ------------------------------------------------------------------------------------------------

def glowcap(m, scale=1.0, collider=True):
    rng = random.Random(7)
    s = scale
    top = Vector((0.30 * s, 0.10 * s, 6.30 * s))
    m.part('stalk', tube([Vector((0, 0, -0.10 * s)), Vector((0.05 * s, 0.0, 2.2 * s)), Vector((0.18 * s, 0.06 * s, 4.4 * s)), top],
                         [(0.0, 0.62 * s), (0.08, 0.44 * s), (0.5, 0.35 * s), (0.92, 0.30 * s), (1.0, 0.36 * s)], 14, 14),
           'stalk', collider=collider)
    m.part('roots', buttresses((0, 0, 0), 5, 1.4 * s, 0.9 * s, 0.22 * s, rng), 'stalk')
    m.part('cap', cap((top.x, top.y, top.z - 0.05 * s), 3.2 * s, 1.55 * s), 'cap-teal')
    m.part('gills', gills((top.x, top.y, top.z + 0.02 * s), 0.5 * s, 2.95 * s, 0.26 * s, 30, 0.02 * s), 'gill', smooth=False)


def spirestalk(m, height=1.0, bend=1.0, tiers=((0.60, 1.5), (0.80, 1.1), (0.985, 0.78))):
    rng = random.Random(11)
    points = [Vector((0, 0, -0.1)), Vector((0.25, -0.1, 3.5)), Vector((-0.15, 0.1, 7.0)), Vector((0.35, -0.2, 9.6)),
              Vector((0.55, -0.3, 11.7))]
    points = [Vector((p.x * bend, p.y * bend, p.z * height)) for p in points]
    m.part('stalk', tube(points, [(0.0, 0.46), (0.06, 0.32), (0.5, 0.24), (0.85, 0.17), (1.0, 0.13)], 12, 18),
           'stalk', collider=True)
    m.part('roots', buttresses((0, 0, 0), 4, 1.1, 0.7, 0.16, rng), 'stalk')
    path = curve(points, 60)
    tier_meshes = []
    under = []
    for t, r in tiers:
        p = path[int(t * (len(path) - 1))]
        tier_meshes.append(cap((p.x, p.y, p.z - 0.10), r, r * 0.55, bell=0.10, sides=20))
        under.append(gills((p.x, p.y, p.z - 0.02), 0.2, r * 0.9, 0.14, 20, 0.014))
    m.part('caps', merge(*tier_meshes), 'cap-violet')
    m.part('gills', merge(*under), 'gill-violet', smooth=False)


def puffcap(m, crown=((0.3, 1.2, 2.3, 1.15), (2.4, 1.0, 1.6, 0.95), (4.4, 1.3, 1.9, 1.05)), trunk_height=2.1):
    rng = random.Random(13)
    # One fused stalk/branch/root skin: no flat cylinder collars at branch insertions.
    trunk_top = Vector((0.1, 0.0, trunk_height))
    flesh = [tube([Vector((0, 0, -0.1)), Vector((-0.12, 0.1, 0.8)), trunk_top],
                  [(0.0, 0.62), (0.17, 0.43), (0.65, 0.36), (1.0, 0.26)], 14, 16),
             buttresses((0, 0, 0), 5, 1.25, 0.85, 0.24, rng)]
    puffs = []
    for i, (a, reach, rise, r) in enumerate(crown):
        d = Vector((math.cos(a), math.sin(a), 0.0))
        end = trunk_top + d * reach + Z * rise
        flesh.append(tube([trunk_top-Z*(1.15-i*.18), trunk_top+d*reach*.18+Z*rise*.15,
                           end-Z*.3, end+Z*r*.35],
                          [(0.0, .32), (.35, .3), (.72, .19), (.9, .34), (1.0, r*.62)], 14, 18))
        # Lobed fruiting body instead of identical spherical bulbs sitting on chopped tubes.
        puffs.append(blob(tuple(end + Z*r*.65), (r, r*.88, r*1.08), 20, 14))
    stalk = m.part('fused-stalk', merge(*flesh), 'stalk', collider=True)
    fuse_stalk(stalk)
    m.part('puffs', merge(*puffs), 'amber')


def fuse_stalk(stalk):
    """Join authored branching tissue before baking; preserve one collidable organism."""
    bpy.context.view_layer.objects.active = stalk
    stalk.select_set(True)
    remesh = stalk.modifiers.new('Organic fused branch junctions', 'REMESH')
    remesh.mode = 'VOXEL'
    remesh.voxel_size = .055
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    soften = stalk.modifiers.new('Growing tissue transitions', 'SMOOTH')
    soften.factor = 1.2
    soften.iterations = 4
    bpy.ops.object.modifier_apply(modifier=soften.name)
    decimate = stalk.modifiers.new('Game silhouette budget', 'DECIMATE')
    decimate.ratio = .24
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    # Decimation can leave folded n-gons whose polygon normal is zero even though their
    # individual triangles have area. Triangulate and clean the authored mesh before UV bake.
    bm = bmesh.new()
    bm.from_mesh(stalk.data)
    # Voxel/decimation can leave disconnected two-sided slivers with cancelling normals.
    # This skin is intentionally one connected organism; retain only its main component.
    unseen = set(bm.verts)
    components = []
    while unseen:
        pending = [unseen.pop()]
        component = set(pending)
        while pending:
            for edge in pending.pop().link_edges:
                for vertex in edge.verts:
                    if vertex in unseen:
                        unseen.remove(vertex)
                        component.add(vertex)
                        pending.append(vertex)
        components.append(component)
    main = max(components, key=len)
    bmesh.ops.delete(bm, geom=[v for c in components if c is not main for v in c], context='VERTS')
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    bmesh.ops.dissolve_degenerate(bm, edges=list(bm.edges), dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(stalk.data)
    bm.free()
    stalk.data.update()
    for poly in stalk.data.polygons: poly.use_smooth = True
    stalk.data.set_sharp_from_angle(angle=math.radians(55.0))
    stalk.select_set(False)


def tanglecap(m, count=4, height=1.0, spread=.221, twist=2.4, crowns=3):
    rng = random.Random(17)
    strands = []
    tips = []
    for i in range(count):
        phase = math.tau * i / count
        points = []
        for k in range(7):
            t = k / 6
            r = .34 * (1.0 - .35 * t) + (spread - .221) * t * t
            a = phase + t * twist
            points.append(Vector((r * math.cos(a), r * math.sin(a), -0.1 + t * (5.2 + 0.5 * i) * height)))
        strands.append(tube(points, [(0.0, 0.24), (0.8, 0.14), (1.0, 0.10)], 10, 16))
        tips.append(points[-1])
    m.part('strands', merge(*strands), 'worldroot', collider=True)
    m.part('roots', buttresses((0, 0, 0), 5, 1.2, 0.6, 0.18, rng), 'worldroot')
    caps, under = [], []
    for i, p in enumerate(tips[:crowns]):
        r = (0.95, 0.75, 0.6, 1.1, .7, .85)[i]
        caps.append(cap((p.x, p.y, p.z - 0.06), r, r * 0.5, sides=18))
        under.append(gills((p.x, p.y, p.z), 0.12, r * 0.88, 0.1, 16, 0.012))
    m.part('caps', merge(*caps), 'cap-teal')
    m.part('gills', merge(*under), 'gill', smooth=False)


def branching_glowcap(m, form):
    # Crown height, reach and count change the silhouette independently of instance scale.
    crowns = {
        'bent': [((2.6, .4, 5.1), 2.7, 1.2, -.22)],
        'twin': [((-1.8, .2, 6.2), 2.2, 1.1, .12), ((2.2, -.5, 4.9), 2.5, 1.3, -.16)],
        'elder': [((.6, -.3, 9.0), 4.5, 1.35, .08)],
    }[form]
    junction = Vector((-.15, .1, 2.6 if form == 'twin' else 1.5))
    flesh = [tube([Vector((0, 0, -.12)), Vector((-.25, .1, .9)), junction],
                  [(0, .8), (.3, .58), (1, .43)], 14, 12),
             buttresses((0, 0, 0), 6, 1.9 if form == 'elder' else 1.4, 1.1, .28, random.Random(83))]
    caps, under = [], []
    for point, radius, depth, tilt in crowns:
        top = Vector(point)
        flesh.append(tube([junction-Z*.4, junction.lerp(top, .35)+Vector((-.35, .2, 0)), top+Z*.25],
                          [(0, .48), (.65, .34), (1, .46)], 14, 20))
        transform = Matrix.Translation(top) @ Matrix.Rotation(tilt, 4, 'Y')
        caps.append(transformed(cap((0, 0, 0), radius, depth, sides=28), transform))
        under.append(transformed(gills((0, 0, .03), .48, radius*.92, .25, 30, .018), transform))
    stalk = m.part('fused-stalk', merge(*flesh), 'stalk', collider=True)
    fuse_stalk(stalk)
    m.part('caps', merge(*caps), 'cap-teal')
    m.part('gills', merge(*under), 'gill', smooth=False)


def fern(m):
    rng = random.Random(19)
    fronds = []
    for i in range(9):
        a = math.tau * i / 9 + rng.uniform(-0.15, 0.15)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        length = rng.uniform(0.85, 1.15)
        points = [Vector((0, 0, 0.04)), d * 0.12 * length + Z * 0.45 * length, d * 0.42 * length + Z * 0.78 * length,
                  d * 0.62 * length + Z * 0.70 * length, d * 0.60 * length + Z * 0.54 * length]
        # Each frond curls within the plane of d and Z, so that plane's normal is a safe reference.
        fronds.append(spike(points, 0.085, 0.006, 12, flat=True, up=Z.cross(d).normalized()))
    m.part('fronds', merge(*fronds), 'fern', smooth=True)
    m.part('crown', blob((0, 0, 0.05), (0.09, 0.09, 0.07), 8, 5), 'fern')


def sporebrush(m):
    rng = random.Random(23)
    stalks, bulbs = [], []
    for i in range(14):
        a = rng.uniform(0, math.tau)
        lean = rng.uniform(0.05, 0.35)
        h = rng.uniform(0.6, 1.05)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        top = d * lean + Z * h
        stalks.append(spike([Vector((0, 0, 0)), d * lean * 0.3 + Z * h * 0.5, top + Z * 0.02], 0.016, 0.016, 8))
        bulbs.append(blob(tuple(top), (0.05, 0.05, 0.06), 8, 5))
    m.part('stalks', merge(*stalks), 'brush', smooth=True)
    m.part('bulbs', merge(*bulbs), 'spore')


def amber_bed(m):
    small_mushrooms(m, 7, 'amber', 'stalk', (0.06, 0.16), 0.0, 29, 0.32)


def lantern_caps(m):
    # Slender bells on tall stems, standing to knee height: lanterns, not buttons.
    small_mushrooms(m, 5, 'lantern', 'stalk', (0.045, 0.075), 0.45, 31, 0.18, height=(5.0, 8.0))


def nursery_caps(m):
    rng = random.Random(37)
    caps = []
    stalks = []
    for i in range(6):
        a = rng.uniform(0, math.tau)
        d = rng.uniform(0.0, 0.35)
        x, y = d * math.cos(a), d * math.sin(a)
        r = rng.uniform(0.12, 0.28)
        h = r * rng.uniform(0.35, 0.8)
        stalks.append(lathe([(0.0, r * 0.25), (h, r * 0.18)], 8, centre=(x, y)))
        caps.append(cap((x, y, h), r, r * 0.28, sides=16))
    m.part('stalks', merge(*stalks), 'stalk')
    m.part('caps', merge(*caps), 'nursery')


def moss_stone(m):
    rng = random.Random(41)
    m.part('stone', blob((0, 0, 0.30), (0.72, 0.55, 0.52), 7, 5), 'stone', smooth=False)
    crust = [blob((rng.uniform(-0.35, 0.35), rng.uniform(-0.25, 0.25), 0.72 + rng.uniform(-0.05, 0.04)),
                  (rng.uniform(0.18, 0.32), rng.uniform(0.15, 0.26), 0.06), 8, 4) for _ in range(5)]
    m.part('moss', merge(*crust), 'moss')
    small_caps = [cap((0.55 * math.cos(a), 0.40 * math.sin(a), 0.42), 0.09, 0.05, sides=12) for a in (0.4, 1.3, 2.2)]
    m.part('shelf-caps', merge(*small_caps), 'nursery')


def root_skirt(m):
    rng = random.Random(43)
    arches = []
    for i in range(5):
        a = math.tau * i / 5 + rng.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        reach = rng.uniform(0.9, 1.3)
        arches.append(tube([Vector((0, 0, 0.35)), d * reach * 0.4 + Z * 0.55, d * reach * 0.85 + Z * 0.25, d * reach - Z * 0.10],
                           [(0.0, 0.10), (0.7, 0.07), (1.0, 0.03)], 8, 12))
    m.part('roots', merge(*arches), 'root')
    m.part('knot', blob((0, 0, 0.30), (0.20, 0.20, 0.18), 8, 5), 'root')


def sapling(m):
    glowcap(m, scale=0.24, collider=False)


# ---- resources -----------------------------------------------------------------------------------

def worldroot_timber(m):
    logs = []
    for i, (y, z, length) in enumerate([(-0.10, 0.09, 0.82), (0.10, 0.09, 0.76), (0.0, 0.25, 0.70)]):
        logs.append(transformed(lathe([(-length / 2, 0.085), (-length / 2 + 0.02, 0.092), (length / 2 - 0.02, 0.088),
                                       (length / 2, 0.080)], 10),
                                Matrix.Translation(Vector((0, y, z))) @ Matrix.Rotation(math.radians(90), 4, 'Y')))
    m.part('logs', merge(*logs), 'worldroot')
    m.part('band', transformed(lathe([(-0.03, 0.205), (0.03, 0.205)], 16, squash=0.92),
                               Matrix.Translation(Vector((0.0, 0.0, 0.16))) @ Matrix.Rotation(math.radians(90), 4, 'Y')), 'root')


def glowcap_flesh(m):
    m.part('flesh', cap((0, 0, 0.04), 0.26, 0.16, sides=16), 'cap-teal')
    m.part('gills', gills((0, 0, 0.07), 0.05, 0.24, 0.05, 14, 0.008), 'gill', smooth=False)


def spire_fibre(m):
    rng = random.Random(47)
    strands = [spike([Vector((-0.40, rng.uniform(-0.06, 0.06), 0.05 + rng.uniform(0, 0.08))),
                      Vector((0.0, rng.uniform(-0.05, 0.05), 0.07 + rng.uniform(0, 0.06))),
                      Vector((0.42, rng.uniform(-0.07, 0.07), 0.05 + rng.uniform(0, 0.08)))], 0.012, 0.012, 8)
               for _ in range(14)]
    m.part('fibres', merge(*strands), 'stalk')
    m.part('tie', transformed(lathe([(-0.025, 0.085), (0.025, 0.085)], 12, squash=0.75),
                              Matrix.Translation(Vector((0.0, 0.0, 0.08))) @ Matrix.Rotation(math.radians(90), 4, 'Y')), 'root')


def understone(m):
    m.part('stone', blob((0, 0, 0.13), (0.24, 0.18, 0.14), 7, 4), 'understone', smooth=False)


# model id -> (builder, atlas size). Trees get a larger atlas: a 12m stalk at 512px is mush.
MODELS = {
    'underworld-flora-fungal-glowcap': (glowcap, 1024),
    'underworld-flora-fungal-spirestalk': (spirestalk, 1024),
    'underworld-flora-fungal-puffcap': (puffcap, 1024),
    'underworld-flora-fungal-tanglecap': (tanglecap, 1024),
    'underworld-flora-fungal-fern': (fern, 512),
    'underworld-flora-fungal-sporebrush': (sporebrush, 512),
    'underworld-flora-fungal-amber-bed': (amber_bed, 512),
    'underworld-flora-fungal-lantern-caps': (lantern_caps, 512),
    'underworld-flora-fungal-nursery-caps': (nursery_caps, 512),
    'underworld-flora-fungal-moss-stone': (moss_stone, 512),
    'underworld-flora-fungal-root-skirt': (root_skirt, 512),
    'underworld-flora-fungal-sapling': (sapling, 512),
    'underworld-resource-worldroot-timber': (worldroot_timber, 512),
    'underworld-resource-glowcap-flesh': (glowcap_flesh, 512),
    'underworld-resource-spire-fibre': (spire_fibre, 512),
    'underworld-resource-understone': (understone, 512),
}

# Each family keeps its prototype and gains three authored growth forms. No runtime mesh generation.
CANOPY_VARIANTS = {
    'glowcap-bent': partial(branching_glowcap, form='bent'),
    'glowcap-twin': partial(branching_glowcap, form='twin'),
    'glowcap-elder': partial(branching_glowcap, form='elder'),
    'spirestalk-young': partial(spirestalk, height=.55, bend=2.0, tiers=((.65, 1.2), (.98, .65))),
    'spirestalk-crooked': partial(spirestalk, height=.85, bend=5.0, tiers=((.35, 1.1), (.64, 1.8), (.96, .8))),
    'spirestalk-towered': partial(spirestalk, height=1.15, bend=1.8, tiers=((.32, 1.7), (.49, 1.5), (.66, 1.3), (.82, 1.0), (.985, .7))),
    'puffcap-forked': partial(puffcap, trunk_height=2.6, crown=((.2, 1.6, 2.2, 1.3), (3.2, 1.2, 1.3, .85))),
    'puffcap-spreading': partial(puffcap, trunk_height=1.5, crown=((.2, 2.2, 1.5, .9), (1.7, 2.0, 1.8, 1.0), (3.3, 2.4, 1.2, 1.2), (4.9, 1.7, 2.0, .8))),
    'puffcap-clustered': partial(puffcap, trunk_height=2.8, crown=((.1, 1.3, 2.7, 1.1), (1.4, 1.8, 1.8, .9), (2.7, 1.4, 3.0, 1.2), (4.0, 1.6, 1.5, .8), (5.2, 1.2, 2.1, .9))),
    'tanglecap-young': partial(tanglecap, count=2, height=.62, spread=.55, twist=1.2, crowns=2),
    'tanglecap-splayed': partial(tanglecap, count=5, height=.9, spread=1.7, twist=1.5, crowns=5),
    'tanglecap-woven': partial(tanglecap, count=6, height=1.2, spread=.65, twist=4.7, crowns=4),
}
MODELS.update({'underworld-flora-fungal-' + name: (builder, 1024) for name, builder in CANOPY_VARIANTS.items()})


def author(model_id):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    builder, size = MODELS[model_id]
    m = Model(model_id, size, SURFACES)
    builder(m)
    scene = bpy.context.scene
    scene['model_id'] = model_id
    scene['runtime_lights'] = '[]'
    scene['surface_finish'] = '2'
    scene['underworld_authoring'] = REVISION
    unwrap(m.parts)
    overlap, coverage = uv_overlap(m.parts)
    if overlap > 0.01:
        raise ValueError(f'{model_id}: {overlap:.1%} of the atlas is claimed by two triangles')
    bake_flora_atlas(m.parts, m.atlas, paint, size=m.atlas_size)
    triangles = sum(len(o.data.loop_triangles) for o in m.parts)
    heights = [(o.matrix_world @ v.co).z for o in m.parts for v in o.data.vertices]
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / (model_id + '.blend')), compress=True)
    print(f'AUTHORED {model_id} parts={len(m.parts)} triangles={triangles} height={max(heights):.2f}m '
          f'atlas={m.atlas_size} uv_coverage={coverage:.0%}', flush=True)


def main():
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    unknown = [a for a in args if a not in MODELS]
    if unknown:
        raise SystemExit('Unknown Fungal Forest model(s): ' + ', '.join(unknown))
    for model_id in args or MODELS:
        author(model_id)


main()
