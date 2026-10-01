"""Author six open combat arenas with biome-specific structure and native collision geometry."""
import bpy, math, random, sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from magenheim_flora_kit import Model, tube, cap, buttresses
from magenheim_blender_kit import lathe, merge, unwrap, blob
from underworld_material_library import bind_underworld_material

ROOT=Path(__file__).resolve().parents[1]
SPECS={
    'first-bloom':('understone','worldroot-bark','glowcap'),
    'blackwater-maw':('flowstone','worldroot-bark','blackwater-pearl'),
    'furnace-heart':('slagstone','charred-root','underworld.geothermal-vent.heat'),
    'white-silence':('clear-ice','rimewood','rimesilver'),
    'rift-titan':('shardstone','titanbone','fracture-crystal'),
    'carrion-crown':('bone','rotwood','carrion-amber'),
}

def author(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    model_id='underworld-boss-arena-'+name
    m=Model(model_id,512,{key:dict(metallic=0,rough=.8) for key in ('stone','structure','accent')})
    aquatic=name=='blackwater-maw'
    # The Drowned Ring keeps the center open to native water instead of trapping its aquatic boss.
    profile=[(-.6,10 if aquatic else 0),(-.6,14),(.15,14),(.65,13),(.65,10 if aquatic else 0)]
    m.part('combat-foundation',lathe(profile,64),'stone',smooth=False,collider=True)
    for ring,r in enumerate((10.2,11.4,12.6)):
        # Segmented relief with real join lines, giving scale without obstructing the fighting floor.
        segments=[]
        for i in range(24):
            a=i*math.tau/24; b=a+math.tau/24*.92
            segments.append(tube([(r*math.cos(a),r*math.sin(a),.70),(r*math.cos(b),r*math.sin(b),.70)],
                [(0,.075),(1,.075)],6,3))
        m.part('floor-inlay-'+str(ring),merge(*segments),'accent',smooth=False)
    rng=random.Random(name)
    for i in range(8):
        a=math.tau*(i+.5)/8; x,y=16*math.cos(a),16*math.sin(a)
        h=3.8+(i%3)*.45
        m.part('buttress-'+str(i),tube([(x,y,0),(x*.98,y*.98,h*.6),(x*.96,y*.96,h)],
            [(0,.65),(.5,.5),(1,.32)],10,10),'structure',collider=True)
        m.part('pillar-foot-'+str(i),buttresses((x,y,0),4,1.1,.7,.18,rng),'structure')
        if name in ('first-bloom','carrion-crown'):
            m.part('shelf-crown-'+str(i),cap((x*.96,y*.96,h),1.25,.6,bell=.25 if name=='carrion-crown' else 0,sides=22),'accent')
        elif name=='furnace-heart':
            m.part('heat-rib-'+str(i),lathe([(h-.6,.35),(h-.2,.6),(h,.52),(h+.3,.12)],14,centre=(x*.96,y*.96)),'accent')
        else:
            shards=[]
            for j in range(3):
                shards.append(tube([(x+.3*(j-1),y,h-.2),(x+.25*(j-1),y,h+.8+.3*j)],[(0,.16),(1,.008)],6,4))
            m.part('crystal-finial-'+str(i),merge(*shards),'accent',smooth=False)
    # Four low approach tongues remain open between the eight perimeter pillars.
    for route in range(4):
        a=route*math.tau/4; side=(-math.sin(a),math.cos(a))
        for edge in (-1,1):
            points=[(r*math.cos(a)+edge*side[0]*1.8,r*math.sin(a)+edge*side[1]*1.8,.35) for r in (12.6,15,18,21)]
            m.part('approach-edge-'+str(route)+'-'+str(edge),tube(points,[(0,.18),(.8,.16),(1,.08)],8,8),'structure',collider=True)
    unwrap(m.parts)
    for key,semantic in zip(('stone','structure','accent'),SPECS[name]): bind_underworld_material(bpy,m.materials[key],semantic)
    scene=bpy.context.scene;scene['model_id']=model_id;scene['runtime_lights']='[]';scene['underworld_authoring']='six-biome-combat-arenas-r1'
    scene['combat_radius_m']=13;scene['open_water_center']=aquatic
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'assets/models/source'/f'{model_id}.blend'),compress=True)
    print('AUTHORED',model_id,len(m.parts),'parts',flush=True)

for name in SPECS: author(name)
