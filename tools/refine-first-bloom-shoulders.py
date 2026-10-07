from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-sides.py').read_text().replace('first-bloom-sides-r18-raw','first-bloom-shoulders-r19-raw').replace('first_bloom_side_shelves','first_bloom_shoulder_sculpt')
exec(compile(source,str(__file__),'exec'))
