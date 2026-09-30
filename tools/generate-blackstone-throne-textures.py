#!/usr/bin/env python3
"""Generate deterministic 2D source artwork for the Blackstone Throne structure family."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import math, random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/"assets"/"textures"/"underworld"/"blackstone"
OUT.mkdir(parents=True,exist_ok=True)
SIZE=512

def noise_surface(name, base, grain, veins=None, seed=1):
    rng=random.Random(seed)
    im=Image.new("RGB",(SIZE,SIZE),base)
    px=im.load()
    for y in range(SIZE):
        for x in range(SIZE):
            broad=7*math.sin(x*.021)+5*math.sin((x+y)*.011)+4*math.cos(y*.017)
            n=rng.randint(-grain,grain)
            px[x,y]=tuple(max(0,min(255,int(c+broad+n))) for c in base)
    draw=ImageDraw.Draw(im,"RGBA")
    for _ in range(28 if veins else 10):
        x=rng.randrange(SIZE); y=rng.randrange(SIZE)
        pts=[(x,y)]
        for __ in range(rng.randrange(3,8)):
            x=max(0,min(SIZE-1,x+rng.randrange(-70,71)))
            y=max(0,min(SIZE-1,y+rng.randrange(18,90)))
            pts.append((x,y))
        draw.line(pts,fill=veins or (8,10,14,90),width=rng.randrange(1,4))
    im=im.filter(ImageFilter.GaussianBlur(.35))
    im.save(OUT/name)

def banner():
    im=Image.new("RGB",(SIZE,SIZE),(24,22,29))
    draw=ImageDraw.Draw(im,"RGBA")
    rng=random.Random(91)
    for y in range(SIZE):
        shade=int(10*math.sin(y*.035)+5*math.sin(y*.111))
        draw.line((0,y,SIZE,y),fill=(max(8,30+shade),max(7,27+shade),max(10,35+shade),255))
    for x in range(0,SIZE,34):
        draw.line((x,0,x+8,SIZE),fill=(255,255,255,8),width=2)
    cx,cy=SIZE//2,SIZE//2-18
    gold=(145,76,41,255); dark=(31,24,29,255)
    r=72
    draw.ellipse((cx-r,cy-r,cx+r,cy+r),outline=gold,width=14)
    draw.ellipse((cx-r+18,cy-r+18,cx+r-18,cy+r-18),fill=dark)
    for i in range(16):
        a=i*math.tau/16
        p1=(cx+math.cos(a)*93,cy+math.sin(a)*93)
        p2=(cx+math.cos(a)*137,cy+math.sin(a)*137)
        draw.line((p1,p2),fill=gold,width=9)
    draw.rectangle((cx-3,cy+140,cx+3,SIZE),fill=(125,62,36,120))
    im.save(OUT/"blackstone-banner-sun-albedo.png")
    sig=Image.new("RGBA",(SIZE,SIZE),(0,0,0,0)); sd=ImageDraw.Draw(sig)
    sd.ellipse((cx-r,cy-r,cx+r,cy+r),outline=gold,width=14)
    for i in range(16):
        a=i*math.tau/16
        sd.line((cx+math.cos(a)*93,cy+math.sin(a)*93,cx+math.cos(a)*137,cy+math.sin(a)*137),fill=gold,width=9)
    sig.save(OUT/"blackstone-sun-sigil.png")

def ember():
    im=Image.new("RGB",(SIZE,SIZE),(35,11,5)); d=ImageDraw.Draw(im,"RGBA"); rng=random.Random(122)
    for _ in range(180):
        x=rng.randrange(SIZE); y=rng.randrange(SIZE); r=rng.randrange(2,14)
        c=rng.choice(((255,76,12,240),(255,138,22,220),(255,206,76,180),(132,26,9,220)))
        d.ellipse((x-r,y-r,x+r,y+r),fill=c)
    im=im.filter(ImageFilter.GaussianBlur(1.2)); im.save(OUT/"blackstone-ember-albedo.png")

noise_surface("blackstone-basalt-albedo.png",(29,32,40),9,(12,10,18,120),71)
noise_surface("blackstone-voidstone-albedo.png",(10,11,17),5,(44,23,54,90),72)
noise_surface("blackstone-bronze-albedo.png",(91,52,38),8,(164,85,42,70),73)
banner(); ember()
print("GENERATED Blackstone Throne 2D source artwork:",", ".join(sorted(p.name for p in OUT.glob("blackstone-*.png"))))
