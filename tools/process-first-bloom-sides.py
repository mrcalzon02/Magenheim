from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/process-first-bloom-pelvic.py').read_text().replace('refine-first-bloom-pelvic.py','refine-first-bloom-sides.py').replace('first-bloom-pelvic-r17','first-bloom-sides-r18')
exec(compile(source,str(__file__),'exec'))
