"""Author Blackwater Deep's own scenery and resources, replacing its vanilla donor stand-ins.

    tools/blender.ps1 author-underworld-blackwater-deep [model-id ...]

Design authority: docs/UNDERWORLD_FLORA_TERRAIN_PLAN.md section 4.2 -- ground-supported column
forests of mineral spires and broken pillars, wet flowstone terraces and rimstone pools, pale blind
growths at the waterline -- and 7.2 for the biome's four raw resources. Replaces Mistlands cliffs,
rock fingers, Yggdrasil roots, a Plains fern and heath shrubs. Same conventions as the Fungal
Forest tool: real size in meters, Z up, only the large forms collide, no per-model lights.
"""
import bpy
import math
import random
import sys
from pathlib import Path

from mathutils import Matrix, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from magenheim_blender_kit import (  # noqa: E402
    bake_atlas, blob, gem, lathe, merge, spec_painter, spike, transformed, unwrap, uv_overlap)
from magenheim_flora_kit import Model, small_mushrooms, tube  # noqa: E402

SOURCE = Path(__file__).resolve().parents[1] / 'assets/models/source'
REVISION = 'blackwater-deep-custom-1'
Z = Vector((0.0, 0.0, 1.0))

SURFACES = {
    # Flowstone: wet, banded in horizontal layers as it was deposited.
    'flowstone': dict(low=(0.10, 0.14, 0.16), high=(0.30, 0.38, 0.40), scale=2, stretch=(0.6, 0.6, 14.0),
                      edge=((0.55, 0.66, 0.68), 0.5), occlusion=1.0, metallic=0.0, rough=0.25),
    'wetstone': dict(low=(0.12, 0.15, 0.17), high=(0.32, 0.37, 0.40), scale=5,
                     veins=((0.08, 0.10, 0.11), 0.02, 4.0), edge=((0.60, 0.68, 0.70), 0.7), occlusion=1.0,
                     metallic=0.0, rough=0.3),
    'silt': dict(low=(0.26, 0.30, 0.30), high=(0.50, 0.55, 0.54), scale=9, edge=((0.70, 0.74, 0.72), 0.5),
                 occlusion=0.9, metallic=0.0, rough=0.9),
    'drowned-root': dict(low=(0.07, 0.10, 0.10), high=(0.22, 0.28, 0.26), scale=3, stretch=(8.0, 8.0, 0.6),
                         edge=((0.40, 0.50, 0.48), 0.4), occlusion=0.95, metallic=0.0, rough=0.5),
    'pale': dict(low=(0.62, 0.64, 0.62), high=(0.90, 0.91, 0.88), scale=7, edge=((1.0, 1.0, 0.98), 0.3),
                 occlusion=0.7, metallic=0.0, rough=0.6, emission=(0.03, 0.04, 0.04)),
    'brine': dict(low=(0.16, 0.34, 0.34), high=(0.46, 0.70, 0.68), scale=9, edge=((0.75, 0.92, 0.90), 0.5),
                  occlusion=0.7, metallic=0.0, rough=0.6),
    'pearl': dict(low=(0.62, 0.72, 0.74), high=(0.92, 0.96, 0.98), scale=6, edge=((1.0, 1.0, 1.0), 0.5),
                  occlusion=0.5, metallic=0.2, rough=0.25, emission=(0.04, 0.08, 0.09)),
    'salt': dict(low=(0.70, 0.74, 0.76), high=(0.96, 0.97, 0.98), scale=10, edge=((1.0, 1.0, 1.0), 0.8),
                 occlusion=0.6, metallic=0.0, rough=0.4),
}
paint = spec_painter(SURFACES, ao_distance=0.25, edge_radius=0.012)


def rippled(z0, z1, r0, r1, rings, wobble, seed, apex=True):
    """A dripstone column: radius falling from r0 to r1 with irregular deposit bulges."""
    rng = random.Random(seed)
    profile = []
    for i in range(rings):
        t = i / (rings - 1)
        r = r0 + (r1 - r0) * t
        profile.append((z0 + (z1 - z0) * t, r * (1.0 + wobble * rng.uniform(-1, 1))))
    if apex:
        profile.append((z1 + (r1 * 1.5), 0.0))
    return profile


def flowstone_spire(m):
    m.part('spire', lathe(rippled(-0.2, 6.2, 1.1, 0.18, 14, 0.12, 3), 14), 'flowstone', collider=True)
    m.part('skirt', lathe([(-0.2, 1.7), (0.15, 1.55), (0.4, 1.2), (0.7, 0.9)], 16), 'flowstone')


