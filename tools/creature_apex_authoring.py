"""Purpose-shaped apex anatomy. Called upstream before the existing rigid rig binds.

Ring profiles describe muscle, stratified stone and woody growth, rather than
subdividing primitives. Existing identities, materials and combat sockets survive.
"""
import math
import bpy
from mathutils import Vector, Matrix

def surface(name, rings, material, parts, bone, sides=16, axis='Z', uvname='ApexUV', irregular=.025):
    # ring: axial position, two radii, cross-section center offsets.
    verts=[];faces=[]
    for j,(z,rx,ry,cx,cy) in enumerate(rings):
        for i in range(sides):
            a=math.tau*i/sides
            ripple=1+irregular*(math.sin(a*3+j*.8)+.4*math.sin(a*7-j*.5))
            p=(cx+rx*math.cos(a)*ripple,cy+ry*math.sin(a)*ripple,z)
            verts.append(p if axis=='Z' else (p[0],p[2],p[1]))
    for j in range(len(rings)-1):
        for i in range(sides):
            a=j*sides+i;b=j*sides+(i+1)%sides
            q=(a,b,b+sides,a+sides)
            faces.append(q if axis=='Z' else tuple(reversed(q)))
    faces += [tuple(reversed(range(sides))),tuple((len(rings)-1)*sides+i for i in range(sides))]
    if axis!='Z':faces[-2:]=[tuple(reversed(f)) for f in faces[-2:]]
    me=bpy.data.meshes.new(name+'_sculpt');me.from_pydata(verts,[],faces);me.update();me.materials.append(material)
    old=bpy.data.objects.get(name)
    if old:
        old.data=me;old.matrix_world=Matrix.Identity(4);o=old
    else:
        o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
    layer=me.uv_layers.new(name=uvname)
    for poly in me.polygons:
        for li in poly.loop_indices:
            index=me.loops[li].vertex_index;j,i=divmod(index,sides)
            # Duplicate the seam through loop UVs, never project the entire body flat.
            u=i/sides
            indices=[v%sides for v in poly.vertices]
            if 0 in indices and sides-1 in indices and i==0:u=1
            layer.data[li].uv=(u,j/max(1,len(rings)-1))
    parts[name]=bone;o['apex_anatomy']='profiled-surface-v1'
    return o

def tube(name,points,radii,material,parts,bone,uvname='ApexUV',sides=12):
    a,b=Vector(points[0]),Vector(points[-1]);direction=b-a
    o=surface(name,[(t*direction.length,r,r*.86,0,0) for t,r in radii],material,parts,bone,sides,uvname=uvname,irregular=.04)
    o.matrix_world=Matrix.Translation(a)@direction.to_track_quat('Z','Y').to_matrix().to_4x4()
    return o

def small(name,loc,scale,mat,parts,bone,uvname='ApexUV'):
    o=surface(name,[(-1,.04,.04,0,0),(-.7,.65,.65,0,0),(0,1,1,0,0),(.6,.72,.72,0,0),(1,.05,.05,0,0)],mat,parts,bone,12,uvname=uvname)
    o.matrix_world=Matrix.Translation(Vector(loc))@Matrix.Diagonal((*scale,1))
    return o

