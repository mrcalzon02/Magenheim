"""Separate torso/shoulder fungal masses from a forward four-cap head and beard."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector,geometry
import first_bloom_head_sculpt as previous
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree,inside
materials=previous.materials
unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-bundles-r21-raw'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;trunk=previous.build(B,arm,record,profile,tube,cap)
 old=json.loads((OUT/'head-refinement.json').read_text())
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];byname={o.name:o for o in meshes}
 records=json.loads(bpy.context.scene['tissue_attachment_contract']);changed=set();new_ids=set();deltas={}
 def rigid_ribbon_end(point,host,index):
  bvh=tree(host);ranked=[]
  for tri in host.data.loop_triangles:
   weights=[{host.vertex_groups[g.group].name:round(g.weight,6) for g in host.data.vertices[i].groups if g.weight>1e-7} for i in tri.vertices]
   if not all(w=={'Chest':1.0} for w in weights):continue
   coords=[host.matrix_world@host.data.vertices[i].co for i in tri.vertices]
   normal=(coords[1]-coords[0]).cross(coords[2]-coords[0])
   if normal.length<1e-10:continue
   hit=geometry.closest_point_on_tri(point,*coords)
   ranked.append(((hit-point).length,hit,sum(coords,Vector())/3,normal.normalized()))
  for distance,hit,center,normal in sorted(ranked,key=lambda q:q[0]):
   for inset in (.15,.35,.60):
    for depth in (.001,.002,.003):
     for sign in (-1,1):
      tangent=normal.cross(Vector((0,0,1)))
      if tangent.length<.01:tangent=normal.cross(Vector((0,1,0)))
      tangent.normalize();phase=math.tau*(index%4)/4
      jitter=(tangent*math.cos(phase)+normal.cross(tangent)*math.sin(phase))*.0004
      p=hit.lerp(center,inset)+jitter+normal*depth*sign
      if inside(p,bvh):return p,{'Chest':1.0}
  raise ValueError('no rigid pale ribbon end '+host.name)
 targets=[(-.52,-.08,2.48),(.48,-.11,2.52),(-.46,-.18,2.67),(.41,-.17,2.70),(-.055,.12,2.74),(.045,.10,2.92),(-.025,.075,3.07),(.015,.05,3.20)]
 for k,target in enumerate(targets):
  o=byname['CathedralMantle'+str(k)];n=len(o.data.vertices)//6
  center=sum((o.matrix_world@o.data.vertices[i].co for i in range(3*n,4*n)),Vector())/n
  delta=Vector(target)-center;deltas[k]=delta
  for v in o.data.vertices:
   p=o.matrix_world@v.co
   if k>=4:p.x=center.x+(p.x-center.x)*(1.12 if k>=6 else .93)
   assign(o,v.index,p+delta,{'Chest' if k<4 else 'Head':1.0})
  o.data.update();changed.add(o.name)
 spine=byname['CrownSpine']
 for v in spine.data.vertices:
  p=spine.matrix_world@v.co
  p.y+=.27*max(0,min(1,(p.z-2.35)/.85))
  assign(spine,v.index,p,{'Head':1.0})
 spine.data.update();changed.add(spine.name)
 # The upper four caps are the head. The six existing upper curtains become
 # its beard; the lower curtains follow their torso-grown shoulder shelves.
 for o in meshes:
  if not o.name.startswith('MantleCurtain'):continue
  k=int(o.name.removeprefix('MantleCurtain').split('_')[0]);delta=deltas[k]
  for v in o.data.vertices:
   p=o.matrix_world@v.co+delta
   if k>=4:p.x*=.80;p.y+=.055
   assign(o,v.index,p,{'Chest' if k<4 else 'Head':1.0})
  if k>=4:
   half=len(o.data.vertices)//2
   for j in range(half//2):
    ids=[2*j,2*j+1,half+2*j,half+2*j+1]
    center=sum((o.matrix_world@o.data.vertices[i].co for i in ids),Vector())/4
    for index in ids:
     p=o.matrix_world@o.data.vertices[index].co;p.x=center.x+(p.x-center.x)*1.8
     assign(o,index,p,{'Head':1.0})
  o.data.update();changed.add(o.name)

 def add_root(o,ids,host,label):
  marker=len(records)+1
  for ordinal,index in enumerate(ids):
   p,w=embed(o.matrix_world@o.data.vertices[index].co,host,.003 if ordinal%2 else .005)
   assign(o,index,p,w);o.data.attributes['attachment_endpoint'].data[index].value=marker
  o.data.update();new_ids.add(marker)
  records.append(dict(id=marker,tissue=o.name,end=label,support=host.name,support_component=host.data.attributes['attachment_component'].data[0].value,endpoint_seating='existing root ring vertices embedded in anatomical host with matching weights'))
 # Give the head core a torso contact, and every cap its corresponding root.
 add_root(spine,list(range(10)),trunk,'head_core_torso_root')
 for k in range(8):
  o=byname['CathedralMantle'+str(k)];n=len(o.data.vertices)//6
  # The underside is the root; the upper closing ring remains the solid cap
  # center. Pulling both into the host would turn the mushroom into a basin.
  host=trunk if k<4 else (spine if k<6 else byname['CathedralMantle'+str(k-1)])
  add_root(o,list(range(n)),host,'shoulder_bundle_root' if k<4 else 'four_cap_head_root')

 # Seat moved frames first, then their crosslinks. Propagate changed-host
 # membership so downstream attachments are checked against the new ribbons.
 for item in records:
  if item['id'] in new_ids:continue
  o=byname[item['tissue']];host=byname[item['support']]
  if o.name not in changed and host.name not in changed:continue
  if o.name.startswith('MantleCurtain'):
   k=int(o.name.removeprefix('MantleCurtain').split('_')[0])
   if k>=4 and item['end']=='end':
    # Beard fibres join the existing pale chest framework rather than turning
    # back up into a shoulder canopy or ending unanchored in front of the face.
    center=sum((o.matrix_world@o.data.vertices[i].co for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']),Vector())
    ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']];center/=len(ids)
    candidates=[q for q in meshes if q.name.startswith('ChestVeil')]
    host=min(candidates,key=lambda q:tree(q).find_nearest(center)[3])
    item['support']=host.name;item['support_component']=host.data.attributes['attachment_component'].data[0].value
  ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']]
  for index in ids:
   if o.name.startswith('MantleCurtain') and item['end']=='end' and host.name.startswith('ChestVeil'):
    p,w=rigid_ribbon_end(o.matrix_world@o.data.vertices[index].co,host,index)
   else:p,w=embed(o.matrix_world@o.data.vertices[index].co,host,.003)
   assign(o,index,p,w)
  changed.add(o.name)

 # Reblend each moved curtain between its host weights. This keeps shoulder
 # tissue on the torso and lets beard fibres transition from Head to Chest.
 for o in meshes:
  if not o.name.startswith(('MantleCurtain','ChestVeil')):continue
  half=len(o.data.vertices)//2
  ends=[r for r in records if r['tissue']==o.name]
  if len(ends)!=2:continue
  weights=[]
  for item in ends:
   ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']]
   blend={}
   for index in ids:
    for g in o.data.vertices[index].groups:
     name=o.vertex_groups[g.group].name;blend[name]=blend.get(name,0)+g.weight/len(ids)
   weights.append(blend)
  for v in o.data.vertices:
   if o.data.attributes['attachment_endpoint'].data[v.index].value:continue
   t=(v.index%half)//2/max(1,half//2-1);blend={}
   for b,w in weights[0].items():blend[b]=blend.get(b,0)+w*(1-t)
   for b,w in weights[1].items():blend[b]=blend.get(b,0)+w*t
   assign(o,v.index,o.matrix_world@v.co,blend)

 # Crosslinks sit at the mid-thickness of their pale support, avoiding drift
 # out of a thin ribbon when Head and Chest rotate relative to one another.
 for item in records:
  if not item['tissue'].startswith('VeilCrosslink') or not item['support'].startswith('ChestVeil'):continue
  o=byname[item['tissue']];host=byname[item['support']]
  ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']]
  half=len(host.data.vertices)//2;bvh=tree(host)
  center=sum((o.matrix_world@o.data.vertices[i].co for i in ids),Vector())/len(ids)
  blends=[(.40,.30,.30),(.30,.40,.30),(.25,.40,.35),(.35,.30,.35)]
  faces=[list(t.vertices) for t in host.data.loop_triangles if all(i<half for i in t.vertices)]
  faces.sort(key=lambda f:(host.matrix_world@sum((host.data.vertices[i].co for i in f),Vector())/3-center).length)
  valid=[f for f in faces if all(inside(host.matrix_world@sum(((host.data.vertices[i].co+host.data.vertices[i+half].co)*w*.5 for i,w in zip(f,blend)),Vector()),bvh) for blend in blends)]
  if not valid:continue
  for index,blend in zip(ids,blends):
   p=Vector();weights={}
   for i,w in zip(valid[0],blend):
    for vertex in (i,i+half):
     p+=host.data.vertices[vertex].co*w*.5
     for g in host.data.vertices[vertex].groups:
      b=host.vertex_groups[g.group].name;weights[b]=weights.get(b,0)+g.weight*w*.5
   assign(o,index,host.matrix_world@p,weights)
  if item['tissue']=='VeilCrosslink-11':
   for index in ids:
    p,w=rigid_ribbon_end(o.matrix_world@o.data.vertices[index].co,host,index);assign(o,index,p,w)
 for item in records:
  o=byname[item['tissue']];host=byname[item['support']];bvh=tree(host)
  ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']]
  assert ids and all(inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(groups=len(set(r['tissue'] for r in records)),endpoints=len(records),records=records),indent=2)+'\n')
 height=max((o.matrix_world@v.co).z for o in meshes for v in o.data.vertices)-min((o.matrix_world@v.co).z for o in meshes for v in o.data.vertices)
 old.update(revised_authored_height=height,pipeline_scale_correction=height/old['original_authored_height'])
 (OUT/'head-refinement.json').write_text(json.dumps(old,indent=2)+'\n')
 (OUT/'head-bundles.json').write_text(json.dumps(dict(shoulder_caps=['CathedralMantle'+str(k) for k in range(4)],head_caps=['CathedralMantle'+str(k) for k in range(4,8)],head_cap_count=4,shoulder_weight='Chest',head_weight='Head',beard='six existing upper curtains rooted in head caps and joining pale chest framework',triangles_added=0,new_attachment_records=len(new_ids),preserved='body scale, chest cavity, shoulder compression, side hip shelves and central bark, limbs and feet'),indent=2)+'\n')
 return trunk
