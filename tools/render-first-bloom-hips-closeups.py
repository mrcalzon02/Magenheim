from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/render-first-bloom-precision-closeups.py').read_text().replace('first-bloom-precision-r4','first-bloom-hips-r16').replace('first-bloom-concept-r3','first-bloom-feet-r15')
source=source.replace("[('caps',2.93,size*.40),('tissue',2.16,size*.27)]", "[('hips-front',1.13,size*.38),('hips-angle',1.13,size*.38),('hips-side',1.13,size*.38)]")
source=source.replace('Vector((1,2,.35))*span*2',"Vector((0,2,.15) if name=='hips-front' else ((2,0,.15) if name=='hips-side' else (1,2,.30)))*span*2")
source=source.replace('scene.cycles.samples=32','scene.cycles.samples=24')
exec(compile(source,str(__file__),'exec'))
