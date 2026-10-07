"""Replace bone-like radial tubes with wall-rooted fungal shelf blades."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
import first_bloom_attachment_repair as prior
materials=prior.materials;unwrap_atlas=prior.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-frills-r6'

def embed(point,host,depth=.002):
 bvh=prior.tree(host);hit,normal,face,distance=bvh.find_nearest(point)
 tri=host.data.loop_triangles[face];center=sum((host.matrix_world@host.data.vertices[i].co for i in tri.vertices),Vector())/3
 for inset in (0,.01,.05,.2,.5):
  contact=hit.lerp(center,inset)
  for d in (depth,.0005,.001,.003):
   for sign in (-1,1):
    seated=contact+normal*d*sign
    if prior.inside(seated,bvh):return seated,prior.bone_weights(host,face,contact)
 raise ValueError('frill attachment cannot seat into '+host.name)

def assign(o,index,position,weights):
 v=o.data.vertices[index];v.co=o.matrix_world.inverted()@position
 for w in list(v.groups):o.vertex_groups[w.group].remove([index])
 for bone,w in weights.items():
  group=o.vertex_groups.get(bone) or o.vertex_groups.new(name=bone);group.add([index],w,'REPLACE')

def build(B,arm,record,profile,tube,cap):
 prior.OUT=OUT
 trunk=prior.build(B,arm,record,profile,tube,cap)
 wall=bpy.data.objects['RecessedChamber'];core=bpy.data.objects['AmberChamberHeart']
 # A small recessed amber growth replaces the long central sternum-like rod.
 for v in core.data.vertices:
  j,k=divmod(v.index,8);r,y=[(.025,-.066),(1,-.052),(.025,-.030)][j];a=math.tau*k/8
  v.co=(.10*r*math.cos(a),y,1.92+.115*r*math.sin(a))
 for index in range(8):
  point,weights=embed(core.matrix_world@core.data.vertices[index].co,wall,.002)
  assign(core,index,point,weights)
 # Remove short linear glowing struts that reinforced the sternum reading.
 for o in list(bpy.context.scene.objects):
  if o.name.startswith('InnerGlow'):bpy.data.objects.remove(o,do_unlink=True)
 frills=[]
 for o in list(bpy.context.scene.objects):
  if not o.name.startswith('ChamberRib'):continue
  s=-1 if '-' in o.name else 1;k=int(o.name.rsplit('_',1)[1]);a=-1.35+k*2.7/10
  root=Vector((s*.43*math.cos(a),.20,1.91+.40*math.sin(a)))
  free=Vector((s*max(.11,abs(root.x)*.40),.175+.012*math.sin(k*.9),1.91+.24*math.sin(a)))
  tangent=Vector((-s*math.sin(a),0,math.cos(a))).normalized()
  centers=[free,free.lerp(root,.5)+Vector((0,.035,0)),root]
  free_width=min(.044,(abs(free.x)-.08)/max(.05,abs(tangent.x)))
  widths=[free_width,.057+.007*math.sin(k*1.1),.039];thickness=[.003,.006,.009]
  for v in o.data.vertices:
   j,i=divmod(v.index,5);w=widths[j];h=thickness[j]
   u,depth=[(-w,-h),(0,-h*1.20),(w,-h),(.78*w,h),(-.78*w,h)][i]
   co=centers[j]+tangent*u+Vector((0,depth,0))
   v.co=co
   if j==0:o.data.attributes['attachment_endpoint'].data[v.index].value=0
  # The sole structural root is the wall-side flange. The leading shelf edge
  # is intentionally free, rather than connecting to a central sternum.
  for index in range(10,15):
   point,weights=embed(o.matrix_world@o.data.vertices[index].co,wall,.002 if index<13 else .004)
   assign(o,index,point,weights)
  o['frill_role']='rooted in cavity wall; broad lamella with free leading edge'
  # Flat blade faces avoid interpolating the thin sidewall normals across the
  # whole lamella, which otherwise makes a flat shelf look like a rounded rib.
  for poly in o.data.polygons:poly.use_smooth=False
  o.data.attributes.new('frill_free_edge','INT','POINT')
  for index in range(5):o.data.attributes['frill_free_edge'].data[index].value=1
  frills.append(o.name)
 changed=set(frills+['AmberChamberHeart']);byname={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'}
 old=json.loads(bpy.context.scene['tissue_attachment_contract']);records=[]
 for item in old:
  if item['tissue'] in frills and item['end']=='start':continue
  if item['tissue'] in frills:item['end']='wall_root';item['anatomy']='fungal shelf frill; leading edge intentionally free'
  o=byname[item['tissue']];host=byname[item['support']]
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  if item['support'] in changed:
   original=[v.co.copy() for v in o.data.vertices]
   deltas=[]
   for index in ids:
    point,weights=embed(o.matrix_world@o.data.vertices[index].co,host,.002)
    deltas.append(point-o.matrix_world@o.data.vertices[index].co);assign(o,index,point,weights)
   # Retain the other attachment, easing the moved end through its nearest span.
   delta=sum(deltas,Vector())/len(deltas)
   if o.name.startswith(('MantleCurtain','ChestVeil','LimbTissue')):
    n=len(o.data.vertices)//4
    for v in o.data.vertices:
     j=(v.index%(2*n))//2
     if j not in (0,n-1):v.co+=delta*(j/(n-1))**2
   elif o.name.startswith('VeilCrosslink'):
    for index in range(4,8):o.data.vertices[index].co+=delta*.25
  records.append(item)
 # Verify the revised attachments before combining; the saved-mesh verifier
 # repeats this check independently after export.
 for item in records:
  o=byname[item['tissue']];host=byname[item['support']];bvh=prior.tree(host)
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  assert all(prior.inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'frill-anatomy.json').write_text(json.dumps(dict(frills=len(frills),attachment_ends=len(records),wall_roots=22,declared_free_leading_edges=22,central_gap_authored_metres=.16,anatomy='broad thin folded shelf blades rooted in the concave cavity wall, not ribs joined to a sternum',amber_focus='small recessed growth replacing the central rod',removed_glowing_struts=6),indent=2)+'\n')
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(groups=len(set(i['tissue'] for i in records)),endpoints=len(records),records=records,free_edges='22 declared frill leading edges; each opposite flange is rooted in cavity wall'),indent=2)+'\n')
 return trunk
