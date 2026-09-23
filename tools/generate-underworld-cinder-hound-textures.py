#!/usr/bin/env python3
"""Generate deterministic production PBR textures for the Sulfurous Wastes Cinder Hound.

The hound reads as a heat-adapted living pack predator, not a glowing lava wolf:
scorched hide, broken basalt scutes, cooler protected joints, heat-stressed vent tissue,
mineral teeth and recessed eyes. No emission maps are authored.
"""
from pathlib import Path
from PIL import Image, ImageFilter
import math, random, statistics

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "assets/textures/underworld/creatures/cinder-hound"
OUT.mkdir(parents=True, exist_ok=True)
SIZE = 1024
SEED = 0xC1D3_48A7
FAMILIES = {
    "scorched-hide": ((64, 48, 40), 172, 29),
    "basalt-scute": ((43, 40, 38), 211, 42),
    "protected-joint": ((73, 47, 42), 112, 22),
    "vent-tissue": ((126, 58, 43), 86, 28),
    "mineral-tooth": ((181, 165, 132), 146, 24),
    "recessed-eye": ((24, 17, 15), 68, 17),
}

def clamp(v): return max(0, min(255, int(v)))
def fields(seed):
    rng=random.Random(seed)
    small=Image.new('L',(64,64)); small.putdata([rng.randrange(256) for _ in range(64*64)])
    low=small.resize((SIZE,SIZE),Image.Resampling.BICUBIC).filter(ImageFilter.GaussianBlur(12))
    fine=Image.effect_noise((SIZE,SIZE),42).filter(ImageFilter.GaussianBlur(.75))
    return low,fine

def fissures(seed, count=38):
    rng=random.Random(seed); im=Image.new('L',(SIZE,SIZE),0); px=im.load()
    for _ in range(count):
        x=rng.randrange(SIZE); y=rng.randrange(SIZE); ang=rng.random()*math.tau
        for step in range(rng.randrange(70,210)):
            ang += rng.uniform(-.10,.10); x += math.cos(ang)*2.5; y += math.sin(ang)*2.5
            if not (2<=x<SIZE-2 and 2<=y<SIZE-2): break
            rad=2 if step%9 else 3
            for yy in range(int(y)-rad,int(y)+rad+1):
                for xx in range(int(x)-rad,int(x)+rad+1):
                    d=((xx-x)**2+(yy-y)**2)**.5
                    if d<=rad: px[xx,yy]=max(px[xx,yy],clamp(255*(1-d/(rad+.01))))
    return im.filter(ImageFilter.GaussianBlur(.65))

def make_family(stem, base, rough_base, normal_strength, index):
    low,fine=fields(SEED+index*109); cracks=fissures(SEED+index*331, 52 if stem=='basalt-scute' else 30)
    lp=list(low.getdata()); fp=list(fine.getdata()); cp=list(cracks.getdata())
    alb=[]; rough=[]; height=[]
    for l,f,c in zip(lp,fp,cp):
        macro=(l-128)/128; micro=(f-128)/128; fracture=c/255
        if stem=='basalt-scute': delta=14*macro+8*micro-38*fracture
        elif stem=='vent-tissue': delta=20*macro+8*micro+15*fracture
        elif stem=='protected-joint': delta=12*macro+7*micro-8*fracture
        elif stem=='mineral-tooth': delta=18*macro+5*micro-10*fracture
        elif stem=='recessed-eye': delta=6*macro+3*micro
        else: delta=18*macro+10*micro-24*fracture
        alb.append(tuple(clamp(ch+delta) for ch in base))
        rough.append(clamp(rough_base+17*macro+8*micro+(25 if stem in ('scorched-hide','basalt-scute') else 5)*fracture))
        height.append(clamp(128+normal_strength*(.55*macro+.25*micro-.78*fracture)))
    a=Image.new('RGB',(SIZE,SIZE)); a.putdata(alb); a.save(OUT/f'{stem}-albedo.png')
    r=Image.new('L',(SIZE,SIZE)); r.putdata(rough); r.save(OUT/f'{stem}-roughness.png')
    h=Image.new('L',(SIZE,SIZE)); h.putdata(height); h=h.filter(ImageFilter.GaussianBlur(.55)); hp=h.load()
    n=Image.new('RGB',(SIZE,SIZE)); np=n.load()
    for y in range(SIZE):
        ym=max(0,y-1); yp=min(SIZE-1,y+1)
        for x in range(SIZE):
            xm=max(0,x-1); xp=min(SIZE-1,x+1)
            dx=(hp[xp,y]-hp[xm,y])/255; dy=(hp[x,yp]-hp[x,ym])/255
            nx=-dx*3.2; ny=-dy*3.2; nz=1.; inv=1/math.sqrt(nx*nx+ny*ny+nz*nz)
            np[x,y]=(clamp(128+127*nx*inv),clamp(128+127*ny*inv),clamp(128+127*nz*inv))
    n.save(OUT/f'{stem}-normal.png')

def mean_luma(path, proxy=64):
    im=Image.open(path).convert('RGB').resize((proxy,proxy),Image.Resampling.LANCZOS)
    return statistics.mean(.2126*r+.7152*g+.0722*b for r,g,b in im.getdata())
def mean_gray(path, proxy=64):
    im=Image.open(path).convert('L').resize((proxy,proxy),Image.Resampling.LANCZOS)
    return statistics.mean(im.getdata())
def std_gray(path, proxy=64):
    im=Image.open(path).convert('L').resize((proxy,proxy),Image.Resampling.LANCZOS)
    return statistics.pstdev(im.getdata())

for idx,(stem,(base,rough,norm)) in enumerate(FAMILIES.items()): make_family(stem,base,rough,norm,idx)
# Combat-distance gates preserve anatomy/material hierarchy after mip-like reduction.
hide=mean_luma(OUT/'scorched-hide-albedo.png'); tooth=mean_luma(OUT/'mineral-tooth-albedo.png')
if tooth-hide < 90: raise RuntimeError(f'Tooth/hide readability too weak at 64px: {tooth-hide:.1f}')
r_scute=mean_gray(OUT/'basalt-scute-roughness.png'); r_vent=mean_gray(OUT/'vent-tissue-roughness.png')
if r_scute-r_vent < 105: raise RuntimeError(f'Vent wetness separation too weak at 64px: {r_scute-r_vent:.1f}')
r_hide=mean_gray(OUT/'scorched-hide-roughness.png'); r_joint=mean_gray(OUT/'protected-joint-roughness.png')
if r_hide-r_joint < 48: raise RuntimeError(f'Protected-joint separation too weak at 64px: {r_hide-r_joint:.1f}')
for stem in FAMILIES:
    if std_gray(OUT/f'{stem}-normal.png') < 2.0: raise RuntimeError(f'{stem}: normal response collapsed at 64px')
expected={f'{s}-{k}.png' for s in FAMILIES for k in ('albedo','roughness','normal')}
actual={p.name for p in OUT.glob('*.png')}
if actual != expected: raise RuntimeError(f'Unexpected Cinder Hound texture set: missing={expected-actual}, extra={actual-expected}')
print(f'Authored {len(actual)} Cinder Hound 1024px PBR maps in {OUT}')
print(f'64px gates: tooth/hide luma +{tooth-hide:.1f}; scute/vent roughness +{r_scute-r_vent:.1f}; hide/joint roughness +{r_hide-r_joint:.1f}; no emission maps')
