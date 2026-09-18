#!/usr/bin/env python3
"""Generate deterministic 1024px source textures for the Fungal Forest Sporeling."""
from pathlib import Path
from math import sin, cos, sqrt
from PIL import Image, ImageChops
import random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/sporeling'; OUT.mkdir(parents=True,exist_ok=True)
SIZE=1024
READABILITY_SIZE=64

def clamp(v): return max(0,min(255,int(v)))
def field(seed, base, amp, grain=18):
    rng=random.Random(seed); px=[]
    phases=[rng.random()*6.283 for _ in range(5)]
    for y in range(SIZE):
        for x in range(SIZE):
            nx=x/SIZE; ny=y/SIZE
            broad=sin(nx*17+phases[0])*cos(ny*13+phases[1])+0.55*sin((nx+ny)*31+phases[2])
            fine=sin(nx*73+phases[3])*cos(ny*67+phases[4])
            n=rng.uniform(-1,1)*grain
            px.append(clamp(base+amp*broad+amp*.22*fine+n))
    im=Image.new('L',(SIZE,SIZE)); im.putdata(px); return im

def rgb_from_luma(luma, tint):
    return Image.merge('RGB',tuple(luma.point(lambda v,c=c: clamp(v*c)) for c in tint))

def save_material(stem, seed, base, tint, rough_base, emission=False):
    height=field(seed,base,27,9)
    albedo=rgb_from_luma(height,tint); albedo.save(OUT/f'{stem}-albedo.png')
    rough=field(seed+101,rough_base,20,7); rough.save(OUT/f'{stem}-roughness.png')
    # Tangent-space normal from central height differences.
    p=height.load(); normal=Image.new('RGB',(SIZE,SIZE)); q=normal.load(); strength=2.4
    for y in range(SIZE):
        ym=(y-1)%SIZE; yp=(y+1)%SIZE
        for x in range(SIZE):
            xm=(x-1)%SIZE; xp=(x+1)%SIZE
            dx=(p[xp,y]-p[xm,y])/255*strength; dy=(p[x,yp]-p[x,ym])/255*strength
            z=1.0; mag=sqrt(dx*dx+dy*dy+z*z); q[x,y]=(clamp((-.5*dx/mag+.5)*255),clamp((-.5*dy/mag+.5)*255),clamp((z/mag*.5+.5)*255))
    normal.save(OUT/f'{stem}-normal.png')
    if emission:
        glow=field(seed+303,48,52,4).point(lambda v: 0 if v<70 else clamp((v-70)*3.0))
        # Broad organic islands, not full-body glow.
        mask=Image.new('L',(SIZE,SIZE),0); mp=mask.load()
        for y in range(SIZE):
            for x in range(SIZE):
                v=128+80*sin(x/83+sin(y/119))+46*cos(y/71+x/137)
                mp[x,y]=clamp(max(0,v-135)*2.2)
        ImageChops.multiply(glow,mask).save(OUT/f'{stem}-emission.png')

save_material('flesh',11,118,(0.72,0.57,0.78),188)
save_material('cap-chitin',23,105,(0.55,0.86,0.88),165)
save_material('joint',37,72,(0.60,0.56,0.66),220)
save_material('gill',41,120,(0.62,0.96,0.87),142,True)
save_material('spore-sac',53,126,(0.83,0.65,0.91),150,True)

expected=3*5+2
files=list(OUT.glob('*.png'))
if len(files)!=expected: raise RuntimeError(f'Expected {expected} Sporeling texture maps, found {len(files)}')
for p in files:
    with Image.open(p) as im:
        if im.size!=(SIZE,SIZE): raise RuntimeError(f'{p.name}: wrong dimensions {im.size}')
        luma=im.convert('L'); extrema=luma.getextrema()
        if extrema[1]-extrema[0]<16: raise RuntimeError(f'{p.name}: insufficient tonal range {extrema}')
        # Source resolution alone is not useful if all readable structure disappears after
        # Valheim mipmapping/UI distance. Preserve medium-frequency contrast at a 64px proxy.
        proxy=luma.resize((READABILITY_SIZE,READABILITY_SIZE),Image.Resampling.LANCZOS)
        proxy_extrema=proxy.getextrema()
        if proxy_extrema[1]-proxy_extrema[0]<8:
            raise RuntimeError(f'{p.name}: detail collapses at gameplay scale {proxy_extrema}')
        if p.name.endswith('-emission.png'):
            lit=sum(1 for v in proxy.getdata() if v>=8)
            coverage=lit/(READABILITY_SIZE*READABILITY_SIZE)
            if coverage<.01 or coverage>.70:
                raise RuntimeError(f'{p.name}: emission coverage {coverage:.1%} is not localized/readable')
print(f'AUTHORED Sporeling texture set: {len(files)} maps at {SIZE}x{SIZE}; {READABILITY_SIZE}px readability proxy passed -> {OUT}',flush=True)