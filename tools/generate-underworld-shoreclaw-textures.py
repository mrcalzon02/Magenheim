#!/usr/bin/env python3
"""Generate deterministic 1024px production PBR textures for the Blackwater Deep Shoreclaw.

Art-only pipeline. Armor wear is edge/localized rather than uniform noise; no emission is
used because Shoreclaw readability comes from silhouette, material response and abrasion.
"""
from pathlib import Path
from math import sin, cos, sqrt, atan2, pi
from PIL import Image, ImageStat
import random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/shoreclaw'; OUT.mkdir(parents=True,exist_ok=True)
SIZE=1024; PROXY=64

def clamp(v): return max(0,min(255,int(v)))
def flat(im):
    reader=getattr(im,'get_flattened_data',None)
    return list(reader() if reader else im.getdata())

def motif(kind,x,y):
    nx=x/SIZE; ny=y/SIZE; cx=nx-.5; cy=ny-.5; r=sqrt(cx*cx+cy*cy); a=atan2(cy,cx)
    if kind=='shell': return .62*cos(r*2*pi*7+sin(a*5))+.38*cos(a*9+r*11)
    if kind=='scute': return .72*cos(ny*2*pi*10+sin(nx*2*pi*3))+.28*sin(nx*2*pi*6)
    if kind=='joint': return .76*sin(ny*2*pi*7+sin(nx*2*pi*2)*.7)+.24*sin((nx+ny)*2*pi*4)
    if kind=='belly': return .68*sin(nx*2*pi*5)+.32*cos(ny*2*pi*6+sin(nx*5))
    if kind=='claw': return .74*cos(nx*2*pi*9+sin(ny*2*pi*3))+.26*cos(ny*2*pi*5)
    if kind=='eye': return .65*cos(r*2*pi*8)+.35*cos(a*6+r*5)
    return 0

def edge_mask(x,y):
    nx=x/(SIZE-1); ny=y/(SIZE-1); d=min(nx,1-nx,ny,1-ny)
    return max(0.0,min(1.0,(.18-d)/.18))**1.6

def height(seed,base,amp,kind):
    rng=random.Random(seed); phase=[rng.random()*2*pi for _ in range(3)]
    im=Image.new('L',(SIZE,SIZE)); p=im.load()
    for y in range(SIZE):
        for x in range(SIZE):
            nx=x/SIZE; ny=y/SIZE
            broad=sin(nx*13+phase[0])*cos(ny*11+phase[1])+.42*sin((nx+ny)*23+phase[2])
            p[x,y]=clamp(base+amp*broad+amp*.50*motif(kind,x,y)+rng.uniform(-4,4))
    return im

def rgb(luma,tint): return Image.merge('RGB',tuple(luma.point(lambda v,c=c: clamp(v*c)) for c in tint))
def material(stem,seed,base,tint,rough,kind,wear=0):
    h=height(seed,base,27,kind); alb=rgb(h,tint); ap=alb.load()
    rr=height(seed+101,rough,12,kind); rp=rr.load()
    # Mineral abrasion is deliberately concentrated at armor/claw edges.
    if wear:
        for y in range(SIZE):
            for x in range(SIZE):
                e=edge_mask(x,y)*wear*(.72+.28*max(0,motif(kind,x,y)))
                if e:
                    r,g,b=ap[x,y]; ap[x,y]=(clamp(r+e*36),clamp(g+e*32),clamp(b+e*22)); rp[x,y]=clamp(rp[x,y]+e*28)
    alb.save(OUT/f'{stem}-albedo.png'); rr.save(OUT/f'{stem}-roughness.png')
    hp=h.load(); n=Image.new('RGB',(SIZE,SIZE)); np=n.load(); strength={'shell':.36,'scute':.42,'joint':.18,'belly':.16,'claw':.38,'eye':.12}[kind]
    for y in range(SIZE):
        ym=(y-1)%SIZE; yp=(y+1)%SIZE
        for x in range(SIZE):
            xm=(x-1)%SIZE; xp=(x+1)%SIZE
            dx=(hp[xp,y]-hp[xm,y])/255*2.4+(motif(kind,(x+8)%SIZE,y)-motif(kind,(x-8)%SIZE,y))*strength
            dy=(hp[x,yp]-hp[x,ym])/255*2.4+(motif(kind,x,(y+8)%SIZE)-motif(kind,x,(y-8)%SIZE))*strength
            m=sqrt(dx*dx+dy*dy+1); np[x,y]=(clamp((-.5*dx/m+.5)*255),clamp((-.5*dy/m+.5)*255),clamp((.5/m+.5)*255))
    n.save(OUT/f'{stem}-normal.png')

