#!/usr/bin/env python3
"""Generate deterministic source textures for the first Fungal Forest flora family.

Produces 512px purpose-authored maps for the materials used by
`author-fungal-forest-flora.py`: fibrous stalk, cap skin and luminous gills.  The maps use
large coherent directional features rather than pixel noise so they survive Valheim camera
distance and mipmapping.  This is source art; Blender material hookup/export is a separate
bounded slice.

Usage:
    python tools/generate-fungal-forest-textures.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import math, random

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'assets' / 'textures' / 'underworld' / 'fungal-forest'
OUT.mkdir(parents=True, exist_ok=True)
SIZE = 512
SEED = 0xF061A1


def clamp(v): return max(0, min(255, int(v)))

def save(img, name):
    path = OUT / name
    img.save(path, optimize=True)
    print(f'GENERATED {path.relative_to(ROOT)}')


def fibre():
    rng=random.Random(SEED+1)
    base=Image.new('RGB',(SIZE,SIZE),(67,52,43)); d=ImageDraw.Draw(base,'RGBA')
    # Long vertical fibres: broad structure first, fine striation second.
    for i in range(145):
        x=rng.randrange(-12,SIZE+12); w=rng.randint(1,5); phase=rng.random()*math.tau
        pts=[]
        for y in range(-8,SIZE+9,8):
            drift=math.sin(y*.027+phase)*rng.uniform(1.5,6.0)
            pts.append((x+drift,y))
        tone=rng.choice([(121,94,73,38),(29,24,22,42),(164,127,91,24)])
        d.line(pts,fill=tone,width=w)
    for i in range(34):
        y=rng.randrange(SIZE); x=rng.randrange(SIZE)
        d.ellipse((x-5,y-2,x+5,y+2),fill=(31,25,23,35))
    save(base.filter(ImageFilter.GaussianBlur(.35)),'fungal-fibre-albedo.png')


def cap_skin():
    rng=random.Random(SEED+2)
    img=Image.new('RGB',(SIZE,SIZE),(82,57,84)); d=ImageDraw.Draw(img,'RGBA')
    cx=SIZE*.5
    # Radial growth bands and sparse mottling make the cap read as grown tissue, not stone.
    for r in range(28,370,15):
        alpha=max(8,42-r//12)
        d.arc((cx-r,-r*.30,cx+r,r*1.70),195,345,fill=(172,125,166,alpha),width=3)
    for i in range(520):
        x=rng.randrange(SIZE); y=rng.randrange(SIZE); rr=rng.randint(2,11)
        tone=rng.choice([(194,150,181,15),(34,27,42,19),(116,77,123,18)])
        d.ellipse((x-rr,y-rr,x+rr,y+rr),fill=tone)
    save(img.filter(ImageFilter.GaussianBlur(.65)),'fungal-cap-albedo.png')


def gills():
    rng=random.Random(SEED+3)
    albedo=Image.new('RGB',(SIZE,SIZE),(48,92,83)); a=ImageDraw.Draw(albedo,'RGBA')
    emission=Image.new('L',(SIZE,SIZE),28); e=ImageDraw.Draw(emission)
    cx,cy=SIZE//2,SIZE//2
    for i in range(72):
        ang=math.tau*i/72 + rng.uniform(-.018,.018)
        inner=22+rng.randrange(0,10); outer=350+rng.randrange(-22,22)
        p0=(cx+math.cos(ang)*inner,cy+math.sin(ang)*inner)
        p1=(cx+math.cos(ang)*outer,cy+math.sin(ang)*outer)
        a.line((p0,p1),fill=(154,224,193,105),width=rng.choice((2,3,4)))
        e.line((p0,p1),fill=rng.randrange(150,225),width=rng.choice((2,3,4)))
    # Soft central glow avoids a dead dark hub when viewed from directly below.
    glow=Image.new('L',(SIZE,SIZE),0); gd=ImageDraw.Draw(glow)
    gd.ellipse((cx-112,cy-112,cx+112,cy+112),fill=180)
    glow=glow.filter(ImageFilter.GaussianBlur(54))
    emission=Image.blend(emission,Image.new('L',(SIZE,SIZE),255),.08)
    emission=Image.max(emission,glow)
    save(albedo.filter(ImageFilter.GaussianBlur(.25)),'fungal-gills-albedo.png')
    save(emission,'fungal-gills-emission.png')


def validate():
    expected=('fungal-fibre-albedo.png','fungal-cap-albedo.png','fungal-gills-albedo.png','fungal-gills-emission.png')
    for name in expected:
        p=OUT/name
        with Image.open(p) as im:
            if im.size != (SIZE,SIZE): raise RuntimeError(f'{name}: wrong dimensions {im.size}')
            extrema=im.convert('L').getextrema()
            if extrema[1]-extrema[0] < 18: raise RuntimeError(f'{name}: insufficient tonal range {extrema}')
    print(f'Validated {len(expected)} Fungal Forest texture maps at {SIZE}px.')

fibre(); cap_skin(); gills(); validate()
