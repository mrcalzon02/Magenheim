"""Recess and pitch the attached front chest assembly around its lower hinge."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
import first_bloom_cavity_attachment as previous
from first_bloom_frill_sculpt import embed,assign
from first_bloom_attachment_repair import tree,inside
materials=previous.materials;unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-looming-r10'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 trunk=previous.build(B,arm,record,profile,tube,cap)
 angle=math.radians(12);pivot=Vector((0,.12,1.40));inset=.065;lower=.070
 def pitched(point):
  d=point-pivot
  return pivot+Vector((d.x,d.y*math.cos(angle)+d.z*math.sin(angle)-inset,d.z*math.cos(angle)-d.y*math.sin(angle)-lower))
 byname={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'}
 wall=byname['RecessedChamber'];changed=set()
 for o in byname.values():
  if o==trunk:
   for v in o.data.vertices:
    p=o.matrix_world@v.co
    front=max(0,min(1,(p.y+.20)/.17));height=max(0,min(1,(p.z-.73)/.28))
    v.co=o.matrix_world.inverted()@p.lerp(pitched(p),front*height)
   changed.add(o.name)
  elif o.name.startswith('ChamberRib') or o.name in ('RecessedChamber','AmberChamberHeart'):
   for v in o.data.vertices:v.co=o.matrix_world.inverted()@pitched(o.matrix_world@v.co)
   changed.add(o.name)
  elif o.name.startswith(('ChestVeil','VeilCrosslink')):
   for v in o.data.vertices:
    p=o.matrix_world@v.co;weight=max(0,min(1,(2.50-p.z)/.22))
    v.co=o.matrix_world.inverted()@p.lerp(pitched(p),weight)
   changed.add(o.name)
 records=json.loads(bpy.context.scene['tissue_attachment_contract'])
 # Seat the full body flange first; all dependent fin/frame contacts follow.
 ordered=sorted(records,key=lambda r:0 if r['end']=='complete_body_flange' else (2 if r['tissue'].startswith('VeilCrosslink') else 1))
 for item in ordered:
  if item['tissue'] not in changed and item['support'] not in changed:continue
  o=byname[item['tissue']];host=byname[item['support']]
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  for ordinal,index in enumerate(ids):
   depth=(.003 if index<96 else .009) if item['end']=='complete_body_flange' else (.001 if ordinal<len(ids)//2 else .004)
   point,weights=embed(o.matrix_world@o.data.vertices[index].co,host,depth)
   assign(o,index,point,weights)
 for item in records:
  o=byname[item['tissue']];host=byname[item['support']];bvh=tree(host)
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  assert all(inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'chest-posture.json').write_text(json.dumps(dict(forward_pitch_degrees=12,recess_authored_metres=inset,lower_authored_metres=lower,pivot=list(pivot),method='rigid cavity/fan pitch with smooth front-torso transition and reseated full flange',triangles_added=0,lower_cavity_authored_z=min(v.co.z for v in wall.data.vertices),hip_joint_authored_z=float(arm.data.bones['Thigh_L'].head_local.z)),indent=2)+'\n')
 return trunk
