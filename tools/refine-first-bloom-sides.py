from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-pelvic.py').read_text().replace('first-bloom-pelvic-r17-raw','first-bloom-sides-r18-raw').replace('first_bloom_pelvic_shelves','first_bloom_side_shelves')
exec(compile(source,str(__file__),'exec'))
