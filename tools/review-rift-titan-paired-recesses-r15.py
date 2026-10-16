from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/review-rift-titan-lined-volume-r14.py').read_text(encoding='utf-8').replace('rift-titan-lined-volume-r14','rift-titan-paired-recesses-r15')
# Add an actual right-side view so both independent recesses are reviewed.
s=s.replace("('crown-profile',(-2,0,0))", "('crown-profile',(-2,0,0)),('crown-right',(2,.65,.05))")
exec(compile(s,str(__file__),'exec'))
