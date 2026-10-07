from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-seated.py').read_text().replace('first-bloom-seated-r9','first-bloom-membranes-r13').replace('first_bloom_cavity_attachment','first_bloom_membrane_detail').replace('first-bloom-fan-r8','first-bloom-medallion-r12')
exec(compile(source,str(__file__),'exec'))
