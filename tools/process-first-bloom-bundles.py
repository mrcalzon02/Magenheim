from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
for filename in ('refine-first-bloom-bundles.py','restore-first-bloom-head-body-scale.py','audit-first-bloom-buried.py','trim-first-bloom-buried.py','verify-first-bloom-attachment-poses.py'):
 source=(ROOT/'tools'/filename).read_text()
 if filename!='refine-first-bloom-bundles.py':
  source=source.replace('first-bloom-head-r20','first-bloom-bundles-r21').replace('first-bloom-membranes-r13','first-bloom-bundles-r21-raw').replace('first-bloom-trim-r14','first-bloom-bundles-r21').replace('first-bloom-attachments-r5','first-bloom-bundles-r21')
 exec(compile(source,str(ROOT/'tools'/filename),'exec'))
