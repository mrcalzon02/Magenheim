"""Retain the pilot's matched cameras and checks for concept revision 3."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-first-bloom-refinement.py').read_text()
source=source.replace('first-bloom-refinement','first-bloom-concept-r3')
source=source.replace("span=size*.43","span=size*.52")
exec(compile(source,str(ROOT/'tools/review-first-bloom-refinement.py'),'exec'),{'__file__':str(ROOT/'tools/review-first-bloom-refinement.py'),'__name__':'__main__'})
