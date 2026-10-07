"""Shape attached hip roots and a forked lower torso without adding loops."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector,geometry
import first_bloom_feet_sculpt as previous
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree,inside
materials=previous.materials
unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-hips-r16-raw'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 trunk=previous.build(B,arm,record,profile,tube,cap)
 records=json.loads(bpy.context.scene['tissue_attachment_contract'])
 changed={trunk.name};new_contacts=[]
 # Mirror the same displacements onto the inner shell. The lower edge rises
 # at the groin while the sides continue into the legs; the chest collar stays.
 for offset in (0,168):
  for k in range(24):
   v=trunk.data.vertices[offset+k];p=v.co.copy()
   arch=max(0,1-(abs(p.x)/.25)**2)
   p.x*=1.40;p.y-=.025*arch;p.z+=.29*arch
   v.co=p
  for k in range(24):
   v=trunk.data.vertices[offset+24+k];v.co.x*=1.12
 trunk.data.update()

 def rigid_seat(point,host):
  # A point blended independently between moving bones can drift out of a
  # thin shell. Seat new hip contacts on triangles with one uniform bone.
  bvh=tree(host);candidates=[]
  for tri in host.data.loop_triangles:
   weights=[{host.vertex_groups[g.group].name:round(g.weight,6) for g in host.data.vertices[i].groups if g.weight>1e-7} for i in tri.vertices]
   if not (weights[0]==weights[1]==weights[2] and len(weights[0])==1):continue
   coords=[host.matrix_world@host.data.vertices[i].co for i in tri.vertices]
   normal=(coords[1]-coords[0]).cross(coords[2]-coords[0])
   if normal.length<1e-10:continue
   hit=geometry.closest_point_on_tri(point,*coords)
   candidates.append(((hit-point).length,hit,sum(coords,Vector())/3,normal.normalized(),weights[0]))
  for distance,hit,center,normal,weights in sorted(candidates,key=lambda c:c[0]):
   for inset in (.08,.20,.40):
    contact=hit.lerp(center,inset)
    for depth in (.004,.006,.002):
     for sign in (-1,1):
      p=contact+normal*depth*sign
      if inside(p,bvh):return p,weights
  raise ValueError('no rigid hip contact on '+host.name)

 def seat(o,indices,host,label):
  marker=len(records)+1
  for ordinal,index in enumerate(indices):
   if host==trunk:p,w=rigid_seat(o.matrix_world@o.data.vertices[index].co,host)
   else:p,w=embed(o.matrix_world@o.data.vertices[index].co,host,.003 if ordinal%2 else .005)
   assign(o,index,p,w)
   o.data.attributes['attachment_endpoint'].data[index].value=marker
  records.append(dict(id=marker,tissue=o.name,end=label,support=host.name,support_component=host.data.attributes['attachment_component'].data[0].value,endpoint_seating='all existing ring vertices embedded with interpolated support weights'))
  new_contacts.append(marker)

 for side,s in [('L',-1),('R',1)]:
  leg=bpy.data.objects['RootedThigh'+side]
  for j,factor in [(0,1.15),(1,1.10),(2,.96)]:
   ids=list(range(j*10,(j+1)*10));center=sum((leg.data.vertices[i].co for i in ids),Vector())/10
   for index in ids:
    d=leg.data.vertices[index].co-center
    d.x*=factor;d.y*=.88 if j<2 else .96
    leg.data.vertices[index].co=center+d+Vector((-s*(.018 if j<2 else .005),.018 if j<2 else 0,.045 if j==0 else (.020 if j==1 else 0)))
  leg.data.update();changed.add(leg.name)
  seat(leg,list(range(10)),trunk,'proximal_hip_ring')
  for k in range(2):
   o=bpy.data.objects['LimbBracketThigh'+side+str(k)]
   center=sum((o.matrix_world@v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
   for v in o.data.vertices:
    p=o.matrix_world@v.co
    p=center+(p-center)*(.91 if k==0 else .97)+Vector((-s*.035,.005,.045 if k==0 else .015))
    v.co=o.matrix_world.inverted()@p
   o.data.update();changed.add(o.name)
  for k in range(2):
   o=bpy.data.objects['HipFork'+side+str(k)]
   delta=(k-.5)*.07
   pts=[Vector((s*(.19+delta),.13,1.32)),Vector((s*(.28+delta),.19,1.20)),Vector((s*(.36+delta),.19,1.03)),Vector((s*(.43+delta),.13,.88))]
   for j,(p,r) in enumerate(zip(pts,(.058,.09,.065,.024))):
    q=(pts[min(j+1,3)]-pts[max(j-1,0)]).normalized().to_track_quat('Z','Y')
    for n in range(6):
     a=math.tau*n/6
     assign(o,j*6+n,p+q@Vector((r*math.cos(a),r*.78*math.sin(a),0)),{'Thigh_'+side:1.0})
   o.data.update();changed.add(o.name)
   seat(o,list(range(6)),trunk,'pelvis_root')
   seat(o,list(range(18,24)),leg,'upper_thigh_root')
 # Repair dependent tissue contacts against the revised surfaces. Feet remain
 # unchanged below their original ankle rings, and the chest is left in place.
 byname={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'}
 reseated=0
 for item in records:
  if item['id'] in new_contacts:continue
  host=byname[item['support']];o=byname[item['tissue']]
  if host.name not in changed and o.name not in changed:continue
  bvh=tree(host)
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  for index in ids:
   p=o.matrix_world@o.data.vertices[index].co
   if inside(p,bvh):continue
   p,w=embed(p,host,.003);assign(o,index,p,w);reseated+=1
 for item in records:
  host=byname[item['support']];o=byname[item['tissue']];bvh=tree(host)
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  assert ids and all(inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(groups=len(set(r['tissue'] for r in records)),endpoints=len(records),records=records),indent=2)+'\n')
 (OUT/'hip-refinement.json').write_text(json.dumps(dict(changed_components=sorted(changed),triangles_added=0,new_attachment_rings=len(new_contacts),reseated_dependent_vertices=reseated,upper_legs='fuller proximal roots, narrower upper brackets, explicit pelvis contact',lower_torso='raised groin arch and lateral fork instead of straight hanging apron',preserved='chest collar, chest shelves, central membrane and lower leg/foot geometry',method='existing loop shaping and intersecting supported contacts; not a Boolean union'),indent=2)+'\n')
 return trunk
