"""Author the three late-biome harvestable species and their native destruction fragments.

Standing plants use real metre dimensions, purpose-unwrapped geometry and the shared PBR
library. These source files are development artifacts; no generator is shipped or run in game.
"""
import bpy, math, random, sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from mathutils import Vector
from magenheim_flora_kit import Model, tube, cap, gills, buttresses
from magenheim_blender_kit import unwrap, merge, spike
from underworld_material_library import bind_underworld_material

ROOT = Path(__file__).resolve().parents[1]
SPECS = {
    'cinderstalk': ('sulfur', 6.0, 'charred-root', 'sulfur-crust', 'underworld.geothermal-vent.heat'),
    'rimecap': ('frozen', 6.5, 'rimewood', 'clear-ice', 'rimesilver'),
    'rotbloom': ('decay', 7.5, 'rotwood', 'decay-spore', 'carrion-amber'),
}

def author(species):
    biome, h, stalk, crown, accent = SPECS[species]
    model_id = f'underworld-flora-{biome}-{species}'
    bpy.ops.wm.read_factory_settings(use_empty=True)
    surfaces = {x: dict(metallic=0, rough=.8) for x in ('stalk','crown','accent')}
    m = Model(model_id, 512, surfaces)
    rng = random.Random(species)
    m.part('stalk', tube([(0,0,-.1),(.2,-.1,h*.3),(-.15,.2,h*.7),(.1,0,h)],
        [(0,.65),(.1,.45),(.6,.28),(1,.16)], 14, 18), 'stalk', collider=True)
    m.part('root-flare', buttresses((0,0,0), 6, 1.35, .95, .23, rng), 'stalk')
    if species == 'cinderstalk':
        branches = []
        for i in range(9):
            a = i*2.4; z = 1.8+i*.43; reach = .7+(i%3)*.3
            branches.append(tube([(0,0,z),(.35*math.cos(a),.35*math.sin(a),z+.25),
                (reach*math.cos(a),reach*math.sin(a),z+.8)], [(0,.2),(.65,.1),(1,.018)], 9, 8))
        m.part('charred-antlers', merge(*branches), 'stalk', collider=True)
        shelves = [cap((.12,-.04,z), r, .24, sides=20) for z,r in ((2.1,.72),(3.5,.85),(5.2,1.1))]
        m.part('sulfur-shelves', merge(*shelves), 'crown')
        m.part('ember-gills', merge(*(gills((.12,-.04,z), .2, r*.9, .11, 16) for z,r in ((2.1,.72),(3.5,.85),(5.2,1.1)))), 'accent', smooth=False)
    elif species == 'rimecap':
        m.part('frozen-canopy', cap((.1,0,h-.35), 2.15, 1.05, sides=30), 'crown', collider=True)
        needles = []
        for i in range(18):
            a = i*math.tau/18; r = 1.5+.15*math.sin(i*2)
            needles.append(tube([(r*math.cos(a),r*math.sin(a),h-.7),
                (r*math.cos(a),r*math.sin(a),h-1.3-(i%4)*.15)], [(0,.11),(1,.002)], 6, 4))
        m.part('hanging-rime', merge(*needles), 'accent', smooth=False)
        m.part('frozen-gills', gills((.1,0,h-.35),.3,1.9,.18,28), 'accent', smooth=False)
    else:
        crowns=[]; frills=[]
        for i in range(4):
            z = h-2.4+i*.62; r=1.2+i*.34; x=(-1 if i%2 else 1)*.42
            crowns.append(cap((x,.12*i,z),r,.7,bell=.6,sides=26))
            frills.append(gills((x,.12*i,z-.16),.25,r*.83,.3,22))
        m.part('slumped-fruiting-shelves',merge(*crowns),'crown',collider=True)
        m.part('carrion-frills',merge(*frills),'accent',smooth=False)
        cords=[]
        for i in range(12):
            a=i*math.tau/12; r=1.3+(i%3)*.22
            cords.append(tube([(r*math.cos(a),r*math.sin(a),h-1.3),
                (r*1.12*math.cos(a),r*1.12*math.sin(a),h-2),
                (r*math.cos(a),r*math.sin(a),h-3-(i%3)*.22)],[(0,.055),(.7,.045),(1,.008)],7,8))
        m.part('hanging-spore-cords',merge(*cords),'stalk')
    unwrap(m.parts)
    for surface, semantic in {'stalk':stalk,'crown':crown,'accent':accent}.items():
        bind_underworld_material(bpy,m.materials[surface],semantic)
    scene=bpy.context.scene
    scene['model_id']=model_id; scene['runtime_lights']='[]'
    scene['underworld_authoring']='late-biome-harvestable-flora-r1'
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'assets/models/source'/f'{model_id}.blend'),compress=True)
    print('AUTHORED',model_id,'parts',len(m.parts),flush=True)

def fragments(species, height, semantic):
    for stage in ('felled', 'stump'):
        model_id = f'underworld-flora-harvest-{species}-{stage}'
        bpy.ops.wm.read_factory_settings(use_empty=True)
        m = Model(model_id, 512, {'fibre':dict(metallic=0,rough=.8),'cut':dict(metallic=0,rough=.8)})
        h = height if stage == 'felled' else .65
        m.part('cut-stalk',tube([(0,0,0),(.05,0,h*.5),(0,0,h)],[(0,.45),(.6,.32),(1,.22)],14,16),'fibre',collider=True)
        # Visible pale cut surface and concentric fibre collars distinguish the harvested state.
        from magenheim_blender_kit import lathe
        m.part('cut-face',lathe([(h+.005,0),(h+.005,.21),(h+.025,.21),(h+.025,0)],20),'cut')
        if stage == 'stump':
            m.part('root-flare',buttresses((0,0,0),5,1.0,.45,.18,random.Random(species)),'fibre')
        unwrap(m.parts)
        bind_underworld_material(bpy,m.materials['fibre'],semantic)
        bind_underworld_material(bpy,m.materials['cut'],'sporeweave-fibre' if species != 'cinderstalk' else 'sulfur-crust')
        scene=bpy.context.scene;scene['model_id']=model_id;scene['runtime_lights']='[]';scene['underworld_authoring']='native-felling-fragments-r1'
        bpy.context.preferences.filepaths.save_version=0
        bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'assets/models/source'/f'{model_id}.blend'),compress=True)
        print('AUTHORED',model_id,flush=True)

if __name__ == '__main__':
    for species in SPECS: author(species)
    for species, height, material in (('glowcap',6.5,'worldroot-bark'),('spirestalk',11.7,'worldroot-bark'),('cinderstalk',6.0,'charred-root')):
        fragments(species,height,material)
