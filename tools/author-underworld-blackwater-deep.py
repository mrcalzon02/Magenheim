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
    blob, gem, lathe, loft, merge, spec_painter, spike, transformed, unwrap, uv_overlap, curve, sweep)
from magenheim_flora_kit import Model, small_mushrooms, tube, bake_flora_atlas  # noqa: E402

SOURCE = Path(__file__).resolve().parents[1] / 'assets/models/source'
REVISION = 'blackwater-deep-quality-2'
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
    'pool': dict(low=(0.025, 0.055, 0.06), high=(0.07, 0.14, 0.15), scale=3,
                 edge=((0.07, 0.14, 0.15), 0), occlusion=0.0, metallic=0.15, rough=0.12),
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


def fluted(profile, sides=24, flutes=8, depth=0.10, seed=0, twist=0.0, centre=(0.0, 0.0)):
    """A surface of revolution whose radius is carved into vertical flutes -- dripstone curtains
    on a spire, channels on a column. profile: (z, r); r == 0 is an apex."""
    rng = random.Random(seed)
    phases = [rng.uniform(0, math.tau) for _ in range(3)]
    cx, cy = centre
    rings = []
    for i, (z, r) in enumerate(profile):
        if r <= 0:
            rings.append([Vector((cx, cy, z))])
            continue
        ring = []
        for j in range(sides):
            a = math.tau * j / sides + twist * i
            groove = 1.0 - depth * (0.5 + 0.5 * math.cos(flutes * a))
            lumps = 1.0 + 0.04 * math.sin(3 * a + phases[0] + z * 1.7) + 0.03 * math.sin(5 * a + phases[1] + z * 2.9)
            ring.append(Vector((cx + r * groove * lumps * math.cos(a), cy + r * groove * lumps * math.sin(a), z)))
        rings.append(ring)
    return loft(rings)


def flowstone_spire(m):
    """Fused dripstone columns, fluted by the water that built them, standing in their own pool."""
    rng = random.Random(3)
    lobes = [((0.0, 0.0), 6.4, 1.05, 3), ((0.75, 0.35), 4.3, 0.62, 5), ((-0.55, 0.60), 3.4, 0.55, 7),
             ((0.15, -0.80), 5.1, 0.70, 9), ((-0.85, -0.35), 2.6, 0.48, 11)]
    columns = []
    for (x, y), h, r, seed in lobes:
        profile = []
        for i in range(16):
            t = i / 15
            radius = r * (1.0 - 0.82 * t ** 0.9) * (1.0 + 0.10 * math.sin(t * 19 + seed))
            profile.append((-0.2 + h * t, radius))
        profile.append((h + r * 0.25, 0.0))
        columns.append(fluted(profile, 20, rng.choice((6, 7, 9)), 0.16, seed, 0.02, (x, y)))
    m.part('columns', merge(*columns), 'flowstone', collider=True)
    rings = [fluted([(z - 0.05, r * 1.18), (z, r * 1.24), (z + 0.05, r * 1.16)], 20, 8, 0.1, 13 + k)
             for k, (z, r) in enumerate(((1.4, 0.88), (2.8, 0.66), (4.1, 0.45)))]
    m.part('deposit-rings', merge(*rings), 'wetstone')
    m.part('pool', fluted([(-0.2, 2.1), (0.12, 2.05), (0.22, 1.85), (0.18, 1.4), (0.35, 1.2)], 28, 11, 0.08, 17), 'flowstone')
    satellites = [lathe([(-0.1, rr), (hh * 0.6, rr * 0.55), (hh, 0.0)], 8, centre=(1.7 * math.cos(a), 1.7 * math.sin(a)))
                  for a, hh, rr in [(rng.uniform(0, math.tau), rng.uniform(0.4, 1.1), rng.uniform(0.10, 0.18)) for _ in range(5)]]
    m.part('stalagmites', merge(*satellites), 'wetstone')
    m.part('silt', blob((0.0, 0.0, 0.14), (1.45, 1.3, 0.06), 16, 4), 'silt')


