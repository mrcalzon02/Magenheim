from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/render-first-bloom-fan-closeups.py').read_text().replace('first-bloom-fan-r8','first-bloom-inset-r11').replace('first-bloom-radiating-r7','first-bloom-looming-r10')
exec(compile(source,str(__file__),'exec'))
