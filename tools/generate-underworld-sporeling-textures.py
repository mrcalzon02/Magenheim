#!/usr/bin/env python3
"""Generate deterministic 1024px source textures for the Fungal Forest Sporeling."""
from pathlib import Path
from math import sin, cos, sqrt, atan2, pi
from PIL import Image, ImageChops
import random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/sporeling'; OUT.mkdir(parents=True,exist_ok=True)
SIZE=1024
READABILITY_SIZE=64

def clamp(v): return max(0,min(255,int(v)))
def field(seed, base, amp, grain=18, motif='flesh'):
    """Layer broad form, material-specific mesostructure and restrained micrograin."""
    rng=random.Random(seed); px=[]
    phases=[rng.random()*6.283 for _ in range(5)]
    for y in range(SIZE):
        for x in range(SIZE):
            nx=x/SIZE; ny=y/SIZE
            broad=sin(nx*17+phases[0])*cos(ny*13+phases[1])+.55*sin((nx+ny)*31+phases[2])
            if motif=='flesh': meso=.55*sin(ny*46+sin(nx*11)*2.4)+.30*sin((nx+ny)*79+phases[3])
            elif motif=='chitin':
                cx=(nx-.5)*2; cy=(ny-.5)*2; radius=sqrt(cx*cx+cy*cy); angle=atan2(cy,cx)
                meso=.62*cos(radius*55+sin(angle*7)*1.6)+.34*cos(angle*18+radius*13)
            elif motif=='joint': meso=.40*sin(nx*91+sin(ny*37)*2)+.40*cos(ny*87+sin(nx*29)*2)
            elif motif=='gill': meso=.78*cos(nx*64+sin(ny*13)*2.2)+.18*sin(ny*29+phases[4])
            elif motif=='sac':
                veins=abs(sin(nx*23+sin(ny*17)*2.5)); meso=.48*cos((nx+ny)*19)+.58*(1.0-min(1.0,veins*4.0))
            else: meso=0
            fine=sin(nx*137+phases[3])*cos(ny*127+phases[4]); n=rng.uniform(-1,1)*grain
            px.append(clamp(base+amp*broad+amp*.38*meso+amp*.10*fine+n))
    im=Image.new('L',(SIZE,SIZE)); im.putdata(px); return im

def rgb_from_luma(luma,tint): return Image.merge('RGB',tuple(luma.point(lambda v,c=c: clamp(v*c)) for c in tint))

def gameplay_relief(motif,x,y):
    """Low-frequency material structure deliberately survives 1024->64 mip filtering."""
    nx=x/SIZE; ny=y/SIZE
    if motif=='flesh': return .70*sin(ny*2*pi*7+sin(nx*2*pi*2)*.8)+.30*sin((nx+ny)*2*pi*5)
    if motif=='chitin':
        cx=nx-.5; cy=ny-.5; r=sqrt(cx*cx+cy*cy); a=atan2(cy,cx)
        return .72*cos(r*2*pi*9+sin(a*5))+.28*cos(a*8+r*7)
    if motif=='joint': return .55*sin(nx*2*pi*11+sin(ny*2*pi*3))+.45*cos(ny*2*pi*10)
    if motif=='gill': return .88*cos(nx*2*pi*10+sin(ny*2*pi*2)*.7)+.12*sin(ny*2*pi*5)
    if motif=='sac': return .62*cos((nx+ny)*2*pi*5)+.38*sin(nx*2*pi*6+sin(ny*2*pi*3))
    return 0

def roughness_map(seed,base,motif):
    """Give each tissue a broad specular identity that remains visible through mip filtering."""
    fine=field(seed,base,12,4,motif); fp=fine.load(); out=Image.new('L',(SIZE,SIZE)); op=out.load()
    amp={'flesh':15,'chitin':22,'joint':11,'gill':20,'sac':17}[motif]
    # Invert selected motifs: raised wet gills/sac catch more light while dry chitin ridges scatter it.
    sign=-1 if motif in ('gill','sac') else 1
    for y in range(SIZE):
        for x in range(SIZE): op[x,y]=clamp(fp[x,y]+sign*amp*gameplay_relief(motif,x,y))
    return out

