from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-blackwater-maw.py').read_text().replace('blackwater-maw-refinement-r3','blackwater-maw-fins-r6-raw').replace('blackwater_maw_refinement','blackwater_maw_fins_texture')
exec(compile(source,str(__file__),'exec'))
