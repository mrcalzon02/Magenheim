#!/usr/bin/env python3
"""Generate deterministic 2048px PBR source textures for the Fungal Forest Crowncap Brute."""
from pathlib import Path
from math import sin, cos, sqrt, atan2, pi
from PIL import Image, ImageStat
import random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/crowncap-brute'; OUT.mkdir(parents=True,exist_ok=True)
SIZE=2048; READABILITY_SIZE=64

def flat(im):
    reader=getattr(im,'get_flattened_data',None)
    return list(reader() if reader else im.getdata())
def clamp(v): return max(0,min(255,int(v)))

def relief(motif,x,y):
    nx=x/SIZE; ny=y/SIZE
    if motif=='hide':
        # Dense load-bearing root hide: broad compression folds and cross-grain scars, never mammal fur.
        return .56*sin(ny*2*pi*8+sin(nx*2*pi*3)*1.0)+.27*sin((nx*.8+ny)*2*pi*18)+.17*cos(nx*2*pi*6)
    if motif=='crown':
        # Layered crown armor: coarse radial growth rings plus broken edge fissures.
        cx=nx-.5; cy=ny-.5; r=sqrt(cx*cx+cy*cy); a=atan2(cy,cx)
        return .66*cos(r*2*pi*10+sin(a*5)*.7)+.22*cos(a*9+r*5)+.12*sin((nx+ny)*2*pi*21)
    if motif=='flesh':
        # Pale mycelial tissue: rope-like longitudinal cords with restrained cross-webbing.
        return .62*sin(ny*2*pi*13+sin(nx*2*pi*5)*.65)+.24*cos(nx*2*pi*10)+.14*sin((nx+ny)*2*pi*25)
    if motif=='gill':
        # Exposed under-cap gills: strong lamellae remain readable as the elite turns or staggers.
        return .76*sin(nx*2*pi*17+sin(ny*2*pi*2)*.55)+.16*cos(ny*2*pi*7)+.08*sin((nx+ny)*2*pi*29)
    return 0

def field(seed,base,amp,motif):
    rng=random.Random(seed); phase=[rng.random()*2*pi for _ in range(3)]
    out=Image.new('L',(SIZE,SIZE)); p=out.load()
    for y in range(SIZE):
        ny=y/SIZE
        for x in range(SIZE):
            nx=x/SIZE
            broad=sin(nx*11+phase[0])*cos(ny*8+phase[1])+.38*sin((nx+ny)*20+phase[2])
            p[x,y]=clamp(base+amp*broad+amp*.56*relief(motif,x,y)+rng.uniform(-4,4))
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
            for x in range(SIZE): ep[x,y]=clamp(max(0,relief(motif,x,y)-.40)*96)
        e.save(OUT/f'{stem}-emission.png')

# Elite ground-control silhouette: dark root mass, dry layered crown, pale cords, exposed warm gills.
# Emission is anatomically restricted to under-cap gills; crown/shoulders must read through mass and roughness.
material('root-hide',1013,74,(.76,.63,.47),222,'hide',3.2)
material('crown-plate',1129,104,(.82,.68,.38),211,'crown',3.1)
material('mycelial-flesh',1237,136,(.88,.82,.62),181,'flesh',2.6)
material('under-cap-gill',1361,144,(.94,.58,.38),154,'gill',2.7,True)

files=list(OUT.glob('*.png'))
if len(files)!=13: raise RuntimeError(f'Expected 13 Crowncap Brute texture maps, found {len(files)}')
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
            if not .02<=coverage<=.38: raise RuntimeError(f'{p.name}: gill emission coverage {coverage:.1%} is not localized')
print(f'AUTHORED Crowncap Brute PBR foundation: {len(files)} maps at {SIZE}x{SIZE}; four material identities survive {READABILITY_SIZE}px combat proxy -> {OUT}',flush=True)
