from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/review-rift-titan-fractured-head-r13.py').read_text().replace('rift-titan-fractured-head-r13','rift-titan-fracture-planes-r16').replace('2026-10-14','2026-10-16')
s=s.replace("('crown-side',(-2,.65,.05))", "('crown-side',(-2,.65,.05)),('crown-profile',(-2,0,0)),('crown-right',(2,.65,.05)),('crown-top',(-.4,.2,2))")
s=s.replace("('crown-side-clay',(-2,.65,.05))", "('crown-side-clay',(-2,.65,.05)),('crown-profile-clay',(-2,0,0))")
exec(compile(s,str(__file__),'exec'))
