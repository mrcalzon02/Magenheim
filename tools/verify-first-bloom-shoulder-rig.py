"""Compare the saved skeleton before and after arm-top compression."""
import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
snapshots=[]
for revision in ('first-bloom-sides-r18','first-bloom-shoulders-r19'):
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies'/('underworld-boss-'+revision+'.blend')))
 arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 snapshots.append({b.name:dict(parent=b.parent.name if b.parent else None,head=list(b.head_local),tail=list(b.tail_local),matrix=[v for row in b.matrix_local for v in row]) for b in arm.data.bones})
assert snapshots[0].keys()==snapshots[1].keys()
worst=0
for name,before in snapshots[0].items():
 after=snapshots[1][name];assert before['parent']==after['parent']
 for key in ('head','tail','matrix'):worst=max(worst,max(abs(a-b) for a,b in zip(before[key],after[key])))
assert worst<1e-6
out=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-shoulders-r19/shoulder-rig-verification.json'
out.write_text(json.dumps(dict(result='PASS unchanged saved skeleton',bones=len(snapshots[0]),max_coordinate_difference=worst,scope='bone hierarchy and rest transforms; skin weights intentionally revised at shoulders'),indent=2)+'\n')
print('VERIFIED saved shoulder skeleton unchanged',len(snapshots[0]),'bones',worst,flush=True)
