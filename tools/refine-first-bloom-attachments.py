"""Repair existing attachments with the prior geometry/bake/export pipeline."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-precision.py').read_text()
source=source.replace('first-bloom-precision-r4','first-bloom-attachments-r5')
source=source.replace('from first_bloom_precision_sculpt import build,materials,unwrap_atlas','from first_bloom_attachment_repair import build,materials,unwrap_atlas')
exec(compile(source,str(ROOT/'tools/refine-first-bloom-precision.py'),'exec'),{'__file__':str(ROOT/'tools/refine-first-bloom-precision.py'),'__name__':'__main__'})
