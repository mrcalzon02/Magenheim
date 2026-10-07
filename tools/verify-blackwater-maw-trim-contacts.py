from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/verify-blackwater-maw-contacts.py').read_text().replace('blackwater-maw-refinement-r3','blackwater-maw-refinement-r4')
source=source.replace("attachment-verification.json","attachment-pose-verification.json")
exec(compile(source,str(__file__),'exec'))
