from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/verify-first-bloom-trim-poses.py').read_text().replace('first-bloom-trim-r14','first-bloom-hips-r16')
exec(compile(source,str(__file__),'exec'))