def crowncap(parts):
    hide=bpy.data.materials['Brute_RootHide'];flesh=bpy.data.materials['Brute_MycelialFlesh'];plate=bpy.data.materials['Brute_CrownPlate'];gill=bpy.data.materials['Brute_GillTissue'];uv='CrowncapBruteUV'
    surface('Pelvis',[(.64,.38,.30,0,0),(.86,.59,.40,0,-.02),(1.15,.67,.43,0,0),(1.40,.51,.36,0,0),(1.64,.40,.30,0,0)],hide,parts,'Pelvis',24,uvname=uv)
    surface('Torso',[(1.30,.43,.33,0,0),(1.54,.55,.39,0,-.04),(1.84,.74,.48,0,-.03),(2.12,.88,.47,0,-.06),(2.37,.85,.39,0,-.09),(2.55,.48,.28,0,-.10)],hide,parts,'Chest',24,uvname=uv)
    surface('UpperTorso',[(2.12,.70,.24,0,.16),(2.27,.82,.28,0,.12),(2.42,.70,.29,0,.10),(2.56,.38,.24,0,.10)],flesh,parts,'Chest',24,uvname=uv)
    surface('Head',[(2.43,.20,.21,0,.16),(2.52,.32,.31,0,.17),(2.65,.38,.29,0,.18),(2.79,.31,.25,0,.12),(2.88,.15,.15,0,.10)],flesh,parts,'Head',20,uvname=uv)
    for i,(z,sx,sy) in enumerate([(2.77,1.12,.78),(2.91,.98,.70),(3.03,.79,.58),(3.12,.57,.43)],1):
        # Wide downturned lip, convex crown, uneven living rim.
        surface(f'CrownPlate_{i}',[(z-.06,sx*.95,sy*.96,0,.02),(z,sx,sy,0,.02),(z+.05,sx*.84,sy*.83,0,0),(z+.11,sx*.52,sy*.49,-.025,-.025),(z+.14,.04,.04,-.035,-.035)],plate,parts,'Head',32,uvname=uv,irregular=.045)
    for s,x in [('L',-.86),('R',.86)]:
        tube('UpperArmMesh_'+s,[(x,0,2.26),(x*1.16,.02,1.51)],[(0,.24),(.18,.34),(.42,.38),(.73,.29),(1,.25)],hide,parts,'UpperArm_'+s,uv)
        tube('ForeArmMesh_'+s,[(x*1.16,.02,1.61),(x*1.22,.18,.79)],[(0,.25),(.23,.38),(.5,.36),(.78,.27),(1,.22)],hide,parts,'ForeArm_'+s,uv)
        small('Fist_'+s,(x*1.22,.20,.70),(.32,.28,.26),hide,parts,'Hand_'+s,uv)
        for k in range(4):
            xx=x*1.22+(k-1.5)*.14
            tube(f'Finger_{s}_{k}',[(xx,.38,.73),(xx,.47,.39)],[(0,.082),(.30,.095),(.65,.075),(1,.040)],hide,parts,'Hand_'+s,uv,8)
        small('Thumb_'+s,(x*1.22+(.29 if x<0 else -.29),.34,.67),(.11,.15,.17),hide,parts,'Hand_'+s,uv)
    for s,x in [('L',-.43),('R',.43)]:
        tube('ThighMesh_'+s,[(x,0,1.23),(x*.98,.03,.60)],[(0,.29),(.3,.37),(.65,.31),(1,.24)],hide,parts,'Thigh_'+s,uv)
        tube('ShinMesh_'+s,[(x*.98,.03,.67),(x*1.02,.12,.17)],[(0,.24),(.3,.29),(.7,.22),(1,.18)],hide,parts,'Shin_'+s,uv)
        for k in range(3):small(f'Toe_{s}_{k}',(x*1.02+(k-1)*.20,.72,.14),(.13,.22,.10),hide,parts,'Foot_'+s,uv)
    # Brow sockets, small recessed eyes and an articulated lower lip give the crown a face.
    for s,x in [('L',-.19),('R',.19)]:
        small('EyeSocket_'+s,(x,.43,2.66),(.13,.055,.082),hide,parts,'Head',uv)
        small('Eye_'+s,(x,.482,2.67),(.045,.022,.032),gill,parts,'Head',uv)
    small('MouthCrease',(0,.45,2.51),(.21,.055,.042),hide,parts,'Head',uv)
    small('LowerLip',(0,.43,2.45),(.23,.10,.065),flesh,parts,'Jaw',uv)
    for i,x in enumerate([-.48,-.24,0,.24,.48],1):
        tube(f'ChestCord_{i}',[(x,.41,2.34),(x*.78,.38,1.51)],[(0,.040),(.2,.052),(.6,.035),(1,.012)],flesh,parts,'Chest',uv,8)

