import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/source/underworld-creature-corpse-orchard.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
print(json.dumps(dict(bones=[dict(name=b.name,parent=b.parent.name if b.parent else None,head=list(b.head_local),tail=list(b.tail_local)) for b in arm.data.bones],actions=str(bpy.context.scene['magenheim_authored_actions'])),indent=2))
