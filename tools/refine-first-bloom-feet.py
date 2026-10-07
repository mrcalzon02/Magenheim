from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-membranes.py').read_text().replace('first-bloom-membranes-r13','first-bloom-feet-r15-raw').replace('first_bloom_membrane_detail','first_bloom_feet_sculpt')
exec(compile(source,str(__file__),'exec'))
