"""Precision pass using the same revision 3 bake/export path and topology."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-concept.py').read_text()
source=source.replace('first-bloom-concept-r3','first-bloom-precision-r4')
source=source.replace('from first_bloom_concept_sculpt import build,materials,unwrap_atlas','from first_bloom_precision_sculpt import build,materials,unwrap_atlas')
source=source.replace('previous_triangles=7940','previous_triangles=9188').replace('(1-triangles/7940)*100','(1-triangles/9188)*100')
exec(compile(source,str(ROOT/'tools/refine-first-bloom-concept.py'),'exec'),{'__file__':str(ROOT/'tools/refine-first-bloom-concept.py'),'__name__':'__main__'})
