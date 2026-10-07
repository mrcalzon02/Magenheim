from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-attachments.py').read_text().replace('first-bloom-attachments-r5','first-bloom-radiating-r7')
source=source.replace('from first_bloom_attachment_repair import build,materials,unwrap_atlas','from first_bloom_radiating_frills import build,materials,unwrap_atlas')
exec(compile(source,str(__file__),'exec'))
