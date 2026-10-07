"""Seat both ends of every tissue/vein group into an explicit support surface.

Persist source component and endpoint attributes so attachment validation can
inspect the saved combined mesh, rather than trusting the authoring operation.
"""
import bpy,json
from pathlib import Path
from mathutils import Vector,geometry
from mathutils.bvhtree import BVHTree
import first_bloom_precision_sculpt as previous
materials=previous.materials
unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-attachments-r5'

def tree(o):
 o.data.update();o.data.calc_loop_triangles()
 return BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[list(t.vertices) for t in o.data.loop_triangles],all_triangles=True)

def inside(point,bvh):
 direction=Vector((.713,.421,.561)).normalized();p=point.copy();hits=0
 for _ in range(100):
  loc,normal,index,distance=bvh.ray_cast(p,direction,100)
  if loc is None:return hits%2==1
  hits+=1;p=loc+direction*.00001
 raise ValueError('attachment inside test failed to converge')

def bone_weights(o,face,loc):
 # Interpolate the actual support triangle's source weights at the contact.
 groups={g.index:g.name for g in o.vertex_groups}
 for tri in [o.data.loop_triangles[face]]:
  ids=tri.vertices;coords=[o.matrix_world@o.data.vertices[i].co for i in ids]
  bary=geometry.barycentric_transform(loc,*coords,Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)))
  if min(bary)<-.001:continue
  result={}
  for index,w in zip(ids,bary):
   for influence in o.data.vertices[index].groups:
    name=groups[influence.group];result[name]=result.get(name,0)+max(0,w)*influence.weight
  total=sum(result.values());return {k:v/total for k,v in result.items() if v/total>1e-7}
 raise ValueError('cannot interpolate attachment support '+o.name)

