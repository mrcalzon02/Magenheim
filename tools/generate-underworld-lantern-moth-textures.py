#!/usr/bin/env python3
"""Author low-frequency production PBR source maps for the Fungal Forest Lantern Moth.

The art target is a moth, not a recolored Bat: charcoal fungal chitin, soft thorax fuzz,
pale translucent-looking wings with broad vein structure, dark antennae and restrained
biological light confined to wing cells. Texture frequency is deliberately broad so the
asset keeps Valheim-like readability instead of acquiring procedural/crinkled surface noise.
"""
from pathlib import Path
from PIL import Image, ImageFilter, ImageDraw
import math, random, statistics

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/lantern-moth'
OUT.mkdir(parents=True,exist_ok=True)
SIZE=1024
SEED=0x1A71_4D07

FAMILIES={
    'thorax-fuzz':((54,65,60),205,20),
    'abdomen-chitin':((44,55,51),165,24),
    'wing-membrane':((112,147,134),138,16),
    'antenna':((42,47,43),190,18),
    'eye':((18,23,21),88,10),
}

def clamp(v): return max(0,min(255,int(v)))

def broad_field(seed):
    rng=random.Random(seed)
    small=Image.new('L',(32,32))
    small.putdata([rng.randrange(256) for _ in range(32*32)])
    return small.resize((SIZE,SIZE),Image.Resampling.BICUBIC).filter(ImageFilter.GaussianBlur(15))

def make_family(stem,base,rough,normal,index):
    field=broad_field(SEED+index*137)
    fp=list(field.getdata())
    alb=[]; rou=[]; height=[]
    for p in fp:
        macro=(p-128)/128
        delta=(18 if stem=='thorax-fuzz' else 12 if stem=='wing-membrane' else 14)*macro
        alb.append(tuple(clamp(c+delta) for c in base))
        rou.append(clamp(rough+12*macro))
        height.append(clamp(128+normal*macro))
    a=Image.new('RGB',(SIZE,SIZE)); a.putdata(alb)
    r=Image.new('L',(SIZE,SIZE)); r.putdata(rou)
    h=Image.new('L',(SIZE,SIZE)); h.putdata(height); h=h.filter(ImageFilter.GaussianBlur(2.0))
    hp=h.load(); n=Image.new('RGB',(SIZE,SIZE)); np=n.load()
    for y in range(SIZE):
        ym=max(0,y-1); yp=min(SIZE-1,y+1)
        for x in range(SIZE):
            xm=max(0,x-1); xp=min(SIZE-1,x+1)
            dx=(hp[xp,y]-hp[xm,y])/255; dy=(hp[x,yp]-hp[x,ym])/255
            nx=-dx*1.6; ny=-dy*1.6; nz=1
            inv=1/math.sqrt(nx*nx+ny*ny+nz*nz)
            np[x,y]=(clamp(128+127*nx*inv),clamp(128+127*ny*inv),clamp(128+127*nz*inv))
    if stem=='wing-membrane':
        draw=ImageDraw.Draw(a,'RGBA')
        for side in (0,1):
            root=(SIZE//2, SIZE-80 if side==0 else 80)
            for i in range(7):
                end=(int((i+1)*SIZE/8), 160 if side==0 else SIZE-160)
                draw.line((root,end),fill=(58,83,73,90),width=11)
        a=a.filter(ImageFilter.GaussianBlur(.6))
    a.save(OUT/f'{stem}-albedo.png'); r.save(OUT/f'{stem}-roughness.png'); n.save(OUT/f'{stem}-normal.png')

for i,(stem,(base,rough,norm)) in enumerate(FAMILIES.items()):
    make_family(stem,base,rough,norm,i)

em=Image.new('L',(SIZE,SIZE),0); draw=ImageDraw.Draw(em)
for cx,cy,rx,ry in ((350,410,150,110),(674,410,150,110),(390,650,105,80),(634,650,105,80)):
    draw.ellipse((cx-rx,cy-ry,cx+rx,cy+ry),fill=155)
em=em.filter(ImageFilter.GaussianBlur(34))
em.save(OUT/'wing-membrane-emission.png')

expected={f'{s}-{k}.png' for s in FAMILIES for k in ('albedo','roughness','normal')}|{'wing-membrane-emission.png'}
actual={p.name for p in OUT.glob('*.png')}
if actual!=expected: raise RuntimeError(f'Lantern Moth texture set mismatch: missing={expected-actual}, extra={actual-expected}')
for p in OUT.glob('*.png'):
    im=Image.open(p)
    if im.size!=(SIZE,SIZE): raise RuntimeError(f'{p.name}: wrong dimensions {im.size}')
wing=Image.open(OUT/'wing-membrane-albedo.png').convert('L').resize((64,64),Image.Resampling.LANCZOS)
if statistics.pstdev(wing.getdata())<3.0: raise RuntimeError('Wing membrane collapsed to flat colour at gameplay scale')
emit=Image.open(OUT/'wing-membrane-emission.png').resize((64,64),Image.Resampling.LANCZOS)
lit=sum(1 for v in emit.getdata() if v>24)/(64*64)
if not .08<lit<.55: raise RuntimeError(f'Wing emission coverage is not restrained: {lit:.2%}')
print(f'AUTHORED {len(actual)} Lantern Moth 1024px PBR/source maps; broad-frequency material treatment, localized wing emission')
