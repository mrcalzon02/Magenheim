#!/usr/bin/env python3
"""Generate deterministic 1024px PBR source textures for the Blackwater Deep Blackwater Lamprey."""
from pathlib import Path
from math import sin, cos, sqrt, atan2, pi
from PIL import Image, ImageStat
import random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/blackwater-lamprey'; OUT.mkdir(parents=True,exist_ok=True)
SIZE=1024; READABILITY_SIZE=64

def clamp(v): return max(0,min(255,int(v)))

def relief(motif,x,y):
    nx=x/SIZE; ny=y/SIZE
    if motif=='dorsal':
        # Slick longitudinal musculature with sparse annular folds: flexible, not scaled fish skin.
        return .52*sin(ny*2*pi*9+sin(nx*2*pi*2)*.42)+.30*cos((nx*.35+ny)*2*pi*17)+.18*sin(nx*2*pi*4)
    if motif=='ventral':
        return .57*sin(nx*2*pi*7+sin(ny*2*pi*2)*.38)+.27*cos(ny*2*pi*11)+.16*sin((nx+ny)*2*pi*15)
    if motif=='fin':
        return .61*cos(nx*2*pi*15+sin(ny*2*pi*2)*.35)+.25*sin(ny*2*pi*8)+.14*cos((nx-ny)*2*pi*18)
    if motif=='oral':
        # Radial sucker folds converge on the mouth and must remain readable during latch closeups.
        cx=nx-.5; cy=ny-.5; r=sqrt(cx*cx+cy*cy); a=atan2(cy,cx)
        return .62*cos(a*18+r*5)+.25*sin(r*2*pi*13)+.13*cos(a*7+r*19)
    if motif=='tooth':
        return .63*sin(nx*2*pi*13)+.23*cos(ny*2*pi*6)+.14*sin((nx+ny)*2*pi*21)
    return 0

def field(seed,base,amp,motif):
    rng=random.Random(seed); phase=[rng.random()*2*pi for _ in range(3)]
    out=Image.new('L',(SIZE,SIZE)); p=out.load()
    for y in range(SIZE):
        ny=y/SIZE
        for x in range(SIZE):
            nx=x/SIZE
            broad=sin(nx*8+phase[0])*cos(ny*7+phase[1])+.32*sin((nx+ny)*16+phase[2])
            p[x,y]=clamp(base+amp*broad+amp*.58*relief(motif,x,y)+rng.uniform(-4,4))
    return out

def rgb(luma,tint): return Image.merge('RGB',tuple(luma.point(lambda v,c=c: clamp(v*c)) for c in tint))

def material(stem,seed,base,tint,rough,motif,normal_strength):
    h=field(seed,base,27,motif); rgb(h,tint).save(OUT/f'{stem}-albedo.png')
    rr=field(seed+101,rough,12,motif); rp=rr.load()
    for y in range(SIZE):
        for x in range(SIZE): rp[x,y]=clamp(rp[x,y]+15*relief(motif,x,y))
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

# Blackwater Lamprey is deliberately non-emissive. The mouth reads by wetness, radial relief and geometry.
material('dorsal-hide',3109,55,(.48,.65,.62),216,'dorsal',2.8)
material('ventral-hide',3221,102,(.66,.77,.68),178,'ventral',2.2)
material('fin-tissue',3331,68,(.50,.68,.64),198,'fin',2.5)
material('oral-tissue',3449,124,(.94,.43,.39),146,'oral',2.8)
material('tooth-enamel',3557,164,(.84,.81,.63),124,'tooth',2.0)

files=list(OUT.glob('*.png'))
if len(files)!=15: raise RuntimeError(f'Expected 15 Blackwater Lamprey texture maps, found {len(files)}')
if list(OUT.glob('*emission*')): raise RuntimeError('Blackwater Lamprey must not gain emissive texture maps')
for p in files:
    with Image.open(p) as im:
        if im.size!=(SIZE,SIZE): raise RuntimeError(f'{p.name}: wrong dimensions {im.size}')
        proxy=im.resize((READABILITY_SIZE,READABILITY_SIZE),Image.Resampling.LANCZOS)
        luma=proxy.convert('L'); lo,hi=luma.getextrema(); sigma=ImageStat.Stat(luma).stddev[0]
        if hi-lo<10 or sigma<2.2: raise RuntimeError(f'{p.name}: detail collapses at gameplay scale range={hi-lo}, sigma={sigma:.2f}')
        if p.name.endswith('-normal.png'):
            stat=ImageStat.Stat(proxy.convert('RGB')); xy=(stat.stddev[0]**2+stat.stddev[1]**2)**.5
            if xy<2.0 or stat.mean[2]<150: raise RuntimeError(f'{p.name}: normal response collapses xy={xy:.2f} z={stat.mean[2]:.1f}')
        elif p.name.endswith('-roughness.png') and (hi-lo<12 or sigma<3.0):
            raise RuntimeError(f'{p.name}: roughness response collapses range={hi-lo}, sigma={sigma:.2f}')
with Image.open(OUT/'dorsal-hide-albedo.png') as d, Image.open(OUT/'ventral-hide-albedo.png') as v:
    dm=ImageStat.Stat(d.resize((64,64)).convert('L')).mean[0]; vm=ImageStat.Stat(v.resize((64,64)).convert('L')).mean[0]
    if vm-dm<20: raise RuntimeError(f'Countershading collapsed: dorsal={dm:.1f} ventral={vm:.1f}')
with Image.open(OUT/'oral-tissue-albedo.png') as o, Image.open(OUT/'dorsal-hide-albedo.png') as d:
    om=ImageStat.Stat(o.resize((64,64)).convert('L')).mean[0]; dm=ImageStat.Stat(d.resize((64,64)).convert('L')).mean[0]
    if om-dm<24: raise RuntimeError(f'Oral-disc readability collapsed: oral={om:.1f} dorsal={dm:.1f}')
print(f'AUTHORED Blackwater Lamprey PBR foundation: {len(files)} maps at {SIZE}x{SIZE}; five material identities survive {READABILITY_SIZE}px gameplay proxy -> {OUT}',flush=True)
