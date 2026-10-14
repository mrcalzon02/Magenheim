"""Render the preserved r10 source with the new head-detail camera, into r11 only."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
out=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-notched-r11/before';out.mkdir(exist_ok=True)
source=(ROOT/'tools/review-first-bloom-refinement.py').read_text(encoding='utf-8').replace('first-bloom-refinement','rift-titan-notched-r11/before').replace('creature-models/2026-10-04','creature-models/2026-10-14').replace("row.get('study_height_metres',4.65)","row.get('study_height_metres',7.0)")
source=source.replace("row=json.loads((OUT/'refinement-metrics.json').read_text())","row=json.loads((ROOT/'docs/art/creature-models/2026-10-13/rift-titan-layered-r10/refinement-metrics.json').read_text())")
start=source.index('for view,direction in [');end=source.index(' target=center;span=size',start)
source=source[:start]+"for view,direction in [('crown-detail',(1,2,.15))]:\n"+source[end:]
source=source.replace(' target=center;span=size',' target=center+Vector((0,0,size*.30));span=size*.48')
source=source[:source.index('data=(OUT/(ID+')]
exec(compile(source,str(__file__),'exec'))
(OUT/'detail-provenance.json').write_text(json.dumps(dict(source=row['source'],source_sha256=hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest(),camera_direction=[1,2,.15],target='actor bounds center plus 0.30 actor span in Z',span='0.48 actor span',scope='preserved r10 source re-rendered for r11 head/shoulder comparison; original study not modified'),indent=2)+'\n',encoding='utf-8')
