"""Saved r13 from exact r14 cameras, including the true left profile."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/review-rift-titan-fractured-before-r13.py').read_text(encoding='utf-8').replace('rift-titan-fractured-head-r13','rift-titan-lined-volume-r14').replace('rift-titan-cracked-head-r12','rift-titan-fractured-head-r13')
s=s.replace("('crown-side',(-2,.65,.05))", "('crown-side',(-2,.65,.05)),('crown-profile',(-2,0,0))")
s=s.replace("('crown-side-clay',(-2,.65,.05))", "('crown-side-clay',(-2,.65,.05)),('crown-profile-clay',(-2,0,0))")
s=s.replace("cameras='exact r13 camera-layout.json transforms and ortho scales'", "cameras='exact r14 camera-layout.json transforms and ortho scales'").replace('actual saved r12 source','actual saved r13 source')
exec(compile(s,str(__file__),'exec'))
