from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/render-first-bloom-hips-closeups.py').read_text().replace('first-bloom-hips-r16','first-bloom-sides-r18').replace('first-bloom-feet-r15','first-bloom-pelvic-r17')
exec(compile(source,str(__file__),'exec'))
