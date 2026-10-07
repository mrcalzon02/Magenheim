from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/trim-first-bloom-buried.py').read_text().replace('first-bloom-membranes-r13','blackwater-maw-refinement-r3').replace('first-bloom-trim-r14','blackwater-maw-refinement-r4').replace('9252','6768').replace('6206','4872')
exec(compile(source,str(__file__),'exec'))
