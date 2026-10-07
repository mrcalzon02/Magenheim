"""Reinterpret the upper thigh brackets as torso-rooted hip overhangs."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector,geometry
import first_bloom_hip_sculpt as previous
from first_bloom_frill_sculpt import assign
from first_bloom_deeper_chest import embed
from first_bloom_attachment_repair import tree,inside
materials=previous.materials
unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-pelvic-r17-raw'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 trunk=previous.build(B,arm,record,profile,tube,cap)
 records=json.loads(bpy.context.scene['tissue_attachment_contract'])
 changed=set();hosttree=tree(trunk);candidates=[]
 for tri in trunk.data.loop_triangles:
  weights=[{trunk.vertex_groups[g.group].name:round(g.weight,6) for g in trunk.data.vertices[i].groups if g.weight>1e-7} for i in tri.vertices]
  if not all(w=={'Chest':1.0} for w in weights):continue
  coords=[trunk.matrix_world@trunk.data.vertices[i].co for i in tri.vertices]
  normal=(coords[1]-coords[0]).cross(coords[2]-coords[0])
  if normal.length>1e-10:candidates.append((coords,normal.normalized(),sum(coords,Vector())/3))
 def torso_seat(point,index):
  ranked=[]
  for coords,normal,center in candidates:
   hit=geometry.closest_point_on_tri(point,*coords);ranked.append(((hit-point).length,hit,normal,center))
  for distance,hit,normal,center in sorted(ranked,key=lambda r:r[0]):
   for inset in (.08,.2,.4):
    for depth in (.004,.006,.002):
     for sign in (-1,1):
      tangent=normal.cross(Vector((0,0,1)))
      if tangent.length<.01:tangent=normal.cross(Vector((0,1,0)))
      tangent.normalize();phase=math.tau*(index%10)/10
      jitter=(tangent*math.cos(phase)+normal.cross(tangent)*math.sin(phase))*.002
      p=hit.lerp(center,inset)+jitter+normal*(depth+(.001 if index>=50 else 0))*sign
      if inside(p,hosttree):return p
  raise ValueError('no torso shelf root')
 for side,s in [('L',-1),('R',1)]:
  for k in range(2):
   o=bpy.data.objects['LimbBracketThigh'+side+str(k)];changed.add(o.name)
   center=Vector((s*(.235+.045*k),.115+.045*k,1.30-.16*k))
   rings=[(-.015,.055),(-.05,.60),(-.065,1),(-.015,1.025),(.055,.73),(.12,.04)]
   for j,(z,r) in enumerate(rings):
    for n in range(10):
     a=math.tau*n/10;wave=1+.055*math.sin(a*3+k*.8)
     p=center+Vector(((.37+.045*k)*math.cos(a)*r*wave,.25*math.sin(a)*r,z))
     # The lateral rim droops over the joint, while the back rises into bark.
     p.z-=.065*max(0,s*(p.x-center.x))/.42+.028*max(0,p.y-center.y)/.25
     p.z+=.012*math.sin(a*3+k)*r
     assign(o,j*10+n,p,{'Chest':1.0})
   # Both central closing rings form the embedded root of a closed shelf.
   marker=len(records)+1
   for index in list(range(10))+list(range(50,60)):
    assign(o,index,torso_seat(o.matrix_world@o.data.vertices[index].co,index),{'Chest':1.0})
    o.data.attributes['attachment_endpoint'].data[index].value=marker
   o.data.update()
   records.append(dict(id=marker,tissue=o.name,end='torso_shelf_root',support=trunk.name,support_component=trunk.data.attributes['attachment_component'].data[0].value,endpoint_seating='both existing central closing rings embedded into torso; entire shelf follows Chest'))
 byname={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'}
 for item in records:
  if item['support'] not in changed:continue
  o=byname[item['tissue']];host=byname[item['support']]
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  for index in ids:
   p,w=embed(o.matrix_world@o.data.vertices[index].co,host,.003);assign(o,index,p,w)
 for item in records:
  o=byname[item['tissue']];host=byname[item['support']];bvh=tree(host)
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  assert ids and all(inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(groups=len(set(r['tissue'] for r in records)),endpoints=len(records),records=records),indent=2)+'\n')
 (OUT/'pelvic-shelf-refinement.json').write_text(json.dumps(dict(changed_components=sorted(changed),triangles_added=0,interpretation='overlapping lower-torso mushroom shelves flaring over hip joints',root_vertices_per_shelf=20,weight='Chest 1.0 for whole shelves and their seated roots',preserved='accepted hips, chest, lower legs and feet'),indent=2)+'\n')
 return trunk
