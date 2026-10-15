from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/review-rift-titan-fractured-head-r13.py').read_text(encoding='utf-8').replace('rift-titan-fractured-head-r13','rift-titan-lined-volume-r14')
# True profile is separate from the oblique angle used by the painted concept.
s=s.replace("('crown-side',(-2,.65,.05))", "('crown-side',(-2,.65,.05)),('crown-profile',(-2,0,0)),('crown-top',(-.4,.2,2))")
s=s.replace("('crown-side-clay',(-2,.65,.05))", "('crown-side-clay',(-2,.65,.05)),('crown-profile-clay',(-2,0,0))")
s=s.replace("('side','crown-side','crown-side-clay')", "('side','crown-side','crown-side-clay','crown-profile','crown-profile-clay')")
exec(compile(s,str(__file__),'exec'))
