from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
import sys
sys.path.insert(0,str(ROOT/'tools'))
from maw_contact_validation import validation_source
source=(ROOT/'tools/verify-blackwater-maw-contacts.py').read_text().replace('blackwater-maw-refinement-r3','blackwater-maw-jaws-r5-raw')
source=validation_source(source)
exec(compile(source,str(__file__),'exec'))
