"""One closed crystalline head with a recessed beveled fissure, not two halves."""
import bpy,bmesh,json
from pathlib import Path
from mathutils import Vector,geometry
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree
from maw_contact_validation import contains
import numpy as np
import rift_titan_notched_stone as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-cracked-head-r12'
HEAD='UnifiedCrystalHead'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;trunk=previous.build(B,arm,record,profile,tube,cap)
 components=json.loads(bpy.context.scene['silhouette_components']);contacts=json.loads(bpy.context.scene['tissue_attachment_contract'])
 old_names=('SplitCrown0','SplitCrown1','SplitCrown2','SplitCrown3','FloatingPrism0','FloatingPrism1')
 tip=max(v.co.z for name in old_names for v in bpy.data.objects[name].data.vertices)
 base=min(v.co.z for v in bpy.data.objects['SplitCrown3'].data.vertices)
 old_triangles=0
 for name in old_names:
  bpy.data.objects[name].data.calc_loop_triangles();old_triangles+=len(bpy.data.objects[name].data.loop_triangles)
 component=components['SplitCrown0']
 # Eight vertices per cross-section: front cortex, lip, bevel down to the
 # purple recess, opposite bevel/lip/cortex, then one continuous rear shell.
 # Shared vertices make the fissure part of the surface, without a through gap.
 stations=((0,.066,.605,.155,-.015),(.15,.175,.645,.235,-.010),(.35,.265,.680,.260,-.008),(.52,.250,.720,.235,.003),(.72,.195,.765,.190,.018),(.88,.118,.805,.100,.022),(1,.012,.850,.018,.028))
 vertices=[];faces=[];roles=[];materials=[];depths=[]
 for j,(t,w,front,thickness,x) in enumerate(stations):
  z=base+(tip-base)*t;crack=x+(-.007,.005,-.008,.008,-.003,.002,0)[j]
  floor_half=min(.023,max(.0015,w*.14));lip_half=floor_half+min(.036,w*.22)
  depth=(.008,.058,.085,.098,.080,.047,.0025)[j]
  lip_y=front+.008*(w/.265);floor_y=front-depth
  ring=((x-w,front-.020*w/.265),(crack-lip_half,lip_y),(crack-floor_half,floor_y),(crack+floor_half,floor_y-.004*w/.265),(crack+lip_half,lip_y),(x+w,front-.018*w/.265),(x+w*.83,front-thickness),(x-w*.84,front-thickness))
  assert all(ring[k][0]<ring[k+1][0] for k in range(5)),j
  assert floor_y>front-thickness+.008,j
  vertices.extend((px,py,z) for px,py in ring);depths.append(depth)
 for j in range(6):
  for k in range(8):
   faces.append((j*8+k,j*8+(k+1)%8,(j+1)*8+(k+1)%8,(j+1)*8+k))
   role=2 if k in (1,3) else 3 if k==2 and j in (1,2,3,4) else 1
   roles.append(role);materials.append(1 if role==3 else 0)
 for j in (0,6):
  points=[Vector((vertices[j*8+k][0],vertices[j*8+k][1],0)) for k in range(8)]
  triangles=geometry.tessellate_polygon([points]);assert len(triangles)==6
  for tri in triangles:
   ids=tuple(j*8+(v if isinstance(v,int) else points.index(v)) for v in tri)
   faces.append(ids if j else tuple(reversed(ids)));roles.append(1);materials.append(0)
 me=bpy.data.meshes.new(HEAD);me.from_pydata(vertices,[],faces);me.materials.append(B.mats['rift-strata']);me.materials.append(B.mats['binding-prism'])
 o=bpy.data.objects.new(HEAD,me);bpy.context.collection.objects.link(o);record(o,'Body','head-crystal')
 me.attributes.new('attachment_component','INT','FACE');me.attributes.new('head_surface_role','INT','FACE')
 for f,role,mat in zip(me.polygons,roles,materials):f.material_index=mat;me.attributes['head_surface_role'].data[f.index].value=role;me.attributes['attachment_component'].data[f.index].value=component
 me.attributes.new('attachment_endpoint','INT','POINT')
 me.attributes.new('head_station','INT','POINT');me.attributes.new('head_section','INT','POINT')
 for v in me.vertices:me.attributes['head_station'].data[v.index].value=v.index//8+1;me.attributes['head_section'].data[v.index].value=v.index%8+1
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED');bm.to_mesh(me);bm.free()
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.1,island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT');me.uv_layers.active.name='BossConstructionUV';o.select_set(False)
 me.calc_loop_triangles();assert len(me.loop_triangles)==108 and old_triangles==116
 for r in contacts:
  if r['support']=='SplitCrown3':r.update(support=HEAD,support_component=component,endpoint_seating='complete three-vertex neck end seated into the connected crystal head rear/base surface with matching Body weights')
 assert len([r for r in contacts if r['support']==HEAD])==6
 # Seat complete neck closing sections, sampling their interiors as well as
 # their corners; the other body and limb geometry is retained from r11.
 repairs=[]
 for r in contacts:
  if r['support']!=HEAD:continue
  neck=bpy.data.objects[r['tissue']];ids=[v.index for v in neck.data.vertices if neck.data.attributes['attachment_endpoint'].data[v.index].value==r['id']]
  for i in ids:
   p,w=embed(neck.data.vertices[i].co,o,.014);assign(neck,i,p,w)
  bvh=tree(o);tris=np.array([[list(o.data.vertices[i].co) for i in t.vertices] for t in o.data.loop_triangles])
  def enclosed():
   points=[neck.data.vertices[i].co for i in ids];a,b,c=points
   return all(contains(p,bvh,tris) for p in (*points,(a+b+c)/3,a*.5+b*.25+c*.25,a*.25+b*.5+c*.25,a*.25+b*.25+c*.5,(a+b)/2,(b+c)/2,(c+a)/2))
  if not enclosed():
   points=[neck.data.vertices[i].co.copy() for i in ids];anchor,weights=embed(sum(points,Vector())/3,o,.026)
   for factor in (.85,.65,.45,.25,.10):
    for i,p in zip(ids,points):assign(neck,i,anchor+(p-anchor)*factor,weights)
    if enclosed():repairs.append(dict(tissue=neck.name,contraction=factor));break
   else:raise ValueError('neck closing face cannot seat into connected crystal head')
 for name in old_names:bpy.data.objects.remove(bpy.data.objects[name],do_unlink=True);components.pop(name)
 components[HEAD]=component
 bpy.context.scene['silhouette_components']=json.dumps(components);bpy.context.scene['tissue_attachment_contract']=json.dumps(contacts)
 (OUT/'head-authoring.json').write_text(json.dumps(dict(interpretation='one continuous crystalline head shape; crack is a beveled surface recess, not an air gap between two upright halves',component=HEAD,removed=old_names,old_head_triangles=old_triangles,new_head_triangles=108,total_triangles=1792,stations=7,vertices_per_station=8,roles={'1':'outer crystal cortex, rear shell and end caps','2':'sloping crack bevel walls','3':'purple crystalline floor recessed inside fissure'},recess_depth_authored=depths,purple_floor_triangles=8,neck_supports=6,neck_repairs=repairs,preserved='r11 shoulders, torso rings, limbs, other crystals, rig/actions and existing surface style',acceptance='head interpretation correction; not final boss or 85% concept-match acceptance'),indent=2)+'\n',encoding='utf-8')
 return trunk
