import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/source/underworld-creature-deep-hunter.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
print('VERIFIED Deep Hunter rig '+json.dumps({b.name:dict(head=list(b.head_local),tail=list(b.tail_local)) for b in arm.data.bones}),flush=True)