def build(B,arm,record,profile,tube,cap):
 trunk=previous.build(B,arm,record,profile,tube,cap)
 meshes=sorted([o for o in bpy.context.scene.objects if o.type=='MESH'],key=lambda o:o.name)
 components={o.name:i+1 for i,o in enumerate(meshes)}
 for o in meshes:
  attr=o.data.attributes.new('attachment_component','INT','FACE')
  for item in attr.data:item.value=components[o.name]
  o.data.attributes.new('attachment_endpoint','INT','POINT')
 byname={o.name:o for o in meshes};cache={o.name:tree(o) for o in meshes};records=[]
 def candidates(prefixes):return [o for o in meshes if o.name.startswith(prefixes)]
 def seat(center,hosts,depths=(.003,.001,.005)):
  options=[]
  for host in hosts:
   hit,normal,face,distance=cache[host.name].find_nearest(center)
   if hit is not None:options.append((distance,host,hit,normal,face))
  for distance,host,hit,normal,face in sorted(options,key=lambda x:x[0]):
   if distance>.5:continue
   tri=host.data.loop_triangles[face]
   centroid=sum((host.matrix_world@host.data.vertices[i].co for i in tri.vertices),Vector())/3
   # Closest points on sharp rims can lie on an edge. Move a small distance
   # into the chosen face before seating, rather than offsetting into air.
   for inset in (0,.01,.05,.20,.50):
    contact=hit.lerp(centroid,inset)
    for depth in depths:
     for sign in (-1,1):
      point=contact+normal*depth*sign
      if inside(point,cache[host.name]):return host,point,bone_weights(host,face,contact),distance
  raise ValueError('no seated support within 0.5 authored metres')
 def repair(o,rings,hosts):
  world=o.matrix_world;inverse=world.inverted();centers=[sum((world@o.data.vertices[i].co for i in ring),Vector())/len(ring) for ring in rings]
  try:targets=[seat(centers[0],hosts[0]),seat(centers[-1],hosts[1])]
  except ValueError as error:raise ValueError(o.name+' '+str(error)) from error
  deltas=[targets[0][1]-centers[0],targets[1][1]-centers[-1]]
  for j,ring in enumerate(rings):
   t=j/(len(rings)-1);weights={}
   for bone,w in targets[0][2].items():weights[bone]=weights.get(bone,0)+(1-t)*w
   for bone,w in targets[1][2].items():weights[bone]=weights.get(bone,0)+t*w
   assert len([w for w in weights.values() if w>1e-7])<=2,(o.name,weights)
   for index in ring:
    v=o.data.vertices[index];v.co=inverse@(world@v.co+deltas[0]*(1-t)**2+deltas[1]*t*t)
    for w in list(v.groups):o.vertex_groups[w.group].remove([index])
    for bone,w in weights.items():
     if w>1e-7:
      vg=o.vertex_groups.get(bone) or o.vertex_groups.new(name=bone);vg.add([index],w,'REPLACE')
  for end,ring in enumerate((rings[0],rings[-1])):
   marker=len(records)+1;host,point,weights,distance=targets[end]
   # Seat the entire endpoint cross-section, not just its center. Otherwise a
   # wide rail can still protrude above the cap as a detached-looking fleck.
   for k,index in enumerate(ring):
    v=o.data.vertices[index]
    depth=.002 if k<len(ring)//2 else .004
    try:_,position,vertex_weights,_=seat(world@v.co,[host],(depth,.001,.003,.005,.0002,.0005))
    except ValueError as error:raise ValueError(o.name+' endpoint vertex into '+host.name) from error
    v.co=inverse@position
    for w in list(v.groups):o.vertex_groups[w.group].remove([index])
    for bone,w in vertex_weights.items():
     vg=o.vertex_groups.get(bone) or o.vertex_groups.new(name=bone);vg.add([index],w,'REPLACE')
    o.data.attributes['attachment_endpoint'].data[index].value=marker
   records.append(dict(id=marker,tissue=o.name,end='start' if end==0 else 'end',support=host.name,support_component=components[host.name],before_surface_distance=distance,endpoint_seating='each rail/ring vertex embedded separately',initial_center_contact_weights=weights))
  cache[o.name]=tree(o)

 # Rooted structures first, then crosslinks supported by the repaired frames.
 for o in meshes:
  if o.name.startswith('ChamberRib'):
   repair(o,[list(range(j*5,(j+1)*5)) for j in range(3)],([byname['AmberChamberHeart']],candidates(('RecessedChamber','HollowCathedralTrunk'))))
 for o in meshes:
  if not o.name.startswith(('MantleCurtain','ChestVeil','LimbTissue')):continue
  n=len(o.data.vertices)//4;rings=[[2*j,2*j+1,2*n+2*j,2*n+2*j+1] for j in range(n)]
  if o.name.startswith('MantleCurtain'):
   k=int(o.name.removeprefix('MantleCurtain').split('_')[0])
   start=[byname['CathedralMantle'+str(k)]]
   end=[byname['CathedralMantle'+str(i)] for i in range(k)]+[byname['CrownSpine']]+candidates(('LimbBracketUpperArm','HollowCathedralTrunk')) if k else candidates(('HollowCathedralTrunk','RecessedChamber','ChamberRib'))
  elif o.name.startswith('ChestVeil'):
   start=candidates(('CathedralMantle0','CathedralMantle1'))
   end=candidates(('HollowCathedralTrunk','RecessedChamber','ChamberRib'))
  else:
   stem=o.name.removeprefix('LimbTissue')[:-1]
   start=candidates(('LimbBracket'+stem,))
   end=candidates(('Rooted'+stem,'LimbBracket'+stem))
  repair(o,rings,(start,end))
 for o in meshes:
  if o.name.startswith('VeilCrosslink'):
   repair(o,[list(range(j*4,(j+1)*4)) for j in range(3)],(candidates(('ChestVeil',)),candidates(('ChestVeil','ChamberRib','AmberChamberHeart'))))
 # Independently reconstruct endpoint centers and require them INSIDE their
 # designated closed host, not merely within a loose proximity tolerance.
 for record in records:
  o=byname[record['tissue']];ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==record['id']]
  assert all(inside(o.matrix_world@o.data.vertices[i].co,cache[record['support']]) for i in ids),record
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(groups=len(records)//2,endpoints=len(records),records=records,attachment_method='explicit intersecting support surfaces; no topological welding'),indent=2)+'\n')
 return trunk
