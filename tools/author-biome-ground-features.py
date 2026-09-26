"""Author low biome features as editable, textured meshes; no runtime geometry generation.
Run tools/blender.ps1 author-biome-ground-features, then export-model-assets with these IDs.
"""
import bpy, math, random, sys
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from magenheim_blender_kit import merge, spike, loft, blob, unwrap, uv_overlap, spec_painter
from magenheim_flora_kit import Model, bake_flora_atlas, tube
ROOT=Path(__file__).resolve().parents[1]
SURFACES={
 'root':dict(low=(.12,.19,.15),high=(.42,.52,.32),scale=9,stretch=(3,3,.6),edge=((.67,.73,.48),.3),occlusion=.7,metallic=0,rough=.9),
 'root-tip':dict(low=(.28,.43,.32),high=(.69,.76,.49),scale=12,edge=((.8,.83,.62),.2),occlusion=.5,metallic=0,rough=.85),
 'sulfur':dict(low=(.71,.59,.18),high=(.87,.76,.33),scale=32,edge=((1,.9,.54),.22),occlusion=.4,metallic=0,rough=.97),
 'silt':dict(low=(.08,.16,.18),high=(.25,.37,.37),scale=16,edge=((.44,.55,.51),.2),occlusion=.6,metallic=0,rough=.7),
 'rime':dict(low=(.25,.43,.53),high=(.7,.85,.89),scale=15,edge=((.9,.98,1),.5),occlusion=.45,metallic=0,rough=.45),
 'shale':dict(low=(.17,.14,.22),high=(.48,.4,.52),scale=10,veins=((.65,.56,.67),.012,6),edge=((.73,.64,.75),.4),occlusion=.8,metallic=0,rough=.88),
 'peat':dict(low=(.13,.12,.065),high=(.4,.36,.17),scale=18,edge=((.59,.52,.27),.3),occlusion=.9,metallic=0,rough=.96),
}
MODELS={
 'rootgrass-carpet':('root',11), 'rootgrass-tufts':('root',27), 'rootgrass-woven':('root',43),
 'sulfur-drift-crescent':('sulfur',17), 'sulfur-drift-rippled':('sulfur',31), 'sulfur-drift-bank':('sulfur',59),
 'blackwater-silt-ripples':('silt',71), 'frozen-rime-fan':('rime',83),
 'fracture-shale-scree':('shale',97), 'decay-peat-mat':('peat',109),
}
PREFIX='underworld-ground-'
def roots(m,name,seed,surface='root'):
 rng=random.Random(seed); strands=[]; tips=[]
 for i in range(48):
  a=rng.random()*math.tau; r=math.sqrt(rng.random()); x=math.cos(a)*r*1.9; y=math.sin(a)*r*1.5
  height=rng.uniform(.18,.48) * (1.5 if 'tufts' in name else .6 if 'woven' in name else 1)
  for j in range(3):
   angle=a+j*2.1+rng.uniform(-.6,.6); lean=rng.uniform(.4,.65) if 'woven' in name else rng.uniform(.1,.38)
   base=Vector((x,y,-.07)); mid=Vector((x+math.cos(angle)*lean*.3,y+math.sin(angle)*lean*.3,height*.65))
   tip=Vector((x+math.cos(angle)*lean,y+math.sin(angle)*lean,height))
   strands.append(spike([base,mid,tip],rng.uniform(.025,.045),.018,5))
 # Low cylindrical runners cross and braid between grass-like root shoots.
 for i in range(20):
  a=rng.random()*math.tau; length=rng.uniform(1.0,1.8); side=rng.uniform(-.7,.7)
  forward=Vector((math.cos(a),math.sin(a),0)); lateral=Vector((-forward.y,forward.x,0))
  points=[]
  for k in range(5):
   t=k/4; p=forward*((t-.5)*length*2)+lateral*(side+math.sin(t*math.pi*2+i)*.18)
   p.z=-.07 if k in (0,4) else .065+.045*math.sin(t*math.pi+i)
   points.append(p)
  tips.append(tube(points,[(0,.005),(.2,.042),(.7,.036),(1,.005)],5,10))
 m.part('root-grass',merge(*strands),surface)
 m.part('woven-runners',merge(*tips),'root-tip' if surface=='root' else surface)
def drift_mesh(rx,ry,height,phase=0,ripples=False,crescent=False):
 # Closed mound with buried rim; asymmetric crest and shallow windward ripples.
 sections=[[Vector((.35,0,height))]]
 for ring in range(1,15):
  r=ring/14; section=[]
  for j in range(48):
   a=j*math.tau/48; edge=1+.065*math.sin(3*a+phase)+.03*math.sin(7*a)
   x=rx*r*math.cos(a)*edge+.35*(1-r); y=ry*r*math.sin(a)*edge
   if crescent: x += .8*(y/ry)**2
   h=height*(1-r*r)**1.5*(1+.24*math.sin(a+phase))-.08*r
   if ripples: h+=.13*math.sin(y*5+x*.7+phase)*math.sin(math.pi*r)**2
   section.append(Vector((x,y,h)))
  sections.append(section)
 sections.append([Vector((0,0,-.16))])
 return loft(sections)
def build(m,name,surface,seed):
 if surface=='root': roots(m,name,seed); return
 if surface=='peat':
  m.part('peat-bed',drift_mesh(1.8,1.5,.14,1,True),surface)
  roots(m,name,seed,surface); return
 if surface in ('sulfur','silt'):
  bank='bank' in name; ripple='ripple' in name; crescent='crescent' in name
  m.part('wind-deposit',drift_mesh(2.3 if bank else 1.9,1.2 if bank else 1.7,
    .62 if bank else .32 if crescent else .2,seed*.1,ripple,crescent),surface)
  return
 rng=random.Random(seed); shapes=[]
 for i in range(24 if surface=='shale' else 32):
  a=rng.random()*math.tau; r=math.sqrt(rng.random())*1.6
  x,y=math.cos(a)*r,math.sin(a)*r
  if surface=='shale': shapes.append(blob((x,y,.05),(rng.uniform(.18,.45),rng.uniform(.12,.32),rng.uniform(.07,.16)),5,3))
  else: shapes.append(spike([Vector((x,y,-.06)),Vector((x+.08,y+.06,.22)),Vector((x+.24,y+.14,rng.uniform(.3,.62)))],.065,.038,4))
 m.part('mineral-fragments',merge(*shapes),surface,smooth=False)
def main():
 args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
 for name in args or MODELS:
  name=name.removeprefix(PREFIX)
  surface,seed=MODELS[name]; model_id=PREFIX+name
  bpy.ops.wm.read_factory_settings(use_empty=True)
  m=Model(model_id,512,SURFACES); build(m,name,surface,seed)
  scene=bpy.context.scene; scene['model_id']=model_id; scene['runtime_lights']='[]'; scene['surface_finish']='2'
  scene['underworld_authoring']='biome-ground-features-2'
  unwrap(m.parts); overlap,coverage=uv_overlap(m.parts)
  if overlap>.01: raise ValueError(f'{name}: overlapping UVs {overlap}')
  bake_flora_atlas(m.parts,m.atlas,spec_painter(SURFACES,ao_distance=.08,edge_radius=.008),size=512)
  bpy.context.preferences.filepaths.save_version=0
  bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'assets/models/source'/f'{model_id}.blend'),compress=True)
  print(f'AUTHORED {model_id} parts={len(m.parts)}',flush=True)
if __name__=='__main__': main()
