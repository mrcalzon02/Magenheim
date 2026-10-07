from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
for filename in ('refine-first-bloom-arm-seams.py','restore-first-bloom-head-body-scale.py','audit-first-bloom-buried.py','trim-first-bloom-buried.py','verify-first-bloom-attachment-poses.py'):
 source=(ROOT/'tools'/filename).read_text()
 if filename!='refine-first-bloom-arm-seams.py':source=source.replace('first-bloom-head-r20','first-bloom-arm-seams-r22').replace('first-bloom-membranes-r13','first-bloom-arm-seams-r22-raw').replace('first-bloom-trim-r14','first-bloom-arm-seams-r22').replace('first-bloom-attachments-r5','first-bloom-arm-seams-r22')
 exec(compile(source,str(ROOT/'tools'/filename),'exec'))
