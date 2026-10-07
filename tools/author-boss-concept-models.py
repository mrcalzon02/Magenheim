"""First concept-guided construction pass: six distinct boss bodies.

Preserve the admitted species rig/actions as a retargeting baseline, rebuild the
visible anatomy, and keep these source studies separate from accepted gameplay.
Uses closed profiled surfaces, explicit UV seams, and merged bone/material parts.
"""
import bpy, bmesh, math, json, sys, hashlib, importlib.util
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(Path(__file__).resolve().parent))
from creature_apex_authoring import surface
spec=importlib.util.spec_from_file_location('boss_texture_profiles',Path(__file__).with_name('generate-boss-concept-textures.py'))
profiles=importlib.util.module_from_spec(spec);spec.loader.exec_module(profiles)
PROFILES=profiles.PROFILES
TEX=ROOT/'assets/textures/underworld/bosses/concept-pass-1'
SPECS={
 'first-bloom':('crowncap-brute',4.65),
 'blackwater-maw':('deep-hunter',10.0),
 'furnace-heart':('furnace-golem',5.2),
 'white-silence':('cryolith-guardian',5.5),
 'rift-titan':('rift-colossus',7.0),
 'carrion-crown':('corpse-orchard',6.8),
}
parts={};mats={};arm=None
DETAIL='--detail-r2' in sys.argv
if DETAIL:TEX=ROOT/'assets/textures/underworld/bosses/concept-detail-r2'

def material(key):
 if key in mats:return mats[key]
 m=bpy.data.materials.new('Boss_'+key);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF')
 bs.inputs['Base Color'].default_value=(1,1,1,1)
 bs.inputs['Roughness'].default_value=PROFILES[key][1]
 bs.inputs['Emission Color'].default_value=(0,0,0,1)
 bs.inputs['Emission Strength'].default_value=.65 if PROFILES[key][3] else 0
 uv=m.node_tree.nodes.new('ShaderNodeTexCoord')
 for kind,socket in [('albedo','Base Color'),('roughness','Roughness'),('normal',None),('emission','Emission Color')]:
  path=TEX/(key+'-'+kind+'.png')
  if not path.exists():continue
  im=bpy.data.images.load(str(path),check_existing=True);im.pack()
  im.colorspace_settings.name='sRGB' if kind in ('albedo','emission') else 'Non-Color'
  tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=im
  m.node_tree.links.new(uv.outputs['UV'],tex.inputs['Vector'])
  if kind=='normal':
   nm=m.node_tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.42
   m.node_tree.links.new(tex.outputs['Color'],nm.inputs['Color']);m.node_tree.links.new(nm.outputs['Normal'],bs.inputs['Normal'])
  else:m.node_tree.links.new(tex.outputs['Color'],bs.inputs[socket])
 m['magenheim_material_source_key']=key
 m['magenheim_material_source_root']=str(TEX.relative_to(ROOT)).replace('\\','/')
 mats[key]=m;return m

def prof(name,rings,key,bone,sides=24,axis='Z',irregular=.04):
 o=surface(name,rings,material(key),parts,bone,sides,axis,'BossConstructionUV',irregular)
 if key not in ('basalt','rift-strata','ancient-ice','binding-prism'):
  for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
 return o

def tube(name,points,radii,key,bone,sides=12):
 # True bent centerline; transported circular sections instead of straight rods.
 pts=[Vector(p) for p in points];verts=[];faces=[]
 for j,(p,r) in enumerate(zip(pts,radii)):
  tangent=(pts[min(j+1,len(pts)-1)]-pts[max(j-1,0)]).normalized()
  q=tangent.to_track_quat('Z','Y')
  for k in range(sides):
   a=math.tau*k/sides
   verts.append(p+q@Vector((r*math.cos(a),r*.83*math.sin(a),0)))
 for j in range(len(pts)-1):
  for k in range(sides):
   a=j*sides+k;b=j*sides+(k+1)%sides;faces.append((a,b,b+sides,a+sides))
 faces.extend([tuple(reversed(range(sides))),tuple((len(pts)-1)*sides+k for k in range(sides))])
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
 o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);me.materials.append(material(key))
 uv=me.uv_layers.new(name='BossConstructionUV')
 for p in me.polygons:
  p.use_smooth=len(p.vertices)==4
  seam=0 in [i%sides for i in p.vertices] and sides-1 in [i%sides for i in p.vertices]
  for li in p.loop_indices:
   j,k=divmod(me.loops[li].vertex_index,sides);uv.data[li].uv=(1 if seam and k==0 else k/sides,j/(len(pts)-1))
 parts[name]=bone;return o

