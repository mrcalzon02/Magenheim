"""R14: preserve approved front outline; rebuild profile, lined fault and neck."""
import bpy,bmesh,json,math
import numpy as np
from pathlib import Path
from mathutils import Vector,geometry
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree
from maw_contact_validation import contains
import rift_titan_notched_stone as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-lined-volume-r14'
HEAD='UnifiedCrystalHead'
# Identical X/Z landmarks to the user-approved r13 frontal outline.
LEFT=((-.030,0),(-.235,.18),(-.263,.20),(-.278,.36),(-.205,.38),(-.200,.70),(-.128,.72),(-.137,.92),(-.082,.93),(-.055,1))
RIGHT=((.025,.02),(.230,.20),(.268,.23),(.254,.41),(.183,.43),(.180,.83),(.125,.845),(.120,.95),(.061,.97),(.008,.94))
PROFILE=((0,.680,.140),(.20,.770,.310),(.40,.740,.400),(.70,.600,.320),(.90,.450,.160),(1,.380,.040))
# Height, left/right lip X, left/right floor X, left/right recess depth.
FAULT=((.070,-.040,.015,-.023,-.004,.065,.030),
       (.190,-.080,.050,-.052,.019,.155,.065),
       (.290,-.160,.023,-.103,-.011,.235,.070),
       (.450,-.095,.055,-.063,.023,.245,.090),
       (.560,-.125,.010,-.089,-.015,.220,.060),
       (.735,-.075,.035,-.054,.012,.175,.055),
       (.910,-.060,-.010,-.042,-.026,.095,.035))
REAR_STATIONS=(0,1,3,5,7,9,10,12,14,16,18,19)

def profile_at(t):
 for (a,fa,da),(b,fb,db) in zip(PROFILE,PROFILE[1:]):
  if t<=b:
   w=(t-a)/(b-a);return fa+(fb-fa)*w,da+(db-da)*w
 return PROFILE[-1][1:]

def normals(o):
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED');bm.to_mesh(o.data);bm.free();o.data.update();o.data.calc_loop_triangles()

