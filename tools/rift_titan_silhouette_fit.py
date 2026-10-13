"""Front-first primary masses, then left-facing profile depth; no detail pass."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
from rift_titan_rock_facets import faceted_plate_geometry
from rift_titan_primary_layout import limb_strata
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
import rift_titan_wrapped_torso as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-silhouette-r7'

def fit_height(z):
 stations=((0,0),(1.177826,1.50),(1.65,1.90),(2.20,2.38),(2.76,3.02),(3.10,3.27),(3.95,4.45))
 for (a,b),(c,d) in zip(stations,stations[1:]):
  if z<=c:return b+(z-a)*(d-b)/(c-a)
 return 4.45+(z-3.95)*1.388235

def lean(z):
 stations=((0,.025),(1.4,.025),(1.9,-.075),(2.76,.10),(3.10,.20),(4,.12))
 for (a,b),(c,d) in zip(stations,stations[1:]):
  if z<=c:return b+(z-a)*(d-b)/(c-a)
 return .12

def fit(p):return Vector((p.x*.95,p.y+lean(p.z),fit_height(p.z)))

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;trunk=previous.build(B,arm,record,profile,tube,cap)
 def replace(o,vertices):
  assert len(vertices)==len(o.data.vertices),(o.name,len(vertices),len(o.data.vertices))
  for v,p in zip(o.data.vertices,vertices):v.co=p
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
 def rock(name,c,w,h,d,tilt=0,seed=0):
  vertices,_=faceted_plate_geometry(c,w,h,d,tilt,seed)
  # Fit the side contour as well as the front. Keep the X/Z outline, taper
  # front/back rock depth toward the ends, and stagger its fracture shoulders.
  center=Vector(c);axis=Vector((math.sin(tilt),0,math.cos(tilt)))
  shaped=[]
  for i,point in enumerate(vertices):
   p=Vector(point);q=p-center;t=min(1,abs(q.dot(axis))/(h*.52))
   taper=1-.48*t*t
   p.y=center.y+q.y*taper+.016*math.sin(seed+i*1.71)*(1-t)
   shaped.append(p)
  replace(bpy.data.objects[name],shaped)
 for side,s in [('L',-1),('R',1)]:
  rock('ShoulderStratum'+side+'0',(s*.68,-.035,2.86),.48,.67,.30,s*.18,5)
  rock('ShoulderStratum'+side+'1',(s*1.12,.015,2.77),.39,.63,.27,s*.24,6)
  rock('ShoulderStratum'+side+'2',(s*.86,-.30,3.00),.43,.30,.28,-s*.18,7)
  for bone in ('Arm'+side,'Forearm'+side,'Leg'+side):
   b=arm.data.bones[bone]
   for j,(p,w,h,d,angle) in enumerate(limb_strata(b.head_local,b.tail_local,s,leg=bone.startswith('Leg'))):
    p=list(p)
    if bone.startswith('Leg'):p[1]+=(.11,.20,.14)[j];w=(.24,.25,.23)[j];d=.24
    elif bone.startswith('Forearm'):p[1]+=(.045,.075,.12)[j];w=(.24,.28,.25)[j];d=.27
    else:p[1]+=(.015,-.03,.04)[j];w=(.24,.25,.22)[j];d=.25
    rock('LongFracture'+bone+str(j),p,w,h,d,angle,j*3+len(bone))
  hand=arm.data.bones['Forearm'+side].tail_local
  rock('Palm'+side,hand+Vector((0,.12,-.12)),.28,.44,.26,s*.08,13)
  for j in range(3):rock('FingerStratum'+side+str(j),hand+Vector(((j-1)*.16,.18,-.42+.025*math.sin(j+1))),.105,.43,.19,s*.10,j+18)
  foot=arm.data.bones['Leg'+side].tail_local
  rock('Foot'+side,(foot.x,.24,.17),.33,.34,.38,s*.03,7)
 # Reuse the four anonymous floating-rock allocations as paired thorax and
 # lower side masses. No new geometry or imported head interpretation.
 for side,s,j in [('L',-1,0),('R',1,1)]:
  rock('FloatingStratum'+str(j),(s*.48,.10,2.71),.31,.58,.23,-s*.24,29+j)
  rock('FloatingStratum'+str(j+2),(s*.40,.025,1.65),.21,.40,.20,s*.18,37+j)
 # One cleft, tapered menhir head instead of three separate parallel pillars.
 left=[(-.29,3.34),(-.232,3.69),(-.024,3.92),(-.006,3.68),(-.0267,3.21),(-.116,3.055)]
 def crown(name,outline,offset=0):
  vertices=[]
  for side in (-1,1):
   for x,z in outline:
    y=.43-.16*(z-3.10)/.82;depth=.055+.050*max(0,1-abs(z-3.45)/.50)
    vertices.append((x,y+side*depth+offset,z))
  for side in (-1,1):vertices.append((sum(x for x,z in outline)/6,.40+side*.12+offset,sum(z for x,z in outline)/6))
  replace(bpy.data.objects[name],vertices)
 crown('SplitCrown0',left)
 crown('SplitCrown1',[(-x,z-.075) for x,z in reversed(left)],-.01)
 rock('SplitCrown2',(0,.17,3.42),.13,.77,.07,.04,13)
 rock('SplitCrown3',(0,.41,3.10),.17,.25,.11,.03,14)
 for j,(x,z) in enumerate(((-.027,3.52),(.021,3.27))):
  o=bpy.data.objects['FloatingPrism'+str(j)];r=.031;length=.32 if j==0 else .20;c=Vector((x,.40,z))
  vertices=[c+Vector((0,0,length*.52)),c-Vector((0,0,length*.48))]+[c+Vector((r*math.cos(k*math.tau/5),r*.8*math.sin(k*math.tau/5),-.05*length)) for k in range(5)]
  replace(o,vertices)
 for o in bpy.context.scene.objects:
  if o.type=='MESH':
   for v in o.data.vertices:
    if o.name.startswith(('ContinuousTorsoRib','WrappedStoneBand','StoneCollarbone','LowerRibKeel','SuspendedCore')):v.co.x*=1.10
    v.co=fit(v.co)
 # Fit the original eight-bone rest layout with the same map. Hierarchy,
 # identities and all seven action channels remain; no additional rig controls.
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
 for bone in arm.data.edit_bones:bone.head=fit(bone.head);bone.tail=fit(bone.tail)
 bpy.ops.object.mode_set(mode='OBJECT');arm.select_set(False)
 # Rest-space reshaping affects host surfaces. Re-seat the existing support
 # chain in its authored dependency order, starting with the collar supports.
 contacts=json.loads(bpy.context.scene['tissue_attachment_contract'])
 for r in contacts:
  o=bpy.data.objects[r['tissue']];host=bpy.data.objects[r['support']]
  ids=[v.index for v in o.data.vertices if o.data.attributes['attachment_endpoint'].data[v.index].value==r['id']]
  for i in ids:
   p,w=embed(o.data.vertices[i].co,host,.009);assign(o,i,p,w)
 bpy.context.scene['silhouette_components']=json.dumps({o.name:o.data.attributes['attachment_component'].data[0].value for o in bpy.context.scene.objects if o.type=='MESH'})
 (OUT/'silhouette-authoring.json').write_text(json.dumps(dict(reference_front='supplied concept-front.png and full sheet frontal figure; one long cleft head, broader mantle and limb stone masses, narrower overall arm spread and fitted lower-body proportion',reference_side='supplied left-facing concept-side.png; tapered depth of shoulder/limb strata, forward head/cage and projecting knees/feet',front_first=True,screen_side_direction='front/head left, back right',horizontal_fit=.95,height_stations=[[0,0],[1.177826,1.50],[1.65,1.90],[2.20,2.38],[2.76,3.02],[3.10,3.27],[3.95,4.45]],rig='eight original bone identities/hierarchy and seven retained actions; rest positions fitted to visible silhouette, native deformation pending',cost='existing vertex and triangle allocations reshaped; no surface detail work',acceptance='silhouette candidate; do not claim exact concept or boss acceptance'),indent=2)+'\n',encoding='utf-8')
 return trunk
