from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/render-first-bloom-precision-closeups.py').read_text().replace('first-bloom-precision-r4','first-bloom-head-r20').replace('first-bloom-concept-r3','first-bloom-shoulders-r19')
source=source.replace("[('caps',2.93,size*.40),('tissue',2.16,size*.27)]", "[('head-front',2.97,size*.50),('head-angle',2.97,size*.50),('head-side',2.97,size*.50)]")
source=source.replace('Vector((1,2,.35))*span*2',"Vector((0,2,.15) if name=='head-front' else ((2,0,.15) if name=='head-side' else (1,2,.30)))*span*2")
source=source.replace('scene.cycles.samples=32','scene.cycles.samples=24')
exec(compile(source,str(__file__),'exec'))
