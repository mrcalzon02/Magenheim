#!/usr/bin/env python3
"""Generate deterministic 1024px source textures for the Fungal Forest Capcrawler."""
from pathlib import Path
from math import sin, cos, sqrt, atan2, pi
from PIL import Image, ImageStat
import random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/capcrawler'; OUT.mkdir(parents=True,exist_ok=True)
SIZE=1024
READABILITY_SIZE=64

def clamp(v): return max(0,min(255,int(v)))
def relief(motif,x,y):
    nx=x/SIZE; ny=y/SIZE
    if motif=='carapace':
        cx=nx-.5; cy=ny-.5; r=sqrt(cx*cx+cy*cy); a=atan2(cy,cx)
        return .68*cos(r*2*pi*8+sin(a*6)*1.2)+.32*cos(a*10+r*9)
    if motif=='plate': return .62*sin(ny*2*pi*8+sin(nx*2*pi*3))+.38*cos(nx*2*pi*6)
    if motif=='flesh': return .72*sin(ny*2*pi*7+sin(nx*2*pi*2)*.8)+.28*sin((nx+ny)*2*pi*5)
    if motif=='mandible': return .78*cos(nx*2*pi*9+sin(ny*2*pi*3))+.22*cos(ny*2*pi*5)
    if motif=='gill': return .90*cos(nx*2*pi*11+sin(ny*2*pi*2)*.7)+.10*sin(ny*2*pi*4)
    return 0

def field(seed,base,amp,motif):
    rng=random.Random(seed); phase=[rng.random()*2*pi for _ in range(3)]; out=Image.new('L',(SIZE,SIZE)); p=out.load()
    for y in range(SIZE):
        for x in range(SIZE):
            nx=x/SIZE; ny=y/SIZE
            broad=sin(nx*15+phase[0])*cos(ny*12+phase[1])+.45*sin((nx+ny)*27+phase[2])
            p[x,y]=clamp(base+amp*broad+amp*.48*relief(motif,x,y)+rng.uniform(-5,5))
    return out

def rgb(luma,tint): return Image.merge('RGB',tuple(luma.point(lambda v,c=c: clamp(v*c)) for c in tint))
def material(stem,seed,base,tint,rough,motif,emission=False):
    h=field(seed,base,28,motif); rgb(h,tint).save(OUT/f'{stem}-albedo.png')
    rr=field(seed+91,rough,13,motif); rp=rr.load(); ramp={'carapace':24,'plate':18,'flesh':12,'mandible':22,'gill':18}[motif]
    for y in range(SIZE):
        for x in range(SIZE): rp[x,y]=clamp(rp[x,y]+ramp*relief(motif,x,y))
    rr.save(OUT/f'{stem}-roughness.png')
    hp=h.load(); n=Image.new('RGB',(SIZE,SIZE)); np=n.load(); broad={'carapace':.34,'plate':.28,'flesh':.20,'mandible':.32,'gill':.30}[motif]
    for y in range(SIZE):
        ym=(y-1)%SIZE; yp=(y+1)%SIZE
        for x in range(SIZE):
            xm=(x-1)%SIZE; xp=(x+1)%SIZE
            dx=(hp[xp,y]-hp[xm,y])/255*2.4+(relief(motif,(x+8)%SIZE,y)-relief(motif,(x-8)%SIZE,y))*broad
            dy=(hp[x,yp]-hp[x,ym])/255*2.4+(relief(motif,x,(y+8)%SIZE)-relief(motif,x,(y-8)%SIZE))*broad
            m=sqrt(dx*dx+dy*dy+1); np[x,y]=(clamp((-.5*dx/m+.5)*255),clamp((-.5*dy/m+.5)*255),clamp((.5/m+.5)*255))
    n.save(OUT/f'{stem}-normal.png')
    if emission:
        e=Image.new('L',(SIZE,SIZE)); ep=e.load()
        for y in range(SIZE):
            for x in range(SIZE): ep[x,y]=clamp(max(0,relief(motif,x,y)-.35)*125)
        e.save(OUT/f'{stem}-emission.png')

# Armored cap remains fungus-derived, but deliberately darker/heavier than the Sporeling's cyan cap.
material('cap-carapace',17,105,(.64,.82,.70),180,'carapace')
material('underside-flesh',29,112,(.72,.58,.68),196,'flesh')
material('leg-plate',41,91,(.58,.72,.64),205,'plate')
material('mandible',53,78,(.66,.62,.57),218,'mandible')
material('gill',67,118,(.58,.91,.73),150,'gill',True)

files=list(OUT.glob('*.png'))
if len(files)!=16: raise RuntimeError(f'Expected 16 Capcrawler texture maps, found {len(files)}')
for p in files:
    with Image.open(p) as im:
        if im.size!=(SIZE,SIZE): raise RuntimeError(f'{p.name}: wrong dimensions {im.size}')
        proxy=im.resize((READABILITY_SIZE,READABILITY_SIZE),Image.Resampling.LANCZOS)
        luma=proxy.convert('L'); lo,hi=luma.getextrema(); sigma=ImageStat.Stat(luma).stddev[0]
        if hi-lo<8 or sigma<2.0: raise RuntimeError(f'{p.name}: detail collapses at combat scale range={hi-lo}, sigma={sigma:.2f}')
        if p.name.endswith('-normal.png'):
            rgb_proxy=proxy.convert('RGB'); stat=ImageStat.Stat(rgb_proxy)
            xy_dev=(stat.stddev[0]**2+stat.stddev[1]**2)**.5
            if xy_dev<2.2: raise RuntimeError(f'{p.name}: normal relief collapses at combat scale xy-dev={xy_dev:.2f}')
            if stat.mean[2]<150: raise RuntimeError(f'{p.name}: normal map loses +Z orientation at combat scale z={stat.mean[2]:.1f}')
        elif p.name.endswith('-roughness.png') and (hi-lo<12 or sigma<3.0):
            raise RuntimeError(f'{p.name}: roughness response collapses at combat scale range={hi-lo}, sigma={sigma:.2f}')
        elif p.name.endswith('-emission.png'):
            cov=sum(v>=8 for v in luma.getdata())/(READABILITY_SIZE*READABILITY_SIZE)
            if not .01<=cov<=.55: raise RuntimeError(f'{p.name}: emission coverage {cov:.1%} is not localized')
print(f'AUTHORED Capcrawler PBR texture set: {len(files)} maps at {SIZE}x{SIZE}; albedo, roughness, normal and emission structure survive {READABILITY_SIZE}px combat proxy -> {OUT}',flush=True)
