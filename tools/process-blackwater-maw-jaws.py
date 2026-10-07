from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
import sys
sys.path.insert(0,str(ROOT/'tools'))
from maw_contact_validation import validation_source
for filename in ('audit-first-bloom-buried.py','trim-first-bloom-buried.py','verify-blackwater-maw-contacts.py'):
 source=(ROOT/'tools'/filename).read_text()
 source=source.replace('first-bloom-membranes-r13','blackwater-maw-jaws-r5-raw').replace('first-bloom-trim-r14','blackwater-maw-jaws-r5')
 if filename=='trim-first-bloom-buried.py':source=source.replace('9252','7140').replace('6206','4872')
 if filename=='verify-blackwater-maw-contacts.py':source=validation_source(source.replace('blackwater-maw-refinement-r3','blackwater-maw-jaws-r5').replace('attachment-verification.json','attachment-pose-verification.json'))
 exec(compile(source,str(ROOT/'tools'/filename),'exec'))
