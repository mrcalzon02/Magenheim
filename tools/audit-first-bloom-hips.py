from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/audit-first-bloom-buried.py').read_text().replace('first-bloom-membranes-r13','first-bloom-hips-r16-raw').replace('first-bloom-trim-r14','first-bloom-hips-r16')
exec(compile(source,str(__file__),'exec'))
