from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from maw_contact_validation import validation_source
source=validation_source((ROOT/'tools/verify-blackwater-maw-contacts.py').read_text().replace('blackwater-maw-refinement-r3','blackwater-maw-restored-fins-r7-raw'))
exec(compile(source,str(__file__),'exec'))
