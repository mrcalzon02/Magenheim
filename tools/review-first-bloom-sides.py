from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-first-bloom-trim.py').read_text().replace('first-bloom-trim-r14','first-bloom-sides-r18')
exec(compile(source,str(__file__),'exec'))
