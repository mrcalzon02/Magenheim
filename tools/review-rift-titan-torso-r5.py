from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-rift-titan-strata-r4.py').read_text(encoding='utf-8').replace('rift-titan-strata-r4','rift-titan-torso-r5')
exec(compile(source,str(__file__),'exec'))
