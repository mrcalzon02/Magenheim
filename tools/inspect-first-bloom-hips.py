import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-first-bloom-feet-r15.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
print('VERIFIED hip bone coordinates '+json.dumps({n:dict(head=list(arm.data.bones[n].head_local),tail=list(arm.data.bones[n].tail_local)) for n in ('Pelvis','Chest','Thigh_L','Shin_L','Foot_L','Thigh_R')}),flush=True)
print('VERIFIED scale '+str(bpy.context.scene['refinement_head_focus'][2]/2.22),flush=True)
