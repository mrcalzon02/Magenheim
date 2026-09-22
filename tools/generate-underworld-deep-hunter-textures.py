#!/usr/bin/env python3
"""Generate deterministic production PBR textures for the Blackwater Deep Hunter.

Art-only texture author for the apex predator. Readability comes from countershaded old hide,
wet fins/oral tissue, mineralized teeth and healed scar tissue. No emission is authored: the
Deep Hunter is a massive physical predator, not another bioluminescent silhouette.
"""
from pathlib import Path
from math import sin, cos, sqrt, atan2, pi
from PIL import Image, ImageStat
import random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/deep-hunter'; OUT.mkdir(parents=True,exist_ok=True)
SIZE=1024; PROXY=64

def clamp(v): return max(0,min(255,int(v)))
def motif(kind,x,y):
    nx=x/SIZE; ny=y/SIZE; cx=nx-.5; cy=ny-.5; r=sqrt(cx*cx+cy*cy); a=atan2(cy,cx)
    if kind=='dorsal': return .62*sin(ny*pi*10+sin(nx*8))+.38*cos((nx+ny)*pi*6)
    if kind=='ventral': return .68*cos(ny*pi*6)+.32*sin(nx*pi*7+sin(ny*5))
    if kind=='fin': return .78*cos(ny*pi*15+sin(nx*7))+.22*sin(nx*pi*5)
    if kind=='oral': return .64*cos(r*pi*13)+.36*sin(a*5+r*7)
    if kind=='tooth': return .74*cos(ny*pi*13)+.26*sin(nx*pi*4)
    if kind=='eye': return cos(r*pi*20)
    if kind=='scar': return .72*cos((nx+ny)*pi*18)+.28*sin((nx-ny)*pi*9)
    return 0

def height(seed,base,amp,kind):
    rng=random.Random(seed); ph=[rng.random()*2*pi for _ in range(3)]
    im=Image.new('L',(SIZE,SIZE)); p=im.load()
    for y in range(SIZE):
        for x in range(SIZE):
            nx=x/SIZE; ny=y/SIZE
            broad=sin(nx*11+ph[0])*cos(ny*9+ph[1])+.36*sin((nx+ny)*19+ph[2])
            # Low-frequency abrasion survives mip reduction; fine noise prevents sterile procedural surfaces.
            abrasion=.24*sin(nx*pi*4+ph[1])*.5+.18*cos(ny*pi*5+ph[0])
            p[x,y]=clamp(base+amp*broad+amp*.50*motif(kind,x,y)+amp*abrasion+rng.uniform(-3,3))
    return im

def rgb(luma,tint): return Image.merge('RGB',tuple(luma.point(lambda v,c=c: clamp(v*c)) for c in tint))
def material(stem,seed,base,tint,rough,kind,normal_strength):
    h=height(seed,base,27,kind); alb=rgb(h,tint); rr=height(seed+101,rough,12,kind)
    alb.save(OUT/f'{stem}-albedo.png'); rr.save(OUT/f'{stem}-roughness.png')
    hp=h.load(); n=Image.new('RGB',(SIZE,SIZE)); np=n.load()
    for y in range(SIZE):
        ym=(y-1)%SIZE; yp=(y+1)%SIZE
        for x in range(SIZE):
            xm=(x-1)%SIZE; xp=(x+1)%SIZE
            dx=(hp[xp,y]-hp[xm,y])/255*2.35+(motif(kind,(x+8)%SIZE,y)-motif(kind,(x-8)%SIZE,y))*normal_strength
            dy=(hp[x,yp]-hp[x,ym])/255*2.35+(motif(kind,x,(y+8)%SIZE)-motif(kind,x,(y-8)%SIZE))*normal_strength
            m=sqrt(dx*dx+dy*dy+1); np[x,y]=(clamp((-.5*dx/m+.5)*255),clamp((-.5*dy/m+.5)*255),clamp((.5/m+.5)*255))
    n.save(OUT/f'{stem}-normal.png')

material('dorsal-hide',17,69,(.44,.57,.58),220,'dorsal',.34)
material('ventral-hide',29,116,(.67,.72,.65),181,'ventral',.23)
material('fin-tissue',41,73,(.46,.62,.64),168,'fin',.30)
material('oral-tissue',53,103,(.88,.40,.36),132,'oral',.25)
material('mineralized-teeth',67,161,(.91,.87,.69),129,'tooth',.35)
material('recessed-eye',79,43,(.32,.39,.37),76,'eye',.15)
material('old-scar',97,128,(.72,.55,.48),188,'scar',.40)

files=list(OUT.glob('*.png'))
if len(files)!=21: raise RuntimeError(f'Expected 21 Deep Hunter maps, found {len(files)}')
if list(OUT.glob('*emission*')): raise RuntimeError('Deep Hunter must not author emission maps')
for p in files:
    with Image.open(p) as im:
        if im.size!=(SIZE,SIZE): raise RuntimeError(f'{p.name}: wrong dimensions {im.size}')
        proxy=im.resize((PROXY,PROXY),Image.Resampling.LANCZOS); luma=proxy.convert('L'); lo,hi=luma.getextrema(); st=ImageStat.Stat(luma)
        if hi-lo<8 or st.stddev[0]<2: raise RuntimeError(f'{p.name}: detail collapses at combat scale')
        if p.name.endswith('-normal.png'):
            ns=ImageStat.Stat(proxy.convert('RGB')); xy=(ns.stddev[0]**2+ns.stddev[1]**2)**.5
            if xy<2 or ns.mean[2]<150: raise RuntimeError(f'{p.name}: inadequate normal response')

def mean(stem,kind):
    with Image.open(OUT/f'{stem}-{kind}.png') as im: return ImageStat.Stat(im.resize((PROXY,PROXY),Image.Resampling.LANCZOS).convert('L')).mean[0]
# Apex readability must survive Valheim-scale distance and mip reduction.
if mean('dorsal-hide','albedo') >= mean('ventral-hide','albedo')-28: raise RuntimeError('Deep Hunter countershading collapses at combat scale')
if mean('dorsal-hide','roughness')-mean('oral-tissue','roughness') < 55: raise RuntimeError('Wet mouth no longer separates materially from old hide')
if mean('dorsal-hide','roughness')-mean('fin-tissue','roughness') < 35: raise RuntimeError('Wet fin tissue no longer separates materially from body hide')
if mean('old-scar','albedo')-mean('dorsal-hide','albedo') < 24: raise RuntimeError('Old scar tissue loses hero-predator readability')
if mean('mineralized-teeth','albedo')-mean('oral-tissue','albedo') < 38: raise RuntimeError('Teeth lose mouth-silhouette contrast')
print(f'AUTHORED Deep Hunter PBR set: {len(files)} maps at {SIZE}px; countershade/scars/wet tissue preserved; no emission -> {OUT}',flush=True)
