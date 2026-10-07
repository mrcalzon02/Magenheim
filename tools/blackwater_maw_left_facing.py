"""Jointed, tapered fin blades guided by the user's close-up."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
import blackwater_maw_spined_fins as previous
from first_bloom_frill_sculpt import assign
from first_bloom_deeper_chest import embed
from first_bloom_attachment_repair import tree,inside
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-left-facing-r10-raw'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;body=previous.build(B,arm,record,profile,tube,cap)
 # Lower, tailward dorsal silhouette; preserve seated roots.
 for name in ('AnteriorDorsalFin','MainSecondDorsalFin','AftDorsalFin','CaudalDorsalFin'):
  fin=bpy.data.objects[name]
  for v in fin.data.vertices:
   row=(v.index%52)//13;k=v.index%13;t=(0,.32,.70,1)[row];u=k/12
   if row:
    v.co.y-=.82*(1-u)**.8*t
    rootz=fin.data.vertices[(v.index//52)*52+k].co.z
    v.co.z=rootz+(v.co.z-rootz)*.72
  fin.data.update()
 records=json.loads(bpy.context.scene['tissue_attachment_contract']);next_component=max(a.value for o in bpy.context.scene.objects if o.type=='MESH' for a in o.data.attributes['attachment_component'].data)+1;report=[]
 for side,s in [('L',-1),('R',1)]:
  bone='Fin_'+side
  def make_joint(name,points):
   nonlocal next_component
   joint=tube(name,points,[.14,.17,.14,.11],'wet-hide',bone,8,'fins')
   for i in range(8,16):assign(joint,i,joint.data.vertices[i].co,{'Spine_1':.25,bone:.75})
   component=next_component;next_component+=1
   attr=joint.data.attributes.new('attachment_component','INT','FACE')
   for a in attr.data:a.value=component
   joint.data.attributes.new('attachment_endpoint','INT','POINT');marker=len(records)+1
   for index in range(8):
    p,w=embed(joint.data.vertices[index].co,body,.075);assign(joint,index,p,w);joint.data.attributes['attachment_endpoint'].data[index].value=marker
   records.append(dict(id=marker,tissue=joint.name,end='fleshy_fin_joint',support=body.name,support_component=body.data.attributes['attachment_component'].data[0].value,endpoint_seating='complete eight-vertex joint root embedded into body'))
   return joint
  upper=make_joint('UpperFinJoint'+side,[(s*.80,.90,-.13),(s*1.08,.72,-.23),(s*1.26,.38,-.34),(s*1.40,.16,-.44)])
  lower=make_joint('LowerFinJoint'+side,[(s*.72,1.36,-.43),(s*.98,1.20,-.56),(s*1.10,1.00,-.68),(s*1.18,.82,-.76)])
  paths=[([(s*1.10,1.00,-.68),(s*1.36,.64,-.91),(s*1.62,.21,-1.14),(s*1.85,-.27,-1.27),(s*2.03,-.66,-1.30),(s*2.12,-.88,-1.29)],[.05,.20,.36,.34,.18,.003],Vector((0,0,1))),([(s*1.26,.38,-.34),(s*1.47,-.05,-.48),(s*1.70,-.54,-.60),(s*1.89,-1.05,-.74),(s*1.98,-1.51,-.87),(s*1.98,-1.74,-.92)],[.04,.15,.22,.21,.11,.003],Vector((0,0,1)))]
  for name,path in zip(('ForwardPectoral'+side,'PleatedPectoral'+side),paths):
   joint=lower if name.startswith('Forward') else upper
   o=bpy.data.objects[name];component=o.data.attributes['attachment_component'].data[0].value
   item=next(r for r in records if r['tissue']==name);centers,widths,guide=path;centers=[Vector(p) for p in centers];n=12;vertices=[];faces=[]
   for j,(p,width) in enumerate(zip(centers,widths)):
    tangent=(centers[min(j+1,len(centers)-1)]-centers[max(0,j-1)]).normalized();wide=(guide-tangent*guide.dot(tangent)).normalized();normal=tangent.cross(wide).normalized()
    for k in range(n):
     a=math.tau*k/n;wave=1+.035*math.sin(k*3.7+j*.9);thickness=(.045 if 2<=j<=4 and k==3 else .027) if j<5 else .002
     vertices.append(p+wide*(width*math.cos(a)*wave)+normal*(thickness*math.sin(a)))
   for j in range(5):
    for k in range(n):a=j*n+k;b=j*n+(k+1)%n;faces.append((a,b,b+n,a+n))
   faces.extend([tuple(reversed(range(n))),tuple(5*n+k for k in range(n))])
   me=bpy.data.meshes.new(name+'_LeafBlade');me.from_pydata(vertices,[],faces);me.update();me.materials.append(B.mats['wet-hide']);me.materials.append(B.mats['bone-armor']);o.data=me;o.vertex_groups.clear()
   for p in me.polygons:p.use_smooth=True;p.material_index=1 if p.index<60 and p.index%n in (0,11) else 0
   for v in me.vertices:assign(o,v.index,v.co,{bone:1.0})
   attr=me.attributes.new('attachment_component','INT','FACE')
   for a in attr.data:a.value=component
   me.attributes.new('attachment_endpoint','INT','POINT')
   for index in range(n):
    point=me.vertices[index].co
    if not inside(point,tree(joint)):point,w=embed(point,joint,.02);assign(o,index,point,w)
    me.attributes['attachment_endpoint'].data[index].value=item['id']
   item.update(end='broad_blade_root' if name.startswith('Forward') else 'trailing_blade_root',support=joint.name,support_component=joint.data.attributes['attachment_component'].data[0].value,endpoint_seating='blade base seated in fleshy fin joint')
   bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
   bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT');me.uv_layers.active.name='BossConstructionUV';o.select_set(False)
   bm=bmesh.new();bm.from_mesh(me);bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED',ngon_method='EAR_CLIP');bm.to_mesh(me);bm.free();me.update()
  report.append(dict(side=side,joints=[lower.name,upper.name],blades=['ForwardPectoral'+side,'PleatedPectoral'+side],shape='broad curved downward blade plus narrower long rearward blade; separate lower-forward and upper-rear body attachments',fan_geometry_removed=True))
 for item in records:
  o=bpy.data.objects[item['tissue']];host=bpy.data.objects[item['support']];host_tree=tree(host)
  ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']]
  assert ids and all(inside(o.matrix_world@o.data.vertices[i].co,host_tree) for i in ids),item
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'blade-fin-anatomy.json').write_text(json.dumps(dict(reference='user close-up: fleshy joint and distinct curved blade silhouettes',joints=report,preserved='body texture, head and jaws, tailward dorsals and broad tail fan',scope='interpretation remains subject to concept review'),indent=2)+'\n')
 return body
