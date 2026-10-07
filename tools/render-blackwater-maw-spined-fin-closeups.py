from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/render-blackwater-maw-restored-fin-closeups.py').read_text().replace('blackwater-maw-restored-fins-r7','blackwater-maw-spined-fins-r8')
exec(compile(source,str(__file__),'exec'))