def mass(name,loc,scale,key,bone,sides=16):
 rings=[(-1,.08),(-.8,.61),(-.48,.90),(0,1),(.42,.89),(.75,.60),(1,.07)]
 return prof(name,[(loc[2]+z*scale[2],r*scale[0],r*scale[1],loc[0],loc[1]) for z,r in rings],key,bone,sides)

def rotate_at(o,center,rotation):
 o.matrix_world=Matrix.Translation(Vector(center))@rotation@Matrix.Translation(-Vector(center))

def slab(name,loc,scale,key,bone,rot=(0,0,0),seed=0):
 # Fractured solid with purpose-sized chamfers and asymmetric corners.
 bpy.ops.mesh.primitive_cube_add(size=2,location=loc)
 o=bpy.context.object;o.name=name;o.scale=scale;o.rotation_euler=rot
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for i,v in enumerate(o.data.vertices):
  v.co.z+=(math.sin(i*2.71+seed)*.10*scale[2])
  v.co.x+=(math.sin(i*1.39+seed)*.06*scale[0])
 bevel=o.modifiers.new('BrokenRockEdges','BEVEL');bevel.width=min(scale)*.20;bevel.segments=1
 bpy.ops.object.modifier_apply(modifier=bevel.name)
 o.data.materials.append(material(key));parts[name]=bone
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
 o.data.uv_layers.active.name='BossConstructionUV'
 return o

def shelf(name,loc,rx,ry,key,bone,tilt=0):
 o=prof(name,[(-.11,rx*.89,ry*.92,0,0),(-.04,rx,ry,0,0),(.05,rx*.90,ry*.86,0,0),(.17,rx*.54,ry*.50,-.07,0),(.24,.025,.02,-.10,0)],key,bone,40,irregular=.075)
 o.matrix_world=Matrix.Translation(Vector(loc))@Matrix.Rotation(tilt,4,'Y');return o

def membrane(name,outline,key,bone,axis='Z'):
 verts=[tuple(p) for p in outline];offset=Vector((0,0,.025) if axis=='Z' else (.025,0,0))
 verts+= [tuple(Vector(p)+offset) for p in outline];n=len(outline)
 faces=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]
 faces +=[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();me.materials.append(material(key))
 o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);parts[name]=bone
 uv=me.uv_layers.new(name='BossConstructionUV')
 axes=(0,1) if axis=='Z' else (1,2)
 for poly in me.polygons:
  for li in poly.loop_indices:
   co=me.vertices[me.loops[li].vertex_index].co
   uv.data[li].uv=(co[axes[0]]*.5,co[axes[1]]*.5)
 return o

