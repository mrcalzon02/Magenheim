"""Concept-driven overlapping strata, forward crest and compact weathered surfaces."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector,Matrix
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from rift_titan_crown_profile import CROWN_PARTS,crown_point,mantle_spec
import rift_titan_crystal_limbs as previous
from rift_titan_joint_silhouette import frame,OUTLINES
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-layered-r10'

def surface_material(mat,kind):
 nodes=mat.node_tree.nodes;links=mat.node_tree.links;nodes.clear()
 bs=nodes.new('ShaderNodeBsdfPrincipled');out=nodes.new('ShaderNodeOutputMaterial');links.new(bs.outputs['BSDF'],out.inputs['Surface'])
 geo=nodes.new('ShaderNodeNewGeometry')
 def noise(scale,detail=3):
  n=nodes.new('ShaderNodeTexNoise');n.inputs['Scale'].default_value=scale;n.inputs['Detail'].default_value=detail;n.inputs['Roughness'].default_value=.72;links.new(geo.outputs['Position'],n.inputs['Vector']);return n.outputs['Fac']
 def ramp(socket,stops):
  n=nodes.new('ShaderNodeValToRGB');r=n.color_ramp
  for e in list(r.elements)[2:]:r.elements.remove(e)
  for i,(p,c) in enumerate(stops):
   e=r.elements[i] if i<2 else r.elements.new(p);e.position=p;e.color=(*c,1)
  links.new(socket,n.inputs['Fac']);return n.outputs['Color']
 def mix(f,a,b):
  n=nodes.new('ShaderNodeMixRGB');links.new(f,n.inputs[0]);links.new(a,n.inputs[1]);links.new(b,n.inputs[2]);return n.outputs[0]
 fine=noise(24,4)
 if kind=='stone':
  macro=ramp(noise(5.5),[(.12,(.012,.018,.026)),(.36,(.034,.045,.055)),(.57,(.080,.096,.102)),(.75,(.18,.16,.12)),(.91,(.25,.22,.16))])
  facets=nodes.new('ShaderNodeTexVoronoi');facets.inputs['Scale'].default_value=18;links.new(geo.outputs['Position'],facets.inputs['Vector'])
  bw=nodes.new('ShaderNodeRGBToBW');links.new(facets.outputs['Color'],bw.inputs['Color'])
  patches=ramp(bw.outputs[0],[(.12,(.012,.023,.029)),(.37,(.045,.062,.073)),(.57,(.115,.136,.143)),(.78,(.24,.23,.195)),(.95,(.30,.29,.22))])
  n=nodes.new('ShaderNodeMixRGB');n.inputs[0].default_value=.90;links.new(macro,n.inputs[1]);links.new(patches,n.inputs[2]);color=n.outputs[0]
  lichen=ramp(noise(8.5),[(.55,(0,0,0)),(.66,(.68,.68,.68)),(.80,(.86,.86,.86))])
  moss=ramp(fine,[(.2,(.038,.045,.018)),(.55,(.115,.12,.038)),(.84,(.23,.205,.085))])
  color=mix(lichen,color,moss)
  scale=nodes.new('ShaderNodeVectorMath');scale.operation='MULTIPLY';scale.inputs[1].default_value=(.8,1.1,2.1);links.new(geo.outputs['Position'],scale.inputs[0])
  cells=nodes.new('ShaderNodeTexVoronoi');cells.feature='DISTANCE_TO_EDGE';cells.inputs['Scale'].default_value=7.5;links.new(scale.outputs['Vector'],cells.inputs['Vector'])
  crack=ramp(cells.outputs['Distance'],[(.013,(.18,.18,.18)),(.040,(1,1,1))])
  multiply=nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=.72;links.new(color,multiply.inputs[1]);links.new(crack,multiply.inputs[2]);color=multiply.outputs[0]
  height=nodes.new('ShaderNodeMath');height.operation='MULTIPLY_ADD';links.new(crack,height.inputs[0]);height.inputs[1].default_value=.68;links.new(fine,height.inputs[2])
  bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.44;bump.inputs['Distance'].default_value=.023;links.new(height.outputs[0],bump.inputs['Height']);links.new(bump.outputs['Normal'],bs.inputs['Normal'])
  rough=ramp(fine,[(.15,(.67,.67,.67)),(.80,(.94,.94,.94))]);links.new(rough,bs.inputs['Roughness'])
 elif kind=='metal':
  color=ramp(noise(12),[(.18,(.014,.018,.024)),(.5,(.036,.042,.047)),(.7,(.13,.078,.045)),(.86,(.20,.14,.086))])
  bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.28;bump.inputs['Distance'].default_value=.009;links.new(fine,bump.inputs['Height']);links.new(bump.outputs['Normal'],bs.inputs['Normal'])
  rough=ramp(fine,[(.2,(.55,.55,.55)),(.8,(.87,.87,.87))]);links.new(rough,bs.inputs['Roughness']);bs.inputs['Metallic'].default_value=.25
 else:
  color=ramp(noise(8,1),[(.15,(.010,.002,.037)),(.48,(.060,.009,.18)),(.8,(.22,.055,.43))])
  glow=ramp(noise(12,1),[(.25,(.015,.003,.065)),(.65,(.17,.03,.35)),(.9,(.28,.07,.55))]);links.new(glow,bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.65;bs.inputs['Roughness'].default_value=.29
 if kind=='stone':
  tone=nodes.new('ShaderNodeMixRGB');tone.blend_type='MULTIPLY';tone.inputs[0].default_value=1;tone.inputs[2].default_value=(.50,.47,.43,1);links.new(color,tone.inputs[1]);color=tone.outputs[0]
 links.new(color,bs.inputs['Base Color']);mat['magenheim_authored_surface']='r10 owned spatial stone fractures/ochre weathering/olive lichen, corroded metal or violet crystal; baked to four existing 512px maps'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;trunk=previous.build(B,arm,record,profile,tube,cap)
 contacts=json.loads(bpy.context.scene['tissue_attachment_contract']);components=json.loads(bpy.context.scene['silhouette_components'])
 def normals(o):
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
 # Apply the independently authored online crest profile to the current r9
 # limb study, retaining the lower head and its six neck attachment hosts.
 for name in CROWN_PARTS:
  o=bpy.data.objects[name]
  for v in o.data.vertices:v.co=crown_point(name,v.co)
  normals(o)
 # The concept core is an irregular intersecting cluster, rather than four
 # equally upright purple stakes. Re-angle the retained crystal allocations.
 for j in range(4):
  o=bpy.data.objects['SuspendedCore'+str(j)];c=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
  rotate=Matrix.Rotation((.13,-.22,.28,-.33)[j],3,'Y')@Matrix.Rotation((.08,.20,-.10,.24)[j],3,'X')
  for v in o.data.vertices:v.co=c+rotate@(v.co-c)+Vector(((-.015,.025,-.03,.02)[j],(.01,-.015,.025,.015)[j],(0,.02,-.015,0)[j]))
  normals(o)
 transformations={};layered=[]
 for side,s in [('L',-1),('R',1)]:
  for bone in ('Arm'+side,'Forearm'+side,'Leg'+side):
   basis=frame(arm.data.bones[bone].head_local-arm.data.bones[bone].tail_local)
   for j in range(3):
    if bone.startswith('Arm') and j==0:continue
    o=bpy.data.objects['LongFracture'+bone+str(j)];c=sum((v.co for v in o.data.vertices),Vector())/14
    length=1.14 if bone.startswith('Leg') else 1.23
    shift=basis@Vector((s*(.025 if j%2 else -.018),-.035 if j==1 else .018,0))
    def transform(p,c=c,basis=basis,length=length,shift=shift,s=s,j=j):
     q=basis.transposed()@(p-c);q.x=q.x*.92+s*q.z*(.09 if j%2 else -.06);q.z*=length
     return c+basis@q+shift
    for v in o.data.vertices:v.co=transform(v.co)
    transformations[o.name]=transform;layered.append(o.name);normals(o)
 for r in contacts:
  if r['end']=='limb-crystal-root' and r['support'] in transformations:
   o=bpy.data.objects[r['tissue']]
   for v in o.data.vertices:v.co=transformations[r['support']](v.co)
   normals(o)
 # Broader diagonal upper-thorax stones seat into the shoulder mass, while the
 # collar and open cage retain their existing size/topology and support chain.
 for name,s,side in [('FloatingStratum0',-1,'L'),('FloatingStratum1',1,'R')]:
  o=bpy.data.objects[name];spec=mantle_spec(s);c=Vector(spec['center']);basis=frame(spec['axis']);w,h,d,seed=(spec[k] for k in ('width','height','depth','seed'));pts=[]
  for face_side in (-1,1):
   for k,(u,z) in enumerate(OUTLINES[spec['variant']]):
    taper=.42+.58*max(0,1-abs(z)/.55)
    pts.append(c+basis@Vector((u*w*(1+.045*math.sin(seed+k*2.1))*(1 if face_side>0 else .88),face_side*d*taper*(1+.12*math.cos(k*1.9+seed)),z*h+face_side*.025*h*math.sin(k*1.5+seed))))
  for face_side in (-1,1):pts.append(c+basis@Vector((w*.025*math.sin(seed+1.3),face_side*d*1.04,h*.04*math.cos(seed+.7))))
  for v,p in zip(o.data.vertices,pts):v.co=p
  normals(o)
  contacts.append(dict(id=len(contacts)+1,tissue=name,end='thorax-shoulder-root',support='ShoulderStratum'+side+'0',support_component=components['ShoulderStratum'+side+'0'],endpoint_seating='six-vertex outer thorax section embedded in the Body-weighted shoulder mass'))
  indices=(0,1,5,6,7,11) if side=='L' else (3,4,5,9,10,11)
  for i in indices:o.data.attributes['attachment_endpoint'].data[i].value=contacts[-1]['id']
 for r in contacts:
  o=bpy.data.objects[r['tissue']];host=bpy.data.objects[r['support']]
  for v in o.data.vertices:
   if o.data.attributes['attachment_endpoint'].data[v.index].value==r['id']:
    p,w=embed(v.co,host,.025 if r['end']=='limb-crystal-root' else .009);assign(o,v.index,p,w)
 bpy.context.scene['tissue_attachment_contract']=json.dumps(contacts)
 for key,kind in [('rift-strata','stone'),('rift-bands','metal'),('rift-metal-ribs','metal'),('binding-prism','crystal')]:surface_material(B.mats[key],kind)
 # Refresh construction UVs on the shaped parts before atlas baking. Spatial
 # authoring makes weathering coherent across the final merged mesh.
 for o in [o for o in bpy.context.scene.objects if o.type=='MESH']:
  normals(o)
 (OUT/'layer-authoring.json').write_text(json.dumps(dict(changes=['forward-raking crest using preserved online crown profile','longer oblique overlapping limb strata; existing root-blended upper-arm plates retained','broader diagonal upper-thorax stones with two complete shoulder seatings','retained core crystals re-angled into an irregular cluster','owned weathered blue-gray/ochre stone, olive lichen, shallow fracture normals, dark rusted ribs and varied violet crystals'],layered_plates=layered,new_thorax_contacts=2,contacts=76,cost='1800 unchanged triangles; four packed 512px maps; one mesh/material; original eight bones and seven actions',remaining='fractured head/shoulder contours, limb edge chips, fuller irregular crystal core and floating crest debris, fine texture direction and native acceptance',acceptance='continued unaccepted candidate; no 85% match claim'),indent=2)+'\n',encoding='utf-8')
 return trunk
