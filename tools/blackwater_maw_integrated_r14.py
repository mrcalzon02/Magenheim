"""Manually defined reference contours and continuous fin sections."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector,Quaternion
from first_bloom_frill_sculpt import assign
from first_bloom_deeper_chest import embed
import blackwater_maw_teeth_eyes_r13 as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-integrated-r14-raw'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 body=previous.build(B,arm,record,profile,tube,cap)
 records=json.loads(bpy.context.scene['tissue_attachment_contract'])
 # Sensory fronds should stop near the shoulder region, not span the torso.
 for o in list(bpy.context.scene.objects):
  if o.type!='MESH' or not o.name.startswith('CrownFeeler'):continue
  root=sum((v.co for v in o.data.vertices[:6]),Vector())/6
  for j in range(1,4):
   ring=list(o.data.vertices[j*6:(j+1)*6]);center=sum((v.co for v in ring),Vector())/6
   delta=root+(center-root)*.55-center
   for v in ring:v.co+=delta
  o.data.update()
 # Replace the oval-cobble surface across all copies of the owned skin material.
 image=bpy.data.images.load(str(ROOT/'assets/textures/underworld/bosses/blackwater-maw-lamellar-study/lamellar-albedo.png'),check_existing=True);image.colorspace_settings.name='sRGB';image.pack()
 for mat in list(bpy.data.materials):
  if not mat.use_nodes:continue
  skin=False
  for n in mat.node_tree.nodes:
   if n.type=='TEX_IMAGE' and n.image and 'blackwater-maw-scales-albedo' in n.image.name:n.image=image;skin=True
  if skin:
   bs=mat.node_tree.nodes.get('Principled BSDF')
   if bs and bs.inputs['Base Color'].links:
    old=bs.inputs['Base Color'].links[0].from_socket;mix=mat.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(.42,.50,.56,1);mat.node_tree.links.new(old,mix.inputs[1]);mat.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
 # Fin skin carries transverse root folds, thinning out into worn scaled blades.
 finmat=B.mats['wet-hide'].copy();finmat.name='Maw integrated fin tissue';nodes=finmat.node_tree.nodes;links=finmat.node_tree.links;bs=nodes['Principled BSDF']
 uv=nodes.new('ShaderNodeUVMap');uv.uv_map='BossConstructionUV';sep=nodes.new('ShaderNodeSeparateXYZ');links.new(uv.outputs[0],sep.inputs[0])
 mult=nodes.new('ShaderNodeMath');mult.operation='MULTIPLY';mult.inputs[1].default_value=math.tau*13;links.new(sep.outputs['X'],mult.inputs[0]);wave=nodes.new('ShaderNodeMath');wave.operation='SINE';links.new(mult.outputs[0],wave.inputs[0])
 bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.30;bump.inputs['Distance'].default_value=.025;links.new(wave.outputs[0],bump.inputs['Height'])
 if bs.inputs['Normal'].links:links.new(bs.inputs['Normal'].links[0].from_socket,bump.inputs['Normal'])
 links.new(bump.outputs[0],bs.inputs['Normal'])
 # Pale worn fragments are surface color and relief, not cube slabs.
 noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=14;noise.inputs['Detail'].default_value=2;links.new(uv.outputs[0],noise.inputs['Vector']);mask=nodes.new('ShaderNodeValToRGB');mask.color_ramp.elements[0].position=.64;mask.color_ramp.elements[0].color=(0,0,0,1);mask.color_ramp.elements[1].position=.75;mask.color_ramp.elements[1].color=(1,1,1,1);links.new(noise.outputs['Fac'],mask.inputs[0])
 mix=nodes.new('ShaderNodeMixRGB');links.new(mask.outputs[0],mix.inputs[0]);links.new(bs.inputs['Base Color'].links[0].from_socket,mix.inputs[1]);mix.inputs[2].default_value=(.19,.20,.155,1);links.new(mix.outputs[0],bs.inputs['Base Color'])
 # Remove the independent short connector. The new middle fin contains its root.
 records=[r for r in records if not r['tissue'].startswith('UpperFinJoint')]
 for side in ('L','R'):bpy.data.objects.remove(bpy.data.objects['UpperFinJoint'+side],do_unlink=True)
 def install(o,vertices,faces,root_ids,uvs,bone):
  component=o.data.attributes['attachment_component'].data[0].value;item=next(r for r in records if r['tissue']==o.name)
  me=bpy.data.meshes.new(o.name+'_ReferenceContours');me.from_pydata(vertices,[],faces);me.materials.append(finmat);o.data=me;o.vertex_groups.clear()
  for v in me.vertices:assign(o,v.index,v.co,{bone:1})
  attr=me.attributes.new('attachment_component','INT','FACE')
  for a in attr.data:a.value=component
  markers=me.attributes.new('attachment_endpoint','INT','POINT')
  for i in root_ids:
   p,w=embed(me.vertices[i].co,body,.090);assign(o,i,p,w);markers.data[i].value=item['id']
  item.update(support=body.name,support_component=body.data.attributes['attachment_component'].data[0].value,end='integrated_middle_fin_root' if o.name.startswith('Pleated') else 'direct_forward_fin_root',endpoint_seating='entire integrated root section embedded into torso with actual support weights')
  uv=me.uv_layers.new(name='BossConstructionUV')
  for p in me.polygons:
   p.use_smooth=True
   for li in p.loop_indices:uv.data[li].uv=uvs[me.loops[li].vertex_index]
  bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED',ngon_method='EAR_CLIP');bm.to_mesh(me);bm.free();me.update()
 for side,s in [('L',-1),('R',1)]:
  bone='Fin_'+side
  # One continuous muscular root -> flat long blade, with no connector seam.
  points=[(s*.63,.92,-.08),(s*1.00,.65,-.16),(s*1.31,.32,-.30),(s*1.62,-.06,-.47),(s*1.91,-.43,-.65),(s*2.20,-.82,-.82),(s*2.43,-1.13,-.99),(s*2.57,-1.38,-1.08)]
  widths=[.27,.28,.30,.31,.29,.23,.12,.0025];depths=[.23,.20,.135,.085,.050,.029,.012,.003]
  centers=[Vector(p) for p in points];root=centers[0].copy();centers=[root+(p-root)*.66 for p in centers];n=10;vertices=[];uvs=[];faces=[]
  for j,p in enumerate(centers):
   tangent=(centers[min(j+1,7)]-centers[max(0,j-1)]).normalized();wide=(Vector((0,0,1))-tangent*tangent.z).normalized();normal=tangent.cross(wide).normalized()
   for k in range(n):
    a=math.tau*k/n;notch=.05 if j in (3,5) and k==5 else 0
    wc,dc=(math.cos(a),math.sin(a)) if j<3 else ((1,0),(.95,.6),(.55,1),(-.55,1),(-.95,.6),(-1,0),(-.95,-.6),(-.55,-1),(.55,-1),(.95,-.6))[k]
    vertices.append(p+wide*((widths[j]-notch)*wc)+normal*(depths[j]*dc));uvs.append((j/7*1.7,k/n*1.3))
  for j in range(7):
   for k in range(n):a=j*n+k;b=j*n+(k+1)%n;faces.append((a,b,b+n,a+n))
  faces.extend((tuple(reversed(range(n))),tuple(7*n+k for k in range(n))))
  o=bpy.data.objects['PleatedPectoral'+side];install(o,vertices,faces,list(range(n)),uvs,bone)
  for p in o.data.polygons:
   if min(p.vertices)>=3*n:p.use_smooth=False
  for i in range(n,2*n):assign(o,i,o.data.vertices[i].co,{'Spine_1':.65,bone:.35})
  # Broad swept fore blade, hand-defined asymmetric, not an elliptical leaf.
  outline=[(s*.55,1.58,-.45),(s*.94,1.28,-.61),(s*1.34,.96,-.86),(s*1.65,.50,-1.10),(s*2.05,-.60,-1.48),(s*1.80,-.28,-1.48),(s*1.76,-.19,-1.40),(s*1.63,.0,-1.45),(s*1.59,.12,-1.34),(s*1.42,.32,-1.30),(s*1.32,.50,-1.20),(s*1.25,.60,-1.11),(s*.90,1.10,-.88),(s*.55,1.45,-.65)]
  n=len(outline);count=n*2+1;center=Vector((s*1.2,.61,-1.00));vertices=[];uvs=[];faces=[]
  origin=Vector(outline[0]);axis=(Vector(outline[4])-origin).normalized();oldnormal=(Vector(outline[2])-origin).cross(axis).normalized()
  wanted=Vector((s,0,.35));wanted=(wanted-axis*wanted.dot(axis)).normalized()
  angle=math.atan2(axis.dot(oldnormal.cross(wanted)),oldnormal.dot(wanted));rotation=Quaternion(axis,angle)
  outline=[origin+rotation@(Vector(p)-origin) for p in outline];center=origin+rotation@(center-origin)
  def condense(p):
   delta=p-origin;along=axis*delta.dot(axis);return origin+along*.74+(delta-along)*1.35
  outline=[condense(p) for p in outline];center=condense(center)
  for layer in range(2):
   offset=wanted*(.025 if layer==0 else -.025)
   for radius in (1,.50):
    for p in outline:
     q=center.lerp(Vector(p),radius)+offset;vertices.append(q);uvs.append(((1.58-q.y)/2.18,(abs(q.x)-.55)/1.50))
   vertices.append(center+offset);uvs.append(((1.58-center.y)/2.18,(abs(center.x)-.55)/1.50))
  for layer in range(2):
   base=layer*count
   for k in range(n):b=(k+1)%n;quad=(base+k,base+b,base+n+b,base+n+k);tri=(base+2*n,base+n+k,base+n+b);faces.extend((quad if layer==0 else tuple(reversed(quad)),tri if layer==0 else tuple(reversed(tri))))
  for k in range(n):b=(k+1)%n;faces.append((k,count+k,count+b,b))
  install(bpy.data.objects['ForwardPectoral'+side],vertices,faces,[0,n-1,count,count+n-1],uvs,bone)
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'reference-rebuild.json').write_text(json.dumps(dict(middle_fin='single surface from thick tapered embedded root into long flattened swept blade; independent joint objects removed',forward_fin='manually defined broad asymmetric contour and torn trailing boundary; direct torso attachment, no shoulder',skin='new owned angular lamellar albedo, darker palette and transverse fin root folds; worn fragments are baked surface detail',preserved='head, tooth beds, rounded eyes, torso rig, four dorsal fins, broad tail and separate feelers',status='reference-driven art candidate; visual match still requires review'),indent=2)+'\n')
 return body
