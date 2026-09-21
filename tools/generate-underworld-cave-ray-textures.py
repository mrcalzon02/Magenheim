#!/usr/bin/env python3
"""Generate deterministic 2048px PBR source textures for the Blackwater Deep Cave Ray."""
from pathlib import Path
from math import sin, cos, sqrt, atan2, pi
from PIL import Image, ImageStat
import random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/cave-ray'; OUT.mkdir(parents=True,exist_ok=True)
SIZE=2048; READABILITY_SIZE=64

def flat(im):
    reader=getattr(im,'get_flattened_data',None)
    return list(reader() if reader else im.getdata())
def clamp(v): return max(0,min(255,int(v)))

def relief(motif,x,y):
    nx=x/SIZE; ny=y/SIZE
    if motif=='dorsal':
        # Matte deep-water hide: broad hydrodynamic mottling with sparse transverse scars.
        return .52*sin(ny*2*pi*7+sin(nx*2*pi*3)*.75)+.31*cos((nx*.7+ny)*2*pi*14)+.17*sin(nx*2*pi*5)
    if motif=='ventral':
        # Softer underside tissue: shallow longitudinal folds, readable without becoming ribbed armor.
        return .58*sin(nx*2*pi*9+sin(ny*2*pi*2)*.55)+.27*cos(ny*2*pi*11)+.15*sin((nx+ny)*2*pi*19)
    if motif=='membrane':
        # Wing membrane: root-to-tip fiber fan with fine cross tension, supporting deformation readability.
        cx=nx-.08; cy=ny-.50; r=sqrt(cx*cx+cy*cy); a=atan2(cy,cx)
        return .61*cos(a*15+r*3)+.25*sin(r*2*pi*13)+.14*cos((nx-ny)*2*pi*23)
    if motif=='photophore':
        # Sensory organs: concentric organic pores; this is the only emissive family.
        cx=nx-.5; cy=ny-.5; r=sqrt(cx*cx+cy*cy)
        return .70*cos(r*2*pi*18)+.20*sin((nx+ny)*2*pi*13)+.10*cos(nx*2*pi*29)
    return 0

def field(seed,base,amp,motif):
    rng=random.Random(seed); phase=[rng.random()*2*pi for _ in range(3)]
    out=Image.new('L',(SIZE,SIZE)); p=out.load()
    for y in range(SIZE):
        ny=y/SIZE
        for x in range(SIZE):
            nx=x/SIZE
            broad=sin(nx*10+phase[0])*cos(ny*8+phase[1])+.36*sin((nx+ny)*18+phase[2])
            p[x,y]=clamp(base+amp*broad+amp*.55*relief(motif,x,y)+rng.uniform(-4,4))
    return out

def rgb(luma,tint): return Image.merge('RGB',tuple(luma.point(lambda v,c=c: clamp(v*c)) for c in tint))

def material(stem,seed,base,tint,rough,motif,normal_strength,emission=False):
    h=field(seed,base,29,motif); rgb(h,tint).save(OUT/f'{stem}-albedo.png')
    rr=field(seed+101,rough,13,motif); rp=rr.load()
    for y in range(SIZE):
        for x in range(SIZE): rp[x,y]=clamp(rp[x,y]+17*relief(motif,x,y))
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
            for x in range(SIZE): ep[x,y]=clamp(max(0,relief(motif,x,y)-.48)*104)
        e.save(OUT/f'{stem}-emission.png')

# Blackwater silhouette stays dark and matte. Membranes read through fiber direction and normal response.
# Bioluminescence is restricted to modeled photophores; neither dorsal nor ventral body tissue emits.
material('dorsal-hide',1709,72,(.55,.68,.69),218,'dorsal',2.8)
material('ventral-tissue',1823,112,(.68,.82,.79),184,'ventral',2.3)
material('wing-membrane',1931,86,(.57,.73,.74),198,'membrane',2.6)
material('photophore-tissue',2017,137,(.64,.92,.84),162,'photophore',2.4,True)

files=list(OUT.glob('*.png'))
if len(files)!=13: raise RuntimeError(f'Expected 13 Cave Ray texture maps, found {len(files)}')
for p in files:
    with Image.open(p) as im:
        if im.size!=(SIZE,SIZE): raise RuntimeError(f'{p.name}: wrong dimensions {im.size}')
        proxy=im.resize((READABILITY_SIZE,READABILITY_SIZE),Image.Resampling.LANCZOS)
        luma=proxy.convert('L'); lo,hi=luma.getextrema(); sigma=ImageStat.Stat(luma).stddev[0]
        if hi-lo<10 or sigma<2.2: raise RuntimeError(f'{p.name}: detail collapses at combat scale range={hi-lo}, sigma={sigma:.2f}')
        if p.name.endswith('-normal.png'):
            stat=ImageStat.Stat(proxy.convert('RGB')); xy=(stat.stddev[0]**2+stat.stddev[1]**2)**.5
            if xy<2.0 or stat.mean[2]<150: raise RuntimeError(f'{p.name}: normal response collapses xy={xy:.2f} z={stat.mean[2]:.1f}')
        elif p.name.endswith('-roughness.png') and (hi-lo<12 or sigma<3.0):
            raise RuntimeError(f'{p.name}: roughness response collapses range={hi-lo}, sigma={sigma:.2f}')
        elif p.name.endswith('-emission.png'):
            coverage=sum(v>=8 for v in flat(luma))/(READABILITY_SIZE*READABILITY_SIZE)
            if not .02<=coverage<=.34: raise RuntimeError(f'{p.name}: photophore emission coverage {coverage:.1%} is not localized')
print(f'AUTHORED Cave Ray PBR foundation: {len(files)} maps at {SIZE}x{SIZE}; four material identities survive {READABILITY_SIZE}px gameplay proxy -> {OUT}',flush=True)
