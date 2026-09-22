#!/usr/bin/env python3
"""Generate deterministic production PBR textures for the Blackwater Deep Lantern Angler.

Art-only texture author. Emission is anatomically confined to the lure light organ; body,
oral tissue and fins remain non-emissive so the lure retains its gameplay silhouette/read.
"""
from pathlib import Path
from math import sin, cos, sqrt, atan2, pi
from PIL import Image, ImageStat
import random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/lantern-angler'; OUT.mkdir(parents=True,exist_ok=True)
SIZE=1024; PROXY=64

def clamp(v): return max(0,min(255,int(v)))
def motif(kind,x,y):
    nx=x/SIZE; ny=y/SIZE; cx=nx-.5; cy=ny-.5; r=sqrt(cx*cx+cy*cy); a=atan2(cy,cx)
    if kind=='dorsal': return .58*sin(ny*2*pi*8+sin(nx*9))+.42*cos((nx+ny)*pi*7)
    if kind=='ventral': return .66*sin(nx*pi*7)+.34*cos(ny*pi*5+sin(nx*6))
    if kind=='fin': return .76*cos(ny*pi*14+sin(nx*8))+.24*sin(nx*pi*5)
    if kind=='oral': return .64*cos(r*pi*15)+.36*sin(a*5+r*8)
    if kind=='tooth': return .72*cos(ny*pi*12)+.28*sin(nx*pi*4)
    if kind=='lure': return .70*cos(ny*pi*10+sin(nx*7))+.30*sin(nx*pi*5)
    if kind=='light': return .68*cos(r*pi*9)+.32*cos(a*7+r*5)
    return 0

def height(seed,base,amp,kind):
    rng=random.Random(seed); ph=[rng.random()*2*pi for _ in range(3)]
    im=Image.new('L',(SIZE,SIZE)); p=im.load()
    for y in range(SIZE):
        for x in range(SIZE):
            nx=x/SIZE; ny=y/SIZE
            broad=sin(nx*12+ph[0])*cos(ny*10+ph[1])+.38*sin((nx+ny)*21+ph[2])
            p[x,y]=clamp(base+amp*broad+amp*.48*motif(kind,x,y)+rng.uniform(-3,3))
    return im

def rgb(luma,tint): return Image.merge('RGB',tuple(luma.point(lambda v,c=c: clamp(v*c)) for c in tint))
def material(stem,seed,base,tint,rough,kind,normal_strength):
    h=height(seed,base,25,kind); alb=rgb(h,tint); rr=height(seed+101,rough,11,kind)
    alb.save(OUT/f'{stem}-albedo.png'); rr.save(OUT/f'{stem}-roughness.png')
    hp=h.load(); n=Image.new('RGB',(SIZE,SIZE)); np=n.load()
    for y in range(SIZE):
        ym=(y-1)%SIZE; yp=(y+1)%SIZE
        for x in range(SIZE):
            xm=(x-1)%SIZE; xp=(x+1)%SIZE
            dx=(hp[xp,y]-hp[xm,y])/255*2.2+(motif(kind,(x+8)%SIZE,y)-motif(kind,(x-8)%SIZE,y))*normal_strength
            dy=(hp[x,yp]-hp[x,ym])/255*2.2+(motif(kind,x,(y+8)%SIZE)-motif(kind,x,(y-8)%SIZE))*normal_strength
            m=sqrt(dx*dx+dy*dy+1); np[x,y]=(clamp((-.5*dx/m+.5)*255),clamp((-.5*dy/m+.5)*255),clamp((.5/m+.5)*255))
    n.save(OUT/f'{stem}-normal.png')

material('dorsal-hide',17,73,(.48,.62,.64),214,'dorsal',.32)
material('ventral-hide',29,112,(.66,.72,.66),177,'ventral',.22)
material('fin-tissue',41,76,(.48,.68,.70),188,'fin',.28)
material('oral-throat',53,105,(.92,.42,.38),139,'oral',.24)
material('tooth',67,157,(.92,.88,.68),126,'tooth',.34)
material('lure-stalk',79,94,(.52,.76,.66),156,'lure',.24)
material('light-organ',97,122,(.52,.92,.76),92,'light',.16)

# The only emissive surface is the physical lure organ. Radial falloff preserves a bright
# biological core and mottled membrane instead of making a uniformly glowing decal.
em=Image.new('L',(SIZE,SIZE)); ep=em.load(); rng=random.Random(211)
for y in range(SIZE):
    for x in range(SIZE):
        nx=x/(SIZE-1)-.5; ny=y/(SIZE-1)-.5; r=sqrt(nx*nx+ny*ny)
        core=max(0,1-r/.64); membrane=.82+.18*cos(r*pi*18+atan2(ny,nx)*4)
        ep[x,y]=clamp(255*(core**.58)*membrane+rng.uniform(-2,2))
em.save(OUT/'light-organ-emission.png')

files=list(OUT.glob('*.png'))
if len(files)!=22: raise RuntimeError(f'Expected 22 Lantern Angler maps, found {len(files)}')
emissions=list(OUT.glob('*emission*'))
if [p.name for p in emissions] != ['light-organ-emission.png']: raise RuntimeError('Emission must be confined to light-organ-emission.png')
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
if mean('dorsal-hide','albedo') >= mean('ventral-hide','albedo')-18: raise RuntimeError('Dorsal/ventral countershading collapsed')
if mean('dorsal-hide','roughness')-mean('oral-throat','roughness') < 45: raise RuntimeError('Wet oral tissue no longer separates from hide')
with Image.open(OUT/'light-organ-emission.png') as im:
    p=im.resize((PROXY,PROXY),Image.Resampling.LANCZOS).convert('L'); px=p.load()
    core=sum(px[x,y] for y in range(24,40) for x in range(24,40))/256
    rim=(sum(px[x,y] for y in range(PROXY) for x in range(PROXY) if x<8 or x>=56 or y<8 or y>=56)/1792)
    if core-rim<70: raise RuntimeError('Lure emission lacks a readable biological core/falloff')
print(f'AUTHORED Lantern Angler PBR set: {len(files)} maps at {SIZE}px; emission confined to physical lure organ -> {OUT}',flush=True)
