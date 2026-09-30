#!/usr/bin/env python3
"""Generate deterministic 2D source artwork for the Blackstone Throne structure family."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import math, random

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/"assets"/"textures"/"underworld"/"blackstone"
PBR=ROOT/"assets"/"material-source"/"blackstone"
OUT.mkdir(parents=True,exist_ok=True)
PBR.mkdir(parents=True,exist_ok=True)
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
    im=Image.new("RGB",(SIZE,SIZE),(18,17,24))
    draw=ImageDraw.Draw(im,"RGBA"); rng=random.Random(91)
    for y in range(SIZE):
        shade=int(5*math.sin(y*.020)+3*math.sin(y*.061))
        base=(22+shade,20+shade,29+shade)
        draw.line((0,y,SIZE,y),fill=(*base,255))
    for _ in range(18):
        x=rng.randrange(18,SIZE-18)
        draw.line((x,0,x+rng.randrange(-10,11),SIZE),fill=(255,255,255,rng.randrange(3,8)),width=1)
    bronze=(118,63,42,255); bronze_soft=(104,54,39,145); dark=(18,16,23,255)
    draw.line((30,16,30,SIZE-22),fill=bronze_soft,width=3)
    draw.line((SIZE-31,16,SIZE-31,SIZE-22),fill=bronze_soft,width=3)
    cx,cy=SIZE//2,198; r=74
    draw.ellipse((cx-r,cy-r,cx+r,cy+r),outline=bronze,width=12)
    draw.ellipse((cx-r+18,cy-r+18,cx+r-18,cy+r-18),fill=dark)
    for i in range(14):
        a=i*math.tau/14
        p1=(cx+math.cos(a)*94,cy+math.sin(a)*94)
        p2=(cx+math.cos(a)*126,cy+math.sin(a)*126)
        draw.line((p1,p2),fill=bronze,width=7)
    im.save(OUT/"blackstone-banner-sun-albedo.png")
    sig=Image.new("RGBA",(SIZE,SIZE),(0,0,0,0)); sd=ImageDraw.Draw(sig)
    sd.ellipse((cx-r,cy-r,cx+r,cy+r),outline=bronze,width=12)
    for i in range(14):
        a=i*math.tau/14
        sd.line((cx+math.cos(a)*94,cy+math.sin(a)*94,cx+math.cos(a)*126,cy+math.sin(a)*126),fill=bronze,width=7)
    sig.save(OUT/"blackstone-sun-sigil.png")

def ember():
    im=Image.new("RGB",(SIZE,SIZE),(35,11,5)); d=ImageDraw.Draw(im,"RGBA"); rng=random.Random(122)
    for _ in range(180):
        x=rng.randrange(SIZE); y=rng.randrange(SIZE); r=rng.randrange(2,14)
        c=rng.choice(((255,76,12,240),(255,138,22,220),(255,206,76,180),(132,26,9,220)))
        d.ellipse((x-r,y-r,x+r,y+r),fill=c)
    im=im.filter(ImageFilter.GaussianBlur(1.2)); im.save(OUT/"blackstone-ember-albedo.png")

def flame():
    im=Image.new("RGB",(SIZE,SIZE),(62,8,2)); px=im.load()
    for y in range(SIZE):
        v=1.0-y/(SIZE-1)
        for x in range(SIZE):
            u=abs((x/(SIZE-1))-.5)*2.0
            heat=max(0.0,min(1.0,(1.0-u)*(.45+.55*v)))
            px[x,y]=(int(130+125*heat),int(28+150*heat),int(4+55*heat))
    im=im.filter(ImageFilter.GaussianBlur(.65))
    im.save(OUT/"blackstone-flame-albedo.png")

def pbr_family(albedo_name,key,metallic,smoothness,normal_strength=1.6,emissive=False):
    """Derive deterministic owned runtime maps from the authored Blackstone albedo.

    The albedo remains the visual authority. Height is intentionally shallow: broad Blackstone
    surfaces should read as dressed material, not crusted detail over every square centimetre.
    """
    source=Image.open(OUT/albedo_name).convert("RGB")
    height=source.convert("L").filter(ImageFilter.GaussianBlur(1.15))
    h=height.load()
    normal=Image.new("RGB",(SIZE,SIZE)); np=normal.load()
    packed=Image.new("RGBA",(SIZE,SIZE)); pp=packed.load()
    emission=Image.new("RGB",(SIZE,SIZE),(0,0,0)) if emissive else None
    ep=emission.load() if emission else None
    sp=source.load()
    for y in range(SIZE):
        ym=max(0,y-1); yp=min(SIZE-1,y+1)
        for x in range(SIZE):
            xm=max(0,x-1); xp=min(SIZE-1,x+1)
            dx=(h[xp,y]-h[xm,y])/255.0*normal_strength
            dy=(h[x,yp]-h[x,ym])/255.0*normal_strength
            nx,ny,nz=-dx,-dy,1.0
            mag=math.sqrt(nx*nx+ny*ny+nz*nz)
            np[x,y]=(int((nx/mag*.5+.5)*255),int((ny/mag*.5+.5)*255),int((nz/mag*.5+.5)*255))
            local=(h[x,y]-128)/128.0
            sm=max(0.04,min(.96,smoothness-local*.035))
            pp[x,y]=(int(max(0,min(1,metallic))*255),0,0,int(sm*255))
            if ep is not None:
                r,g,b=sp[x,y]
                energy=max(r,g,b)/255.0
                ep[x,y]=(int(r*energy),int(g*energy),int(b*energy))
    normal.save(PBR/(key+"-normal.png"))
    packed.save(PBR/(key+"-metallic-smoothness.png"))
    if emission is not None:
        emission.filter(ImageFilter.GaussianBlur(.65)).save(PBR/(key+"-emission.png"))

noise_surface("blackstone-basalt-albedo.png",(29,32,40),9,(12,10,18,120),71)
noise_surface("blackstone-voidstone-albedo.png",(10,11,17),5,(44,23,54,90),72)
noise_surface("blackstone-bronze-albedo.png",(91,52,38),8,(164,85,42,70),73)
banner(); ember(); flame()
pbr_family("blackstone-basalt-albedo.png","blackstone-basalt",.02,.22,1.25)
pbr_family("blackstone-voidstone-albedo.png","blackstone-voidstone",.05,.34,1.05)
pbr_family("blackstone-bronze-albedo.png","blackstone-royal-bronze",.82,.68,.72)
pbr_family("blackstone-banner-sun-albedo.png","blackstone-banner-cloth",0,.18,.48)
pbr_family("blackstone-ember-albedo.png","blackstone-ember",.04,.76,.58,True)
pbr_family("blackstone-flame-albedo.png","blackstone-flame",0,.80,.30,True)
print("GENERATED Blackstone Throne 2D source artwork:",
      len(list(OUT.glob("blackstone-*.png"))),"albedo/graphic files +",
      len(list(PBR.glob("blackstone-*.png"))),"owned PBR maps")
