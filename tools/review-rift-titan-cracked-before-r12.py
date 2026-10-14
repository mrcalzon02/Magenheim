"""Render the saved r11 head with matching front and left-facing close-up cameras."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
out=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-cracked-head-r12/before';out.mkdir(exist_ok=True)
source=(ROOT/'tools/review-first-bloom-refinement.py').read_text(encoding='utf-8').replace('first-bloom-refinement','rift-titan-cracked-head-r12/before').replace('creature-models/2026-10-04','creature-models/2026-10-14').replace("row.get('study_height_metres',4.65)","row.get('study_height_metres',7.0)")
source=source.replace("row=json.loads((OUT/'refinement-metrics.json').read_text())","row=json.loads((ROOT/'docs/art/creature-models/2026-10-14/rift-titan-notched-r11/refinement-metrics.json').read_text())")
start=source.index('for view,direction in [');end=source.index(' target=center;span=size',start)
source=source[:start]+"for view,direction in [('crown-detail',(1,2,.15)),('crown-side',(-2,.65,.05))]:\n"+source[end:]
source=source.replace(' target=center;span=size',' target=center+Vector((0,0,size*.30));span=size*.48')
source=source.replace(" scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))"," if view=='crown-side':assert cam.rotation_euler.to_matrix().col[0].dot(Vector((0,1,0)))<0\n scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))")
source=source[:source.index('data=(OUT/(ID+')]
exec(compile(source,str(__file__),'exec'))
(OUT/'detail-provenance.json').write_text(json.dumps(dict(source=row['source'],source_sha256=hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest(),cameras={'crown-detail':[1,2,.15],'crown-side':[-2,.65,.05]},target='actor bounds center plus 0.30 actor span in Z',span='0.48 actor span',scope='actual saved r11 source re-rendered into r12; previous study unchanged'),indent=2)+'\n',encoding='utf-8')
