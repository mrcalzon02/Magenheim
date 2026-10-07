"""Purpose-shaped secondary anatomy for donor-comparison revision 2.

Density comes from gills, bark ridges, armor overlaps, jaw margins and fractures,
not subdividing the first pass's plain primitives to inflate triangle counts.
"""
import bpy,math,hashlib,random
from mathutils import Vector,Matrix,Euler
B=None;original_tube=None

def seed_for(name):return int(hashlib.sha256(name.encode()).hexdigest()[:8],16)

def configure(base):
 global B,original_tube
 B=base;original_tube=base.tube
 base.slab=rock
 base.shelf=shelf

def kill(prefix):
 for name in list(B.parts):
  if name.startswith(prefix):
   o=bpy.data.objects.get(name)
   if o:bpy.data.objects.remove(o,do_unlink=True)
   del B.parts[name]

def ridge(name,points,radii,key,bone,sides=10,flat=1):
 # Interpolate shaped paths at the authored bends, with tapered asymmetric sections.
 pts=[Vector(p) for p in points];smooth=[];rs=[]
 for i in range(len(pts)-1):
  a=pts[max(0,i-1)];b=pts[i];c=pts[i+1];d=pts[min(len(pts)-1,i+2)]
  for k in range(3):
   t=k/3
   smooth.append((2*b+(c-a)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t)*.5)
   rs.append(radii[i]*(1-t)+radii[i+1]*t)
 smooth.append(pts[-1]);rs.append(radii[-1])
 o=original_tube(name,smooth,rs,key,bone,sides)
 if flat!=1:
  # Flatten across local section axis, preserving centerline and body attachment.
  for j,p in enumerate(smooth):
   tangent=(smooth[min(j+1,len(smooth)-1)]-smooth[max(j-1,0)]).normalized()
   axis=tangent.to_track_quat('Z','Y')@Vector((0,1,0))
   for k in range(sides):
    v=o.data.vertices[j*sides+k];v.co+=axis*((v.co-p).dot(axis)*(flat-1))
 return o

def rock(name,loc,scale,key,bone,rot=(0,0,0),seed=0):
 rng=random.Random(seed_for(name)+seed);sides=12
 angles=[math.tau*k/sides+rng.uniform(-.055,.055) for k in range(sides)]
 shape=[rng.uniform(.79,1.12) for k in range(sides)]
 verts=[];faces=[]
 # Unequal bevel cuts and layered broken shoulders, not a chamfered cube.
 for j,(z,r) in enumerate([(-1,.54),(-.80,.96),(-.31,1),(.19,.91),(.69,.82),(1,.39)]):
  cx=rng.uniform(-.13,.13);cy=rng.uniform(-.13,.13)
  for k,a in enumerate(angles):
   radial=shape[k]*(1+rng.uniform(-.12,.12))
   verts.append((scale[0]*(cx+math.cos(a)*r*radial),scale[1]*(cy+math.sin(a)*r*radial),scale[2]*(z+rng.uniform(-.06,.06))))
 for j in range(5):
  for k in range(sides):
   a=j*sides+k;b=j*sides+(k+1)%sides
   faces.extend([(a,b,b+sides),(a,b+sides,a+sides)])
 faces.extend([tuple(reversed(range(sides))),tuple(5*sides+k for k in range(sides))])
 me=bpy.data.meshes.new(name+'_fracture');me.from_pydata(verts,[],faces);me.update();me.materials.append(B.material(key))
 o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);o.location=loc;o.rotation_euler=rot;B.parts[name]=bone
 uv=me.uv_layers.new(name='BossConstructionUV')
 for p in me.polygons:
  axis=max(range(3),key=lambda i:abs(p.normal[i]));axes=[i for i in range(3) if i!=axis]
  for li in p.loop_indices:
   co=me.vertices[me.loops[li].vertex_index].co
   uv.data[li].uv=((co[axes[0]]/scale[axes[0]]+1)*.5,(co[axes[1]]/scale[axes[1]]+1)*.5)
 return o

