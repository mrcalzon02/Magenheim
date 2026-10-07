"""Individually shaped fangs and rounded inset cheek sensory bulbs."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
from first_bloom_frill_sculpt import assign
from first_bloom_deeper_chest import embed
from first_bloom_attachment_repair import tree
import blackwater_maw_face_r12 as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-teeth-eyes-r13-raw'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;previous._build.__globals__['OUT']=OUT
 body=previous.build(B,arm,record,profile,tube,cap)
 records=json.loads(bpy.context.scene['tissue_attachment_contract']);report=[]
 def seal_light_pole(o,sides):
  # A single smooth pole avoids a tiny flat cap becoming a black atlas island.
  bm=bmesh.new();bm.from_mesh(o.data);bm.verts.ensure_lookup_table()
  pole=[bm.verts[i] for i in range(len(bm.verts)-sides,len(bm.verts))]
  center=sum((v.co for v in pole),Vector())/sides
  bmesh.ops.pointmerge(bm,verts=pole,merge_co=center)
  bmesh.ops.dissolve_degenerate(bm,dist=1e-9,edges=list(bm.edges))
  bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
  for p in o.data.polygons:p.use_smooth=True
 organ=bpy.data.objects['InnerThroatOrgan']
 # Keep the exposed bulb ahead of the throat folds instead of cutting its face.
 for v in organ.data.vertices:
  if v.index>=6:v.co.y+=.10
 seal_light_pole(organ,6)
 # Preserve the concept's vertical oval as the reduced head is refined.
 for mesh_object in list(bpy.context.scene.objects):
  if mesh_object.type!='MESH':continue
  for v in mesh_object.data.vertices:
   t=max(0,min(1,(v.co.y-1.70)/.45));t=t*t*(3-2*t);v.co.x*=1-.12*t
  mesh_object.data.update()
 def replace_mesh(o,temp,root,weights,item,component):
  o.data=temp.data;bpy.data.objects.remove(temp,do_unlink=True);o.vertex_groups.clear()
  bone='Jaw' if o.name.startswith('Lower') else 'Head'
  for v in o.data.vertices:assign(o,v.index,v.co,{bone:1})
  attr=o.data.attributes.new('attachment_component','INT','FACE')
  for a in attr.data:a.value=component
  markers=o.data.attributes.new('attachment_endpoint','INT','POINT')
  for i,(p,w) in enumerate(zip(root,weights)):assign(o,i,p,w);markers.data[i].value=item['id']
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED',ngon_method='EAR_CLIP');bm.to_mesh(o.data);bm.free();o.data.update()
 for o in list(bpy.context.scene.objects):
  if o.type!='MESH' or not o.name.startswith(('UpperFang','UpperInner','LowerFang','LowerInner')):continue
  root=[v.co.copy() for v in o.data.vertices[:6]];c=sum(root,Vector())/6;weights=[{o.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>1e-7} for v in o.data.vertices[:6]]
  component=o.data.attributes['attachment_component'].data[0].value;item=next(r for r in records if r['tissue']==o.name)
  oldtip=sum((v.co for v in o.data.vertices[18:24]),Vector())/6;upper=o.name.startswith('Upper');inner='Inner' in o.name;k=int(''.join(ch for ch in o.name if ch.isdigit()))
  length=abs(oldtip.z-c.z)*(1.40 if upper else 1.22)
  length=min(.40 if upper else .29,max(.12 if not inner else .07,length))
  rake=(-1 if upper else 1)*((.14 if upper else .07)+(.08 if upper else .07)*((k*3)%5)/4)
  dx=(1 if c.x>0 else -1)*(.018 if upper else -.014)
  direction=-1 if upper else 1
  points=[c,c+Vector((dx*.15,rake*.28,direction*length*.36)),c+Vector((dx*.60,rake*.70,direction*length*.77)),c+Vector((dx,rake,direction*length))]
  radius=sum((p-c).length for p in root)/6;radius=min(.070,max(.042 if not inner else .025,radius*1.50))
  temp=B.tube(o.name+'_new',points,[radius,radius*.74,radius*.30,.0015],'ivory','Head' if upper else 'Jaw',6)
  replace_mesh(o,temp,root,weights,item,component)
  for p in o.data.polygons:p.use_smooth=False
  report.append(dict(part=o.name,length_author_units=length,profile_rake='rearward upper fang' if upper else 'forward lower fang',root='original verified gum section retained'))
 head=bpy.data.objects['VaultedSkull']
 # Position the largest organs high at the rear cheek, with smaller neighbors.
 for side in (-1,1):
  for k,(y,z,radius) in enumerate(((2.05,.49,.064),(2.16,.32,.057),(1.96,.25,.044),(2.28,.44,.038))):
   o=bpy.data.objects['Sensory'+str(side)+'_'+str(k)];component=o.data.attributes['attachment_component'].data[0].value;item=next(r for r in records if r['tissue']==o.name)
   hit,normal,face,d=tree(head).ray_cast(Vector((side*3,y,z)),Vector((-side,0,0)),6);assert hit is not None,(side,k)
   center=hit+normal*.018
   temp=B.mass(o.name+'_bulb',center,(radius,radius,radius*.84),'light-organ','Head',8)
   # Orient the bulb's poles outward from its own socket.
   q=normal.to_track_quat('Z','Y')
   for v in temp.data.vertices:v.co=center+q@(v.co-center)
   seal_light_pole(temp,8)
   roots=[];weights=[]
   for v in temp.data.vertices[:8]:p,w=embed(v.co,head,.016);roots.append(p);weights.append(w)
   replace_mesh(o,temp,roots,weights,item,component)
   mat=B.mats['light-organ'].copy();mat.name=o.name+'_RoundedBulb';nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes['Principled BSDF']
   geo=nodes.new('ShaderNodeNewGeometry');dot=nodes.new('ShaderNodeVectorMath');dot.operation='DOT_PRODUCT';dot.inputs[1].default_value=normal;links.new(geo.outputs['Normal'],dot.inputs[0])
   ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.18;ramp.color_ramp.elements[0].color=(.003,.014,.016,1);ramp.color_ramp.elements[1].position=.95;ramp.color_ramp.elements[1].color=(.055,.30,.34,1);ramp.color_ramp.elements.new(.65).color=(.012,.12,.14,1);links.new(dot.outputs['Value'],ramp.inputs[0]);links.new(ramp.outputs[0],bs.inputs['Base Color']);links.new(ramp.outputs[0],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.55;bs.inputs['Roughness'].default_value=.22
   for link in list(bs.inputs['Normal'].links):links.remove(link)
   o.data.materials.clear();o.data.materials.append(mat);o.data.materials.append(B.mats['wet-hide'])
   for p in o.data.polygons:p.use_smooth=True;p.material_index=1 if max(p.vertices)<16 else 0
   item['endpoint_seating']='eight-vertex inset bulb root with support weights'
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'teeth-eyes-authoring.json').write_text(json.dumps(dict(teeth=report,eyes='eight rounded inset bulbs in asymmetric-size rear cheek clusters; dark socket rim and outward cyan highlight',scope='teeth and eyes only; preceding head, scaled neck, low brow, fin and separated feeler corrections retained',status='concept review required'),indent=2)+'\n')
 return body
