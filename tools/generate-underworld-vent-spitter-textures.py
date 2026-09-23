#!/usr/bin/env python3
"""Generate deterministic production PBR textures for the Sulfurous Wastes Vent Spitter.

The Spitter reads as a geothermal pressure animal: heat-cured hide, dry mineral baffles,
elastic wet pressure-sac/throat tissue, dark mouth tissue, and mineral teeth. The ranged
charge must remain anatomically readable before VFX. No emission maps are authored.
"""
from pathlib import Path
from PIL import Image, ImageFilter
import math, random, statistics

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/creatures/vent-spitter'
OUT.mkdir(parents=True,exist_ok=True)
SIZE=1024
SEED=0x5A17_7E57
FAMILIES={
    'heat-hide':((76,55,42),198,32),
    'mineral-baffle':((48,45,41),232,45),
    'pressure-sac':((143,56,39),78,29),
    'throat-tissue':((119,43,34),91,27),
    'mouth-tissue':((62,25,24),73,22),
    'mineral-tooth':((157,137,94),174,25),
}

def clamp(v): return max(0,min(255,int(v)))
def fields(seed):
    rng=random.Random(seed)
    small=Image.new('L',(64,64)); small.putdata([rng.randrange(256) for _ in range(64*64)])
    low=small.resize((SIZE,SIZE),Image.Resampling.BICUBIC).filter(ImageFilter.GaussianBlur(11))
    fine=Image.effect_noise((SIZE,SIZE),45).filter(ImageFilter.GaussianBlur(.7))
    return low,fine

def fissures(seed,count,width=2):
    rng=random.Random(seed); im=Image.new('L',(SIZE,SIZE),0); px=im.load()
    for _ in range(count):
        x=rng.randrange(SIZE); y=rng.randrange(SIZE); ang=rng.random()*math.tau
        for step in range(rng.randrange(50,180)):
            ang+=rng.uniform(-.13,.13); x+=math.cos(ang)*2.8; y+=math.sin(ang)*2.8
            if not (3<=x<SIZE-3 and 3<=y<SIZE-3): break
            rad=width+(1 if step%19==0 else 0)
            for yy in range(int(y)-rad,int(y)+rad+1):
                for xx in range(int(x)-rad,int(x)+rad+1):
                    d=((xx-x)**2+(yy-y)**2)**.5
                    if d<=rad: px[xx,yy]=max(px[xx,yy],clamp(255*(1-d/(rad+.01))))
    return im.filter(ImageFilter.GaussianBlur(.6))

def make(stem,base,rough_base,norm,index):
    low,fine=fields(SEED+index*149); crack=fissures(SEED+index*367,48 if stem in ('heat-hide','mineral-baffle') else 18,3 if stem=='mineral-baffle' else 2)
    alb=[]; rough=[]; height=[]
    for l,f,c in zip(low.getdata(),fine.getdata(),crack.getdata()):
        macro=(l-128)/128; micro=(f-128)/128; fracture=c/255
        if stem=='heat-hide': delta=15*macro+8*micro-22*fracture
        elif stem=='mineral-baffle': delta=11*macro+8*micro-38*fracture
        elif stem=='pressure-sac': delta=23*macro+6*micro+7*fracture
        elif stem=='throat-tissue': delta=19*macro+7*micro+4*fracture
        elif stem=='mouth-tissue': delta=11*macro+5*micro
        else: delta=13*macro+5*micro-10*fracture
        alb.append(tuple(clamp(ch+delta) for ch in base))
        rough.append(clamp(rough_base+15*macro+7*micro+(20 if stem=='mineral-baffle' else 4)*fracture))
        height.append(clamp(128+norm*(.55*macro+.24*micro-.68*fracture)))
    a=Image.new('RGB',(SIZE,SIZE)); a.putdata(alb); a.save(OUT/f'{stem}-albedo.png')
    r=Image.new('L',(SIZE,SIZE)); r.putdata(rough); r.save(OUT/f'{stem}-roughness.png')
    h=Image.new('L',(SIZE,SIZE)); h.putdata(height); h=h.filter(ImageFilter.GaussianBlur(.5)); hp=h.load(); n=Image.new('RGB',(SIZE,SIZE)); np=n.load()
    for y in range(SIZE):
        ym=max(0,y-1); yp=min(SIZE-1,y+1)
        for x in range(SIZE):
            xm=max(0,x-1); xp=min(SIZE-1,x+1); dx=(hp[xp,y]-hp[xm,y])/255; dy=(hp[x,yp]-hp[x,ym])/255
            nx=-dx*3.2; ny=-dy*3.2; nz=1.; inv=1/math.sqrt(nx*nx+ny*ny+nz*nz)
            np[x,y]=(clamp(128+127*nx*inv),clamp(128+127*ny*inv),clamp(128+127*nz*inv))
    n.save(OUT/f'{stem}-normal.png')

def mean_luma(p):
    im=Image.open(p).convert('RGB').resize((64,64),Image.Resampling.LANCZOS); return statistics.mean(.2126*r+.7152*g+.0722*b for r,g,b in im.getdata())
def mean_gray(p): return statistics.mean(Image.open(p).convert('L').resize((64,64),Image.Resampling.LANCZOS).getdata())
def std_gray(p): return statistics.pstdev(Image.open(p).convert('L').resize((64,64),Image.Resampling.LANCZOS).getdata())

for i,(stem,(base,rough,norm)) in enumerate(FAMILIES.items()): make(stem,base,rough,norm,i)
# 64px gameplay-distance gates: pressure anatomy must read before particles are involved.
sac=mean_luma(OUT/'pressure-sac-albedo.png'); hide=mean_luma(OUT/'heat-hide-albedo.png'); tooth=mean_luma(OUT/'mineral-tooth-albedo.png')
if sac-hide<35: raise RuntimeError(f'Pressure sac collapses into hide at 64px: +{sac-hide:.1f} luma')
if tooth-hide<65: raise RuntimeError(f'Teeth collapse into hide at 64px: +{tooth-hide:.1f} luma')
r_baffle=mean_gray(OUT/'mineral-baffle-roughness.png'); r_sac=mean_gray(OUT/'pressure-sac-roughness.png'); r_throat=mean_gray(OUT/'throat-tissue-roughness.png')
if r_baffle-r_sac<135: raise RuntimeError(f'Dry baffle/wet sac separation too weak: {r_baffle-r_sac:.1f}')
if r_baffle-r_throat<120: raise RuntimeError(f'Dry baffle/wet throat separation too weak: {r_baffle-r_throat:.1f}')
for stem in FAMILIES:
    if std_gray(OUT/f'{stem}-normal.png')<2.0: raise RuntimeError(f'{stem}: normal response collapsed at 64px')
expected={f'{s}-{k}.png' for s in FAMILIES for k in ('albedo','roughness','normal')}; actual={p.name for p in OUT.glob('*.png')}
if actual!=expected: raise RuntimeError(f'Unexpected Vent Spitter texture set: missing={expected-actual}, extra={actual-expected}')
print(f'Authored {len(actual)} Vent Spitter 1024px PBR maps in {OUT}')
print(f'64px gates: sac/hide luma +{sac-hide:.1f}; tooth/hide +{tooth-hide:.1f}; baffle/sac roughness +{r_baffle-r_sac:.1f}; baffle/throat +{r_baffle-r_throat:.1f}; no emission maps')