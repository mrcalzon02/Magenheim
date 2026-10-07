from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-blackwater-maw-spined-fins.py').read_text().replace('blackwater-maw-spined-fins-r8-raw','blackwater-maw-face-r12-raw').replace('blackwater_maw_spined_fins','blackwater_maw_face_r12')
exec(compile(source,str(__file__),'exec'))
