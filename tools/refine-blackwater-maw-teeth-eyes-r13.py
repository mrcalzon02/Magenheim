from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-blackwater-maw-spined-fins.py').read_text().replace('tools/refine-blackwater-maw.py','tools/refine-maw-antialiased.py').replace('blackwater-maw-spined-fins-r8-raw','blackwater-maw-teeth-eyes-r13-raw').replace('blackwater_maw_spined_fins','blackwater_maw_teeth_eyes_r13')
exec(compile(source,str(__file__),'exec'))
