"""Two solid head views for outline rejection before the complete review."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
builder=(ROOT/'tools/review-rift-titan-fractured-head-r13.py').read_text(encoding='utf-8')
exec(compile(builder[:builder.index("exec(compile(s,str(__file__),'exec'))")],str(__file__),'exec'))
start=s.index('for view,direction in [');end=s.index(' target=center;span=size',start)
s=s[:start]+"for view,direction in [('crown-front-clay',(0,2,0)),('crown-side-clay',(-2,.65,.05))]:\n"+s[end:]
s=s[:s.index('data=(OUT/(ID+')]
exec(compile(s,str(__file__),'exec'))