def bloom():
 prof('ButtressPelvis',[(.64,.28,.25,0,0),(.90,.37,.27,0,0),(1.18,.38,.29,0,0),(1.45,.30,.26,0,0)],'root-bark','Pelvis')
 prof('HollowRibTrunk',[(1.30,.39,.31,0,-.05),(1.60,.43,.34,0,-.08),(1.95,.55,.37,0,-.12),(2.28,.72,.33,0,-.15),(2.48,.40,.28,0,-.08)],'root-bark','Chest')
 mass('GillHeart',(0,.32,2.10),(.24,.13,.30),'living-gill','Chest')
 for s,side in [('L',-1),('R',1)]:
  for k in range(9):
   z=1.69+k*.078
   tube(f'RibGill_{s}_{k}',[(side*.12,.40,z),(side*(.39+k*.026),.41,z+.15),(side*(.48+k*.019),.21,z+.25)],[.019,.041,.014],'living-gill','Chest',10)
  for label,bone in [('UpperArm','UpperArm_'+s),('ForeArm','ForeArm_'+s),('Thigh','Thigh_'+s),('Shin','Shin_'+s)]:
   b=arm.data.bones[bone];a=b.head_local;end=b.tail_local
   tube(label+s,[a,a.lerp(end,.23)+Vector((side*.05,0,0)),a.lerp(end,.63),end],[.14,.21,.19,.12],'root-bark',bone,20)
   for j in range(3):
    off=Vector((math.cos(j*math.tau/3)*.15,math.sin(j*math.tau/3)*.13,0))
    tube(label+s+'LivingCord'+str(j),[a+off*.7,a.lerp(end,.4)+off,a.lerp(end,.75)+Vector((-off.x,off.y,0)),end+off*.4],[.04,.065,.055,.015],'root-bark',bone,10)
  for k in range(4):
   x=side*.93+(k-1.5)*.105
   tube(f'TalonHand{s}{k}',[(x,.20,.78),(x+side*.06,.34,.54),(x+side*.09,.43,.29),(x+side*.04,.49,.24)],[.067,.061,.038,.009],'root-bark','Hand_'+s)
  b=arm.data.bones['Foot_'+s];a=b.head_local
  for k in range(4):
   tube(f'ButtressToe{s}{k}',[a,(a.x+(k-1.5)*.11,.36,.12),(a.x+(k-1.5)*.22,.69,.05)],[.16,.13,.025],'root-bark','Foot_'+s)
  for k in range(3):shelf(f'ArmShelf{s}{k}',(side*(.74+k*.10),-.015,2.24-k*.36),.31,.30,'ochre-shelf','UpperArm_'+s if k==0 else 'ForeArm_'+s,side*.25)
 # Hierarchical asymmetric mantle instead of the Brute's symmetric stack.
 for k,(x,y,z,rx,ry,tilt) in enumerate([(-.46,-.04,2.73,.93,.71,-.15),(.50,-.14,2.85,.80,.63,.23),(-.20,-.20,3.02,.68,.56,-.11),(.12,-.20,3.19,.46,.41,.16),(-.10,-.18,3.34,.30,.29,-.20)]):
  shelf('AncientCrown'+str(k),(x,y,z),rx,ry,'ochre-shelf','Head',tilt)
  for j in range(7):
   xx=x+(j-3)*rx*.20
   tube(f'CrownCurtain{k}_{j}',[(xx,y+ry*.64,z-.02),(xx+.03,y+ry*.72,z-.18),(xx+.04,y+ry*.69,z-.35-(j%3)*.07)],[.024,.035,.008],'ivory','Head',8)
 mass('RecessedFace',(0,.16,2.60),(.24,.17,.23),'root-bark','Head')
 for s in (-1,1):mass('Eye'+str(s),(s*.11,.329,2.65),(.026,.018,.023),'living-gill','Head')

