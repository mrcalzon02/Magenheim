from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-shoulders.py').read_text().replace('first-bloom-shoulders-r19-raw','first-bloom-head-r20-raw').replace('first_bloom_shoulder_sculpt','first_bloom_head_sculpt')
exec(compile(source,str(__file__),'exec'))
