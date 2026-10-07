from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-feet.py').read_text().replace('first-bloom-feet-r15-raw','first-bloom-hips-r16-raw').replace('first_bloom_feet_sculpt','first_bloom_hip_sculpt')
exec(compile(source,str(__file__),'exec'))
