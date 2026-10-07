from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from maw_contact_validation import validation_source
for filename in ('audit-first-bloom-buried.py','trim-first-bloom-buried.py','verify-blackwater-maw-contacts.py'):
 source=(ROOT/'tools'/filename).read_text().replace('first-bloom-membranes-r13','blackwater-maw-spined-fins-r8-raw').replace('first-bloom-trim-r14','blackwater-maw-spined-fins-r8')
 if filename=='trim-first-bloom-buried.py':source=source.replace('9252','8032').replace('6206','4872')
 if filename=='verify-blackwater-maw-contacts.py':source=validation_source(source.replace('blackwater-maw-refinement-r3','blackwater-maw-spined-fins-r8').replace('attachment-verification.json','attachment-pose-verification.json'))
 exec(compile(source,str(ROOT/'tools'/filename),'exec'))
