from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-head.py').read_text().replace('first-bloom-head-r20-raw','first-bloom-bundles-r21-raw').replace('first_bloom_head_sculpt','first_bloom_head_bundles')
exec(compile(source,str(__file__),'exec'))
