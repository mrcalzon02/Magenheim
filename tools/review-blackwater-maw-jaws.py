from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-blackwater-maw-trim.py').read_text().replace('blackwater-maw-refinement-r4','blackwater-maw-jaws-r5')
exec(compile(source,str(__file__),'exec'))
