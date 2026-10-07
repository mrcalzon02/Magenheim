from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/render-first-bloom-frill-closeups.py').read_text().replace('first-bloom-frills-r6','first-bloom-radiating-r7').replace('first-bloom-attachments-r5','first-bloom-frills-r6')
exec(compile(source,str(__file__),'exec'))
