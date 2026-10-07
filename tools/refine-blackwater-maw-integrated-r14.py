from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-blackwater-maw-spined-fins.py').read_text().replace('tools/refine-blackwater-maw.py','tools/refine-maw-dark-surface.py').replace('blackwater-maw-spined-fins-r8-raw','blackwater-maw-integrated-r14-raw').replace('blackwater_maw_spined_fins','blackwater_maw_integrated_r14')
exec(compile(source,str(__file__),'exec'))
