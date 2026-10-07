from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/render-first-bloom-precision-closeups.py').read_text().replace('first-bloom-precision-r4','first-bloom-shoulders-r19').replace('first-bloom-concept-r3','first-bloom-sides-r18')
source=source.replace("[('caps',2.93,size*.40),('tissue',2.16,size*.27)]", "[('shoulders-front',2.22,size*.48),('shoulders-angle',2.22,size*.48),('shoulders-rear',2.22,size*.48)]")
source=source.replace('Vector((1,2,.35))*span*2',"Vector((0,2,.12) if name=='shoulders-front' else ((1,-2,.25) if name=='shoulders-rear' else (1,2,.30)))*span*2")
source=source.replace('scene.cycles.samples=32','scene.cycles.samples=24')
exec(compile(source,str(__file__),'exec'))
