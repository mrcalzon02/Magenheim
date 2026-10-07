from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-first-bloom-trim.py').read_text().replace('first-bloom-trim-r14','blackwater-maw-refinement-r4')
old="exec(compile(source,str(__file__),'exec'))\n(OUT/'trim-boundary-verification.json')"
new="source=source.replace(\"assert abs(hi.z-lo.z-row.get('study_height_metres',4.65))<.005\",\"assert abs(hi.y-lo.y-10)<.005\")\nexec(compile(source,str(__file__),'exec'))\n(OUT/'trim-boundary-verification.json')"
assert old in source;source=source.replace(old,new)
exec(compile(source,str(__file__),'exec'))
