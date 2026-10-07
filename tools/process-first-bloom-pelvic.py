"""Build, audit, trim and check the revised torso shelves in dependency order."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
for filename in ('refine-first-bloom-pelvic.py','audit-first-bloom-buried.py','trim-first-bloom-buried.py','verify-first-bloom-attachment-poses.py'):
 source=(ROOT/'tools'/filename).read_text()
 source=source.replace('first-bloom-membranes-r13','first-bloom-pelvic-r17-raw').replace('first-bloom-trim-r14','first-bloom-pelvic-r17').replace('first-bloom-attachments-r5','first-bloom-pelvic-r17')
 exec(compile(source,str(ROOT/'tools'/filename),'exec'))
