from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-feet-r15'
OUT.mkdir(parents=True,exist_ok=True)
source=(ROOT/'tools/render-first-bloom-precision-closeups.py').read_text().replace('first-bloom-precision-r4','first-bloom-feet-r15').replace('first-bloom-concept-r3','first-bloom-trim-r14')
source=source.replace("[('caps',2.93,size*.40),('tissue',2.16,size*.27)]", "[('feet-front',.25,size*.48),('feet-angle',.25,size*.48),('feet-side',.25,size*.48)]")
source=source.replace('Vector((1,2,.35))*span*2',"Vector((0,2,.30) if name=='feet-front' else ((2,0,.30) if name=='feet-side' else (1,2,.55)))*span*2")
source=source.replace('scene.cycles.samples=32','scene.cycles.samples=24')
exec(compile(source,str(__file__),'exec'))