def broken_column(m):
    """A drowned colonnade's pillar: fluted shaft on a stepped plinth, snapped, its upper drum and
    capital fallen into the silt beside it."""
    rng = random.Random(5)
    plinth = merge(*[transformed(lathe([(z0, w), (z1, w)], 4, phase=math.pi / 4), Matrix.Identity(4))
                     for z0, z1, w in ((-0.2, 0.25, 1.05), (0.25, 0.5, 0.88))])
    m.part('plinth', plinth, 'wetstone', smooth=False, collider=True)
    shaft = fluted([(0.45, 0.66), (0.62, 0.66), (0.66, 0.58), (1.8, 0.56), (2.6, 0.54), (3.3, 0.52)], 40, 10, 0.22, 19)
    verts, faces = shaft
    shaft = ([Vector((v.x, v.y, v.z + (.20*math.sin(math.atan2(v.y,v.x)*3)+.11*math.cos(math.atan2(v.y,v.x)*7) if v.z>3.2 else 0))) for v in verts], faces)
    m.part('shaft', shaft, 'wetstone', collider=True)
    m.part('band', lathe([(1.70, 0.60), (1.74, 0.64), (1.86, 0.64), (1.90, 0.60)], 32), 'flowstone')
    shards = []
    for k in range(9):
        a = math.tau * k / 9 + rng.uniform(-0.2, 0.2)
        r = rng.uniform(0.18, 0.44)
        shards.append(gem((r * math.cos(a), r * math.sin(a), 3.3 + rng.uniform(0.0, 0.1)),
                          rng.uniform(0.08, 0.16), rng.uniform(0.18, 0.62) * (1.2 - r), 5, axis='Z', twist=rng.uniform(0, 1)))
    m.part('fracture', merge(*shards), 'wetstone', smooth=False)
    drum = transformed(fluted([(-0.6, 0.52), (0.6, 0.50)], 40, 10, 0.22, 23),
                       Matrix.Translation(Vector((1.55, 0.55, 0.38))) @ Matrix.Rotation(math.radians(96), 4, 'Y')
                       @ Matrix.Rotation(math.radians(20), 4, 'X'))
    m.part('fallen-drum', drum, 'wetstone', collider=True)
    capital = transformed(merge(lathe([(0.0, 0.62), (0.12, 0.70), (0.28, 0.82), (0.36, 0.82), (0.36, 0.0)], 16),
                                lathe([(0.36, 0.80), (0.52, 0.80)], 4, phase=math.pi / 4)),
                          Matrix.Translation(Vector((-1.3, 1.0, 0.04))) @ Matrix.Rotation(math.radians(-12), 4, 'X'))
    m.part('capital', capital, 'wetstone', smooth=False)
    rubble = [blob((rng.uniform(-1.6, 1.6), rng.uniform(-1.6, 1.6), 0.06), (rng.uniform(0.08, 0.22), rng.uniform(0.07, 0.18), rng.uniform(0.06, 0.14)), 6, 4)
              for _ in range(9)]
    m.part('rubble', merge(*rubble), 'wetstone', smooth=False)
    m.part('silt', blob((0.2, 0.3, -0.015), (1.9, 1.6, 0.04), 16, 4), 'silt')


def terrace(profile, radius, centre, seed, sides=32, squash=0.72):
    """Shared irregular outline through strata; top pool levels remain horizontal."""
    rings = []
    for z, scale in profile:
        rings.append([Vector((centre[0] + radius * scale * (1 + .14 * math.sin(3*a+seed)
                            + .09 * math.sin(5*a-seed) + .045 * math.sin(11*a)) * math.cos(a),
                              centre[1] + radius * scale * squash * (1 + .14 * math.sin(3*a+seed)
                            + .09 * math.sin(5*a-seed) + .045 * math.sin(11*a)) * math.sin(a), z))
                      for a in [math.tau*j/sides for j in range(sides)]])
    return loft(rings)