def save_material(stem,seed,base,tint,rough_base,motif,emission=False):
    height=field(seed,base,27,7,motif); rgb_from_luma(height,tint).save(OUT/f'{stem}-albedo.png')
    roughness_map(seed+101,rough_base,motif).save(OUT/f'{stem}-roughness.png')
    p=height.load(); normal=Image.new('RGB',(SIZE,SIZE)); q=normal.load(); strength=2.4
    # Fine source height supplies close-up texture; gameplay_relief supplies a second, broad
    # normal band so the material still reads after engine mip filtering at combat distance.
    broad_strength={'flesh':.24,'chitin':.32,'joint':.20,'gill':.34,'sac':.26}[motif]
    for y in range(SIZE):
        ym=(y-1)%SIZE; yp=(y+1)%SIZE
        for x in range(SIZE):
            xm=(x-1)%SIZE; xp=(x+1)%SIZE
            dx=(p[xp,y]-p[xm,y])/255*strength; dy=(p[x,yp]-p[x,ym])/255*strength
            bx=gameplay_relief(motif,(x+8)%SIZE,y)-gameplay_relief(motif,(x-8)%SIZE,y)
            by=gameplay_relief(motif,x,(y+8)%SIZE)-gameplay_relief(motif,x,(y-8)%SIZE)
            dx+=bx*broad_strength; dy+=by*broad_strength
            mag=sqrt(dx*dx+dy*dy+1); q[x,y]=(clamp((-.5*dx/mag+.5)*255),clamp((-.5*dy/mag+.5)*255),clamp((.5/mag+.5)*255))
    normal.save(OUT/f'{stem}-normal.png')
    if emission:
        glow=field(seed+303,48,52,3,motif).point(lambda v: 0 if v<70 else clamp((v-70)*3.0)); mask=Image.new('L',(SIZE,SIZE),0); mp=mask.load()
        for y in range(SIZE):
            for x in range(SIZE):
                v=150+90*cos(x/32+sin(y/113)*1.7)+28*cos(y/91) if motif=='gill' else 128+80*sin(x/83+sin(y/119))+46*cos(y/71+x/137)
                mp[x,y]=clamp(max(0,v-150)*2.4)
        ImageChops.multiply(glow,mask).save(OUT/f'{stem}-emission.png')

save_material('flesh',11,118,(.72,.57,.78),188,'flesh')
save_material('cap-chitin',23,105,(.55,.86,.88),165,'chitin')
save_material('joint',37,72,(.60,.56,.66),220,'joint')
save_material('gill',41,120,(.62,.96,.87),142,'gill',True)
save_material('spore-sac',53,126,(.83,.65,.91),150,'sac',True)

expected=17; files=list(OUT.glob('*.png'))
if len(files)!=expected: raise RuntimeError(f'Expected {expected} Sporeling texture maps, found {len(files)}')
for p in files:
    with Image.open(p) as im:
        if im.size!=(SIZE,SIZE): raise RuntimeError(f'{p.name}: wrong dimensions {im.size}')
        luma=im.convert('L'); extrema=luma.getextrema()
        if extrema[1]-extrema[0]<16: raise RuntimeError(f'{p.name}: insufficient tonal range {extrema}')
        proxy=luma.resize((READABILITY_SIZE,READABILITY_SIZE),Image.Resampling.LANCZOS); proxy_extrema=proxy.getextrema()
        if proxy_extrema[1]-proxy_extrema[0]<8: raise RuntimeError(f'{p.name}: detail collapses at gameplay scale {proxy_extrema}')
        if p.name.endswith('-emission.png'):
            coverage=sum(1 for v in proxy.getdata() if v>=8)/(READABILITY_SIZE*READABILITY_SIZE)
            if coverage<.01 or coverage>.70: raise RuntimeError(f'{p.name}: emission coverage {coverage:.1%} is not localized/readable')
print(f'AUTHORED Sporeling dual-band PBR texture set: {len(files)} maps at {SIZE}x{SIZE}; broad normal and roughness identity survives {READABILITY_SIZE}px combat proxy -> {OUT}',flush=True)