def deep_hunter(parts):
    dorsal=bpy.data.materials['DeepHunter_Dorsal'] if 'DeepHunter_Dorsal' in bpy.data.materials else bpy.data.objects['Head'].data.materials[0]
    uv='DeepHunterUV'
    # Overlapping joint collars replace the old chain of separated ovoids.
    specs=[('Head','Head',[(1.55,.82,.56),(2.05,1.00,.69),(2.55,.89,.62),(3.03,.62,.42),(3.48,.32,.24)]),
           ('Shoulders','Spine_1',[(.10,.88,.66),(.65,1.00,.76),(1.20,.98,.75),(1.84,.83,.57)]),
           ('TrunkA','Spine_2',[(-.98,.72,.57),(-.45,.82,.65),(.20,.89,.67),(.68,.94,.71)]),
           ('TrunkB','Spine_3',[(-1.80,.53,.43),(-1.2,.70,.55),(-.63,.78,.61),(-.28,.83,.64)]),
           ('PeduncleA','Spine_4',[(-2.4,.35,.29),(-1.9,.49,.40),(-1.4,.62,.49),(-1.1,.69,.55)]),
           ('PeduncleB','Spine_5',[(-2.98,.22,.23),(-2.6,.31,.29),(-2.2,.41,.34),(-1.9,.49,.41)]),
           ('TailBase','Tail_1',[(-3.43,.13,.19),(-3.1,.20,.23),(-2.8,.27,.27),(-2.57,.31,.29)])]
    for name,bone,rings in specs:surface(name,[(y,rx,rz,0,.04) for y,rx,rz in rings],dorsal,parts,bone,24,'Y',uv,.018)
    fin=bpy.data.objects['PectoralFin_L'].data.materials[0]
    for s,side in [('L',-1),('R',1)]:
        # Sculpted ray ribs provide a structural fin silhouette, not a floating quad.
        for i in range(6):
            tube(f'FinRay_{s}_{i}',[(side*.68,1.02-i*.13,-.12),(side*(1.70-i*.073),.55-i*.18,-.22)],[(0,.031),(.65,.023),(1,.008)],fin,parts,'Fin_'+s,uv,6)
        mouth=bpy.data.objects['OralCavity'].data.materials[0]
        small('EyeSocket_'+s,(side*.77,2.73,.31),(.16,.12,.15),mouth,parts,'Head',uv)
        # Backward swept gill covers sit on the head/shoulder junction.
        for i in range(3):
            tube(f'GillSlit_{s}_{i}',[(side*(.86-i*.015),1.58+i*.13,.42),(side*(.91-i*.015),1.36+i*.13,-.30)],[(0,.015),(.5,.035),(1,.012)],mouth,parts,'Head',uv,6)

def construct(parts,kind):
    prefix='FG' if kind=='furnace-golem' else 'CG' if kind=='cryolith-guardian' else 'rift-colossus'
    rift=kind=='rift-colossus'; furnace=kind=='furnace-golem'
    names={'torso':prefix+('_breast' if rift else '_Torso'),'pelvis':prefix+('_pelvis' if rift else '_Pelvis'),'head':prefix+('_head' if rift else '_Head')}
    material=bpy.data.objects[names['torso']].data.materials[0];bone='Body' if rift else 'Torso'
    uv='FractureUV' if rift else 'ApexUV';width=.70 if rift else .52
    # Chamfered strata alternate widths and lateral offsets, making an assembled
    # rock body rather than a softened sphere or a rectangular toy chassis.
    surface(names['torso'],[(.76,width*.57,.23,.02,0),(.88,width*.78,.28,-.02,0),(1.06,width*.96,.31,.02,0),(1.14,width*.85,.28,-.02,.01),(1.34,width,.34,0,0),(1.43,width*.87,.29,.03,-.02),(1.61,width*.67,.26,0,-.03)],material,parts,bone,10,uvname=uv,irregular=.09)
    surface(names['pelvis'],[(.41,.25,.21,0,0),(.50,.38,.27,.02,0),(.65,.44,.28,0,0),(.82,.32,.24,0,0)],bpy.data.objects[names['pelvis']].data.materials[0],parts,'Body' if rift else 'Pelvis',10,uvname=uv,irregular=.09)
    surface(names['head'],[(1.60,.19,.19,0,.02),(1.72,.29,.24,.015,.04),(1.88,.28,.22,-.02,.03),(2.00,.17,.15,0,0)],material,parts,'Body' if rift else 'Head',8,uvname=uv,irregular=.065)
    for side,s in [(-1,'L'),(1,'R')]:
        for label in (['Arm','Forearm','Leg'] if rift else ['Upper','Lower','UpperLeg','LowerLeg']):
            name=prefix+'_'+label+s if rift else prefix+'_'+label+'_'+s
            obj=bpy.data.objects[name];bpy.context.view_layer.update()
            # Original cylinder orientation supplies the existing rig endpoints.
            bounds=[obj.matrix_world@Vector(v) for v in obj.bound_box]
            center=sum(bounds,Vector())/8;axis=obj.matrix_world.to_3x3()@Vector((0,0,1));axis.normalize()
            length=obj.dimensions.z
            radius=.18 if label in ('Arm','Forearm','Upper','Lower') else .16
            oldmat=obj.data.materials[0]
            tube(name,[center-axis*length*.52,center+axis*length*.52],[(0,radius*.66),(.13,radius*1.1),(.35,radius*1.24),(.52,radius*.95),(.71,radius*1.18),(.88,radius*.87),(1,radius*.64)],oldmat,parts,parts[name],uv,8)
        # A recessed brow and eyes contrast the broad chest without floating lights.
        darkname=prefix+('_cavity' if rift else '_FurnaceMouth' if furnace else '_Core')
        dark=bpy.data.objects[darkname].data.materials[0]
        for i in (-1,1):small(f'{prefix}_EyeSlit_{s}_{i}',(side*.10,.253,1.82),(.055,.025,.022),dark,parts,'Body' if rift else 'Head',uv)
        for i in range(3):
            small(f'{prefix}_Knuckle_{s}_{i}',(side*(.88 if rift else .76)+(i-1)*.07,.38,.76 if not rift else .55),(.052,.09,.065),material,parts,('Forearm'+s) if rift else 'Lower_'+s,uv)
    if furnace:
        # Masonry around the furnace cavity establishes a chest opening.
        for label,a,b in [('L',(-.29,.37,.98),(-.29,.37,1.54)),('R',(.29,.37,.98),(.29,.37,1.54)),('Top',(-.29,.37,1.54),(.29,.37,1.54)),('Bottom',(-.29,.37,.98),(.29,.37,.98))]:
            tube('FG_FurnaceRim_'+label,[a,b],[(0,.07),(.2,.085),(.8,.08),(1,.07)],material,parts,'Torso',uv,8)
    else:
        crystal=bpy.data.objects[prefix+('_fracture-heart' if rift else '_IcePlate_1')].data.materials[0]
        for side,s in [(-1,'L'),(1,'R')]:
            for i in range(3):
                tube(f'{prefix}_ShoulderCrystal_{s}_{i}',[(side*(width-.09+i*.10),-.08+i*.10,1.47),(side*(width+.16+i*.09),-.14+i*.12,1.91+i*.06)],[(0,.08),(.55,.105),(1,.008)],crystal,parts,'Arm'+s if rift else 'Upper_'+s,uv,5)

