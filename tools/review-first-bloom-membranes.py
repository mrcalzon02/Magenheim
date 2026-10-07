from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-first-bloom-seated.py').read_text().replace('first-bloom-seated-r9','first-bloom-membranes-r13')
exec(compile(source,str(__file__),'exec'))
