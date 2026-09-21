#!/usr/bin/env python3
"""Generate deterministic 2048px PBR source textures for the Fungal Forest Puffback."""
from pathlib import Path
from math import sin, cos, sqrt, atan2, pi
from PIL import Image, ImageStat
import random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/puffback'; OUT.mkdir(parents=True,exist_ok=True)
SIZE=2048; READABILITY_SIZE=64

def flat(im):
    reader=getattr(im,'get_flattened_data',None)
    return list(reader() if reader else im.getdata())
def clamp(v): return max(0,min(255,int(v)))

def relief(motif,x,y):
    nx=x/SIZE; ny=y/SIZE
    if motif=='hide':
        # Thick root-hide: broad vertical folds plus broken cross-grain, not mammal fur.
        return .55*sin(ny*2*pi*9+sin(nx*2*pi*4)*1.1)+.28*sin((nx*.7+ny)*2*pi*19)+.17*cos(nx*2*pi*7)
    if motif=='bladder':
        # Taut inflated tissue with large radial lobes and fine pore dimpling.
        cx=nx-.5; cy=ny-.5; r=sqrt(cx*cx+cy*cy); a=atan2(cy,cx)
        return .58*cos(r*2*pi*7+sin(a*6)*.7)+.25*cos(a*8)+.17*sin((nx+ny)*2*pi*24)
    if motif=='plate':
        # Dry shelf-fungal growth rings survive distance reduction as broad bands.
        cx=nx-.5; cy=ny-.5; r=sqrt(cx*cx+cy*cy); a=atan2(cy,cx)
        return .70*cos(r*2*pi*11+sin(a*4)*.6)+.30*cos(a*7+r*4)
    if motif=='gill':
        # Parallel vent lamellae visually identify the spore-discharge anatomy.
        return .72*sin(nx*2*pi*15+sin(ny*2*pi*3)*.8)+.18*cos(ny*2*pi*8)+.10*sin((nx+ny)*2*pi*23)
    return 0

def field(seed,base,amp,motif):
    rng=random.Random(seed); phase=[rng.random()*2*pi for _ in range(3)]
    out=Image.new('L',(SIZE,SIZE)); p=out.load()
    for y in range(SIZE):
        ny=y/SIZE
        for x in range(SIZE):
            nx=x/SIZE
            broad=sin(nx*12+phase[0])*cos(ny*9+phase[1])+.40*sin((nx+ny)*22+phase[2])
            p[x,y]=clamp(base+amp*broad+amp*.54*relief(motif,x,y)+rng.uniform(-4,4))
    return out

def rgb(luma,tint): return Image.merge('RGB',tuple(luma.point(lambda v,c=c: clamp(v*c)) for c in tint))

def material(stem,seed,base,tint,rough,motif,normal_strength,emission=False):
    h=field(seed,base,30,motif); rgb(h,tint).save(OUT/f'{stem}-albedo.png')
    rr=field(seed+101,rough,14,motif); rp=rr.load()
    for y in range(SIZE):
        for x in range(SIZE): rp[x,y]=clamp(rp[x,y]+18*relief(motif,x,y))
    rr.save(OUT/f'{stem}-roughness.png')
    hp=h.load(); n=Image.new('RGB',(SIZE,SIZE)); np=n.load()
    for y in range(SIZE):
        ym=(y-1)%SIZE; yp=(y+1)%SIZE
        for x in range(SIZE):
            xm=(x-1)%SIZE; xp=(x+1)%SIZE
            dx=(hp[xp,y]-hp[xm,y])/255*normal_strength; dy=(hp[x,yp]-hp[x,ym])/255*normal_strength
            m=sqrt(dx*dx+dy*dy+1)
            np[x,y]=(clamp((-.5*dx/m+.5)*255),clamp((-.5*dy/m+.5)*255),clamp((.5/m+.5)*255))
    n.save(OUT/f'{stem}-normal.png')
    if emission:
        e=Image.new('L',(SIZE,SIZE)); ep=e.load()
        for y in range(SIZE):
            for x in range(SIZE): ep[x,y]=clamp(max(0,relief(motif,x,y)-.38)*105)
        e.save(OUT/f'{stem}-emission.png')

# Neutral territorial grazer: dark root hide, ochre bladder, dry fungal armor, pale discharge vents.
# Only vent tissue receives restrained emission so the bladder remains readable by mass/value, not neon glow.
material('root-hide',613,78,(.78,.67,.50),218,'hide',3.0)
material('spore-bladder',719,132,(.91,.72,.39),172,'bladder',2.5)
material('fungal-plate',823,106,(.78,.68,.45),205,'plate',2.8)
material('vent-gill',929,142,(.92,.76,.45),158,'gill',2.5,True)

files=list(OUT.glob('*.png'))
if len(files)!=13: raise RuntimeError(f'Expected 13 Puffback texture maps, found {len(files)}')
for p in files:
    with Image.open(p) as im:
        if im.size!=(SIZE,SIZE): raise RuntimeError(f'{p.name}: wrong dimensions {im.size}')
        proxy=im.resize((READABILITY_SIZE,READABILITY_SIZE),Image.Resampling.LANCZOS)
        luma=proxy.convert('L'); lo,hi=luma.getextrema(); sigma=ImageStat.Stat(luma).stddev[0]
        if hi-lo<10 or sigma<2.2: raise RuntimeError(f'{p.name}: detail collapses at combat scale range={hi-lo}, sigma={sigma:.2f}')
        if p.name.endswith('-normal.png'):
            stat=ImageStat.Stat(proxy.convert('RGB')); xy=(stat.stddev[0]**2+stat.stddev[1]**2)**.5
            if xy<2.2 or stat.mean[2]<150: raise RuntimeError(f'{p.name}: normal response collapses xy={xy:.2f} z={stat.mean[2]:.1f}')
        elif p.name.endswith('-roughness.png') and (hi-lo<12 or sigma<3.0):
            raise RuntimeError(f'{p.name}: roughness response collapses range={hi-lo}, sigma={sigma:.2f}')
        elif p.name.endswith('-emission.png'):
            coverage=sum(v>=8 for v in flat(luma))/(READABILITY_SIZE*READABILITY_SIZE)
            if not .02<=coverage<=.42: raise RuntimeError(f'{p.name}: vent emission coverage {coverage:.1%} is not localized')
print(f'AUTHORED Puffback PBR foundation: {len(files)} maps at {SIZE}x{SIZE}; four material identities survive {READABILITY_SIZE}px combat proxy -> {OUT}',flush=True)
