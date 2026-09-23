#!/usr/bin/env python3
"""Generate deterministic production PBR textures for the Sulfurous Wastes Basalt Crawler.

The crawler reads as a geological defensive animal: dry fractured basalt slabs, warmer
fresh fracture/keel surfaces, mineral-packed plate overlaps, abraded impact anatomy,
and wet protected tissue. No emission maps are authored; silhouette and material response
must carry the creature at Valheim gameplay distance.
"""
from pathlib import Path
from PIL import Image, ImageFilter
import math, random, statistics

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/basalt-crawler'
OUT.mkdir(parents=True,exist_ok=True)
SIZE=1024
SEED=0xBA5A_17C4
FAMILIES={
    'basalt-plate':((45,43,40),218,46),
    'fracture-edge':((91,67,49),181,39),
    'mineral-overlap':((128,113,78),202,31),
    'impact-abrasion':((72,63,54),194,42),
    'protected-tissue':((91,47,39),92,24),
    'recessed-eye':((25,17,14),61,18),
}

def clamp(v): return max(0,min(255,int(v)))
def fields(seed):
    rng=random.Random(seed)
    small=Image.new('L',(64,64)); small.putdata([rng.randrange(256) for _ in range(64*64)])
    low=small.resize((SIZE,SIZE),Image.Resampling.BICUBIC).filter(ImageFilter.GaussianBlur(11))
    fine=Image.effect_noise((SIZE,SIZE),48).filter(ImageFilter.GaussianBlur(.7))
    return low,fine

def cracks(seed,count,width=2):
    rng=random.Random(seed); im=Image.new('L',(SIZE,SIZE),0); px=im.load()
    for _ in range(count):
        x=rng.randrange(SIZE); y=rng.randrange(SIZE); ang=rng.random()*math.tau
        for step in range(rng.randrange(65,220)):
            ang+=rng.uniform(-.12,.12); x+=math.cos(ang)*2.7; y+=math.sin(ang)*2.7
            if not (3<=x<SIZE-3 and 3<=y<SIZE-3): break
            rad=width+(1 if step%17==0 else 0)
            for yy in range(int(y)-rad,int(y)+rad+1):
                for xx in range(int(x)-rad,int(x)+rad+1):
                    d=((xx-x)**2+(yy-y)**2)**.5
                    if d<=rad: px[xx,yy]=max(px[xx,yy],clamp(255*(1-d/(rad+.01))))
    return im.filter(ImageFilter.GaussianBlur(.6))

def make(stem,base,rough_base,norm,index):
    low,fine=fields(SEED+index*137); fiss=cracks(SEED+index*359,58 if stem=='basalt-plate' else 34,3 if stem=='basalt-plate' else 2)
    lp=list(low.getdata()); fp=list(fine.getdata()); cp=list(fiss.getdata()); alb=[]; rough=[]; height=[]
    for l,f,c in zip(lp,fp,cp):
        macro=(l-128)/128; micro=(f-128)/128; fracture=c/255
        if stem=='basalt-plate': delta=13*macro+9*micro-43*fracture
        elif stem=='fracture-edge': delta=16*macro+8*micro+15*fracture
        elif stem=='mineral-overlap': delta=20*macro+7*micro-8*fracture
        elif stem=='impact-abrasion': delta=11*macro+6*micro+24*fracture
        elif stem=='protected-tissue': delta=18*macro+8*micro-5*fracture
        else: delta=6*macro+3*micro
        alb.append(tuple(clamp(ch+delta) for ch in base))
        rough.append(clamp(rough_base+18*macro+8*micro+(24 if stem in ('basalt-plate','impact-abrasion') else 5)*fracture))
        height.append(clamp(128+norm*(.55*macro+.25*micro-.82*fracture)))
    a=Image.new('RGB',(SIZE,SIZE)); a.putdata(alb); a.save(OUT/f'{stem}-albedo.png')
    r=Image.new('L',(SIZE,SIZE)); r.putdata(rough); r.save(OUT/f'{stem}-roughness.png')
    h=Image.new('L',(SIZE,SIZE)); h.putdata(height); h=h.filter(ImageFilter.GaussianBlur(.5)); hp=h.load(); n=Image.new('RGB',(SIZE,SIZE)); np=n.load()
    for y in range(SIZE):
        ym=max(0,y-1); yp=min(SIZE-1,y+1)
        for x in range(SIZE):
            xm=max(0,x-1); xp=min(SIZE-1,x+1); dx=(hp[xp,y]-hp[xm,y])/255; dy=(hp[x,yp]-hp[x,ym])/255
            nx=-dx*3.3; ny=-dy*3.3; nz=1.; inv=1/math.sqrt(nx*nx+ny*ny+nz*nz)
            np[x,y]=(clamp(128+127*nx*inv),clamp(128+127*ny*inv),clamp(128+127*nz*inv))
    n.save(OUT/f'{stem}-normal.png')

def mean_luma(p):
    im=Image.open(p).convert('RGB').resize((64,64),Image.Resampling.LANCZOS); return statistics.mean(.2126*r+.7152*g+.0722*b for r,g,b in im.getdata())
def mean_gray(p): return statistics.mean(Image.open(p).convert('L').resize((64,64),Image.Resampling.LANCZOS).getdata())
def std_gray(p): return statistics.pstdev(Image.open(p).convert('L').resize((64,64),Image.Resampling.LANCZOS).getdata())

for i,(stem,(base,rough,norm)) in enumerate(FAMILIES.items()): make(stem,base,rough,norm,i)
# Mip-like 64px acceptance gates: geological armor must remain distinct from vulnerable anatomy.
plate=mean_luma(OUT/'basalt-plate-albedo.png'); mineral=mean_luma(OUT/'mineral-overlap-albedo.png')
if mineral-plate<65: raise RuntimeError(f'Mineral/plate readability too weak at 64px: {mineral-plate:.1f}')
r_plate=mean_gray(OUT/'basalt-plate-roughness.png'); r_tissue=mean_gray(OUT/'protected-tissue-roughness.png')
if r_plate-r_tissue<105: raise RuntimeError(f'Armor/tissue roughness separation too weak at 64px: {r_plate-r_tissue:.1f}')
edge=mean_luma(OUT/'fracture-edge-albedo.png')
if edge-plate<35: raise RuntimeError(f'Fresh fracture edges collapse into armor at 64px: {edge-plate:.1f}')
for stem in FAMILIES:
    if std_gray(OUT/f'{stem}-normal.png')<2.0: raise RuntimeError(f'{stem}: normal response collapsed at 64px')
expected={f'{s}-{k}.png' for s in FAMILIES for k in ('albedo','roughness','normal')}; actual={p.name for p in OUT.glob('*.png')}
if actual!=expected: raise RuntimeError(f'Unexpected Basalt Crawler texture set: missing={expected-actual}, extra={actual-expected}')
print(f'Authored {len(actual)} Basalt Crawler 1024px PBR maps in {OUT}')
print(f'64px gates: mineral/plate luma +{mineral-plate:.1f}; edge/plate luma +{edge-plate:.1f}; plate/tissue roughness +{r_plate-r_tissue:.1f}; no emission maps')
