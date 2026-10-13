from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-rift-titan-primary.py').read_text(encoding='utf-8').replace('rift-titan-primary-r3','rift-titan-strata-r4').replace('creature-models/2026-10-12','creature-models/2026-10-13')
exec(compile(source,str(__file__),'exec'))
