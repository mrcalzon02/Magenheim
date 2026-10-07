from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-attachments.py').read_text().replace('first-bloom-attachments-r5','first-bloom-frills-r6')
source=source.replace('from first_bloom_attachment_repair import build,materials,unwrap_atlas','from first_bloom_frill_sculpt import build,materials,unwrap_atlas')
exec(compile(source,str(ROOT/'tools/refine-first-bloom-attachments.py'),'exec'),{'__file__':str(ROOT/'tools/refine-first-bloom-attachments.py'),'__name__':'__main__'})
