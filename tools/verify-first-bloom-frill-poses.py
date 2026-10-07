from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/verify-first-bloom-attachment-poses.py').read_text().replace('first-bloom-attachments-r5','first-bloom-frills-r6')
exec(compile(source,str(ROOT/'tools/verify-first-bloom-attachment-poses.py'),'exec'),{'__file__':str(ROOT/'tools/verify-first-bloom-attachment-poses.py'),'__name__':'__main__'})
