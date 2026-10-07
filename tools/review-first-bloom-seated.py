from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-first-bloom-attachments.py').read_text().replace('first-bloom-attachments-r5','first-bloom-seated-r9').replace('groups=len(records)//2',"groups=len(set(r['tissue'] for r in records))")
exec(compile(source,str(__file__),'exec'))