def shelf(name,loc,rx,ry,key,bone,tilt=0):
 rng=random.Random(seed_for(name));n=48
 # Radial shelf with irregular torn scallops and an inward rolled living rim.
 profiles=[(-.09,.06),(-.065,.34),(-.045,.67),(-.075,.91),(-.08,1),(-.025,1.03),(.025,.99),(.055,.93),(.10,.77),(.15,.53),(.19,.27),(.205,.022)]
 verts=[];faces=[]
 for j,(z,r) in enumerate(profiles):
  for k in range(n):
   a=math.tau*k/n
   wave=.075*math.sin(a*5+.6)+.026*math.sin(a*13-1.2)+.016*math.sin(a*27)
   ripple=1+wave*(r**2)
   verts.append((rx*math.cos(a)*r*ripple,ry*math.sin(a)*r*ripple,z+.025*math.sin(a*7)*r))
 for j in range(len(profiles)-1):
  for k in range(n):
   a=j*n+k;b=j*n+(k+1)%n;faces.append((a,b,b+n,a+n))
 faces.extend([tuple(reversed(range(n))),tuple((len(profiles)-1)*n+k for k in range(n))])
 me=bpy.data.meshes.new(name+'_livingShelf');me.from_pydata(verts,[],faces);me.update();me.materials.append(B.material(key))
 o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
 transform=Matrix.Translation(Vector(loc))@Matrix.Rotation(tilt,4,'Y');o.matrix_world=transform;B.parts[name]=bone
 uv=me.uv_layers.new(name='BossConstructionUV')
 for poly in me.polygons:
  poly.use_smooth=len(poly.vertices)==4
  seam=0 in [v%n for v in poly.vertices] and n-1 in [v%n for v in poly.vertices]
  for li in poly.loop_indices:
   j,k=divmod(me.loops[li].vertex_index,n);uv.data[li].uv=(1 if seam and k==0 else k/n,j/(len(profiles)-1))
 # Individual radial lamellae beneath the caps are thin folded surfaces.
 count=24 if rx>.45 else 8
 for k in range(count):
  a=math.tau*k/count
  path=[transform@Vector((rx*r*math.cos(a),ry*r*math.sin(a),z)) for r,z in [(.19,-.060),(.44,-.11),(.69,-.14),(.93,-.12)]]
  ridge(name+'_Lamella'+str(k),path,[.008,.026,.024,.005],'living-gill' if rx>.45 else 'ivory',bone,8,.23)
 return o

def bark_body(name,bone,phase):
 b=B.arm.data.bones[bone];a=b.head_local;end=b.tail_local
 # Interlacing structural roots at changing angular offsets around each joint.
 d=(end-a);q=d.to_track_quat('Z','Y')
 for k in range(7):
  angle=math.tau*k/7+phase
  points=[]
  for t in [0,.20,.48,.73,1]:
   theta=angle+t*.90
   offset=q@Vector((math.cos(theta)*(.12+.018*math.sin(t*math.pi)),math.sin(theta)*.11,0))
   points.append(a+d*t+offset)
  ridge(name+'_FibrousStrand'+str(k),points,[.02,.042,.053,.040,.012],'root-bark',bone,10)
 # Scarred joint collar with asymmetrical knots, not a ball joint.
 for k in range(3):
  p=a+d*(.25+k*.23)+q@Vector((math.cos(phase+k)*.15,math.sin(phase+k)*.12,0))
  o=B.mass(name+'_Knot'+str(k),p,(.065,.05,.095),'root-bark',bone,12)
  B.rotate_at(o,p,q.to_matrix().to_4x4())

