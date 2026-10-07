from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-bundles.py').read_text().replace('first-bloom-bundles-r21-raw','first-bloom-arm-seams-r22-raw').replace('first_bloom_head_bundles','first_bloom_arm_seam_repair')
exec(compile(source,str(__file__),'exec'))