def broken_column(m):
    rng = random.Random(5)
    shaft = lathe([(-0.2, 0.62), (0.1, 0.62), (0.3, 0.52), (3.6, 0.48)], 12)
    m.part('shaft', shaft, 'wetstone', collider=True)
    shards = [gem((0.25 * math.cos(a), 0.25 * math.sin(a), 3.7), rng.uniform(0.12, 0.22), rng.uniform(0.25, 0.45), 5, axis='Z')
              for a in (0.4, 2.1, 3.9, 5.2)]
    m.part('break', merge(*shards), 'wetstone', smooth=False)
    m.part('fallen', transformed(lathe([(-0.7, 0.46), (0.7, 0.46)], 12),
                                 Matrix.Translation(Vector((1.2, 0.3, 0.35))) @ Matrix.Rotation(math.radians(84), 4, 'Y')),
           'wetstone', collider=True)


def rimstone_mound(m):
    m.part('mound', lathe([(-0.2, 2.6), (0.35, 2.55), (0.45, 2.2), (0.95, 2.1), (1.05, 1.6), (1.55, 1.5),
                           (1.65, 1.0), (2.2, 0.9), (2.35, 0.0)], 20), 'flowstone', collider=True)
    m.part('pools', merge(*[lathe([(z, r), (z + 0.03, r * 0.96)], 20) for z, r in ((0.40, 2.25), (1.0, 1.65), (1.60, 1.05))]),
           'silt')


def drowned_root_arch(m):
    rng = random.Random(9)
    arches = []
    for i in range(3):
        a = i * 2.1 + rng.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0))
        arches.append(tube([-d * 1.8 - Z * 0.2, -d * 0.9 + Z * 2.6, d * 0.9 + Z * 2.9 + Vector((0, 0, 0.3 * i)), d * 1.9 - Z * 0.2],
                           [(0.0, 0.34), (0.5, 0.22), (1.0, 0.30)], 10, 16))
    m.part('roots', merge(*arches), 'drowned-root', collider=True)


def brine_fern(m):
    rng = random.Random(19)
    fronds = []
    for i in range(9):
        a = math.tau * i / 9 + rng.uniform(-0.15, 0.15)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        k = rng.uniform(0.85, 1.15)
        points = [Vector((0, 0, 0.04)), d * 0.12 * k + Z * 0.40 * k, d * 0.42 * k + Z * 0.62 * k, d * 0.62 * k + Z * 0.52 * k,
                  d * 0.62 * k + Z * 0.36 * k]
        fronds.append(spike(points, 0.08, 0.006, 12, flat=True, up=Z.cross(d).normalized()))
    m.part('fronds', merge(*fronds), 'brine')


def palefinger(m):
    rng = random.Random(23)
    fingers = []
    for i in range(11):
        a = rng.uniform(0, math.tau)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        lean, h = rng.uniform(0.05, 0.3), rng.uniform(0.4, 1.0)
        fingers.append(tube([d * rng.uniform(0, 0.2), d * lean * 0.5 + Z * h * 0.5, d * lean + Z * h],
                            [(0.0, 0.045), (0.85, 0.035), (1.0, 0.0)], 8, 8))
    m.part('fingers', merge(*fingers), 'pale')


def pearl_caps(m):
    small_mushrooms(m, 6, 'pearl', 'pale', (0.05, 0.12), 0.1, 29, 0.28)


def wet_stone(m):
    m.part('stone', blob((0, 0, 0.28), (0.66, 0.52, 0.46), 7, 5), 'wetstone', smooth=False)


def fingerstone_rubble(m):
    rng = random.Random(31)
    stones = [transformed(blob((0, 0, 0), (rng.uniform(0.1, 0.22), rng.uniform(0.08, 0.16), rng.uniform(0.25, 0.5)), 6, 4),
                          Matrix.Translation(Vector((rng.uniform(-0.5, 0.5), rng.uniform(-0.5, 0.5), 0.1)))
                          @ Matrix.Rotation(rng.uniform(0.6, 1.4), 4, 'X') @ Matrix.Rotation(rng.uniform(0, 3), 4, 'Z'))
              for _ in range(6)]
    m.part('rubble', merge(*stones), 'wetstone', smooth=False)


