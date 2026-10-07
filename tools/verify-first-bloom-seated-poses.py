from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/verify-first-bloom-attachment-poses.py').read_text().replace('first-bloom-attachments-r5','first-bloom-seated-r9')
exec(compile(source,str(__file__),'exec'))
