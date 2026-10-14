"""Trade simpler face ridges for real chipped head and mantle contours."""
import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector,geometry
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree
from maw_contact_validation import contains
import numpy as np
import rift_titan_layered_weathering as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-notched-r11'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;trunk=previous.build(B,arm,record,profile,tube,cap)
 components=json.loads(bpy.context.scene['silhouette_components']);contacts=json.loads(bpy.context.scene['tissue_attachment_contract']);changes=[]
 def shell(name,front,back,label):
  o=bpy.data.objects[name];old=o.data;old.calc_loop_triangles();before=len(old.loop_triangles)
  assert not any(a.value for a in old.attributes['attachment_endpoint'].data),name
  owners={o.vertex_groups[g.group].name for v in old.vertices for g in v.groups if g.weight>1e-7};assert len(owners)==1,(name,owners)
  n=len(front);assert n==len(back)
  # Explicit X/Z projected triangulation respects inward notches. A fan would
  # bridge across their negative space and fill the chip back in.
  flat=[Vector((p.x,p.z,0)) for p in front]
  caps=[tuple(v if isinstance(v,int) else flat.index(v) for v in t) for t in geometry.tessellate_polygon([flat])]
  assert len(caps)==n-2,(name,n,caps)
  faces=[tuple(reversed(t)) for t in caps]+[tuple(n+i for i in t) for t in caps]+[(k,(k+1)%n,(k+1)%n+n,k+n) for k in range(n)]
  me=bpy.data.meshes.new(name+' chipped shell');me.from_pydata([*front,*back],[],faces);me.materials.append(B.mats['rift-strata']);o.data=me
  for g in list(o.vertex_groups):o.vertex_groups.remove(g)
  vg=o.vertex_groups.new(name=next(iter(owners)));vg.add(list(range(2*n)),1,'REPLACE')
  attr=me.attributes.new('attachment_component','INT','FACE')
  for a in attr.data:a.value=components[name]
  me.attributes.new('attachment_endpoint','INT','POINT')
  bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED');bm.to_mesh(me);bm.free()
  bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.1,island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT');me.uv_layers.active.name='BossConstructionUV';o.select_set(False)
  me.calc_loop_triangles();changes.append(dict(name=name,before=before,after=len(me.loop_triangles),vertices=len(me.vertices),change=label))
 def rings(name):
  o=bpy.data.objects[name];assert len(o.data.vertices)==14,name
  return [v.co.copy() for v in list(o.data.vertices)[:6]],[v.co.copy() for v in list(o.data.vertices)[6:12]]
 # Eight stations make room for an abrupt outer-edge step on each head half.
 for name,s in [('SplitCrown0',-1),('SplitCrown1',1)]:
  front,back=rings(name)
  edge=max(range(6),key=lambda k:(abs(front[k].x)+abs(front[(k+1)%6].x))/2)
  result=[]
  for points in (front,back):
   out=[]
   for k,p in enumerate(points):
    out.append(p)
    if k==edge:
     a=p.lerp(points[(k+1)%6],.40 if s<0 else .47);b=p.lerp(points[(k+1)%6],.52 if s<0 else .60)
     a.x-=s*.075;b.x+=s*.008
     a.y+=.015 if points is back else -.012;out.extend((a,b))
   result.append(out)
  shell(name,*result,'eight-station outer head step; concave cap triangulation')
 # The narrow rear head support does not need two extra central face ridges.
 front,back=rings('SplitCrown2')
 def corner_area(k):
  a,b,c=front[k-1],front[k],front[(k+1)%6]
  return abs((b.x-a.x)*(c.z-b.z)-(b.z-a.z)*(c.x-b.x))
 remove=min(range(6),key=corner_area)
 shell('SplitCrown2',[p for k,p in enumerate(front) if k!=remove],[p for k,p in enumerate(back) if k!=remove],'five-station rear plate; eight triangles transferred to visible head notches')
 # Six face-ridge reductions fund the six eight-station shoulder profiles.
 for name in ('PalmL','PalmR','FootL','FootR','PelvisKeystone0','PelvisKeystone2'):
  shell(name,*rings(name),'retain all six perimeter stations; remove two central face-ridge vertices, four triangles')
 for side,s in [('L',-1),('R',1)]:
  for j in range(3):
   name='ShoulderStratum'+side+str(j);front,back=rings(name)
   edge=max(range(6),key=lambda k:(abs(front[k].x)+abs(front[(k+1)%6].x))/2)
   result=[]
   for points in (front,back):
    out=[]
    for k,p in enumerate(points):
     out.append(p)
     if k==edge:
      a=p.lerp(points[(k+1)%6],.28+.07*j);b=p.lerp(points[(k+1)%6],.49+.055*j)
      a.x-=s*(.065+.018*j);b.x+=s*.010
      a.z-=.020 if j==1 else 0
      out.extend((a,b))
    result.append(out)
   shell(name,*result,'individual eight-station shoulder chip; deeper uneven outer step')
  for bone in ('Forearm'+side,'Leg'+side):
   name='LongFracture'+bone+'1';front,back=rings(name)
   edge=max(range(6),key=lambda k:(abs(front[k].x)+abs(front[(k+1)%6].x))/2)
   result=[]
   for points in (front,back):
    out=[]
    for k,p in enumerate(points):
     out.append(p)
     if k==edge:
      notch=p.lerp(points[(k+1)%6],.43 if bone.startswith('Leg') else .58);notch.x-=s*.085;out.append(notch)
    result.append(out)
   shell(name,*result,'seven-station forearm/shin notch replaces six-station face-ridge layout at identical cost')
 # Every old support is re-evaluated after topology changes, including complete
 # arm/shoulder sections, thorax sections, crystal bases and the digit/palm chain.
 for r in contacts:
  o=bpy.data.objects[r['tissue']];host=bpy.data.objects[r['support']]
  for v in o.data.vertices:
   if o.data.attributes['attachment_endpoint'].data[v.index].value==r['id']:
    p,w=embed(v.co,host,.025 if r['end']=='limb-crystal-root' else .010);assign(o,v.index,p,w)
 # A concave chip can be bridged by an otherwise seated root loop. Test the
 # actual closing triangles and contract only failing sections toward a common
 # interior anchor; retain complete sections and all original marker/weight IDs.
 coherent=[];bary=((1/3,1/3,1/3),(.5,.25,.25),(.25,.5,.25),(.25,.25,.5),(.5,.5,0),(0,.5,.5),(.5,0,.5))
 for r in contacts:
  if r['end'] not in ('proximal-stone-root','finger-palm-root','limb-crystal-root','thorax-shoulder-root'):continue
  o=bpy.data.objects[r['tissue']];host=bpy.data.objects[r['support']];ids=[v.index for v in o.data.vertices if o.data.attributes['attachment_endpoint'].data[v.index].value==r['id']]
  o.data.calc_loop_triangles();caps=[tuple(t.vertices) for t in o.data.loop_triangles if all(i in ids for i in t.vertices)]
  bvh=tree(host);triangles=np.array([[list(host.data.vertices[i].co) for i in t.vertices] for t in host.data.loop_triangles])
  def enclosed():
   samples=[o.data.vertices[i].co for i in ids]
   for face in caps:
    a,b,c=[o.data.vertices[i].co for i in face]
    samples.extend(a*u+b*v+c*w for u,v,w in bary)
   return all(contains(p,bvh,triangles) for p in samples)
  if enclosed():continue
  old=[o.data.vertices[i].co.copy() for i in ids];center=sum(old,Vector())/len(old);anchor,weights=embed(center,host,.035)
  for factor in (.85,.70,.55,.40,.25,.10):
   for i,p in zip(ids,old):assign(o,i,anchor+(p-anchor)*factor,weights)
   if enclosed():
    coherent.append(dict(tissue=o.name,support=host.name,root_vertices=len(ids),closing_triangles=len(caps),contraction=factor));break
  else:raise ValueError('cannot seat complete closing section in '+host.name)
 assert sum(r['after']-r['before'] for r in changes)==0,changes
 bpy.context.scene['tissue_attachment_contract']=json.dumps(contacts)
 (OUT/'notch-authoring.json').write_text(json.dumps(dict(changes=changes,net_triangle_change=0,triangles_transferred=32,contacts=76,coherent_root_repairs=coherent,scope='real outline notches and angular cap facets, not texture-only cracks; r10 surfaces/rig/torso/crystals retained',remaining='more individual fragmentation, crest debris/core density, intentional texture placement and native acceptance',acceptance='continued candidate; 85% not claimed'),indent=2)+'\n',encoding='utf-8')
 return trunk
