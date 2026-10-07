"""Compress the arm tops into the torso beneath the existing mantle."""
import bpy,json
from pathlib import Path
from mathutils import Vector
import first_bloom_side_shelves as previous
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree,inside
materials=previous.materials
unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-shoulders-r19-raw'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 trunk=previous.build(B,arm,record,profile,tube,cap)
 records=json.loads(bpy.context.scene['tissue_attachment_contract'])
 changed=set();roots=[]
 for side,s in [('L',-1),('R',1)]:
  o=bpy.data.objects['RootedUpperArm'+side]
  for j,inward,back,xf,yf in [(0,.21,.12,.75,.85),(1,.10,.055,.87,.92),(2,.025,.015,.96,.98)]:
   ids=list(range(j*10,(j+1)*10));center=sum((o.data.vertices[i].co for i in ids),Vector())/10
   for index in ids:
    p=o.data.vertices[index].co-center;p.x*=xf;p.y*=yf
    p+=center+Vector((-s*inward,-back,0))
    if j==1:assign(o,index,o.matrix_world@p,{'Chest':.25,'UpperArm_'+side:.75})
    else:o.data.vertices[index].co=p
  o.data.update();changed.add(o.name)
  marker=len(records)+1
  for index in range(10):
   p,w=embed(o.matrix_world@o.data.vertices[index].co,trunk,.004)
   assert w=={'Chest':1.0},('shoulder root must follow torso',w)
   assign(o,index,p,w)
   o.data.attributes['attachment_endpoint'].data[index].value=marker
  records.append(dict(id=marker,tissue=o.name,end='compressed_shoulder_root',support=trunk.name,support_component=trunk.data.attributes['attachment_component'].data[0].value,endpoint_seating='all ten original arm-root vertices embedded in torso with Chest weights; next ring blends toward UpperArm'))
  roots.append(marker)
  for k in range(2):
   bracket=bpy.data.objects['LimbBracketUpperArm'+side+str(k)]
   center=sum((bracket.matrix_world@v.co for v in bracket.data.vertices),Vector())/len(bracket.data.vertices)
   for v in bracket.data.vertices:
    p=bracket.matrix_world@v.co;d=p-center
    d.x*=.92 if k==0 else .97
    p=center+d+Vector((-s*(.10 if k==0 else .035),-.035 if k==0 else -.010,.025 if k==0 else 0))
    v.co=bracket.matrix_world.inverted()@p
   bracket.data.update();changed.add(bracket.name)
 # The preexisting white tissue must follow the compressed supporting surfaces.
 byname={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'}
 reseated=0
 for item in records:
  if item['id'] in roots or item['support'] not in changed:continue
  o=byname[item['tissue']];host=byname[item['support']]
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  for index in ids:
   p,w=embed(o.matrix_world@o.data.vertices[index].co,host,.003)
   assign(o,index,p,w);reseated+=1
 for item in records:
  o=byname[item['tissue']];host=byname[item['support']];bvh=tree(host)
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  assert ids and all(inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(groups=len(set(r['tissue'] for r in records)),endpoints=len(records),records=records),indent=2)+'\n')
 (OUT/'shoulder-refinement.json').write_text(json.dumps(dict(changed_components=sorted(changed),triangles_added=0,arm_root_inward_shift=.21,arm_root_back_shift=.12,arm_root_section_x_scale=.75,root_vertices_per_arm=10,reseated_tissue_vertices=reseated,rig='unchanged skeleton; Chest roots and two-bone Chest/UpperArm transition',preserved='mantle, chest cavity, lateral hip shelves and central bark, lower arms, hands, legs and feet'),indent=2)+'\n')
 return trunk
