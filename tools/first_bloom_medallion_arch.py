"""Enlarge the central face and close the arch with the existing lowest fins."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
import first_bloom_deeper_chest as previous
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree,inside
materials=previous.materials;unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-medallion-r12'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 # The prior module's geometry function also uses its own OUT global.
 trunk=previous.build(B,arm,record,profile,tube,cap)
 core=bpy.data.objects['AmberChamberHeart'];wall=bpy.data.objects['RecessedChamber']
 center=sum((v.co for v in core.data.vertices[8:16]),Vector())/8
 vertical=Vector((0,math.sin(math.radians(12)),math.cos(math.radians(12))))
 before=[v.co.copy() for v in core.data.vertices]
 for v in core.data.vertices:
  d=v.co-center;v.co+=Vector((d.x*.25,0,0))+vertical*(d.dot(vertical)*.25)
 # Preserve a supported recessed base while enlarging the visible medallion.
 for index in range(8):
  point,weights=previous.embed(core.matrix_world@core.data.vertices[index].co,wall,.003)
  assign(core,index,point,weights)
 pair=[bpy.data.objects['ChamberRib'+str(s)+'_0'] for s in (-1,1)]
 for o,s in zip(pair,(-1,1)):
  for j,target in [(1,s*.004),(2,s*.002),(3,0)]:
   ids=range(j*5,(j+1)*5);cx=sum(o.data.vertices[i].co.x for i in ids)/5
   for index in ids:o.data.vertices[index].co.x+=target-cx
 records=json.loads(bpy.context.scene['tissue_attachment_contract'])
 byname={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'};changed={core.name}|{o.name for o in pair}
 for item in records:
  if item['tissue'] not in changed and item['support'] not in changed:continue
  o=byname[item['tissue']];host=byname[item['support']]
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  for ordinal,index in enumerate(ids):
   if inside(o.matrix_world@o.data.vertices[index].co,tree(host)):continue
   point,weights=previous.embed(o.matrix_world@o.data.vertices[index].co,host,.003 if ordinal<len(ids)//2 else .006)
   assign(o,index,point,weights)
 marker=max(r['id'] for r in records)+1
 for index in range(8):core.data.attributes['attachment_endpoint'].data[index].value=marker
 records.append(dict(id=marker,tissue=core.name,end='medallion_recessed_base',support=wall.name,support_component=wall.data.attributes['attachment_component'].data[0].value,endpoint_seating='all eight recessed base vertices seated into cavity wall'))
 for item in records:
  o=byname[item['tissue']];host=byname[item['support']];bvh=tree(host)
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  assert all(inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 # Actual triangle overlap checks the closed lower meeting, rather than only
 # recording the intended left/right ring centers.
 overlap=tree(pair[0]).overlap(tree(pair[1]));assert overlap,'lowest fins do not meet'
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(groups=len(set(r['tissue'] for r in records)),endpoints=len(records),records=records),indent=2)+'\n')
 anatomy=json.loads((OUT/'frill-anatomy.json').read_text());anatomy['attachment_ends']=len(records)
 (OUT/'frill-anatomy.json').write_text(json.dumps(anatomy,indent=2)+'\n')
 (OUT/'medallion-arch.json').write_text(json.dumps(dict(medallion_width_scale=1.25,medallion_height_scale=1.25,medallion_depth_scale=1,arch_method='bring the two existing lowest frills together',arch_intersecting_triangle_pairs=len(overlap),triangles_added=0,attachment_records=len(records)),indent=2)+'\n')
 return trunk
