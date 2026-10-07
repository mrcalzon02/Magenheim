from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/verify-blackwater-maw-fin-inventory.py').read_text().replace('blackwater-maw-restored-fins-r7','blackwater-maw-spined-fins-r8')
source=source.replace("('dorsal_root','pectoral_root','forward_pectoral_root','tail_root')","('dorsal_root','pectoral_root','forward_pectoral_root','pelvic_root','tail_root')")
source=source.replace("v['kind']=='pectoral_root'","v['kind']=='pelvic_root'")
exec(compile(source,str(__file__),'exec'))
