from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/render-first-bloom-precision-closeups.py').read_text().replace('first-bloom-precision-r4','first-bloom-fan-r8').replace('first-bloom-concept-r3','first-bloom-radiating-r7')
source=source.replace("[('caps',2.93,size*.40),('tissue',2.16,size*.27)]", "[('tissue-front',1.91,size*.27),('tissue',1.91,size*.27),('tissue-depth',1.91,size*.27)]")
source=source.replace("Vector((1,2,.35))*span*2", "Vector((0,2,.08) if name=='tissue-front' else ((1.8,1,.20) if name=='tissue-depth' else (1,2,.35)))*span*2")
source=source.replace('scene.cycles.samples=32','scene.cycles.samples=24')
exec(compile(source,str(__file__),'exec'))