def bloom():
 kill('RibGill');kill('CrownCurtain');kill('Eye');kill('RecessedFace')
 kill('ButtressPelvis');kill('HollowRibTrunk');kill('GillHeart')
 # Lean, ribbed living trunk joins the hips into the shoulder vault.
 B.prof('AncientTrunk',[(.76,.25,.24,0,-.025),(1.07,.36,.25,0,-.04),(1.35,.27,.25,0,-.07),(1.65,.36,.29,.025,-.08),(1.95,.49,.32,0,-.11),(2.20,.67,.29,-.035,-.13),(2.45,.43,.25,0,-.09)],'root-bark','Chest',40,irregular=.13)
 B.mass('RecessedGillOrgan',(0,.22,2.02),(.15,.08,.27),'living-gill','Chest',24)
 for k in range(14):
  a=math.tau*k/14
  ridge('TrunkButtress'+str(k),[(math.cos(a)*.26,math.sin(a)*.20,.89),(math.cos(a+.18)*.31,math.sin(a+.18)*.25,1.25),(math.cos(a+.28)*.37,math.sin(a+.28)*.29,1.75),(math.cos(a+.33)*.54,math.sin(a+.33)*.24,2.26)],[.02,.075,.065,.016],'root-bark','Chest',10)
 for side,s in [('L',-1),('R',1)]:
  for part in ['UpperArm','ForeArm','Thigh','Shin']:bark_body('Ancient'+part+side,part+'_'+side,s*.4)
  # Gill cathedral spreads from a central wound into broad shoulder vaults.
  for k in range(16):
   t=k/15
   ridge('ChestLamella'+side+str(k),[(s*.055,.43,1.61+t*.46),(s*(.18+t*.16),.47,1.72+t*.50),(s*(.46+t*.12),.32,2.03+t*.40)],[.015,.041,.014],'living-gill','Chest',8,.27)
  for k in range(5):
   x=s*(.40+k*.047)
   ridge('PelvicRoot'+side+str(k),[(s*.14,-.04,1.59),(s*.29,.18,1.20),(x,.13,.74),(s*.70+(k-2)*.10,.11,.28)],[.04,.071,.053,.015],'root-bark','Pelvis' if k<2 else 'Thigh_'+side,10)
  for k in range(4):
   ridge('FingerKnuckle'+side+str(k),[(s*.94+(k-1.5)*.10,.17,.82),(s*.97+(k-1.5)*.11,.29,.65),(s*1.03+(k-1.5)*.13,.36,.39)],[.076,.065,.018],'root-bark','Hand_'+side,12)
 # Deep mask with brow folds; eyes remain in their sockets under the mantle.
 B.prof('RootMask',[(2.39,.11,.075,0,.22),(2.48,.23,.14,0,.22),(2.61,.28,.17,0,.20),(2.75,.22,.13,.02,.17),(2.81,.045,.06,.03,.12)],'root-bark','Head',28,irregular=.12)
 for s in (-1,1):
  ridge('MaskBrow'+str(s),[(s*.025,.36,2.69),(s*.14,.385,2.71),(s*.235,.30,2.69)],[.037,.065,.012],'root-bark','Head',12)
  B.mass('DeepEye'+str(s),(s*.105,.376,2.645),(.025,.014,.025),'living-gill','Head',14)
 ridge('RootJaw',[(-.15,.34,2.49),(0,.39,2.43),(.15,.34,2.49)],[.054,.079,.034],'root-bark','Jaw',14)
 B.mass('RecessedRootMouth',(0,.371,2.51),(.118,.025,.048),'oral-tissue','Jaw',20)
 for s in (-1,1):
  for k in range(4):
   x=s*(.035+k*.047)
   ridge('FacialRootFold'+str(s)+'_'+str(k),[(x,.27,2.80),(x+s*.02,.345,2.71),(x+s*.012,.370,2.60),(x*.88,.36,2.52)],[.014,.027,.026,.004],'root-bark','Head',10)
 for k in range(8):
  a=k*2.39996;x=math.cos(a)*(.22+k*.035);y=math.sin(a)*.20-.20
  shelf('BackShelf'+str(k),(x,y,1.34+k*.14),.19+k*.014,.16,'ochre-shelf','Chest',math.sin(a)*.4)

