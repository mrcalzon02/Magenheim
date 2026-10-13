from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
from maw_contact_validation import validation_source
source=(ROOT/'tools/verify-blackwater-maw-contacts.py').read_text(encoding='utf-8').replace('blackwater-maw-refinement-r3','rift-titan-strata-r4').replace('creature-models/2026-10-04','creature-models/2026-10-13')
source=validation_source(source)
source=source.replace('VERIFIED Maw contacts','VERIFIED Rift cage contacts')
exec(compile(source,str(__file__),'exec'))
