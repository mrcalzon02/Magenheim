"""Swept structural fin rays, recessed torn membranes and actual perforations."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
import blackwater_maw_fin_restoration as previous
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree,inside,bone_weights
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-spined-fins-r8-raw'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 # The preceding recipe stores its output directory in its executed globals.
 previous.build.__globals__['OUT']=OUT
 body=previous.build(B,arm,record,profile,tube,cap)
 records=json.loads(bpy.context.scene['tissue_attachment_contract']);report=[]
 def rebuild(o,vertices,faces,weights,root_ids,marker,bone_faces):
  component=o.data.attributes['attachment_component'].data[0].value
  me=bpy.data.meshes.new(o.name+'_SpinedSurface');me.from_pydata(vertices,[],faces);me.update()
  me.materials.append(B.mats['fin-membrane']);me.materials.append(B.mats['bone-armor'])
  for p in me.polygons:p.use_smooth=True;p.material_index=1 if p.index in bone_faces else 0
  o.data=me;o.vertex_groups.clear()
  for i,w in enumerate(weights):assign(o,i,Vector(vertices[i]),w)
  attr=me.attributes.new('attachment_component','INT','FACE')
  for a in attr.data:a.value=component
  me.attributes.new('attachment_endpoint','INT','POINT')
  for i in root_ids:me.attributes['attachment_endpoint'].data[i].value=marker
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT');me.uv_layers.active.name='BossConstructionUV';o.select_set(False)
  bm=bmesh.new();bm.from_mesh(me);bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED',ngon_method='EAR_CLIP');bm.to_mesh(me);bm.free();me.update()
 def old_root(o,marker,back=False):
  ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==marker]
  half=len(o.data.vertices)//2;ids=[i for i in ids if (i>=half)==back]
  points=[o.data.vertices[i].co.copy() for i in ids];weights=[]
  for i in ids:weights.append({o.vertex_groups[g.group].name:g.weight for g in o.data.vertices[i].groups if g.weight>1e-7})
  return points,weights
 def sample(points,weights,t):
  x=t*(len(points)-1);a=min(len(points)-2,int(x));f=x-a;w={}
  for b,v in weights[a].items():w[b]=w.get(b,0)+v*(1-f)
  for b,v in weights[a+1].items():w[b]=w.get(b,0)+v*f
  w={b:v for b,v in w.items() if v>1e-7};point=points[a].lerp(points[a+1],f)
  if len(w)>2:
   nearest=a if f<.5 else a+1;point=points[nearest].copy();w=dict(weights[nearest])
   inward=Vector((-point.x,0,.06-point.z)).normalized();point+=inward*.005
  assert len(w)<=2,w
  total=sum(w.values());return point,{b:v/total for b,v in w.items()}
 def faces_for(n,rows,holes):
  count=n*rows;faces=[];bone_faces=set()
  for layer in range(2):
   for j in range(rows-1):
    for k in range(n-1):
     if (j,k) in holes:continue
     a=layer*count+j*n+k;f=(a,a+1,a+n+1,a+n)
     if k==0:bone_faces.add(len(faces))
     faces.append(f if layer==0 else tuple(reversed(f)))
  boundary=list(range(n))+[j*n+n-1 for j in range(1,rows)]+list(reversed(range((rows-1)*n,rows*n-1)))+[j*n for j in reversed(range(1,rows-1))]
  for a,b in zip(boundary,boundary[1:]+boundary[:1]):faces.append((a,b,b+count,a+count))
  for j,k in holes:
   a=j*n+k;ring=[a,a+1,a+n+1,a+n]
   for a,b in zip(ring,ring[1:]+ring[:1]):faces.append((a,b,b+count,a+count))
  return faces,bone_faces
 for item in records:
  if item['end'] not in ('dorsal_root','pectoral_root','forward_pectoral_root'):continue
  o=bpy.data.objects[item['tissue']];front_roots,front_w=old_root(o,item['id']);back_roots,back_w=old_root(o,item['id'],True);dorsal=item['end']=='dorsal_root';n=13 if dorsal else 19;rows=4;count=n*rows
  leader={'AnteriorDorsalFin':1.32,'MainSecondDorsalFin':1.30,'AftDorsalFin':.82,'CaudalDorsalFin':.43}.get(o.name,0)
  vertices=[];weights=[]
  for layer in range(2):
   roots,rw=(front_roots,front_w) if layer==0 else (back_roots,back_w)
   for j,t in enumerate((0,.32,.70,1)):
    for k in range(n):
     u=k/(n-1);root,w=sample(roots,rw,u);ray=k%3==0;pelvic=item['end']=='pectoral_root'
     if pelvic:
      s=-1 if o.name.endswith('L') else 1;a=-.25-.87*u;radius=.76 if layer==0 else .73
      root=Vector((s*.50*radius*math.cos(a),-1.40,.02+.37*radius*math.sin(a)));w={'Spine_5':1.0}
     if dorsal:
      height=leader*(1-u)**.75+.035
      if not ray:height*=.92 if k%3==1 else .96
      tip=root+Vector((0,-.43*(1-u)**.8,height))
      normal=Vector((1,0,0));free=dict(w)
     else:
      s=-1 if o.name.endswith('L') else 1;forward=item['end']=='forward_pectoral_root'
      tip=Vector((s*((2.60 if forward else 2.25)-(1.25 if forward else 1.10)*u**.8),(.05 if forward else -.35)-(1.65 if forward else 1.90)*u,(-.95 if forward else -.65)-.28*math.sin(u*math.pi)))
      if pelvic:tip=Vector((s*(1.35-.55*u**.8),-1.80-.70*u,-.48-.20*math.sin(u*math.pi)))
      if not ray:tip=root.lerp(tip,.93 if k%3==1 else .96)
      normal=Vector((0,0,1));free={'Spine_5':1.0} if pelvic else {'Fin_'+('L' if s<0 else 'R'):1.0}
     p=root.lerp(tip,t)
     if j:
      relief=(.025 if ray else -.010)*math.sin(t*math.pi)
      p+=normal*((1 if layer==0 else -1)*(.013+(.021 if ray else 0)*(1-.4*t))+relief)
     vertices.append(p);weights.append(w if j==0 else free)
  holes={(1,7)} if dorsal else {(1,7),(1,13)}
  for hj,hk in holes:
   ring=[hj*n+hk,hj*n+hk+1,(hj+1)*n+hk+1,(hj+1)*n+hk]
   center=sum((vertices[i] for i in ring),Vector())/4
   for i,factor in zip(ring,(.85,1.10,.72,1.03)):
    delta=(vertices[i]-center)*(factor-1)
    vertices[i]+=delta;vertices[i+count]+=delta
  faces,bone_faces=faces_for(n,rows,holes)
  rebuild(o,vertices,faces,weights,list(range(n))+list(range(count,count+n)),item['id'],bone_faces)
  if item['end']=='pectoral_root':item['end']='pelvic_root'
  report.append(dict(part=o.name,structural_rays=(n-1)//3+1,membrane_holes=len(holes),profile='tailward-swept dorsal spines' if dorsal else ('distinct rear pelvic rays' if item['end']=='pelvic_root' else 'back-swept tapering pectoral rays'),membrane='recessed between raised rays; scalloped torn trailing boundary'))
 tail=bpy.data.objects['BroadCaudalFan'];tn=15;count=31
 for v in tail.data.vertices:
  local=v.index%count
  if tn<=local<2*tn:
   k=local-tn;v.co.x=(1 if v.index<count else -1)*(.055 if k%3==0 else .019)
 tail.data.materials.append(B.mats['bone-armor'])
 for p in tail.data.polygons:
  if p.index%(tn*2) in (4,5,6,22,23,24):p.material_index=1
 tail.data.update()
 for item in records:
  host=bpy.data.objects[item['support']];o=bpy.data.objects[item['tissue']];host_tree=tree(host)
  ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']]
  assert ids and all(inside(o.matrix_world@o.data.vertices[i].co,host_tree) for i in ids),item
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'fin-contours.json').write_text(json.dumps(dict(parts=report,tail='broad two-lobed fan with raised radial supports; previous dimensions preserved',block_scales=0,concept='swept spines, recessed membranes, real perforations and torn edges',scope='anatomical fin surface revision; body, jaw and texture assets preserved'),indent=2)+'\n')
 return body