def maw():
 kill('OpenTrapHead');kill('DeepThroat');kill('Mandible');kill('TrapTooth');kill('JawTooth');kill('DorsalSpine')
 # A full wedge skull, with a lower overhanging vault and deep dark mouth lining.
 rings=[(1.47,.72,.48),(1.79,.96,.63),(2.16,1.07,.72),(2.56,.96,.69),(2.93,.82,.57),(3.27,.78,.50),(3.49,.81,.52)]
 n=48;verts=[];faces=[]
 # Upper 250-degree shell terminates at two real jaw commissures.
 for j,(y,rx,rz) in enumerate(rings):
  for k in range(n):
   a=-.60+(math.pi+1.20)*k/(n-1)
   verts.append((math.cos(a)*rx,y,.15+math.sin(a)*rz))
 for j in range(len(rings)-1):
  for k in range(n-1):
   a=j*n+k;faces.append((a,a+1,a+n+1,a+n))
 count=len(verts);verts +=[(x*.91,y+.015,z*.91-.01) for x,y,z in verts]
 faces +=[tuple(count+i for i in reversed(f)) for f in list(faces)]
 for ring in [0,len(rings)-1]:
  for k in range(n-1):
   a=ring*n+k;faces.append((a,a+count,a+count+1,a+1))
 for j in range(len(rings)-1):
  for k in [0,n-1]:
   a=j*n+k;faces.append((a,a+n,a+n+count,a+count))
 me=bpy.data.meshes.new('MawSkull');me.from_pydata(verts,[],faces);me.update();me.materials.append(B.material('wet-hide'))
 o=bpy.data.objects.new('WedgeSkull',me);bpy.context.collection.objects.link(o);B.parts[o.name]='Head'
 uv=me.uv_layers.new(name='BossConstructionUV')
 for p in me.polygons:
  p.use_smooth=True
  for li in p.loop_indices:
   j,k=divmod(me.loops[li].vertex_index%count,n);uv.data[li].uv=(k/(n-1),j/(len(rings)-1))
 B.mass('DeepOralCavity',(0,1.66,.05),(.69,.18,.48),'oral-tissue','Head',24)
 B.prof('HingedLowerJaw',[(1.80,.38,.12,0,-.36),(2.14,.63,.15,0,-.61),(2.55,.79,.17,0,-.72),(2.94,.82,.14,0,-.69),(3.32,.66,.10,0,-.61),(3.53,.30,.065,0,-.53)],'wet-hide','Jaw',32,'Y',.045)
 for side,s in [('L',-1),('R',1)]:
  ridge('ThickJawMargin'+side,[(s*.36,1.83,-.35),(s*.70,2.36,-.64),(s*.82,2.95,-.63),(s*.54,3.41,-.48)],[.16,.16,.14,.095],'wet-hide','Jaw',16)
  for k in range(16):
   t=k/15;y=1.96+t*1.36
   section=min(range(len(rings)),key=lambda j:abs(rings[j][0]-y))
   _,rx,rz=rings[section];x=s*math.cos(.60)*rx;z=.15-math.sin(.60)*rz
   length=.17+.07*math.sin(t*math.pi)+.035*(k%3)
   ridge('UpperFang'+side+str(k),[(x,y,z),(x*.95,y+.04,z-length*.58),(x*.87,y+.10,z-length)],[.044,.023,.0025],'ivory','Head',12)
   jawx=s*(.52+math.sin(t*math.pi)*.22)
   ridge('LowerFang'+side+str(k),[(jawx,y,-.61),(jawx*.94,y+.04,-.47),(jawx*.88,y+.10,-.29+(k%2)*.035)],[.039,.021,.0025],'ivory','Jaw',12)
  # Stratified armored brows/cheeks distinguish skull from scaled hide.
  for k in range(8):
   y=1.58+k*.21;z=.37+.16*math.sin(k*.4)
   rock('CranialScute'+side+str(k),(s*(.62+.05*math.sin(k)),y,z),(.15,.21,.095),'wet-hide','Head',(.2,s*.36,s*.1),k)
  for k in range(12):
   y=1.02-k*.27;x=s*(.84-k*.032);z=.28
   bone='Spine_'+str(min(5,1+k//3))
   rock('FlankScale'+side+str(k),(x,y,z),(.09,.17,.13),'wet-hide',bone,(0,s*.40,s*.08),k)
  # Strengthened fin roots and individual membrane folds.
  for k in range(10):
   t=k/9
   ridge('FinFold'+side+str(k),[(s*.69,1.08-t*.8,-.18),(s*(1.46-t*.10),.42-t*.95,-.31),(s*(1.80-t*.43),.38-t*1.25,-.36)],[.024,.018,.006],'fin-membrane','Fin_'+side,8,.35)
 # Front trap teeth grow directly from the arch, following its curved margin.
 for k in range(17):
  a=-.48+(math.pi+.96)*k/16
  root=Vector((math.cos(a)*.805,3.475,.15+math.sin(a)*.515))
  toward=Vector((0,3.45,-.20))-root;toward.normalize();length=.21+.085*math.sin(k*.8)**2
  ridge('FrontTrapFang'+str(k),[root,root+toward*length*.54+Vector((0,.028,0)),root+toward*length+Vector((0,.045,0))],[.049,.027,.0025],'ivory','Head',12)
 for k in range(13):
  y=1.18-k*.32;z=.66-k*.035
  ridge('SawDorsal'+str(k),[(0,y,z),(0,y-.11,z+.29),(0,y-.26,z+.48),(0,y-.43,z+.54)],[.105,.09,.052,.004],'wet-hide','Spine_'+str(min(5,1+k//3)),12)

def furnace():
 kill('BuriedHead');kill('Brow')
 # Interlocking basalt sternum with exposed seams and chipped jaw mask.
 rock('FurnaceMask',(0,.075,1.81),(.24,.18,.22),'basalt','Head',(.09,.08,.04),22)
 for s in (-1,1):
  rock('HeavyBrow'+str(s),(s*.11,.24,1.88),(.14,.07,.07),'basalt','Head',(.08,s*.25,0),23)
  B.mass('RecessedFireEye'+str(s),(s*.105,.286,1.83),(.035,.020,.027),'molten-organ','Head',12)
 for side,s in [('L',-1),('R',1)]:
  for k in range(5):
   ridge('BoilerSeam'+side+str(k),[(s*.16,.37,.98+k*.135),(s*.28,.395,1.00+k*.135),(s*.44,.29,1.07+k*.135)],[.014,.018,.006],'molten-organ','Torso',8)
  for k in range(6):
   rock('ShoulderSpall'+side+str(k),(s*(.49+(k%3)*.13),-.03+(k//3)*.17,1.58+(k%2)*.14),(.15,.17,.11),'basalt','Torso',(.2,s*.3,k*.22),k+8)
  for bone in ['Upper_'+side,'Lower_'+side,'UpperLeg_'+side,'LowerLeg_'+side]:
   b=B.arm.data.bones[bone];a=b.head_local;end=b.tail_local
   for k in range(5):
    t=(k+.5)/5;p=a.lerp(end,t)+Vector((s*.10,.13,0))
    rock('JointScale'+bone+str(k),p,(.10,.075,.09),'basalt',bone,(.1,s*.25,k*.27),k+4)
  hand=B.arm.data.bones['Lower_'+side].tail_local
  for k in range(4):
   ridge('FistSplit'+side+str(k),[hand+Vector(((k-1.5)*.09,.34,-.13)),hand+Vector(((k-1.5)*.09,.37,.03)),hand+Vector(((k-1.5)*.10,.32,.18))],[.013,.009,.004],'molten-organ','Lower_'+side,8)
 # Ash-stratified dorsal buttress, high vent openings with dark inner rims.
 for k in range(9):
  rock('DorsalSlag'+str(k),(((k%3)-1)*.24,-.38,.93+(k//3)*.25),(.14,.13,.18),'basalt','Torso',(.22,(k%3-1)*.3,.12),k)

def silence():
 kill('FaceVeil');kill('RecessedIceEye');kill('IceRib')
 # Cleft faceless glacier mask, not a square head with two beads.
 for s in (-1,1):
  rock('IceMaskHalf'+str(s),(s*.075,.12,1.80),(.073,.10,.20),'ancient-ice','Head',(.1,s*.21,s*.075),12)
  ridge('FrozenEyeCrease'+str(s),[(s*.04,.239,1.81),(s*.10,.245,1.83),(s*.14,.21,1.82)],[.008,.014,.004],'frozen-core','Head',8,.38)
 for side,s in [('L',-1),('R',1)]:
  for k in range(10):
   t=k/9
   ridge('CleftRib'+side+str(k),[(s*.11,.10,1.04+t*.50),(s*(.22+.05*math.sin(t*math.pi)),.20,1.13+t*.48),(s*.32,.01,1.23+t*.44)],[.018,.043,.009],'ancient-ice','Torso',8,.52)
  for k in range(12):
   x=s*(.24+(k%4)*.105);z=1.55+(k//4)*.048
   ridge('MantleIcicle'+side+str(k),[(x,-.11,z),(x+s*.047,-.16,z-.23),(x+s*.063,-.13,z-.48-.033*(k%4))],[.045,.023,.002],'ancient-ice','Torso',6)
  for bone in ['Upper_'+side,'Lower_'+side,'UpperLeg_'+side,'LowerLeg_'+side]:
   b=B.arm.data.bones[bone];a=b.head_local;end=b.tail_local
   for k in range(5):
    p=a.lerp(end,(k+.5)/5)+Vector((s*.087,.10,0))
    ridge('JointIce'+bone+str(k),[p+Vector((0,0,.075)),p,p+Vector((s*.04,-.02,-.12))],[.055,.045,.002],'ancient-ice',bone,6)

def titan():
 # Further split the crown and armor edges into load-bearing fracture planes.
 for side,s in [('L',-1),('R',1)]:
  for k in range(8):
   rock('AsymmetricShoulder'+side+str(k),(s*(.54+(k%4)*.17),-.12+(k//4)*.15,2.79+.10*math.sin(k*1.7)),(.17,.23,.13),'rift-strata','Body',(.20,s*.4,(k-3)*.15),k)
  for bone in ['Arm'+side,'Forearm'+side,'Leg'+side]:
   b=B.arm.data.bones[bone];a=b.head_local;end=b.tail_local
   for k in range(5):
    p=a.lerp(end,(k+.5)/5)+Vector((s*.19,.11,0))
    rock('OuterFracture'+bone+str(k),p,(.08,.14,.15),'rift-strata',bone,(.20,s*.30,k*.27),k)
  for k in range(3):
   ridge('CoreBinder'+side+str(k),[(s*.08,.45,2.30),(s*.27,.35,2.14+k*.17),(s*.55,.25,2.04+k*.20)],[.015,.026,.004],'binding-prism','Body',8)
 for k in range(7):
  x=(k-3)*.14;z=3.16+.12*math.sin(k*1.1)
  rock('SplitMenhir'+str(k),(x,-.01,z),(.055,.14,.32),'rift-strata','Body',(.14,(k-3)*.075,.06),k+19)

def carrion():
 kill('CrownSepal');kill('BoneCrown');kill('AmberWound');kill('ThroatDark');kill('ExposedRib')
 # Predator flower crown with overlapping torn sepals and an actual dark funnel.
 B.prof('CarrionThroat',[(.17,.015,.018,0,1.44),(.185,.075,.09,0,1.44),(.235,.20,.26,0,1.44),(.33,.325,.385,0,1.44),(.345,.35,.415,0,1.44),(.26,.265,.34,0,1.44),(.19,.16,.21,0,1.44),(.135,.015,.018,0,1.44)],'oral-tissue','Stalk_2',32,'Y',.05)
 for k in range(17):
  a=math.tau*k/17+(.12 if k%3 else -.08);x=math.cos(a);z=math.sin(a)
  ridge('TornSepal'+str(k),[(x*.19,.24,1.45+z*.18),(x*.37,.13,1.43+z*.31),(x*(.45+.05*math.sin(k)),.01,1.43+z*.50),(x*.35,-.13,1.46+z*(.69+(k%3)*.035))],[.049,.11,.081,.004],'carrion-sepal','Stalk_2',12,.44)
  ridge('SplinterCrown'+str(k),[(x*.17,.29,1.45+z*.18),(x*.23,.34,1.44+z*.25),(x*.28,.28,1.43+z*.37)],[.023,.024,.002],'ivory','Stalk_2',10)
 B.mass('AmberThroatOrgan',(0,.29,1.35),(.078,.042,.085),'amber-organ','Stalk_2',18)
 for side,s in [('L',-1),('R',1)]:
  for k in range(11):
   z=.27+k*.073
   ridge('CarrionRib'+side+str(k),[(s*.09,.22,z),(s*(.20+.06*math.sin(k*.4)),.28,z+.06),(s*.30,.11,z+.14)],[.018,.028,.007],'ivory','Stalk_2',10)
  bone='Stalk_1' if s<0 else 'Stalk_4'
  for k in range(7):
   z=1.10-k*.061;x=s*(.27+k*.035)
   ridge('GraftedBranch'+side+str(k),[(x,-.08,z),(s*(.48+k*.044),.06,z+.18),(s*(.67+k*.045),.13,z+.06),(s*(.71+k*.050),.17,z-.23)],[.044,.061,.037,.005],'decay-root',bone,10)
  for k in range(5):
   a=k*1.1+s
   ridge('TrunkTwist'+side+str(k),[(s*.15,-.11,.15),(s*.25,.13,.55),(s*.24,.07,1.04),(s*.14,-.02,1.44)],[.074,.080,.054,.011],'decay-root','Stalk_2',12)
 # Mature root buttresses divide into smaller soil-gripping branches.
 for k in range(22):
  a=k*math.tau/22;r=.72+(k%3)*.11
  ridge('RootFinger'+str(k),[(math.cos(a)*.31,math.sin(a)*.27,.39),(math.cos(a)*.54,math.sin(a)*.44,.15),(math.cos(a)*r,math.sin(a)*r*.8,.055),(math.cos(a+.10)*(r+.16),math.sin(a+.10)*(r+.16)*.8,.025)],[.04,.071,.035,.003],'decay-root','Root',10)
 # Torn bark leaves follow the trunk, with exposed fibrous joins below them.
 for k in range(13):
  a=k*2.39996;z=.38+(k%5)*.17;r=.31 if z<.9 else .35
  x=math.cos(a)*r;y=math.sin(a)*r*.74
  ridge('BarkSheath'+str(k),[(x*.91,y*.91,z+.23),(x*1.04,y*1.04,z+.08),(x*1.12,y*1.12,z-.11)],[.018,.09,.013],'decay-root','Stalk_2',10,.33)
 for side,s in [('L',-1),('R',1)]:
  bone='Stalk_1' if s<0 else 'Stalk_4'
  for k in range(3):
   x=s*(.64+k*.17)+s*.05
   for j in range(7):
    a=math.tau*j/7
    ridge('SeedCapsule'+side+str(k)+'_'+str(j),[(x+math.cos(a)*.03,.23+math.sin(a)*.03,.66),(x+math.cos(a)*.072,.23+math.sin(a)*.063,.55),(x+math.cos(a)*.035,.23+math.sin(a)*.035,.43)],[.006,.015,.004],'carrion-sepal',bone,8)

def revise(name,base):
 globals()[{'first-bloom':'bloom','blackwater-maw':'maw','furnace-heart':'furnace','white-silence':'silence','rift-titan':'titan','carrion-crown':'carrion'}[name]]()
 bpy.context.scene['donor_quality_bar']={'first-bloom':'Troll','blackwater-maw':'Serpent','furnace-heart':'StoneGolem','white-silence':'StoneGolem','rift-titan':'StoneGolem','carrion-crown':'Greydwarf_Shaman'}[name]
 bpy.context.scene['detail_revision']='anatomy / silhouette / owned multiscale surface; donor acceptance pending'
