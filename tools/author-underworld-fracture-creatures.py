"""Seven mineral organisms with independent silhouettes, rigid rigs and saved combat actions."""
import bpy, importlib.util, math, sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from underworld_material_library import bind_underworld_material
spec=importlib.util.spec_from_file_location('frozen_anatomy',Path(__file__).with_name('author-underworld-frozen-creatures.py'))
kit=importlib.util.module_from_spec(spec);spec.loader.exec_module(kit)

SPECS={'fracture-wisp':('aerial',.7),'rift-skitter':('crawler',1.1),'shardwing':('winged',1.9),
       'gravity-leech':('worm',1.8),'chasm-stalker':('crawler',2.6),'stonebound':('construct',2.8),
       'rift-colossus':('construct',4.3)}

def author(name,kind,size):
    kit.clear_scene();parts={};bones={'Root':((0,0,0),(0,0,.2),None),
        'Body':((0,0,.2),(0,.2,.5),'Root')}
    rock=kit.mat('Fracture_'+name+'_strata',(.22,.18,.28));bind_underworld_material(bpy,rock,'shardstone')
    crystal=kit.mat('Fracture_'+name+'_facets',(.48,.31,.72));bind_underworld_material(bpy,crystal,'fracture-crystal')
    seam=kit.mat('Fracture_'+name+'_joint',(.15,.12,.17));bind_underworld_material(bpy,seam,'titanbone')
    def keep(o,b):parts[o.name]=b;kit.uv(o,'FractureUV');return o
    def org(label,loc,scale,material=rock,b='Body',sub=2):return keep(kit.organic(name+'_'+label,loc,scale,material,sub),b)
    def segment(label,a,z,r,material=seam,b='Body'):return keep(kit.seg(name+'_'+label,a,z,r,material),b)
    def shard(label,loc,r,d,b='Body',rot=(0,0,0)):return keep(kit.cone(name+'_'+label,loc,r,d,crystal,rot,verts=5),b)
    if kind=='aerial':
        org('hollow-core',(0,0,.45),(.13,.13,.18),crystal)
        for i in range(12):
            a=i*math.tau/12;b='Orbit'+str(i);r=.25+.04*math.sin(i)
            bones[b]=((0,0,.45),(r*math.cos(a),r*math.sin(a),.45),'Body')
            org('floating-shard'+str(i),(r*math.cos(a),r*math.sin(a),.45+.1*math.sin(i*2)),(.05,.08,.12),rock,b)
            shard('orbit-tip'+str(i),(r*math.cos(a),r*math.sin(a),.59),.025,.12,b)
    elif kind=='worm':
        previous='Body'
        for i in range(14):
            b='Ring'+str(i);y=-.7+i*.105;r=.11+.04*math.sin(i*math.pi/13)
            bones[b]=((0,y,.18),(0,y+.09,.18),previous);previous=b
            org('annulus'+str(i),(0,y,.18),(r,.07,r),seam,b)
            for side in (-1,1):shard('dorsal'+str(i)+'_'+str(side),(side*r*.7,y,.30),.033,.14,b,rot=(0,side*.35,0))
        for i in range(10):
            a=i*math.tau/10;segment('radial-tooth'+str(i),(.11*math.cos(a),.78,.18+.11*math.sin(a)),(.05*math.cos(a),.83,.18+.05*math.sin(a)),.018,crystal,'Ring13')
    elif kind=='crawler':
        long=name=='chasm-stalker';org('thorax',(0,0,.35),(.25,.38,.22));org('head',(0,.4,.32),(.15,.18,.12),seam)
        for i in range(7):
            y=-.36+i*.10;org('overlapping-scutum'+str(i),(0,y,.50),(.29,.09,.07),rock)
            shard('dorsal-fin'+str(i),(0,y,.60),.055,.19+i*.012)
        for i in range(4):
            y=-.26+i*.18
            for side in (-1,1):
                s='L' if side<0 else 'R';u=s+'Hip'+str(i);l=s+'Shin'+str(i)
                hip=(side*.18,y,.30);knee=(side*(.75 if long else .43),y-.08,.22);foot=(side*(1.0 if long else .58),y+.13,.035)
                bones[u]=(hip,knee,'Body');bones[l]=(knee,foot,u)
                segment(u,hip,knee,.043,b=u);segment(l,knee,foot,.024,b=l)
                org('knee'+u,knee,(.065,.055,.045),rock,u);shard('claw'+l,foot,.025,.09,l)
        for side in (-1,1):segment('mandible'+str(side),(side*.09,.47,.3),(side*.13,.64,.22),.025,crystal)
    elif kind=='winged':
        org('sternum',(0,0,.36),(.18,.31,.17),seam);org('jaw',(0,.34,.4),(.12,.17,.1),rock)
        for side in (-1,1):
            s='L' if side<0 else 'R';b='Wing'+s;bones[b]=((side*.1,0,.36),(side*.95,0,.36),'Body')
            for feather in range(9):
                a=(feather-4)*.12;tip=(side*(.6+feather*.06),-.1+feather*.07,.35)
                segment('wing-ray'+s+str(feather),(side*.13,0,.35),tip,.025,rock,b)
                keep(kit.box(name+'_mineral-vane'+s+str(feather),tip,(.26,.085,.018),crystal,(0,side*.15,a*side)),b)
        for i in range(6):shard('tail-keel'+str(i),(0,-.3-i*.09,.36),.045,.15)
    else:
        broad=name=='rift-colossus';width=.55 if broad else .37
        org('pelvis',(0,0,.58),(width,.26,.25),rock);org('breast',(0,0,1.20),(width*1.3,.31,.44),rock)
        org('cavity',(0,.30,1.23),(.19,.045,.25),seam);org('fracture-heart',(0,.35,1.23),(.095,.05,.13),crystal)
        org('head',(0,0,1.84),(.19,.18,.21),rock);shard('crown',(0,0,2.12),.16,.33)
        for side in (-1,1):
            s='L' if side<0 else 'R'
            for label,a,z,r in [('Arm',(side*width,0,1.48),(side*(width+.28),0,1.03),.14),
                               ('Forearm',(side*(width+.28),0,1.03),(side*(width+.34),.11,.55),.18),
                               ('Leg',(side*.23,0,.63),(side*.27,0,.08),.14)]:
                b=label+s;parent='Arm'+s if label=='Forearm' else 'Body';bones[b]=(a,z,parent)
                segment(b,a,z,r,rock,b)
                for band in range(4):
                    t=(band+.5)/4;loc=tuple(a[j]+(z[j]-a[j])*t for j in range(3));org('stratum'+b+str(band),loc,(r*1.25,r*1.1,.06),rock,b)
            org('foot'+s,(side*.27,.12,.06),(.19,.27,.07),seam,'Leg'+s)
            org('shoulder'+s,(side*width,0,1.55),(.28,.28,.22),rock,'Arm'+s)
            for i in range(4):shard('shoulder-spire'+s+str(i),(side*(width+i*.05),-.12+i*.08,1.77),.055,.23+i*.04,'Arm'+s)
    arm,root=kit.armature('RIG_'+name+'_FRACTURE');made={'Root':root}
    for b,(h,t,parent) in bones.items():
        if b!='Root':made[b]=kit.bone(arm,b,h,t,made[parent])
    prefix=''.join(x.title() for x in name.split('-'))
    actions=[prefix+'_'+a for a in ('Idle','Walk','Attack','Guard','Hit','Stagger','Death')]
    kit.finalize_rig(arm,parts,'underworld-creature-'+name,actions,kind+' mineral anatomy with articulated strata and own PBR',12)
    animated=[b for b in bones if b not in ('Root','Body')]
    for action in actions:
        kit.begin_action(arm,action)
        end=48 if action.endswith('Death') else 32
        for f in (1,8,16,24,end):
            phase=(f-1)/max(1,end-1);wave=math.sin(phase*math.tau)
            for i,b in enumerate(animated):
                amount=wave*(12 if action.endswith(('Walk','Idle')) else 28)
                kit.key(arm,b,f,(amount*((-1)**i),0,amount*.25))
            kit.key(arm,'Body',f,(0,0,phase*86 if action.endswith('Death') else wave*4))
    arm.animation_data.action=None;kit.reset_pose(arm)
    # Models author in metres; gameplay scaling is compensated by the runtime binder.
    factor=size/2.3 if kind=='construct' else size/(2 if kind=='winged' else 1.5 if kind=='worm' else 2 if name=='chasm-stalker' else .7 if kind=='aerial' else 1.2)
    for o in list(bpy.context.scene.objects):
        if o.type=='MESH':
            for v in o.data.vertices:v.co*=factor
            o.location*=factor
    bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
    for b in arm.data.edit_bones:b.head*=factor;b.tail*=factor
    bpy.ops.object.mode_set(mode='OBJECT');bpy.context.view_layer.update()
    kit.save('underworld-creature-'+name)

for name,(kind,size) in SPECS.items():author(name,kind,size)
