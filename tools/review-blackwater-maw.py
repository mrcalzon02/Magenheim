from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-first-bloom-refinement.py').read_text().replace('first-bloom-refinement','blackwater-maw-refinement-r3').replace('First Bloom','Blackwater Maw')
source=source.replace("assert abs(hi.z-lo.z-row.get('study_height_metres',4.65))<.005", "assert abs(hi.y-lo.y-10)<.005")
exec(compile(source,str(__file__),'exec'))
