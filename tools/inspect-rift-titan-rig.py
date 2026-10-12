import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/source/underworld-creature-rift-colossus.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
print('VERIFIED RIFT_RIG',json.dumps(dict(bones=[dict(name=b.name,head=list(b.head_local),tail=list(b.tail_local),parent=b.parent.name if b.parent else None) for b in arm.data.bones],actions=str(bpy.context.scene.get('magenheim_authored_actions','')))),flush=True)