def bark_root(points, radius, seed, samples=18):
    path = curve(points, samples)
    def profile(k, t):
        r = radius * (1 - .9*t) * (1 + .18*math.sin(t*25+seed))
        return [(r*(1+.12*math.cos(5*a+seed))*math.cos(a),
                 r*(1+.12*math.cos(5*a+seed))*math.sin(a))
                for a in [math.tau*j/15 for j in range(15)]]
    direction = (Vector(points[-1])-Vector(points[0])).normalized()
    return sweep(path, profile, Vector((1,0,0)) if abs(direction.z)>.7 else Z)


def rimstone_mound(m):
    # Offset, overlapping basins climb one bank; no concentric stair-stack.
    basins = [(-.45,-.38,2.1,.36,11), (.52,.25,1.64,.91,17),
              (-.20,.65,1.24,1.47,23), (.43,.92,.80,1.98,31)]
    banks, pools, curtains = [], [], []
    for x,y,r,z,seed in basins:
        banks.append(terrace([(-.15,.88),(z-.25,1),(z-.07,1.03),(z,1),
                              (z+.055,.96),(z-.09,.87)],r,(x,y),seed))
        pools.append(terrace([(z-.095,.88),(z-.085,.88)],r,(x,y),seed))
        for j in range(7):
            a = -2.0 + j*.16
            px,py = x+r*.93*math.cos(a),y+r*.69*math.sin(a)
            curtains.append(tube([Vector((px,py,z-.03)),Vector((px+.06,py-.04,z*.55)),
                                  Vector((px+.18,py-.1,-.04))],
                                 [(0,.075),(0.7,.10),(1,.14)],7,6))
    m.part('scalloped-banks',merge(*banks),'flowstone',collider=True)
    m.part('still-pools',merge(*pools),'pool')
    m.part('spill-curtain',merge(*curtains),'wetstone')


def drowned_root_arch(m):
    roots, rootlets, growths, ridges = [], [], [], []
    paths = [[(-2,-.35,-.18),(-1.4,-.25,2.5),(.5,.05,3.6),(1.7,.2,.8),(2,.3,-.1)],
             [(-1.6,.8,-.18),(-1.1,.9,1.8),(.1,.6,2.9),(1.1,-.6,2),(1.7,-1,-.15)]]
    for i,path in enumerate(paths):
        roots.append(bark_root([Vector(p) for p in path],.48-i*.1,9+i,26))
        samples=curve([Vector(p) for p in path],18)
        for j in range(5):
            ridge=[]
            for k,p in enumerate(samples):
                t=k/(len(samples)-1)
                tangent=(samples[min(k+1,len(samples)-1)]-samples[max(0,k-1)]).normalized()
                side=tangent.cross(Vector((0,1,0))).normalized()
                a=j*math.tau/5+.5*t
                radius=(.48-i*.1)*(1-.9*t)*(1+.18*math.sin(t*25+9+i))
                ridge.append(p+radius*(side*math.cos(a)+Vector((0,1,0))*math.sin(a)))
            ridges.append(tube(ridge,[(0,.032),(.8,.018),(1,.003)],5,18))
    for base,end in [((-1.45,-.25,1.4),(-2.25,-.1,1.9)),
                     ((.95,-.4,1.7),(1.65,-.9,2.25)),
                     ((-1.3,.85,.8),(-2.0,1.1,.3))]:
        a,b=Vector(base),Vector(end)
        rootlets.append(bark_root([a,(a+b)*.5+Z*.14,b],.16,13,10))
    rng=random.Random(9)
    for i in range(12):
        side = -1 if i<6 else 1
        base=Vector((side*1.7, (.3 if side>0 else -.35),.4))
        d=Vector((side*rng.uniform(.35,.85),rng.uniform(-.9,.9),0))
        rootlets.append(bark_root([base,base+d*.45-Z*.2,base+d-Z*.55],.12, i,8))
    # Hanging pale threads follow gravity and leave the arch opening legible.
    for i in range(7):
        x=-.9+i*.3; z=2.75+.22*math.sin(i*.5)
        growths.append(tube([Vector((x,.07,z)),Vector((x+.06,.09,z-.3)),
                             Vector((x+.04,.09,z-.5-rng.random()*.3))],
                            [(0,.025),(.75,.018),(1,0)],6,7))
    m.part('knuckled-roots',merge(*roots),'drowned-root',collider=True)
    m.part('bark-ridges',merge(*ridges),'drowned-root')
    m.part('anchoring-rootlets',merge(*rootlets),'drowned-root')
    m.part('hanging-growths',merge(*growths),'pale')
    m.part('foot-silt',merge(*[blob((x,y,-.015),(.75,.55,.08),12,4)
                             for x,y in [(-1.8,-.2),(1.8,.1),(-1.6,.8),(1.7,-1)]]),'silt')


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