material('shell-armor',17,94,(.67,.84,.76),202,'shell',1.0)
material('ridge-scute',29,105,(.72,.82,.66),216,'scute',.85)
material('joint-tissue',41,103,(.78,.60,.52),151,'joint')
material('underside',53,122,(.82,.72,.58),174,'belly')
material('claw-armor',67,91,(.72,.86,.72),194,'claw',1.0)
material('eye',79,53,(.55,.64,.55),88,'eye')

files=list(OUT.glob('*.png'))
if len(files)!=18: raise RuntimeError(f'Expected 18 Shoreclaw maps, found {len(files)}')
if list(OUT.glob('*emission*')): raise RuntimeError('Shoreclaw must not use generic emission')
for p in files:
    with Image.open(p) as im:
        if im.size!=(SIZE,SIZE): raise RuntimeError(f'{p.name}: wrong dimensions {im.size}')
        proxy=im.resize((PROXY,PROXY),Image.Resampling.LANCZOS); luma=proxy.convert('L'); lo,hi=luma.getextrema(); sigma=ImageStat.Stat(luma).stddev[0]
        if hi-lo<8 or sigma<2: raise RuntimeError(f'{p.name}: detail collapses at combat scale range={hi-lo} sigma={sigma:.2f}')
        if p.name.endswith('-normal.png'):
            st=ImageStat.Stat(proxy.convert('RGB')); xy=(st.stddev[0]**2+st.stddev[1]**2)**.5
            if xy<2.2 or st.mean[2]<150: raise RuntimeError(f'{p.name}: normal response inadequate xy={xy:.2f} z={st.mean[2]:.1f}')
        if p.name.endswith('-roughness.png') and (hi-lo<12 or sigma<3): raise RuntimeError(f'{p.name}: roughness collapses')
# Material separation: flexible joints must remain less rough than shell armor.
def mean(stem,kind):
    with Image.open(OUT/f'{stem}-{kind}.png') as im: return ImageStat.Stat(im.resize((PROXY,PROXY),Image.Resampling.LANCZOS).convert('L')).mean[0]
if mean('shell-armor','roughness')-mean('joint-tissue','roughness')<28: raise RuntimeError('Shell/joint roughness separation collapsed')
# Edge abrasion must survive mip-like reduction on both primary armored identities.
for stem in ('shell-armor','claw-armor'):
    with Image.open(OUT/f'{stem}-albedo.png') as im:
        p=im.resize((PROXY,PROXY),Image.Resampling.LANCZOS).convert('L'); px=p.load()
        edge=sum(px[x,y] for y in range(PROXY) for x in range(PROXY) if x<8 or x>=56 or y<8 or y>=56)
        ec=sum(1 for y in range(PROXY) for x in range(PROXY) if x<8 or x>=56 or y<8 or y>=56)
        center=sum(px[x,y] for y in range(16,48) for x in range(16,48))/(32*32)
        if edge/ec-center<3: raise RuntimeError(f'{stem}: localized edge abrasion does not survive combat proxy')
print(f'AUTHORED Shoreclaw PBR set: {len(files)} maps at {SIZE}px; six material families retain combat-distance structure; no emission -> {OUT}',flush=True)
