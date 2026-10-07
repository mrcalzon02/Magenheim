"""Compress the crown vertically above its base, leaving body geometry intact."""
import bpy,json
from pathlib import Path
from mathutils import Vector
import first_bloom_shoulder_sculpt as previous
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree,inside
materials=previous.materials
unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-head-r20-raw'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 trunk=previous.build(B,arm,record,profile,tube,cap)
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 old_height=max((o.matrix_world@v.co).z for o in meshes for v in o.data.vertices)-min((o.matrix_world@v.co).z for o in meshes for v in o.data.vertices)
 changed=set()
 for o in meshes:
  if not o.name.startswith(('CathedralMantle','CrownSpine','MantleCurtain')):continue
  changed.add(o.name)
  if o.name.startswith('CathedralMantle'):
   n=len(o.data.vertices)//6
   for v in o.data.vertices:
    if v.index//n>=4:
     # Lower the domed centers into shelf-like tops; the small crest is flatter.
     v.co.z=-.015+(v.co.z+.015)*(.35 if o.name=='CathedralMantle7' else .60)
  for v in o.data.vertices:
   p=o.matrix_world@v.co
   if p.z>2.35:
    t=min(1,(p.z-2.35)/1.25)
    p.y+=.075*(1-t)
    p.z=2.35+(p.z-2.35)*.80
   v.co=o.matrix_world.inverted()@p
  o.data.update()
 records=json.loads(bpy.context.scene['tissue_attachment_contract']);byname={o.name:o for o in meshes};reseated=0
 # Keep this thin crosslink at its supporting ribbon's mid-thickness, where
 # the changed Head/Chest blend remains covered during the inherited sweep.
 item=next(r for r in records if r['tissue']=='VeilCrosslink-11' and r['end']=='start')
 o=byname[item['tissue']];host=byname[item['support']]
 ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
 center=sum((o.data.vertices[i].co for i in ids),Vector())/len(ids);half=len(host.data.vertices)//2
 bvh=tree(host);blends=[(.40,.30,.30),(.30,.40,.30),(.25,.40,.35),(.35,.30,.35)]
 faces=[list(t.vertices) for t in host.data.loop_triangles if all(i<half for i in t.vertices)]
 faces.sort(key=lambda f:(sum((host.data.vertices[i].co for i in f),Vector())/3-center).length)
 face=next(f for f in faces if all(inside(host.matrix_world@sum(((host.data.vertices[i].co+host.data.vertices[i+half].co)*w*.5 for i,w in zip(f,blend)),Vector()),bvh) for blend in blends))
 for index,blend in zip(ids,blends):
  point=Vector();weights={}
  for i,w in zip(face,blend):
   for vertex in (i,i+half):
    point+=host.data.vertices[vertex].co*w*.5
    for influence in host.data.vertices[vertex].groups:
     name=host.vertex_groups[influence.group].name;weights[name]=weights.get(name,0)+influence.weight*w*.5
  assign(o,index,host.matrix_world@point,weights)
 changed.add(o.name)
 for item in records:
  if item['tissue'] not in changed and item['support'] not in changed:continue
  o=byname[item['tissue']];host=byname[item['support']];bvh=tree(host)
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  for index in ids:
   p=o.matrix_world@o.data.vertices[index].co
   if inside(p,bvh):continue
   p,w=embed(p,host,.003);assign(o,index,p,w);reseated+=1;changed.add(o.name)
 for item in records:
  o=byname[item['tissue']];host=byname[item['support']];bvh=tree(host)
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  assert ids and all(inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 new_height=max((o.matrix_world@v.co).z for o in meshes for v in o.data.vertices)-min((o.matrix_world@v.co).z for o in meshes for v in o.data.vertices)
 (OUT/'head-refinement.json').write_text(json.dumps(dict(changed_components=sorted(changed),vertical_scale_above_crown_base=.80,crown_base_authored_z=2.35,cap_dome_height_scale=.60,crest_dome_height_scale=.35,max_forward_sweep_authored=.075,original_authored_height=old_height,revised_authored_height=new_height,pipeline_scale_correction=new_height/old_height,triangles_added=0,reseated_tissue_vertices=reseated,preserved='crown width and small crest; shoulders, torso, hips, limbs and feet'),indent=2)+'\n')
 return trunk
