"""Primary concept anatomy: layered strata around an open binding cage."""
import bpy,bmesh,math,json
from rift_titan_primary_layout import SHOULDER_STRATA, limb_strata
from rift_titan_rock_facets import faceted_plate_geometry
from rift_titan_primary_masses import primary_mass_strata
from rift_titan_head_layout import head_parts
from rift_titan_skull_geometry import eye_socket_geometry
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-12/rift-titan-primary-r3'

def build(B,arm,record,profile,tube,cap):
 stone=B.material('rift-strata');crystal=B.material('binding-prism')
 dark=stone.copy();dark.name='Rift dark structural bands';B.mats['rift-bands']=dark
 bs=dark.node_tree.nodes['Principled BSDF'];old=bs.inputs['Base Color'].links[0].from_socket;mix=dark.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(.15,.12,.10,1);dark.node_tree.links.new(old,mix.inputs[1]);dark.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
 def solid(name,points,faces,key,bone,category):
  me=bpy.data.meshes.new(name);me.from_pydata(points,[],faces);me.materials.append(B.mats[key]);o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);record(o,bone,category)
  bpy.context.view_layer.objects.active=o;o.select_set(True);bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED',ngon_method='EAR_CLIP');bm.to_mesh(me);bm.free()
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT');me.uv_layers.active.name='BossConstructionUV';o.select_set(False);return o
 def plate(name,c,width,height,depth,bone='Body',tilt=0,seed=0,key='rift-strata'):
  # Sculpt a shallow ridge into the existing six-sided silhouette. The authored
  # front/back perimeter and object identity stay unchanged for rig continuity.
  vertices,faces=faceted_plate_geometry(c,width,height,depth,tilt,seed)
  return solid(name,vertices,faces,key,bone,'strata')
 def prism(name,c,radius,length,bone='Body'):
  c=Vector(c);n=5;points=[c+Vector((0,0,length*.52)),c-Vector((0,0,length*.48))]+[c+Vector((radius*math.cos(k*math.tau/n),radius*.8*math.sin(k*math.tau/n),-.05*length)) for k in range(n)]
  faces=[(0,k+2,(k+1)%n+2) for k in range(n)]+[(1,(k+1)%n+2,k+2) for k in range(n)]
  return solid(name,points,faces,'binding-prism',bone,'binding-crystals')
 # Broad layered shoulders framing the open neck and chest; no solid barrel.
 first=None
 for side,s in [('L',-1),('R',1)]:
  for j,(x,z,w,h,d,angle) in enumerate(SHOULDER_STRATA):
   o=plate('ShoulderStratum'+side+str(j),(s*x,-.015+j*.025,z),w,h,d,tilt=s*angle,seed=j+5)
   if first is None:first=o
  # Three elongated plates per upper arm and leg, rather than identical stacks.
  for bone in ('Arm'+side,'Forearm'+side,'Leg'+side):
   b=arm.data.bones[bone];a=b.head_local;end=b.tail_local;direction=end-a
   for j,(p,w,h,depth,angle) in enumerate(limb_strata(a,end,s,leg=bone.startswith('Leg'))):
    plate('LongFracture'+bone+str(j),p,w,h,depth,bone,angle,j*3+len(bone))
   tube('JointBinding'+bone,[a,a.lerp(end,.5),end],[.033,.026,.019],'rift-bands',bone,4,'bindings')
  hand=arm.data.bones['Forearm'+side].tail_local
  plate('Palm'+side,hand+Vector((s*.02,.02,-.12)),.27,.38,.22,'Forearm'+side,s*.04,13)
  for j in range(3):plate('FingerStratum'+side+str(j),hand+Vector(((j-1)*.17,.08,-.42)),.078,.44,.12,'Forearm'+side,s*.08,j+18)
  foot=arm.data.bones['Leg'+side].tail_local
  plate('Foot'+side,(foot.x,.12,.16),.37,.23,.37,'Leg'+side,s*.03,7)
 # Load-bearing thorax and lower-body stone masses, matched to the adopted
 # concept's integrated shoulders and side-supported exposed crystal cage.
 for name,center,width,height,depth,tilt,seed in primary_mass_strata():
  plate(name,center,width,height,depth,'Body',tilt,seed)
 # Pelvis hangs below the open core, keeping the torso's negative space.
 for j,x in enumerate((-.30,0,.30)):plate('PelvisKeystone'+str(j),(x,-.02,1.24+.05*(j%2)),.20,.37,.22,seed=j+4)
 # Asymmetric cleft menhirs, not a row of pipe-like horns.
 for j,(x,y,z,w,h,angle) in enumerate(((-.23,-.07,3.40,.19,.77,-.09),(.04,-.04,3.56,.13,.79,.03),(.28,-.11,3.35,.13,.64,.14),(-.03,.17,3.26,.18,.49,-.10))):plate('SplitCrown'+str(j),(x,y,z),w,h,.17,tilt=angle,seed=11+j)
 # Two bowed horizontal cage bands, with four oblique structural straps.
 center=Vector((0,.14,2.25))
 for j,z in enumerate((2.04,2.47)):
  points=[(math.cos(k*math.tau/12)*.52,.14+math.sin(k*math.tau/12)*.29,z+.035*math.sin(k*math.tau/6)) for k in range(13)]
  tube('CoreHoop'+str(j),points,[.035]*13,'rift-bands','Body',4,'cage')
 for s in (-1,1):
  for rear in (-1,1):tube('DiagonalCage'+str(s)+'_'+str(rear),[(s*.40,.14+rear*.22,1.93),(s*.49,.14+rear*.28,2.28),(s*.29,.14+rear*.16,2.63)],[.032,.041,.027],'rift-bands','Body',4,'cage')
 for j,(x,y,z,r,l) in enumerate(((0,.14,2.25,.17,.68),(-.19,.10,2.27,.09,.46),(.19,.19,2.34,.08,.51),(0,.07,1.98,.07,.30))):prism('SuspendedCore'+str(j),(x,y,z),r,l)
 # Replace anonymous debris with the boss concept's stone skull and eyes.
 head_stones,head_eyes,remaining_motes=head_parts()
 for name,center,width,height,depth,tilt,seed,material in head_stones:
  if name.startswith('EyeWell'):
   vertices,faces=eye_socket_geometry(center,width,height,depth,tilt)
   solid(name,vertices,faces,material,'Body','head-eye-socket')
  else:
   plate(name,center,width,height,depth,'Body',tilt,seed,material)
 for name,center,radius,length in (*head_eyes,*remaining_motes):
  prism(name,center,radius,length)
 for s in (-1,1):tube('ShoulderBinding'+str(s),[(s*.14,.02,2.78),(s*.47,-.02,2.81),(s*.88,0,2.69)],[.034,.040,.025],'rift-bands','Body',4,'bindings')
 bpy.context.scene['concept_target']='docs/art/creature-concepts/2026-10-03/the-rift-titan.png'
 (OUT/'concept-pass.json').write_text(json.dumps(dict(scope='primary silhouette and open cage; not final surface detail',anatomy='integrated thorax and stone brow, jaw, cheeks and physically recessed purple eye sockets beneath cleft crown, elongated limb strata and exposed core',donor='StoneGolem',donor_triangles=1200,soft_cap=1800,preserved='existing Rift Colossus rig hierarchy and seven named actions',remaining='user silhouette review, individualized fracture/lichen textures, refinement of plate overlaps and cage clearance, native loader/animation/performance acceptance',status='in progress; not adequately reworked'),indent=2)+'\n')
 return first
