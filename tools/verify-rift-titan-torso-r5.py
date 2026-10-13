from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/verify-rift-titan-cage-r4.py').read_text(encoding='utf-8').replace('rift-titan-strata-r4','rift-titan-torso-r5').replace('Rift cage contacts','Rift torso contacts')
exec(compile(source,str(__file__),'exec'))