def orchard(parts):
    prefix='CO';uv='ApexUV'
    core=bpy.data.objects.get(prefix+'_RootCore')
    if core is None:
        core=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.endswith('_RootCore'));prefix=core.name.removesuffix('_RootCore')
    flesh=core.data.materials[0];rot=bpy.data.objects[prefix+'_Root_1'].data.materials[0]
    surface(prefix+'_RootCore',[(.02,.29,.27,0,0),(.12,.49,.40,0,-.02),(.30,.38,.32,0,0),(.45,.22,.23,0,-.03)],flesh,parts,'Root',20,uvname=uv,irregular=.10)
    for i in range(8):
        a=math.tau*i/8
        tube(f'{prefix}_Root_{i+1}',[(math.cos(a)*.18,math.sin(a)*.18,.25),(math.cos(a)*1.05,math.sin(a)*1.05,.04)],[(0,.12),(.18,.10),(.4,.075),(.68,.050),(1,.013)],rot,parts,'Root',uv,10)
    for i in range(4):
        x=(i-1.5)*.25;y=-.08+.11*(i%2);bone=f'Stalk_{i+1}';name=prefix+'_'+bone
        surface(name,[(.26,.15,.13,x,y),(.50,.13,.11,x+.04,y-.015),(.76,.10,.09,x-.025,y+.04),(1.0,.13,.105,x+.025,y+.015),(1.27,.16,.14,x,y)],flesh,parts,bone,14,uvname=uv,irregular=.085)
        for k in range(5):
            a=math.tau*k/5
            # Curved sepals fold around the maw rather than five flat ellipsoids.
            o=surface(f'{name}_Petal_{k+1}',[(0,.08,.035,0,0),(.12,.13,.045,.02,0),(.27,.115,.035,-.03,0),(.36,.055,.024,-.08,0),(.41,.008,.008,-.10,0)],rot,parts,bone,10,uvname=uv)
            o.matrix_world=Matrix.Translation(Vector((x+math.cos(a)*.09,y+math.sin(a)*.09,1.24)))@Matrix.Rotation(a,4,'Z')@Matrix.Rotation(-.9,4,'Y')

def improve(model_id,parts):
    name=model_id.removeprefix('underworld-creature-')
    if name=='crowncap-brute':crowncap(parts)
    elif name=='deep-hunter':deep_hunter(parts)
    elif name in ('furnace-golem','cryolith-guardian','rift-colossus'):construct(parts,name)
    elif name=='corpse-orchard':orchard(parts)
    else:return
    bpy.context.scene['apex_fidelity_pass']='profiled-anatomy-v1'
