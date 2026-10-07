from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/render-first-bloom-precision-closeups.py').read_text().replace('first-bloom-precision-r4','blackwater-maw-restored-fins-r7').replace('first-bloom-concept-r3','blackwater-maw-jaws-r5')
source=source.replace("[('caps',2.93,size*.40),('tissue',2.16,size*.27)]", "[('fin-inventory',.90,7.3),('tail-fan',.10,5.8)]")
source=source.replace("scale=scene['refinement_head_focus'][2]/2.22", "scale=1.0")
source=source.replace('Vector((0,.19*scale,z*scale))',"Vector((0,-4.80,z)) if name=='tail-fan' else Vector((0,-.90,z))")
source=source.replace('Vector((1,2,.35))*span*2',"Vector((2,.10,.15))*span*2")
source=source.replace('scene.cycles.samples=32','scene.cycles.samples=24')
exec(compile(source,str(__file__),'exec'))