def stone_layers(m, radius, height, seed, name='stone'):
    # Interpenetrating offset strata, not separated concentric display plinths.
    layers=[]
    for i,(factor,dx,dy) in enumerate([(1,0,0),(.98,-.04,.02),(.91,.05,-.015),(.80,-.025,.03)]):
        z=-.08+i*height*.19
        layer=terrace([(z,.96),(z+height*.10,1),(z+height*.30,.92)],
                      radius*factor,(dx,dy),seed+i*.7,20)
        layers.append(transformed(layer,Matrix.Rotation((i-1.5)*.025,4,'Y')))
    m.part(name,merge(*layers),'wetstone',smooth=False)
    crust=terrace([(height*.62,.66),(height*.72,.64),(height*.76,.42)],radius,(-.025,.03),seed+2.1,20)
    m.part('mineral-crust',crust,'flowstone',smooth=False)
    chips=[blob((radius*.88*math.cos(a),radius*.65*math.sin(a),.015),
                (radius*.16,radius*.11,height*.14),6,3) for a in (.2,1.7,2.4,4.6)]
    m.part('detached-chips',merge(*chips),'wetstone',smooth=False)


def wet_stone(m):
    stone_layers(m,.68,.55,7)


def fingerstone_rubble(m):
    rng=random.Random(31)
    pieces=[]
    for i in range(5):
        r=rng.uniform(.10,.18); h=rng.uniform(.35,.7)
        mesh=fluted([(-h*.5,r*.9),(0,r),(h*.35,r*.8),(h*.5,r*.55)],16,4,.20,31+i)
        pieces.append(transformed(mesh,Matrix.Translation(Vector((rng.uniform(-.45,.45),rng.uniform(-.4,.4),.14)))
                       @ Matrix.Rotation(rng.uniform(.85,1.45),4,'Y') @ Matrix.Rotation(i*1.7,4,'Z')))
    m.part('broken-fingers',merge(*pieces),'wetstone',smooth=False)
    m.part('crust',terrace([(-.07,.9),(.02,1),(.055,.7)],.6,(0,0),31,16),'flowstone')


def root_fan(m):
    rng = random.Random(37)
    roots = []
    for i in range(6):
        a = -0.9 + i * 0.36 + rng.uniform(-0.1, 0.1)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        roots.append(bark_root([Vector((0, 0, 0.25)), d * 0.5 + Z * 0.35, d * 1.1 - Z * 0.05], .11,37+i,10))
    m.part('roots', merge(*roots), 'drowned-root')
    m.part('knot', blob((0, 0, 0.2), (0.18, 0.18, 0.16), 8, 5), 'drowned-root')


def lakebed_shelf(m):
    stone_layers(m,1.15,.34,43,'shelf-strata')


def root_fingers(m):
    rng = random.Random(41)
    roots = [bark_root([Vector((rng.uniform(-0.3, 0.3), rng.uniform(-0.3, 0.3), -0.1)), Vector((rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2), 0.6)),
                   Vector((rng.uniform(-0.3, 0.3), rng.uniform(-0.3, 0.3), rng.uniform(0.9, 1.4)))],
                  .10, 41+i, 10) for i in range(5)]
    m.part('roots', merge(*roots), 'drowned-root')


def flowstone_chunk(m):
    m.part('chunk', terrace([(-.02,.8),(.035,1),(.075,.92),(.09,1.02),(.14,.79),(.155,.85),(.22,.55)], .22,(0,0),47,12), 'flowstone', smooth=False)


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
    bake_flora_atlas(m.parts, m.atlas, paint, size=m.atlas_size)
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


if __name__ == '__main__':
    main()
