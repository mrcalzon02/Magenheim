"""R15: continuous frontal plate and two deep crystal-lined flank pockets."""
import bpy,bmesh,json
import numpy as np
from pathlib import Path
from mathutils import Vector,geometry
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree
from maw_contact_validation import contains
import rift_titan_head_volume as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-paired-recesses-r15'
# Canonical side loop: front lower corner, front middle, front upper corner,
# upper point, rear upper corner, rear middle, rear lower corner.
# Each point is (height fraction, fraction of flank depth behind front plate).
POCKET=((.145,.31),(.335,.18),(.610,.20),(.765,.43),(.655,.77),(.405,.87),(.205,.79))
INSET=(.060,.095,.085,.045,.075,.110,.085)

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;trunk=previous.build(B,arm,record,profile,tube,cap)
 head=bpy.data.objects[previous.HEAD];old=head.data
 base=min(v.co.z for v in old.vertices);height=max(v.co.z for v in old.vertices)-base
 # The reference shows dark upper socket depth with localized violet lower
 # exposures, not a uniformly bright purple panel filling the whole opening.
 mat=old.materials[1];nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF')
 geo=next(n for n in nodes if n.type=='NEW_GEOMETRY');sep=nodes.new('ShaderNodeSeparateXYZ');links.new(geo.outputs['Position'],sep.inputs[0])
 normalize=nodes.new('ShaderNodeMapRange');normalize.inputs['From Min'].default_value=base;normalize.inputs['From Max'].default_value=base+height;links.new(sep.outputs['Z'],normalize.inputs[0])
 mask=nodes.new('ShaderNodeValToRGB');stops=((.10,.08),(.23,1.),(.36,.55),(.53,.07),(.80,.025))
 for i,(position,value) in enumerate(stops):
  e=mask.color_ramp.elements[i] if i<2 else mask.color_ramp.elements.new(position);e.position=position;e.color=(value,value,value,1)
 links.new(normalize.outputs[0],mask.inputs[0])
 for socket in ('Base Color','Emission Color'):
  source=bs.inputs[socket].links[0].from_socket;mul=nodes.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1;links.new(source,mul.inputs[1]);links.new(mask.outputs[0],mul.inputs[2]);links.new(mul.outputs[0],bs.inputs[socket])
 # The outer 32 vertices are the actual preceding front/back shell landmarks.
 # Keeping them verbatim preserves both approved outline and corrected rake.
 vertices=[tuple(old.vertices[i].co) for i in range(32)]
 components=json.loads(bpy.context.scene['silhouette_components']);component=components[head.name]
 faces=[];roles=[];lining=[];sides=[];front_plate=[]
 def face(ids,role=1,lined=False,side=0,plate=False):
  faces.append(tuple(ids));roles.append(role);lining.append(lined or role==3);sides.append(side);front_plate.append(plate)
 def polygon(ids,projection='XZ',role=1,side=0,plate=False):
  a,b=(0,2) if projection=='XZ' else (1,2)
  points=[Vector((vertices[i][a],vertices[i][b],0)) for i in ids]
  triangles=geometry.tessellate_polygon([points]);assert len(triangles)==len(ids)-2,(len(ids),len(triangles))
  for tri in triangles:face([ids[v if isinstance(v,int) else points.index(v)] for v in tri],role,side=side,plate=plate)
 def bridge(a,b,side):
  # Join two ordered chains using cross-chain diagonals only. Generic n-gon
  # triangulation can reuse a front-plate diagonal across the adjacent flank.
  def progress(chain):
   lengths=[0.]
   for i,j in zip(chain,chain[1:]):lengths.append(lengths[-1]+(Vector(vertices[i])-Vector(vertices[j])).length)
   return [v/lengths[-1] for v in lengths]
  pa,pb=progress(a),progress(b);i=j=0
  while i<len(a)-1 or j<len(b)-1:
   if j==len(b)-1 or (i<len(a)-1 and pa[i+1]<=pb[j+1]):face((a[i],a[i+1],b[j]),side=side);i+=1
   else:face((a[i],b[j+1],b[j]),side=side);j+=1
 polygon(list(range(20)),plate=True)
 polygon(list(range(20,32)))
 face((9,10,26,25));face((19,0,20,31))
 def at_height(ids,t):
  target=base+height*t;pts=sorted((Vector(vertices[i]) for i in ids),key=lambda p:p.z)
  for a,b in zip(pts,pts[1:]):
   if a.z<=target<=b.z:return a.lerp(b,(target-a.z)/(b.z-a.z))
  raise ValueError(('socket height outside host flank',t))
 socket_rows=[]
 for side,sign,front,back in ((1,-1,list(range(10)),list(range(25,19,-1))),(2,1,list(range(19,9,-1)),list(range(26,32)))):
  lip=[];floor=[]
  for j,(t,s) in enumerate(POCKET):
   if side==2:t=t*.98+.008;s=min(.90,s+.018)
   a=at_height(front,t);b=at_height(back,t);p=a.lerp(b,s)
   lip.append(len(vertices));vertices.append(tuple(p))
  for j,i in enumerate(lip):
   p=Vector(vertices[i]);p.x-=sign*INSET[j]*(1 if side==1 else .88)
   p.y-=.009 if j in (4,5,6) else 0
   floor.append(len(vertices));vertices.append(tuple(p))
  # Four simple strips triangulate the flank around its hole. Every rim edge
  # belongs to the shell and the inward wall; no floating eye or overlay plane.
  bridge(front,[lip[0],lip[1],lip[2],lip[3]],side)
  polygon([front[-1],back[0],lip[4],lip[3]],'YZ',side=side)
  bridge(back,[lip[4],lip[5],lip[6]],side)
  polygon([back[-1],front[0],lip[0],lip[6]],'YZ',side=side)
  for j in range(7):face((lip[j],lip[(j+1)%7],floor[(j+1)%7],floor[j]),2,j in (3,4,5),side)
  polygon(floor,'YZ',3,side)
  socket_rows.append(dict(side='left' if side==1 else 'right',lip_vertices=lip,floor_vertices=floor,authored_depths=[INSET[j]*(1 if side==1 else .88) for j in range(7)]))
 assert len(vertices)==60
 me=bpy.data.meshes.new('PairedRecessCrystalHead');me.from_pydata(vertices,[],faces)
 for mat in old.materials:me.materials.append(mat)
 head.data=me
 for name,domain in (('attachment_component','FACE'),('head_surface_role','FACE'),('head_crystal_lining','FACE'),('head_socket_side','FACE'),('head_front_plate','FACE'),('attachment_endpoint','POINT'),('head_outline_vertex','POINT'),('head_socket_ring','POINT'),('head_socket_corner','POINT')):me.attributes.new(name,'INT',domain)
 for f,role,lined,side,plate in zip(me.polygons,roles,lining,sides,front_plate):
  f.material_index=int(lined)
  for name,value in (('attachment_component',component),('head_surface_role',role),('head_crystal_lining',int(lined)),('head_socket_side',side),('head_front_plate',int(plate))):me.attributes[name].data[f.index].value=value
 for v in me.vertices:
  i=v.index;me.attributes['head_outline_vertex'].data[i].value=i+1 if i<20 else 0
  if i>=32:
   me.attributes['head_socket_ring'].data[i].value=(i-32)//7+1;me.attributes['head_socket_corner'].data[i].value=(i-32)%7+1
 group=head.vertex_groups.get('Body')
 if group is None:group=head.vertex_groups.new(name='Body')
 group.add(list(range(60)),1,'REPLACE');previous.normals(head);assert len(me.loop_triangles)==116
 bpy.context.view_layer.objects.active=head;head.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.1,island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT');me.uv_layers.active.name='BossConstructionUV';head.select_set(False)
 # Recheck only the six head ends; preserve all remaining neck/collar geometry.
 contacts=json.loads(bpy.context.scene['tissue_attachment_contract']);root_moves=[];bvh=tree(head);tris=np.array([[list(me.vertices[i].co) for i in t.vertices] for t in me.loop_triangles])
 for r in [r for r in contacts if r['support']==head.name]:
  neck=bpy.data.objects[r['tissue']];ids=[v.index for v in neck.data.vertices if neck.data.attributes['attachment_endpoint'].data[v.index].value==r['id']]
  def enclosed():
   a,b,c=[neck.data.vertices[i].co for i in ids]
   return all(contains(p,bvh,tris) for p in (a,b,c,(a+b+c)/3,(a+b)/2,(b+c)/2,(c+a)/2,a*.5+b*.25+c*.25,a*.25+b*.5+c*.25,a*.25+b*.25+c*.5))
  if enclosed():continue
  old_points=[neck.data.vertices[i].co.copy() for i in ids]
  for i in ids:
   p,w=embed(neck.data.vertices[i].co,head,.014);assign(neck,i,p,w)
  if not enclosed():
   points=[neck.data.vertices[i].co.copy() for i in ids];anchor,weights=embed(sum(points,Vector())/3,head,.025)
   for contraction in (.85,.65,.45,.25,.10):
    for i,p in zip(ids,points):assign(neck,i,anchor+(p-anchor)*contraction,weights)
    if enclosed():break
   else:raise ValueError('head root cannot seat into paired-pocket head')
  root_moves.append(dict(tissue=neck.name,max_authored_move=max((neck.data.vertices[i].co-p).length for i,p in zip(ids,old_points))))
  previous.normals(neck)
 (OUT/'paired-recess-authoring.json').write_text(json.dumps(dict(interpretation='one continuous stone front plate with a deep purple-crystal-lined pocket on each flank behind it; no central purple stripe or median split',preserved='all 32 r14 front/back shell vertices, rake and outer volume; neck/collar geometry except any necessary head-end seating',head_triangles=116,head_vertices=60,total_actor_triangles=1800,sockets=socket_rows,root_moves=root_moves,acceptance='unaccepted side-pocket interpretation candidate'),indent=2)+'\n',encoding='utf-8')
 return trunk
