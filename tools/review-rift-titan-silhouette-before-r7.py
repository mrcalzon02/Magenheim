"""Render preserved r6 with the same front and left-facing r7 comparison cameras."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-first-bloom-refinement.py').read_text(encoding='utf-8').replace('first-bloom-refinement','rift-titan-wrap-r6').replace('creature-models/2026-10-04','creature-models/2026-10-13').replace("row.get('study_height_metres',4.65)","row.get('study_height_metres',7.0)")
source=source.replace("bpy.ops.wm.open_mainfile", "OUT=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-silhouette-r7/before'\nOUT.mkdir(parents=True,exist_ok=True)\nbpy.ops.wm.open_mainfile")
start=source.index('for view,direction in [');end=source.index(':\n',start)
source=source[:start]+"for view,direction in [('front-clay',(0,2,.06)),('side-clay',(-2,0,.06)),('side-reference',(-2,.65,.06))]"+source[end:]
source=source.replace("view=='clay'","view in ('front-clay','side-clay')")
source=source.replace(" scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))"," if view!='front-clay':assert cam.rotation_euler.to_matrix().col[0].dot(Vector((0,1,0)))<0, 'front must project screen-left'\n scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))")
source=source[:source.index("data=(OUT/(ID+'.glb')).read_bytes()")]
exec(compile(source,str(__file__),'exec'))
