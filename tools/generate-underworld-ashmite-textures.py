#!/usr/bin/env python3
"""Generate deterministic production PBR textures for the Sulfurous Wastes Ashmite.

The Ashmite establishes the biome's creature material language: heat-cracked volcanic
chitin, sulfur/mineral encrustation, cooler protected joints, and scorched mouthparts.
No emission is authored: readable heat adaptation comes from value/roughness/normal
contrast rather than generic glowing-monster treatment.
"""
from pathlib import Path
from PIL import Image, ImageFilter
import math, random, statistics

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "assets/textures/underworld/creatures/ashmite"
OUT.mkdir(parents=True, exist_ok=True)
SIZE = 1024
SEED = 0xA54_117E
FAMILIES = {
    "heat-chitin": ((55, 47, 39), 178, 30),
    "sulfur-crust": ((143, 126, 66), 205, 38),
    "protected-joint": ((48, 37, 32), 118, 24),
    "mouthpart": ((70, 54, 42), 152, 34),
    "eye": ((22, 18, 15), 78, 18),
}

def clamp(v): return max(0, min(255, int(v)))
def fields(seed):
    rng=random.Random(seed)
    # Low-frequency fields survive mip/downsampling; fine field prevents plastic flatness.
    small=Image.new('L',(64,64)); small.putdata([rng.randrange(256) for _ in range(64*64)])
    low=small.resize((SIZE,SIZE),Image.Resampling.BICUBIC).filter(ImageFilter.GaussianBlur(11))
    fine=Image.effect_noise((SIZE,SIZE),38).filter(ImageFilter.GaussianBlur(.7))
    return low,fine

def crack_mask(seed):
    rng=random.Random(seed); im=Image.new('L',(SIZE,SIZE),0); px=im.load()
    # Branched thermal fractures: long enough to remain readable at gameplay distance.
    for _ in range(54):
        x=rng.randrange(SIZE); y=rng.randrange(SIZE); ang=rng.random()*math.tau
        for step in range(rng.randrange(80,230)):
            ang += rng.uniform(-.12,.12); x += math.cos(ang)*2.2; y += math.sin(ang)*2.2
            if not (2<=x<SIZE-2 and 2<=y<SIZE-2): break
            r=2 if step%7 else 3
            for yy in range(int(y)-r,int(y)+r+1):
                for xx in range(int(x)-r,int(x)+r+1):
                    d=((xx-x)**2+(yy-y)**2)**.5
                    if d<=r: px[xx,yy]=max(px[xx,yy],clamp(255*(1-d/(r+.01))))
    return im.filter(ImageFilter.GaussianBlur(.65))

def make_family(stem,base,rough_base,normal_strength,index):
    low,fine=fields(SEED+index*97); cracks=crack_mask(SEED+index*313)
    lp=list(low.getdata()); fp=list(fine.getdata()); cp=list(cracks.getdata())
    alb=[]; rough=[]; height=[]
    for i,(l,f,c) in enumerate(zip(lp,fp,cp)):
        macro=(l-128)/128; micro=(f-128)/128; fracture=c/255
        if stem=='sulfur-crust':
            # Pale mineral deposition gathers around fracture margins.
            delta=30*macro+13*micro+18*fracture
        elif stem=='protected-joint': delta=13*macro+7*micro-12*fracture
        elif stem=='eye': delta=7*macro+4*micro
        else: delta=20*macro+10*micro-30*fracture
        alb.append(tuple(clamp(ch+delta) for ch in base))
        rough.append(clamp(rough_base+19*macro+10*micro+(24 if stem=='heat-chitin' else 8)*fracture))
        height.append(clamp(128+normal_strength*(.55*macro+.28*micro-.80*fracture)))
    a=Image.new('RGB',(SIZE,SIZE)); a.putdata(alb); a.save(OUT/f'{stem}-albedo.png')
    r=Image.new('L',(SIZE,SIZE)); r.putdata(rough); r.save(OUT/f'{stem}-roughness.png')
    h=Image.new('L',(SIZE,SIZE)); h.putdata(height); h=h.filter(ImageFilter.GaussianBlur(.55))
    hp=h.load(); n=Image.new('RGB',(SIZE,SIZE)); np=n.load()
    for y in range(SIZE):
        ym=max(0,y-1); yp=min(SIZE-1,y+1)
        for x in range(SIZE):
            xm=max(0,x-1); xp=min(SIZE-1,x+1)
            dx=(hp[xp,y]-hp[xm,y])/255; dy=(hp[x,yp]-hp[x,ym])/255
            nx=-dx*3.2; ny=-dy*3.2; nz=1.0; inv=1/math.sqrt(nx*nx+ny*ny+nz*nz)
            np[x,y]=(clamp(128+127*nx*inv),clamp(128+127*ny*inv),clamp(128+127*nz*inv))
    n.save(OUT/f'{stem}-normal.png')

def mean_luma(path,proxy=64):
    im=Image.open(path).convert('RGB').resize((proxy,proxy),Image.Resampling.LANCZOS)
    return statistics.mean(.2126*r+.7152*g+.0722*b for r,g,b in im.getdata())
def mean_gray(path,proxy=64):
    im=Image.open(path).convert('L').resize((proxy,proxy),Image.Resampling.LANCZOS)
    return statistics.mean(im.getdata())
def std_gray(path,proxy=64):
    im=Image.open(path).convert('L').resize((proxy,proxy),Image.Resampling.LANCZOS)
    return statistics.pstdev(im.getdata())

for idx,(stem,(base,rough,norm)) in enumerate(FAMILIES.items()): make_family(stem,base,rough,norm,idx)
# Combat-distance gates: sulfur crust must remain readable against dark shell, joints wetter,
# and thermal cracking must survive downsampling as actual material variation.
ch=mean_luma(OUT/'heat-chitin-albedo.png'); su=mean_luma(OUT/'sulfur-crust-albedo.png')
if su-ch < 48: raise RuntimeError(f'Sulfur/chitin separation too weak at 64px: {su-ch:.1f}')
rch=mean_gray(OUT/'heat-chitin-roughness.png'); rj=mean_gray(OUT/'protected-joint-roughness.png')
if rch-rj < 42: raise RuntimeError(f'Joint wetness separation too weak at 64px: {rch-rj:.1f}')
for stem in FAMILIES:
    if std_gray(OUT/f'{stem}-normal.png') < 2.0: raise RuntimeError(f'{stem}: normal response collapsed at 64px')
expected={f'{s}-{k}.png' for s in FAMILIES for k in ('albedo','roughness','normal')}
actual={p.name for p in OUT.glob('*.png')}
if actual != expected: raise RuntimeError(f'Unexpected Ashmite texture set: missing={expected-actual}, extra={actual-expected}')
print(f'Authored {len(actual)} Ashmite 1024px PBR maps in {OUT}')
print(f'64px gates: sulfur/chitin luma +{su-ch:.1f}; chitin/joint roughness +{rch-rj:.1f}; no emission maps')
