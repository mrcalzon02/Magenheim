from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-blackwater-maw-trim.py').read_text().replace('blackwater-maw-refinement-r4','blackwater-maw-restored-fins-r7')
source=source.replace('assert abs(hi.y-lo.y-10)<.005',"assert abs(hi.y-lo.y-row.get('study_length_metres',10))<.005")
exec(compile(source,str(__file__),'exec'))