def maw():
 # Preserve tail motion hierarchy, replace ovoid anatomy with a continuous taper.
 for name,bone,rings in [
  ('Shoulders','Spine_1',[(.15,.93,.66),(.60,1.05,.77),(1.15,1.00,.72),(1.75,.87,.60)]),
  ('TrunkA','Spine_2',[(-1.0,.67,.51),(-.4,.83,.64),(.3,.97,.69),(.7,1.00,.71)]),
  ('TrunkB','Spine_3',[(-1.9,.44,.34),(-1.3,.61,.46),(-.7,.76,.55),(-.3,.81,.61)]),
  ('Peduncle','Spine_4',[(-2.5,.28,.24),(-2.0,.41,.34),(-1.5,.57,.43)]),
  ('TailRoot','Spine_5',[(-3.0,.15,.17),(-2.6,.26,.23),(-2.2,.37,.29)]),
  ('TailJoint','Tail_1',[(-3.48,.07,.13),(-3.1,.16,.18),(-2.8,.21,.19)])]:
  prof(name,[(y,rx,rz,0,.04) for y,rx,rz in rings],'wet-hide',bone,32,'Y',.018)
 # Open annular head: rear throat to flared forward lip, no solid blob filling the gape.
 pts=[];rads=[]
 for j in range(7):
  t=j/6;pts.append((0,1.6+t*1.82,.03));rads.append(.38+t*.66)
 o=tube('OpenTrapHead',pts,rads,'wet-hide','Head',40)
 # Remove cap faces to expose a genuine mouth opening; inner throat remains behind.
 me=o.data
 # tube's ring axis is Y, section is round; scale upper vault / lower mouth for wedge profile.
 for v in me.vertices:v.co.z*=.84
 bm=bmesh.new();bm.from_mesh(me)
 bmesh.ops.delete(bm,geom=[f for f in bm.faces if len(f.verts)!=4],context='FACES_ONLY')
 bm.to_mesh(me);bm.free();me.update()
 # Closed rim and inner mouth lining give the open arch actual tissue thickness.
 outer=[tuple(v.co) for v in me.vertices];faces=[tuple(p.vertices) for p in me.polygons];count=len(outer)
 inner=[(x*.945,y,z*.945) for x,y,z in outer]
 faces += [tuple(count+i for i in reversed(f)) for f in list(faces)]
 for ring in [0,6]:
  for k in range(40):
   a=ring*40+k;b=ring*40+(k+1)%40
   faces.append((a,b,b+count,a+count) if ring==6 else (b,a,a+count,b+count))
 me.clear_geometry();me.from_pydata(outer+inner,[],faces);me.update()
 uv=me.uv_layers.active or me.uv_layers.new(name='BossConstructionUV')
 for poly in me.polygons:
  poly.use_smooth=True
  ids=[i%40 for i in poly.vertices];seam=0 in ids and 39 in ids
  for li in poly.loop_indices:
   j,k=divmod(me.loops[li].vertex_index%count,40)
   uv.data[li].uv=(1 if seam and k==0 else k/40,j/6)
 mass('DeepThroat',(0,1.60,.01),(.40,.12,.38),'oral-tissue','Head')
 for side,s in [('L',-1),('R',1)]:
  tube('Mandible'+side,[(s*.38,1.75,-.40),(s*.77,2.25,-.75),(s*.78,3.15,-.74),(s*.26,3.50,-.66)],[.12,.16,.13,.08],'wet-hide','Jaw',20)
  for i in range(11):
   y=2.12+i*.12;x=s*(.71+.11*math.sin(i/10*math.pi))
   tube(f'TrapTooth{side}{i}',[(x,y,.64),(x*.86,y+.07,.31),(x*.76,y+.10,.01)],[.061,.035,.003],'ivory','Head',10)
   tube(f'JawTooth{side}{i}',[(x,y,-.68),(x*.86,y+.08,-.41),(x*.80,y+.12,-.24)],[.051,.028,.003],'ivory','Jaw',10)
  for k in range(3):mass(f'SensoryOrgan{side}{k}',(s*(.90-.04*k),2.23+k*.22,.43+k*.065),(.06,.04,.055),'light-organ','Head')
  for k in range(4):
   tube(f'GillSlit{side}{k}',[(s*.94,1.35+k*.14,.50),(s*1.01,1.24+k*.14,.05),(s*.88,1.20+k*.14,-.38)],[.025,.038,.018],'oral-tissue','Spine_1',8)
  # Broad lateral fins as closed thin lobes with structural rays.
  membrane('PectoralWeb'+side,[(s*.73,1.07,-.23),(s*1.94,.40,-.36),(s*1.57,-.29,-.38),(s*1.32,-1.03,-.36),(s*.73,.0,-.22)],'fin-membrane','Fin_'+side)
  for k in range(7):
   tube(f'PectoralRay{side}{k}',[(s*.73,1.07-k*.16,-.22),(s*(1.78-k*.06),.56-k*.22,-.32),(s*(1.93-k*.10),.40-k*.24,-.36)],[.07,.06,.008],'fin-membrane','Fin_'+side,10)
 for k in range(9):
  y=1.36-k*.43;z=.72-k*.045
  tube('DorsalSpine'+str(k),[(0,y,z),(0,y-.13,z+.55),(0,y-.43,z+.69)],[.17,.12,.006],'wet-hide','Spine_'+str(min(5,1+k//2)),10)
 for s in (-1,1):
  membrane('CaudalWeb'+str(s),[(0,-3.35,0),(0,-3.61,s*.28),(0,-3.61,s*1.28),(0,-3.95,s*.32)],'fin-membrane','Tail_2','X')
  for k in range(7):tube(f'CaudalLobe{s}_{k}',[(0,-3.35,0),(0,-3.60,s*(.20+k*.14)),(0,-3.95+k*.055,s*(.32+k*.16))],[.10,.08,.006],'fin-membrane','Tail_2',10)

def furnace():
 key='basalt';core='molten-organ'
 # Wide boiler cage, squat buried head, asymmetrical excavator arms.
 slab('HipAnchor',(0,-.05,.70),(.37,.25,.20),key,'Pelvis',(.05,-.08,0),3)
 slab('RearBoiler',(0,-.21,1.30),(.47,.18,.48),key,'Torso',(.08,.04,0),4)
 mass('MoltenChamber',(0,.14,1.27),(.32,.25,.37),core,'FurnaceCore',24)
 for side,s in [('L',-1),('R',1)]:
  # Oblique broken masonry braces leave the incandescent heart exposed.
  for k in range(5):
   slab(f'FurnaceRib{side}{k}',(s*(.31+.045*k),.27,1.02+k*.13),(.21,.17,.085),key,'Torso',(.05,s*.26,s*.12),k+4)
  slab('ShoulderCap'+side,(s*.63,-.015,1.63),(.34,.33,.23),key,'Torso',(.12,s*.25,s*.09),12)
  for bname in ['Upper_'+side,'Lower_'+side,'UpperLeg_'+side,'LowerLeg_'+side]:
   b=arm.data.bones[bname];a=b.head_local;end=b.tail_local
   tube('HotJoint'+bname,[a,a.lerp(end,.5),end],[.13,.11,.09],core,bname,10)
   for k in range(2):
    p=a.lerp(end,.27+.44*k)
    slab('ArmorBlock'+bname+str(k),p,(.22 if 'Leg' not in bname else .16,.21,.16),key,bname,(0,s*.14,(-1 if k else 1)*.10),k+7)
  hand=arm.data.bones['Lower_'+side].tail_local
  slab('HammerFist'+side,hand,(.30 if s<0 else .36,.34,.25),key,'Lower_'+side,(0,s*.08,.06),2)
  for k in range(3):slab('Knuckle'+side+str(k),(hand.x+(k-1)*.13,hand.y+.27,hand.z),(.064,.09,.11),key,'Lower_'+side,(0,0,0),k)
  end=arm.data.bones['LowerLeg_'+side].tail_local
  slab('LoadBearingFoot'+side,(end.x,end.y+.04,.095),(.23,.35,.10),key,'LowerLeg_'+side,(0,0,s*.09),4)
  for k in range(3):
   x=s*(.36+k*.15);z=1.84+k*.09
   slab('ChimneyBase'+side+str(k),(x,-.24,z),(.10,.13,.27),key,'Torso',(.14,s*.17,0),k)
   mass('VentOpening'+side+str(k),(x+s*.035,-.31,z+.25),(.064,.08,.032),core,'Torso',12)
 slab('BuriedHead',(0,.02,1.78),(.21,.18,.16),key,'Head',(.10,0,.06),5)
 for s in (-1,1):slab('Brow'+str(s),(s*.09,.21,1.81),(.10,.06,.046),key,'Head',(0,s*.15,0),1)
 tube('MoltenMouth',[(-.15,.213,1.70),(0,.23,1.69),(.15,.213,1.70)],[.022,.026,.012],core,'Head',8)

def silence():
 key='ancient-ice';core='frozen-core'
 # Tall hollow spine with thin mantle ribs, unlike the furnace's barrel body.
 tube('FrozenSpine',[(0,-.09,.64),(0,-.10,1.04),(0,-.08,1.48),(0,-.04,1.74)],[.17,.16,.19,.13],key,'Torso',12)
 mass('SilentHeart',(0,.09,1.30),(.13,.12,.30),core,'Torso',18)
 slab('NarrowPelvis',(0,-.04,.70),(.24,.19,.20),key,'Pelvis',(.02,0,.06),2)
 for side,s in [('L',-1),('R',1)]:
  for k in range(5):
   # Thin plates extend downward into a ragged cape on each shoulder.
   x=s*(.27+k*.115);z=1.52+(k%2)*.055
   tube('MantleBlade'+side+str(k),[(x,-.035,z),(x+s*.05,.00,z-.22),(x+s*.07,.035,z-.51-.055*k)],[.145,.102,.004],key,'Torso',6)
  for k in range(6):
   tube('IceRib'+side+str(k),[(s*.10,.18,1.10+k*.077),(s*.27,.17,1.14+k*.077),(s*.36,.06,1.23+k*.06)],[.027,.043,.006],key,'Torso',8)
  for bname in ['Upper_'+side,'Lower_'+side,'UpperLeg_'+side,'LowerLeg_'+side]:
   b=arm.data.bones[bname];a=b.head_local;end=b.tail_local
   tube('IceSkeleton'+bname,[a,a.lerp(end,.36),a.lerp(end,.68),end],[.10,.13,.085,.075],core,bname,8)
   for k in range(3):
    p=a.lerp(end,.18+k*.28)
    o=slab('Splinter'+bname+str(k),p,(.115,.11,.21),key,bname,(.16,s*.18,s*.16),k+1)
  hand=arm.data.bones['Lower_'+side].tail_local
  for k in range(4):
   x=hand.x+(k-1.5)*.067
   tube('LongIceClaw'+side+str(k),[(x,hand.y+.04,hand.z),(x+s*.025,hand.y+.11,hand.z-.24),(x+s*.04,hand.y+.17,hand.z-.48-(k%2)*.065)],[.041,.024,.003],key,'Lower_'+side,6)
  end=arm.data.bones['LowerLeg_'+side].tail_local
  for k in range(3):tube('IceFoot'+side+str(k),[(end.x+(k-1)*.075,end.y-.06,.14),(end.x+(k-1)*.085,end.y+.22,.045),(end.x+(k-1)*.14,end.y+.34,.025)],[.07,.052,.006],key,'LowerLeg_'+side,6)
 # Face hides beneath a cleft vertical glacier crown, with no conventional skull.
 for k in range(5):
  x=(k-2)*.095
  tube('CleftCrown'+str(k),[(x,.0,1.64),(x,.0,1.88),(x+(k-2)*.028,-.02,2.14+.12*(k%2))],[.102,.096,.008],key,'Head',6)
 slab('FaceVeil',(0,.17,1.78),(.14,.045,.17),key,'Head',(.08,0,0),5)
 for s in (-1,1):mass('RecessedIceEye'+str(s),(s*.062,.22,1.80),(.020,.012,.026),core,'Head',12)

def titan():
 key='rift-strata';core='binding-prism'
 # Sparse stone cage suspended around the binding wound. No solid torso barrel.
 center=Vector((0,.32,2.34))
 for k in range(5):
  a=math.tau*k/5
  tube('BindingCrystal'+str(k),[center+Vector((math.cos(a)*.13,0,math.sin(a)*.13)),center+Vector((math.cos(a)*.24,-.04,math.sin(a)*.24)),center+Vector((math.cos(a)*.08,-.10,math.sin(a)*.08))],[.13,.17,.006],core,'Body',6)
 for k in range(9):
  a=math.tau*k/9
  p=center+Vector((math.cos(a)*.53,-.02,math.sin(a)*.49))
  slab('OrbitRib'+str(k),p,(.21,.18,.13),key,'Body',(.10,-a,a*.25),k+3)
 for side,s in [('L',-1),('R',1)]:
  for k in range(4):
   slab('ShoulderStrata'+side+str(k),(s*(.69+k*.14),-.04,2.81-k*.055),(.23,.32,.14),key,'Body',(.07,s*.13,s*.18),k)
  for bname in ['Arm'+side,'Forearm'+side,'Leg'+side]:
   b=arm.data.bones[bname];a=b.head_local;end=b.tail_local
   # Leave real breaks between aligned fragments; supernatural binding bridges gaps.
   tube('BindingThread'+bname,[a,a.lerp(end,.5),end],[.035,.028,.018],core,bname,8)
   for k in range(4):
    p=a.lerp(end,(k+.5)/4)
    slab('SuspendedMass'+bname+str(k),p,(.24 if 'Leg' not in bname else .27,.24,.14),key,bname,(.13,s*.14,(-1)**k*.19),k+5)
  hand=arm.data.bones['Forearm'+side].tail_local
  for k in range(3):slab('BrokenHand'+side+str(k),hand+Vector(((k-1)*.17,.10,-.15)),(.095,.17,.21),key,'Forearm'+side,(.08,s*.11,0),k)
  end=arm.data.bones['Leg'+side].tail_local
  slab('TitanFoot'+side,(end.x,end.y+.11,.16),(.37,.45,.17),key,'Leg'+side,(0,s*.09,0),6)
 for k in range(3):
  slab('PelvisShard'+str(k),((k-1)*.32,0,1.13),(.18,.24,.24),key,'Body',(.12,(k-1)*.16,.03),k)
 # Offset towering crown of split menhirs, rather than an ice-style helmet.
 for k in range(4):
  x=(k-1.5)*.20
  slab('CrownMenhir'+str(k),(x,-.04,3.15+(k%2)*.19),(.095,.13,.47),key,'Body',(.11,(k-1.5)*.09,.05),k)
 tube('BrowBinding',[(-.21,.25,2.97),(0,.32,2.95),(.21,.25,2.97)],[.025,.04,.022],core,'Body',8)

def carrion():
 # A single massive rooted sovereign, rather than the donor's four small flowers.
 prof('RootTrunk',[(.04,.65,.48,0,0),(.22,.49,.36,0,0),(.55,.37,.31,0,-.02),(.90,.34,.29,-.02,-.04),(1.18,.46,.27,0,-.08),(1.42,.39,.23,.04,-.10)],'decay-root','Stalk_2',28,irregular=.12)
 for k in range(14):
  a=math.tau*k/14;x=math.cos(a);y=math.sin(a)
  tube('RootFlare'+str(k),[(x*.17,y*.17,.80),(x*.36,y*.31,.35),(x*.67,y*.48,.13),(x*(1.02+(k%3)*.13),y*.81,.035)],[.09,.15,.12,.013],'decay-root','Root',12)
 mass('ThroatDark',(0,.22,1.42),(.32,.06,.34),'oral-tissue','Stalk_2')
 mass('AmberWound',(0,.285,1.43),(.13,.035,.18),'amber-organ','Stalk_2')
 for k in range(13):
  a=math.tau*k/13;x=math.cos(a);z=math.sin(a)
  tube('CrownSepal'+str(k),[(x*.18,.18,1.43+z*.19),(x*.35,.10,1.43+z*.32),(x*.48,-.04,1.43+z*.48),(x*.38,-.12,1.43+z*.63)],[.105,.145,.098,.007],'carrion-sepal','Stalk_2',12)
  tube('BoneCrown'+str(k),[(x*.21,.26,1.43+z*.22),(x*.34,.30,1.43+z*.38),(x*.35,.33,1.43+z*.48)],[.037,.025,.004],'ivory','Stalk_2',8)
 for side,s in [('L',-1),('R',1)]:
  bone='Stalk_1' if s<0 else 'Stalk_4'
  tube('BranchArm'+side,[(s*.30,-.02,1.12),(s*.60,.02,1.21),(s*.81,.12,1.05),(s*.94,.17,.79)],[.12,.10,.075,.035],'decay-root',bone,14)
  for k in range(3):
   x=s*(.64+k*.17)
   tube('PodStem'+side+str(k),[(x,.13,1.05),(x+s*.04,.20,.73),(x+s*.05,.23,.60)],[.02,.016,.01],'decay-root',bone,8)
   mass('HangingSeed'+side+str(k),(x+s*.05,.23,.54),(.075,.07,.12),'amber-organ',bone)
  for k in range(5):
   z=.33+k*.13
   tube('ExposedRib'+side+str(k),[(s*.12,.30,z),(s*.31,.32,z+.07),(s*.37,.16,z+.13)],[.025,.04,.011],'ivory','Stalk_2',10)

def finalize(name,donor,target):
 global parts,mats,arm
 scene=bpy.context.scene
 # Replace raw studies with one renderer per bone and material.
 groups={}
 for objname,bone in parts.items():
  o=bpy.data.objects[objname];groups.setdefault((bone,o.data.materials[0].name),[]).append(o)
 for (bone,matname),objs in groups.items():
  bpy.ops.object.select_all(action='DESELECT')
  for o in objs:o.select_set(True)
  bpy.context.view_layer.objects.active=objs[0]
  if len(objs)>1:bpy.ops.object.join()
  o=bpy.context.object;o.name=bone+'__'+matname
  # Recalculate outward normals and unwrap missing/rebuilt mouth UVs.
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False)
  if not o.data.uv_layers:bpy.ops.uv.smart_project(island_margin=.015)
  bpy.ops.object.mode_set(mode='OBJECT')
  o.vertex_groups.clear();vg=o.vertex_groups.new(name=bone);vg.add(list(range(len(o.data.vertices))),1,'REPLACE')
  mod=o.modifiers.new('BossConstructionRig','ARMATURE');mod.object=arm;o.parent=arm
  o['game_collision']=False;o['game_crystal']='null'
 meshes=[o for o in scene.objects if o.type=='MESH']
 bpy.context.view_layer.update()
 pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
 low=Vector([min(p[i] for p in pts) for i in range(3)]);high=Vector([max(p[i] for p in pts) for i in range(3)])
 dim=high-low;factor=target/(dim.y if name=='blackwater-maw' else dim.z)
 # Geometry and skeleton share the exact scaling; actions keep canonical names.
 for o in meshes:
  for v in o.data.vertices:v.co*=factor
  o.location*=factor
 bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
 for b in arm.data.edit_bones:b.head*=factor;b.tail*=factor
 bpy.ops.object.mode_set(mode='OBJECT')
 for action in bpy.data.actions:
  for layer in action.layers:
   for strip in layer.strips:
    for slot in action.slots:
     bag=strip.channelbag(slot)
     if bag:
      for curve in bag.fcurves:
       if curve.data_path.endswith('.location'):
        for kp in curve.keyframe_points:
         kp.co.y*=factor;kp.handle_left.y*=factor;kp.handle_right.y*=factor
 arm.name='Boss_'+name+'_Rig';arm.data.pose_position='REST';arm.animation_data.action=None
 for b in arm.pose.bones:b.matrix_basis=Matrix.Identity(4)
 scene['magenheim_model_id']='underworld-boss-'+name
 scene['concept_target']='docs/art/creature-concepts/2026-10-03/the-'+name+'.png'
 scene['concept_pass']='donor-comparison-detail-r2; acceptance pending' if DETAIL else 'construction-1; authored, not live accepted'
 scene['inherited_animation_source']=donor
 scene['boss_target_metres']=target
 scene['magenheim_skinning']='rigid-segment-weighted'
 # Keep the retargeting clips but remove unused donor geometry and packed maps.
 for action in bpy.data.actions:
  if action.name in str(scene['magenheim_authored_actions']).split(','):action.use_fake_user=True
 bpy.data.orphans_purge(do_local_ids=True,do_linked_ids=True,do_recursive=True)
 bpy.context.preferences.filepaths.save_version=0
 path=ROOT/'assets/models/source'/('underworld-boss-'+name+('-detail-r2' if DETAIL else '')+'.blend')
 bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
 for o in meshes:o.data.calc_loop_triangles()
 tris=sum(len(o.data.loop_triangles) for o in meshes)
 print(f'AUTHORED {name}: {tris} triangles / {len(meshes)} bone-material parts / {target}m',flush=True)
 return dict(id=path.stem,concept=scene['concept_target'],concept_sha256=hashlib.sha256((ROOT/scene['concept_target']).read_bytes()).hexdigest(),source=path.relative_to(ROOT).as_posix(),target_metres=target,triangles=tris,renderers=len(meshes),materials=sorted(mats),rig_source=donor,animation_status='inherited baseline; retargeting inspection required',status='authored construction study; not installed or accepted')

def main():
 global parts,mats,arm
 records=[]
 for name,(donor,target) in SPECS.items():
  bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/source'/('underworld-creature-'+donor+'.blend')))
  arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
  arm.animation_data.action=None;arm.data.pose_position='REST'
  for b in arm.pose.bones:b.matrix_basis=Matrix.Identity(4)
  for o in list(bpy.context.scene.objects):
   if o!=arm:bpy.data.objects.remove(o,do_unlink=True)
  parts={};mats={}
  if DETAIL:
   from boss_detail_authoring import configure
   configure(sys.modules[__name__])
  if name=='first-bloom':bloom()
  elif name=='blackwater-maw':maw()
  elif name=='carrion-crown':carrion()
  elif name=='furnace-heart':furnace()
  elif name=='white-silence':silence()
  else:titan()
  if DETAIL:
   from boss_detail_authoring import revise
   revise(name,sys.modules[__name__])
  records.append(finalize(name,donor,target))
 review=ROOT/'docs/art/creature-models/2026-10-04'
 if DETAIL:review=review/'detail-r2'
 review.mkdir(parents=True,exist_ok=True)
 (review/'boss-construction-manifest.json').write_text(json.dumps(records,indent=2)+'\n')

if __name__=='__main__':main()
