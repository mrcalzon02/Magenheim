#!/usr/bin/env python3
"""Generate deterministic 1024px PBR source textures for the Blackwater Deep Gloomfin."""
from pathlib import Path
from math import sin, cos, sqrt, atan2, pi
from PIL import Image, ImageStat
import random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/gloomfin'; OUT.mkdir(parents=True,exist_ok=True)
SIZE=1024; READABILITY_SIZE=64

def clamp(v): return max(0,min(255,int(v)))

def relief(motif,x,y):
    nx=x/SIZE; ny=y/SIZE
    if motif=='dorsal':
        # Dense hydrodynamic hide: longitudinal muscle bands broken by irregular transverse scars.
        return .50*sin(ny*2*pi*7+sin(nx*2*pi*3)*.55)+.31*cos((nx*.45+ny)*2*pi*15)+.19*sin(nx*2*pi*5)
    if motif=='ventral':
        # Paler countershaded belly with softer compression folds.
        return .58*sin(nx*2*pi*8+sin(ny*2*pi*2)*.45)+.27*cos(ny*2*pi*10)+.15*sin((nx+ny)*2*pi*17)
    if motif=='fin':
        # Directional fin rays make the three dorsal fins readable without emission.
        cx=nx-.12; cy=ny-.5; r=sqrt(cx*cx+cy*cy); a=atan2(cy,cx)
        return .62*cos(a*13+r*2)+.25*sin(r*2*pi*11)+.13*cos((nx-ny)*2*pi*19)
    if motif=='mouth':
        # Wet folded oral tissue; coarse enough to survive gameplay distance.
        return .54*sin(nx*2*pi*6+sin(ny*2*pi*3))+.30*cos(ny*2*pi*12)+.16*sin((nx+ny)*2*pi*20)
    if motif=='tooth':
        # Mineral enamel with subtle longitudinal growth striation, never emissive.
        return .64*sin(nx*2*pi*12)+.22*cos(ny*2*pi*5)+.14*sin((nx+ny)*2*pi*23)
    return 0

def field(seed,base,amp,motif):
    rng=random.Random(seed); phase=[rng.random()*2*pi for _ in range(3)]
    out=Image.new('L',(SIZE,SIZE)); p=out.load()
    for y in range(SIZE):
        ny=y/SIZE
        for x in range(SIZE):
            nx=x/SIZE
            broad=sin(nx*9+phase[0])*cos(ny*7+phase[1])+.34*sin((nx+ny)*17+phase[2])
            p[x,y]=clamp(base+amp*broad+amp*.58*relief(motif,x,y)+rng.uniform(-4,4))
    return out

def rgb(luma,tint): return Image.merge('RGB',tuple(luma.point(lambda v,c=c: clamp(v*c)) for c in tint))

def material(stem,seed,base,tint,rough,motif,normal_strength):
    h=field(seed,base,28,motif); rgb(h,tint).save(OUT/f'{stem}-albedo.png')
    rr=field(seed+101,rough,12,motif); rp=rr.load()
    for y in range(SIZE):
        for x in range(SIZE): rp[x,y]=clamp(rp[x,y]+16*relief(motif,x,y))
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

# Blackwater countershading stays extremely dark. Silhouette readability comes from geometry,
# directional fin relief and material response rather than bioluminescence.
material('dorsal-hide',2309,58,(.48,.64,.65),220,'dorsal',2.9)
material('ventral-hide',2417,104,(.65,.78,.74),184,'ventral',2.3)
material('fin-tissue',2521,72,(.52,.72,.72),202,'fin',2.7)
material('mouth-tissue',2633,118,(.92,.48,.43),154,'mouth',2.4)
material('tooth-enamel',2741,158,(.86,.83,.67),126,'tooth',2.0)

files=list(OUT.glob('*.png'))
if len(files)!=15: raise RuntimeError(f'Expected 15 Gloomfin texture maps, found {len(files)}')
if list(OUT.glob('*emission*')): raise RuntimeError('Gloomfin must not gain emissive texture maps')
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
# Countershade gate: dorsal remains materially darker than the belly at gameplay distance.
with Image.open(OUT/'dorsal-hide-albedo.png') as d, Image.open(OUT/'ventral-hide-albedo.png') as v:
    dm=ImageStat.Stat(d.resize((64,64)).convert('L')).mean[0]; vm=ImageStat.Stat(v.resize((64,64)).convert('L')).mean[0]
    if vm-dm<22: raise RuntimeError(f'Countershading collapsed: dorsal={dm:.1f} ventral={vm:.1f}')
print(f'AUTHORED Gloomfin PBR foundation: {len(files)} maps at {SIZE}x{SIZE}; five material identities survive {READABILITY_SIZE}px gameplay proxy -> {OUT}',flush=True)