def crystal_lining(B):
 mat=B.mats['binding-prism'].copy();mat.name='Head fracture violet crystal lining'
 nodes=mat.node_tree.nodes;links=mat.node_tree.links;nodes.clear()
 geo=nodes.new('ShaderNodeNewGeometry');dot=nodes.new('ShaderNodeVectorMath');dot.operation='DOT_PRODUCT';dot.inputs[1].default_value=(.36,.72,.59);links.new(geo.outputs['Normal'],dot.inputs[0])
 remap=nodes.new('ShaderNodeMath');remap.operation='MULTIPLY_ADD';remap.inputs[1].default_value=.5;remap.inputs[2].default_value=.5;links.new(dot.outputs['Value'],remap.inputs[0])
 ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.18;ramp.color_ramp.elements[0].color=(.015,.002,.040,1);ramp.color_ramp.elements[1].position=.90;ramp.color_ramp.elements[1].color=(.235,.055,.42,1)
 e=ramp.color_ramp.elements.new(.52);e.color=(.070,.010,.18,1);links.new(remap.outputs[0],ramp.inputs[0])
 bs=nodes.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=.34;links.new(ramp.outputs['Color'],bs.inputs['Base Color'])
 glow=nodes.new('ShaderNodeMixRGB');glow.blend_type='MULTIPLY';glow.inputs[0].default_value=1;glow.inputs[2].default_value=(.42,.42,.42,1);links.new(ramp.outputs['Color'],glow.inputs[1]);links.new(glow.outputs[0],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.45
 out=nodes.new('ShaderNodeOutputMaterial');links.new(bs.outputs[0],out.inputs[0]);B.mats['head-fracture-lining']=mat
 return mat

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;trunk=previous.build(B,arm,record,profile,tube,cap)
 components=json.loads(bpy.context.scene['silhouette_components']);contacts=json.loads(bpy.context.scene['tissue_attachment_contract'])
 old_names=('SplitCrown0','SplitCrown1','SplitCrown2','SplitCrown3','FloatingPrism0','FloatingPrism1')
 tip=max(v.co.z for name in old_names for v in bpy.data.objects[name].data.vertices);base=min(v.co.z for v in bpy.data.objects['SplitCrown3'].data.vertices);height=tip-base;component=components['SplitCrown0']
 outline=LEFT+tuple(reversed(RIGHT));vertices=[];faces=[];roles=[];lining=[]
 for rear in (False,True):
  for i in (REAR_STATIONS if rear else range(20)):
   x,t=outline[i]
   front,depth=profile_at(t)
   # A variable prismatic cross-section, not a constant-thickness extrusion.
   # Back and side facets change depth at the actual outline breaks.
   y=front-(depth if rear else 0)-(.025 if i<10 else .018)+x*(.30 if rear else .55)
   vertices.append((x*(.87 if rear else 1),y,base+height*t))
 for column in range(4):
  for j,(t,ll,rl,lf,rf,dl,dr) in enumerate(FAULT):
   front,depth=profile_at(t);x=(ll,rl,lf,rf)[column]
   y=(front+(-.010 if j%2==0 else .010) if column<2 else front-(dl if column==2 else dr))+.55*x
   vertices.append((x,y,base+height*t))
 def face(ids,role=1,lined=False):faces.append(tuple(ids));roles.append(role);lining.append(lined or role==3)
 def polygon(ids,role=1):
  points=[Vector((vertices[i][0],vertices[i][2],0)) for i in ids]
  triangles=geometry.tessellate_polygon([points]);assert len(triangles)==len(ids)-2
  for tri in triangles:face([ids[v if isinstance(v,int) else points.index(v)] for v in tri],role)
 # Condense eight redundant rear outline points, reinvesting their triangles
 # in two more visible fracture stations. The approved front stays untouched.
 for j,i in enumerate(REAR_STATIONS):
  k=REAR_STATIONS[(j+1)%12];arc=[i]
  while arc[-1]!=k:arc.append((arc[-1]+1)%20)
  for a,b in zip(arc,arc[1:]):face((a,b,20+j))
  face((k,20+(j+1)%12,20+j))
 polygon(list(range(20,32)))
 polygon(list(range(10))+list(reversed(range(32,39))))
 polygon(list(range(10,20))+list(range(39,46)))
 face((9,10,45,38));face((19,0,32,39))
 lips=list(range(32,39))+list(reversed(range(39,46)));floors=list(range(46,53))+list(reversed(range(53,60)))
 for j in range(14):face((lips[j],lips[(j+1)%14],floors[(j+1)%14],floors[j]),2,j in (*range(7),13))
 polygon(floors,3)
 me=bpy.data.meshes.new(HEAD);me.from_pydata(vertices,[],faces);me.materials.append(B.mats['rift-strata']);me.materials.append(crystal_lining(B))
 head=bpy.data.objects.new(HEAD,me);bpy.context.collection.objects.link(head);record(head,'Body','head-crystal')
 for name,domain in (('attachment_component','FACE'),('head_surface_role','FACE'),('head_crystal_lining','FACE'),('attachment_endpoint','POINT'),('head_station','POINT'),('head_section','POINT'),('head_outline_vertex','POINT')):me.attributes.new(name,'INT',domain)
 for f,role,lined in zip(me.polygons,roles,lining):
  f.material_index=int(lined);me.attributes['attachment_component'].data[f.index].value=component;me.attributes['head_surface_role'].data[f.index].value=role;me.attributes['head_crystal_lining'].data[f.index].value=int(lined)
 for v in me.vertices:
  i=v.index;me.attributes['head_outline_vertex'].data[i].value=i+1 if i<20 else 0
  me.attributes['head_station'].data[i].value=(i-32)%7+1 if i>=32 else 0;me.attributes['head_section'].data[i].value=(i-32)//7+1 if i>=32 else 0
 normals(head);assert len(me.loop_triangles)==116 and len(me.vertices)==60
 for r in contacts:
  if r['support']=='SplitCrown3':r.update(support=HEAD,support_component=component)
 # The six original 14-triangle bars now occupy front and back arcs, with
 # complete collar/head sections, instead of six parallel bars in one plane.
 collar=bpy.data.objects['StoneCollarbone'];cp=[v.co for v in collar.data.vertices]
 cx=(min(p.x for p in cp)+max(p.x for p in cp))/2;cy=(min(p.y for p in cp)+max(p.y for p in cp))/2;cz=sum(p.z for p in cp)/len(cp)
 rx=(max(p.x for p in cp)-min(p.x for p in cp))*.47;ry=(max(p.y for p in cp)-min(p.y for p in cp))*.47
 neck_report=[]
 for j,angle in enumerate((-.18,0,.18,math.pi-.18,math.pi,math.pi+.18)):
  neck=bpy.data.objects['NeckMetalRib'+str(j)];s=math.sin(angle);c=math.cos(angle)
  bottom=Vector((cx+rx*s,cy+ry*c,cz))
  front,depth=profile_at(.12);top=Vector(((-.05,0,.05)[j%3],front-depth*.55+.045*c,base+height*.12))
  middle=bottom.lerp(top,.53);middle.x=(-.085,0,.085)[j%3];middle.y+=.035*c
  points=[bottom,middle,top];radii=(.016,.024,.015);normal=Vector((1,0,0));last=None
  for row,(p,radius) in enumerate(zip(points,radii)):
   tangent=(points[min(row+1,2)]-points[max(row-1,0)]).normalized()
   if last is not None:normal=last.rotation_difference(tangent)@normal
   normal=(normal-tangent*normal.dot(tangent)).normalized();cross=tangent.cross(normal).normalized()
   for k in range(3):
    a=math.tau*k/3;assign(neck,row*3+k,p+radius*(normal*math.cos(a)+cross*.83*math.sin(a)),{'Body':1.0})
   last=tangent
  for r in [r for r in contacts if r['tissue']==neck.name]:
   host=head if r['support']==HEAD else collar;ids=[v.index for v in neck.data.vertices if neck.data.attributes['attachment_endpoint'].data[v.index].value==r['id']]
   assert len(ids)==3
   for i in ids:
    p,w=embed(neck.data.vertices[i].co,host,.014);assign(neck,i,p,w)
   bvh=tree(host);host.data.calc_loop_triangles();tris=np.array([[list(host.data.vertices[i].co) for i in t.vertices] for t in host.data.loop_triangles])
   def enclosed():
    a,b,c=[neck.data.vertices[i].co for i in ids]
    return all(contains(p,bvh,tris) for p in (a,b,c,(a+b+c)/3,(a+b)/2,(b+c)/2,(c+a)/2,a*.5+b*.25+c*.25,a*.25+b*.5+c*.25,a*.25+b*.25+c*.5))
   if not enclosed():
    old=[neck.data.vertices[i].co.copy() for i in ids];anchor,weights=embed(sum(old,Vector())/3,host,.025)
    for contraction in (.85,.65,.45,.25,.10):
     for i,p in zip(ids,old):assign(neck,i,anchor+(p-anchor)*contraction,weights)
     if enclosed():break
    else:raise ValueError('neck cap cannot seat into '+host.name)
   r['endpoint_seating']='complete three-vertex volumetric neck section seated into named host with Body weights'
  normals(neck)
  neck_report.append(dict(name=neck.name,centers=[list(sum((neck.data.vertices[row*3+k].co for k in range(3)),Vector())/3) for row in range(3)],triangles=len(neck.data.loop_triangles)))
 for old in old_names:bpy.data.objects.remove(bpy.data.objects[old],do_unlink=True);components.pop(old)
 components[HEAD]=component;bpy.context.scene['silhouette_components']=json.dumps(components);bpy.context.scene['tissue_attachment_contract']=json.dumps(contacts)
 (OUT/'head-neck-authoring.json').write_text(json.dumps(dict(approved_front_outline='unchanged X/Z coordinates from r13; depth and fracture only',front_axis='+Y',left_axis='-X',profile=PROFILE,fault=FAULT,fracture='mainly frontal, recessed much farther on the left; violet lining on left inner wall and floor, not a centered purple ribbon',head_vertices=60,head_triangles=116,neck_ribs=neck_report,neck_triangles=84,total_actor_triangles=1800,remaining='head/chest surface refinement and all other outstanding audit regions; user and native acceptance remain separate'),indent=2)+'\n',encoding='utf-8')
 return trunk
