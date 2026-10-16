from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/review-rift-titan-concept-body-r17.py').read_text().replace('rift-titan-concept-body-r17','rift-titan-broken-strata-r18')
exec(compile(s,str(__file__),'exec'))
