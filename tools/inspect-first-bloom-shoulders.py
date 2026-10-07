import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-first-bloom-sides-r18.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
scale=bpy.context.scene['refinement_head_focus'][2]/2.22
print('VERIFIED authored shoulder coordinates '+json.dumps({n:dict(head=[x/scale for x in arm.data.bones[n].head_local],tail=[x/scale for x in arm.data.bones[n].tail_local]) for n in ('UpperArm_L','ForeArm_L','UpperArm_R','ForeArm_R','Chest')}),flush=True)
