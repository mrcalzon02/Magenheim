from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-first-bloom-attachments.py').read_text().replace('first-bloom-attachments-r5','first-bloom-frills-r6')
source=source.replace("groups=len(records)//2","groups=len(set(r['tissue'] for r in records))")
exec(compile(source,str(ROOT/'tools/review-first-bloom-attachments.py'),'exec'),{'__file__':str(ROOT/'tools/review-first-bloom-attachments.py'),'__name__':'__main__'})
