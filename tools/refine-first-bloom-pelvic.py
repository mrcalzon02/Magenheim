from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-hips.py').read_text().replace('first-bloom-hips-r16-raw','first-bloom-pelvic-r17-raw').replace('first_bloom_hip_sculpt','first_bloom_pelvic_shelves')
exec(compile(source,str(__file__),'exec'))
