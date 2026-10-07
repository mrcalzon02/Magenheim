"""Seat the recessed chamber's entire outer flange into the torso shell."""
import bpy,json,math
from pathlib import Path
import first_bloom_fan_guide as previous
from first_bloom_frill_sculpt import embed,assign
from first_bloom_attachment_repair import tree,inside
materials=previous.materials;unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-seated-r9'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 trunk=previous.build(B,arm,record,profile,tube,cap)
 wall=bpy.data.objects['RecessedChamber']
 records=json.loads(bpy.context.scene['tissue_attachment_contract'])
 # Grow the existing torso aperture into a fitted organic collar. Its original
 # opening was rectangular and sat behind the oval chamber, leaving a gap.
 lip=sorted(set([j*24+k for j in (2,5) for k in range(2,11)]+[j*24+k for j in (3,4) for k in (2,10)]))
 assert len(lip)==22 and len(trunk.data.vertices)==336
 for index in lip:
  original=trunk.data.vertices[index].co.copy()
  angle=math.atan2((original.z-1.91)/.43,original.x/.44)
  for offset,r,y in [(0,.967,.237),(168,.956,.202)]:
   trunk.data.vertices[index+offset].co=(.44*r*math.cos(angle),y,1.91+.43*r*math.sin(angle))
 # The fitted chest collar follows the chest bone as one structure. The pelvis
 # blend remains below it, where the trunk transitions into the waist.
 for v in trunk.data.vertices:
  if v.co.z>=1.40:assign(trunk,v.index,trunk.matrix_world@v.co,{'Chest':1.0})
 # Extend that support down the existing front apron. The flange's bottom
 # corners interpolate against these triangles, not just the aperture lip.
 for offset in (0,168):
  for k in range(1,12):
   index=offset+24+k;assign(trunk,index,trunk.matrix_world@trunk.data.vertices[index].co,{'Chest':1.0})
 rim=[v.index for v in wall.data.vertices if math.hypot(v.co.x/.44,(v.co.z-1.91)/.43)>.96]
 assert len(rim)==48,len(rim)
 moves=[]
 for index in rim:
  before=wall.matrix_world@wall.data.vertices[index].co
  # Different embedding depths preserve the closed front/back flange volume.
  point,weights=embed(before,trunk,.003 if index<96 else .009)
  assign(wall,index,point,weights);moves.append((point-before).length)
 marker=max(r['id'] for r in records)+1
 for index in rim:wall.data.attributes['attachment_endpoint'].data[index].value=marker
 records.append(dict(id=marker,tissue=wall.name,end='complete_body_flange',support=trunk.name,support_component=trunk.data.attributes['attachment_component'].data[0].value,endpoint_seating='all 48 front/back outer flange vertices embedded into torso shell with interpolated support weights'))
 # Keep the previously approved fan; only its wall contacts follow the reseated
 # bowl. Recheck all other supports rather than presuming they remain seated.
 byname={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'}
 for item in records:
  if item['id']==marker:continue
  if item['support'] not in (wall.name,trunk.name):continue
  o=byname[item['tissue']]
  host=byname[item['support']]
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  for ordinal,index in enumerate(ids):
   depth=.001 if ordinal<len(ids)//2 else .004
   try:point,weights=embed(o.matrix_world@o.data.vertices[index].co,host,depth)
   except ValueError as error:raise ValueError(str(item)+' vertex '+str(index)) from error
   assign(o,index,point,weights)
 for item in records:
  o=byname[item['tissue']];host=byname[item['support']];bvh=tree(host)
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  assert all(inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 anatomy=json.loads((OUT/'frill-anatomy.json').read_text());anatomy.update(attachment_ends=len(records),cavity_body_flange_vertices=48)
 (OUT/'frill-anatomy.json').write_text(json.dumps(anatomy,indent=2)+'\n')
 (OUT/'cavity-attachment.json').write_text(json.dumps(dict(rim_vertices=48,result='all outer front/back flange vertices seated in torso shell',max_authored_displacement_metres=max(moves),triangles_added=0,method='intersecting supported flange, not a topology weld',support=trunk.name),indent=2)+'\n')
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(groups=len(set(i['tissue'] for i in records)),endpoints=len(records),records=records),indent=2)+'\n')
 return trunk
