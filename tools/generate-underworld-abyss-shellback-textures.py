#!/usr/bin/env python3
"""Generate deterministic production PBR textures for the Blackwater Deep Abyss Shellback.

Art-only texture author. The elite's readability comes from laminated mineral armor versus
wet vulnerable underside/joints. No emission is authored: Shellback is a fortress animal,
not a bioluminescent creature.
"""
from pathlib import Path
from math import sin, cos, sqrt, pi
from PIL import Image, ImageStat
import random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/abyss-shellback'; OUT.mkdir(parents=True,exist_ok=True)
SIZE=1024; PROXY=64

def clamp(v): return max(0,min(255,int(v)))
def motif(kind,x,y):
    nx=x/SIZE; ny=y/SIZE; edge=min(nx,ny,1-nx,1-ny)
    if kind=='shell': return .56*cos(ny*pi*11+sin(nx*9))+.44*sin((nx+ny)*pi*7)
    if kind=='mineral': return .68*cos(nx*pi*16)+.32*sin(ny*pi*9+sin(nx*12))
    if kind=='joint': return .72*sin(ny*pi*8+sin(nx*7))+.28*cos(nx*pi*5)
    if kind=='belly': return .62*cos(ny*pi*6)+.38*sin(nx*pi*5+sin(ny*6))
    if kind=='claw': return .58*cos((nx+ny)*pi*10)+.42*sin(nx*pi*12)
    if kind=='eye':
        r=sqrt((nx-.5)**2+(ny-.5)**2); return cos(r*pi*18)
    return 0

def height(seed,base,amp,kind):
    rng=random.Random(seed); ph=[rng.random()*2*pi for _ in range(3)]
    im=Image.new('L',(SIZE,SIZE)); p=im.load()
    for y in range(SIZE):
        for x in range(SIZE):
            nx=x/SIZE; ny=y/SIZE
            broad=sin(nx*13+ph[0])*cos(ny*9+ph[1])+.34*sin((nx+ny)*23+ph[2])
            p[x,y]=clamp(base+amp*broad+amp*.50*motif(kind,x,y)+rng.uniform(-3,3))
    return im

def rgb(luma,tint): return Image.merge('RGB',tuple(luma.point(lambda v,c=c: clamp(v*c)) for c in tint))
def material(stem,seed,base,tint,rough,kind,normal_strength):
    h=height(seed,base,26,kind); alb=rgb(h,tint); rr=height(seed+101,rough,12,kind)
    alb.save(OUT/f'{stem}-albedo.png'); rr.save(OUT/f'{stem}-roughness.png')
    hp=h.load(); n=Image.new('RGB',(SIZE,SIZE)); np=n.load()
    for y in range(SIZE):
        ym=(y-1)%SIZE; yp=(y+1)%SIZE
        for x in range(SIZE):
            xm=(x-1)%SIZE; xp=(x+1)%SIZE
            dx=(hp[xp,y]-hp[xm,y])/255*2.3+(motif(kind,(x+8)%SIZE,y)-motif(kind,(x-8)%SIZE,y))*normal_strength
            dy=(hp[x,yp]-hp[x,ym])/255*2.3+(motif(kind,x,(y+8)%SIZE)-motif(kind,x,(y-8)%SIZE))*normal_strength
            m=sqrt(dx*dx+dy*dy+1); np[x,y]=(clamp((-.5*dx/m+.5)*255),clamp((-.5*dy/m+.5)*255),clamp((.5/m+.5)*255))
    n.save(OUT/f'{stem}-normal.png')

material('laminated-shell',17,82,(.55,.68,.61),226,'shell',.34)
material('mineral-rim-keel',31,126,(.76,.79,.65),205,'mineral',.42)
material('joint-tissue',47,91,(.74,.46,.38),137,'joint',.24)
material('vulnerable-underside',59,137,(.90,.57,.42),151,'belly',.20)
material('heavy-claw',71,91,(.61,.70,.61),194,'claw',.36)
material('eye',83,48,(.38,.48,.42),82,'eye',.14)

files=list(OUT.glob('*.png'))
if len(files)!=18: raise RuntimeError(f'Expected 18 Abyss Shellback maps, found {len(files)}')
if list(OUT.glob('*emission*')): raise RuntimeError('Abyss Shellback must not author emission maps')
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
# Gameplay vulnerability must remain legible even after combat-distance reduction.
if mean('vulnerable-underside','albedo')-mean('laminated-shell','albedo') < 28: raise RuntimeError('Underside vulnerability loses albedo separation at combat scale')
if mean('laminated-shell','roughness')-mean('joint-tissue','roughness') < 55: raise RuntimeError('Wet joints no longer separate materially from shell armor')
if mean('mineral-rim-keel','albedo')-mean('laminated-shell','albedo') < 18: raise RuntimeError('Mineral plate rims/keels lose fortress silhouette readability')
if mean('heavy-claw','roughness') >= mean('laminated-shell','roughness')-18: raise RuntimeError('Claw wear no longer separates from laminated shell')
print(f'AUTHORED Abyss Shellback PBR set: {len(files)} maps at {SIZE}px; vulnerable underside/joints preserved; no emission -> {OUT}',flush=True)