def root_fan(m):
    rng = random.Random(37)
    roots = []
    for i in range(6):
        a = -0.9 + i * 0.36 + rng.uniform(-0.1, 0.1)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        roots.append(tube([Vector((0, 0, 0.25)), d * 0.5 + Z * 0.35, d * 1.1 - Z * 0.05], [(0.0, 0.09), (1.0, 0.03)], 8, 10))
    m.part('roots', merge(*roots), 'drowned-root')
    m.part('knot', blob((0, 0, 0.2), (0.18, 0.18, 0.16), 8, 5), 'drowned-root')


def lakebed_shelf(m):
    m.part('shelf', blob((0, 0, 0.12), (1.1, 0.8, 0.22), 9, 4), 'silt', smooth=False)


def root_fingers(m):
    rng = random.Random(41)
    roots = [tube([Vector((rng.uniform(-0.3, 0.3), rng.uniform(-0.3, 0.3), -0.1)), Vector((rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2), 0.6)),
                   Vector((rng.uniform(-0.3, 0.3), rng.uniform(-0.3, 0.3), rng.uniform(0.9, 1.4)))],
                  [(0.0, 0.08), (1.0, 0.0)], 8, 10) for _ in range(5)]
    m.part('roots', merge(*roots), 'drowned-root')


def flowstone_chunk(m):
    m.part('chunk', blob((0, 0, 0.12), (0.22, 0.16, 0.13), 7, 4), 'flowstone', smooth=False)


def pale_fibre(m):
    rng = random.Random(47)
    strands = [spike([Vector((-0.38, rng.uniform(-0.05, 0.05), 0.05 + rng.uniform(0, 0.07))), Vector((0.0, rng.uniform(-0.04, 0.04), 0.07)),
                      Vector((0.40, rng.uniform(-0.06, 0.06), 0.05 + rng.uniform(0, 0.07)))], 0.012, 0.012, 8) for _ in range(12)]
    m.part('fibres', merge(*strands), 'pale')
    m.part('tie', transformed(lathe([(-0.025, 0.08), (0.025, 0.08)], 12, squash=0.75),
                              Matrix.Translation(Vector((0.0, 0.0, 0.08))) @ Matrix.Rotation(math.radians(90), 4, 'Y')), 'drowned-root')


def blackwater_pearl(m):
    m.part('pearl', blob((0, 0, 0.07), (0.07, 0.07, 0.07), 16, 10), 'pearl')


def deep_salt(m):
    rng = random.Random(53)
    crystals = [gem((rng.uniform(-0.08, 0.08), rng.uniform(-0.08, 0.08), 0.08), rng.uniform(0.03, 0.06), rng.uniform(0.07, 0.12), 6, axis='Z')
                for _ in range(6)]
    m.part('crystals', merge(*crystals), 'salt', smooth=False)


P = 'underworld-flora-blackwater-'
MODELS = {
    P + 'flowstone-spire': (flowstone_spire, 1024), P + 'broken-column': (broken_column, 1024),
    P + 'rimstone-mound': (rimstone_mound, 1024), P + 'drowned-root-arch': (drowned_root_arch, 1024),
    P + 'brine-fern': (brine_fern, 512), P + 'palefinger': (palefinger, 512), P + 'pearl-caps': (pearl_caps, 512),
    P + 'wet-stone': (wet_stone, 512), P + 'fingerstone-rubble': (fingerstone_rubble, 512), P + 'root-fan': (root_fan, 512),
    P + 'lakebed-shelf': (lakebed_shelf, 512), P + 'root-fingers': (root_fingers, 512),
    'underworld-resource-blackwater-flowstone': (flowstone_chunk, 512), 'underworld-resource-pale-fibre': (pale_fibre, 512),
    'underworld-resource-blackwater-pearl': (blackwater_pearl, 512), 'underworld-resource-deep-salt': (deep_salt, 512),
}


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
    bake_atlas(m.parts, m.atlas, paint, size=m.atlas_size)
    triangles = sum(len(o.data.loop_triangles) for o in m.parts)
    heights = [(o.matrix_world @ v.co).z for o in m.parts for v in o.data.vertices]
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / (model_id + '.blend')), compress=True)
    print(f'AUTHORED {model_id} parts={len(m.parts)} triangles={triangles} height={max(heights):.2f}m', flush=True)


def main():
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    unknown = [a for a in args if a not in MODELS]
    if unknown:
        raise SystemExit('Unknown Blackwater Deep model(s): ' + ', '.join(unknown))
    for model_id in args or MODELS:
        author(model_id)


main()
