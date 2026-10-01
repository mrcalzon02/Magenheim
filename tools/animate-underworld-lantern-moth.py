#!/usr/bin/env python3
"""Author Lantern Moth HOST-INSECT-FLY actions. World translation remains runtime-owned."""
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[1]
BLEND=ROOT/'assets/models/source/underworld-creature-lantern-moth.blend'
if not BLEND.exists(): raise RuntimeError(f'Missing {BLEND}; run author-underworld-lantern-moth first')
bpy.ops.wm.open_mainfile(filepath=str(BLEND))
arm=bpy.data.objects.get('RIG_LanternMoth_HOST_INSECT_FLY')
if not arm: raise RuntimeError('Lantern Moth rig missing')
BONES=['Thorax','Abdomen','Head','ForeWing_L','ForeWing_R','HindWing_L','HindWing_R','Antenna1_L','Antenna1_R','Antenna2_L','Antenna2_R']

def curves(act):
    direct=getattr(act,'fcurves',None)
    if direct is not None:return list(direct)
    out=[]
    for layer in getattr(act,'layers',[]):
        for strip in getattr(layer,'strips',[]):
            for bag in getattr(strip,'channelbags',[]):out.extend(bag.fcurves)
    return out

def action(name,end,poses,linear=False):
    old=bpy.data.actions.get(name)
    if old:bpy.data.actions.remove(old)
    act=bpy.data.actions.new(name);act.use_fake_user=True;arm.animation_data_create();arm.animation_data.action=act
    keyed=set(k for p in poses.values() for k in p)
    for frame,pose in sorted(poses.items()):
        for b in keyed:
            pb=arm.pose.bones[b];pb.rotation_mode='XYZ';pb.rotation_euler=(0,0,0)
        for b,rot in pose.items():arm.pose.bones[b].rotation_euler=rot
        for b in keyed:arm.pose.bones[b].keyframe_insert('rotation_euler',frame=frame,group=b)
    for fc in curves(act):
        for kp in fc.keyframe_points:kp.interpolation='LINEAR' if linear else 'BEZIER'
    act.frame_start=1;act.frame_end=end;return act

def flap(a=.72,body=.035):
    return {'ForeWing_L':(0,a,0),'ForeWing_R':(0,-a,0),'HindWing_L':(0,a*.78,0),'HindWing_R':(0,-a*.78,0),'Thorax':(body,0,0),'Abdomen':(-body*.7,0,0),'Antenna1_L':(.04,0,.05),'Antenna1_R':(.04,0,-.05)}
def down(a=.58):
    p=flap(-a,-.025);p['Head']=(.025,0,0);return p
neutral={b:(0,0,0) for b in BONES}

action('LanternMoth_Hover',24,{1:flap(.60),7:down(.54),13:flap(.60),19:down(.54),24:flap(.60)},True)
action('LanternMoth_Flight',16,{1:flap(.82,.05),5:down(.72),9:flap(.82,.05),13:down(.72),16:flap(.82,.05)},True)
left=flap(.65);left.update({'Thorax':(0,0,.23),'Head':(0,0,.12),'ForeWing_R':(0,-.90,0),'ForeWing_L':(0,.45,0)})
right=flap(.65);right.update({'Thorax':(0,0,-.23),'Head':(0,0,-.12),'ForeWing_L':(0,.90,0),'ForeWing_R':(0,-.45,0)})
action('LanternMoth_BankLeft',20,{1:neutral,6:left,14:left,20:neutral})
action('LanternMoth_BankRight',20,{1:neutral,6:right,14:right,20:neutral})
action('LanternMoth_Takeoff',28,{1:neutral,7:flap(.35),13:down(.55),19:flap(.82,.06),24:down(.75),28:flap(.82,.05)})
fold={'ForeWing_L':(.12,.12,-.65),'ForeWing_R':(.12,-.12,.65),'HindWing_L':(.08,.10,-.45),'HindWing_R':(.08,-.10,.45),'Thorax':(.08,0,0)}
action('LanternMoth_Land',32,{1:flap(.65),9:down(.45),17:flap(.30),25:fold,32:fold})
idle=dict(fold);idle.update({'Antenna1_L':(.07,0,.06),'Antenna1_R':(.07,0,-.06)})
action('LanternMoth_GroundIdle',56,{1:fold,18:idle,36:fold,56:idle})
alert=flap(.95,.08);alert.update({'Head':(-.10,0,0),'Antenna1_L':(-.08,0,.16),'Antenna1_R':(-.08,0,-.16)})
action('LanternMoth_AlertFlee',20,{1:neutral,4:alert,9:down(.88),14:alert,20:down(.88)},True)
action('LanternMoth_Hit',14,{1:neutral,4:{'Thorax':(.14,0,.25),'ForeWing_L':(0,.30,.20),'ForeWing_R':(0,-.55,-.20)},8:{'Thorax':(-.06,0,-.10)},14:neutral})
death={'Thorax':(.45,0,1.10),'Abdomen':(-.35,0,.25),'ForeWing_L':(.15,.12,-.65),'ForeWing_R':(.15,-.12,.65),'HindWing_L':(.10,.08,-.45),'HindWing_R':(.10,-.08,.45)}
action('LanternMoth_Death',42,{1:neutral,9:down(.30),20:death,42:death})
arm.animation_data.action=None
for pb in arm.pose.bones:
 pb.rotation_euler=(0,0,0);pb.location=(0,0,0);pb.scale=(1,1,1)
bpy.context.view_layer.update()
arm['production_contract']='production-creature-r1'
arm['authored_actions']=','.join('LanternMoth_'+name for name in ('Hover','Flight','BankLeft','BankRight','Takeoff','Land','GroundIdle','AlertFlee','Hit','Death'))
sc=bpy.context.scene; sc['magenheim_authored_actions']=arm['authored_actions']; sc['magenheim_skinning']='rigid-segment-weighted'
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
print('AUTHORED 10 Lantern Moth HOST-INSECT-FLY actions; four-wing flap/bank/landing contract, no root translation')
