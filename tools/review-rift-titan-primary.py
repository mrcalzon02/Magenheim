from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/review-first-bloom-refinement.py').read_text(encoding='utf-8').replace('first-bloom-refinement','rift-titan-primary-r3').replace('creature-models/2026-10-04','creature-models/2026-10-12').replace("row.get('study_height_metres',4.65)","row.get('study_height_metres',7.0)")
exec(compile(s,str(__file__),'exec'))
