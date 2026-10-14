from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-rift-titan-notched-r11.py').read_text(encoding='utf-8').replace('rift-titan-notched-r11','rift-titan-cracked-head-r12')
source=source.replace("('crown-detail',(1,2,.15))","('crown-detail',(1,2,.15)),('crown-side',(-2,.65,.05))")
source=source.replace('if view=="crown-detail":','if view in ("crown-detail","crown-side"):').replace("('side','side-clay','side-reference','side-reference-clay')","('side','side-clay','side-reference','side-reference-clay','crown-side')")
exec(compile(source,str(__file__),'exec'))
