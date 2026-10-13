"""Correct the saved r5/r6 side cameras: creature front is screen-left.

Render the actual saved models from -X, preserving opposite-facing evidence.
No image mirroring, source geometry edits or new silhouette claims.
"""
from pathlib import Path
import shutil,json,hashlib
ROOT=Path(__file__).resolve().parents[1]
for revision in ('rift-titan-torso-r5','rift-titan-wrap-r6'):
 out=ROOT/'docs/art/creature-models/2026-10-13'/revision
 row=json.loads((out/'refinement-metrics.json').read_text());ID=row['id']
 old=out/(ID+'--side.png');preserved=out/(ID+'--side-right-facing.png')
 if not preserved.exists():shutil.copy2(old,preserved)
 source=(ROOT/'tools/review-first-bloom-refinement.py').read_text(encoding='utf-8')
 source=source.replace('first-bloom-refinement',revision).replace('creature-models/2026-10-04','creature-models/2026-10-13').replace("row.get('study_height_metres',4.65)","row.get('study_height_metres',7.0)")
 start=source.index('for view,direction in [');end=source.index(':\n',start)
 source=source[:start]+"for view,direction in [('side',(-2,0,.06))]"+source[end:]
 source=source.replace(" scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))", " assert cam.rotation_euler.to_matrix().col[0].dot(Vector((0,1,0)))<0, 'front +Y must project to screen-left'\n scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))")
 # Side-only output does not overwrite the existing full verification report.
 source=source[:source.index("data=(OUT/(ID+'.glb')).read_bytes()")]
 exec(compile(source,str(__file__),'exec'))
 (out/'side-orientation.json').write_text(json.dumps(dict(source=row['source'],source_sha256=hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest(),camera_direction=[-2,0,.06],model_front_axis='+Y',screen_direction='front/head left, back right',method='actual saved model rendered from -X; no raster mirroring',preserved_opposite_view=preserved.name),indent=2)+'\n',encoding='utf-8')
print('VERIFIED left-facing saved-model side views for r5 and r6')
