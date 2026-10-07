from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/verify-first-bloom-radiating-poses.py').read_text().replace('first-bloom-radiating-r7','first-bloom-fan-r8')
exec(compile(source,str(__file__),'exec'))
