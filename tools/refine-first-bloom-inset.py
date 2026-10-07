from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-seated.py').read_text().replace('first-bloom-seated-r9','first-bloom-inset-r11').replace('first_bloom_cavity_attachment','first_bloom_deeper_chest').replace('first-bloom-fan-r8','first-bloom-looming-r10')
exec(compile(source,str(__file__),'exec'))
