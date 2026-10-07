from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/render-first-bloom-precision-closeups.py').read_text()
source=source.replace("OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-precision-r4'","OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-frills-r6'")
source=source.replace("[('before','underworld-boss-first-bloom-concept-r3'),('after','underworld-boss-first-bloom-precision-r4')]","[('before','underworld-boss-first-bloom-attachments-r5'),('after','underworld-boss-first-bloom-frills-r6')]")
exec(compile(source,str(ROOT/'tools/render-first-bloom-precision-closeups.py'),'exec'),{'__file__':str(ROOT/'tools/render-first-bloom-precision-closeups.py'),'__name__':'__main__'})
