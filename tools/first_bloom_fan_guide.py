"""Spread recessed chest lamellae around the user's circular fan guide."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
import first_bloom_radiating_frills as previous
from first_bloom_frill_sculpt import embed,assign
from first_bloom_attachment_repair import tree,inside
materials=previous.materials;unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-fan-r8'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 trunk=previous.build(B,arm,record,profile,tube,cap)
 wall=bpy.data.objects['RecessedChamber'];core=bpy.data.objects['AmberChamberHeart']
 # The join sits at the fan's center rather than the bottom of a V-shaped fan.
 for v in core.data.vertices:
  j,k=divmod(v.index,8);a=math.tau*k/8
  x,y,z=[(.017,-.06,1.91),(.068,.09,1.91),(.044,.205,1.91)][j]
  v.co=(x*math.cos(a),y,z+.11*math.sin(a))
 for index in range(8):
  point,weights=embed(core.matrix_world@core.data.vertices[index].co,wall)
  assign(core,index,point,weights)
 fins=[];floor=tree(wall)
 for o in list(bpy.context.scene.objects):
  if not o.name.startswith('ChamberRib'):continue
  s=-1 if '-' in o.name else 1;k=int(o.name.rsplit('_',1)[1]);angle=-1.40+k*2.80/10
  root=Vector((s*.032,.15,1.845+.013*k))
  end=Vector((s*.43*math.cos(angle),.20,1.91+.40*math.sin(angle)))
  centers=[root,root.lerp(end,.36),root.lerp(end,.73),end]
  # Lower/middle fins fold gently down before reaching the rim; upper fins
  # follow their own radial angles instead of being pushed to a side rail.
  bend=.028*max(0,1-abs(k-4)/6)
  centers[1].z-=bend;centers[2].z-=bend*.6
  for j,c in enumerate(centers):
   direction=centers[min(3,j+1)]-centers[max(0,j-1)]
   tangent=Vector((-direction.z,0,direction.x)).normalized()
   w=[.006,.009,.012,.009][j]
   # Broad shelf faces recede from the exposed front edge toward the bowl.
   hit=floor.find_nearest(Vector((c.x,-.12,c.z)))[0]
   back=hit.y-.006;front=[.21,.225,.23,.22][j]
   c.y=(front+back)/2;depth=(front-back)/2
   for i,(u,d) in enumerate([(-w,-depth),(w,-depth),(w,depth),(-w*.2,depth*1.035),(-w,depth)]):
    o.data.vertices[j*5+i].co=c+tangent*u+Vector((0,d,0))
  fins.append(o.name)
 byname={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'}
 records=json.loads(bpy.context.scene['tissue_attachment_contract']);changed=set(fins+[core.name])
 # Re-seat full endpoint sections after the fan and central join move.
 for item in records:
  if item['tissue'] not in changed and item['support'] not in changed:continue
  o=byname[item['tissue']];host=byname[item['support']]
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  for index in ids:
   point,weights=embed(o.matrix_world@o.data.vertices[index].co,host,.001)
   assign(o,index,point,weights)
 for item in records:
  o=byname[item['tissue']];host=byname[item['support']];bvh=tree(host)
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  assert all(inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 anatomy=dict(frills=22,attachment_ends=len(records),central_join_roots=22,wall_roots=22,anatomy='full radial fan of deep recessed shelves around a central fleshy join; upper fins spread across the roof',angular_span_degrees=math.degrees(2.8),central_join_authored_z=1.91,triangle_change=0)
 (OUT/'frill-anatomy.json').write_text(json.dumps(anatomy,indent=2)+'\n')
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(groups=60,endpoints=len(records),records=records),indent=2)+'\n')
 return trunk
