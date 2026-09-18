#!/usr/bin/env python3
"""Quantitative fidelity gate for the Fungal Forest Sporeling texture set.

Runs without Blender so local handoff can reject flat, clipped, noisy or over-emissive
maps before creature authoring. Source resolution is not enough: albedo, roughness, normal
and emission must retain useful structure after mip-like downsampling to combat distance.
"""
from pathlib import Path
from statistics import mean, pstdev
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
TEX=ROOT/'assets/textures/underworld/creatures/sporeling'
SIZE=(1024,1024)
COMBAT_PROXY=(64,64)
STEMS=('flesh','cap-chitin','joint','gill','spore-sac')


def load(name,mode=None):
    p=TEX/name
    if not p.exists(): raise RuntimeError(f'Missing texture: {p}')
    im=Image.open(p)
    if im.size!=SIZE: raise RuntimeError(f'{name}: expected {SIZE}, got {im.size}')
    return im.convert(mode) if mode else im


def luminance(im): return im.convert('L')
def percentile(values,q):
    data=sorted(values); return data[min(len(data)-1,max(0,int((len(data)-1)*q)))]
def sampled(im,step=8):
    p=im.load(); return [p[x,y] for y in range(0,im.height,step) for x in range(0,im.width,step)]
def proxy(im): return im.resize(COMBAT_PROXY,Image.Resampling.LANCZOS)
def flat(im):
    """Pillow 12.3 deprecates Image.getdata and removes it in 14; older Pillow lacks the successor.

    The warning is not cosmetic here: Windows PowerShell 5.1 turns any native stderr line into an
    ErrorRecord, so a DeprecationWarning failed build.ps1 even though this gate exited 0.
    """
    reader=getattr(im,'get_flattened_data',None)
    return list(reader() if reader else im.getdata())
def proxy_values(im): return flat(luminance(proxy(im)))
def normal_proxy_values(im): return flat(proxy(im))
def channel_means(im):
    vals=flat(proxy(im.convert('RGB'))); n=len(vals)
    return tuple(sum(v[i] for v in vals)/n for i in range(3))

def chroma_signature(im):
    """Mean RGB ratios at combat scale; luminance alone cannot prove tissue color identity."""
    r,g,b=channel_means(im); total=max(1.0,r+g+b)
    return (r/total,g/total,b/total)

for stem in STEMS:
    alb=load(f'{stem}-albedo.png','RGB'); vals=sampled(luminance(alb))
    p05,p95=percentile(vals,.05),percentile(vals,.95)
    if p95-p05<24: raise RuntimeError(f'{stem}: albedo dynamic range too flat ({p05}..{p95})')
    if p05<4 or p95>251: raise RuntimeError(f'{stem}: albedo clips excessively ({p05}..{p95})')
    pv=proxy_values(alb); pp10,pp90=percentile(pv,.10),percentile(pv,.90)
    if pp90-pp10<14: raise RuntimeError(f'{stem}: albedo loses material read at 64px combat scale ({pp10}..{pp90})')
    if pstdev(pv)<5: raise RuntimeError(f'{stem}: albedo becomes visually flat at 64px combat scale (sd={pstdev(pv):.2f})')

    rough=load(f'{stem}-roughness.png','L'); rv=sampled(rough)
    if pstdev(rv)<5: raise RuntimeError(f'{stem}: roughness lacks material variation (sd={pstdev(rv):.2f})')
    if not 45<=mean(rv)<=235: raise RuntimeError(f'{stem}: implausible mean roughness ({mean(rv):.1f})')
    rpv=proxy_values(rough); rp10,rp90=percentile(rpv,.10),percentile(rpv,.90)
    if rp90-rp10<8: raise RuntimeError(f'{stem}: roughness variation disappears at 64px combat scale ({rp10}..{rp90})')
    if pstdev(rpv)<3: raise RuntimeError(f'{stem}: roughness becomes visually uniform at 64px combat scale (sd={pstdev(rpv):.2f})')

    normal=load(f'{stem}-normal.png','RGB'); nv=sampled(normal)
    blue=mean(v[2] for v in nv); rgdev=mean(abs(v[0]-128)+abs(v[1]-128) for v in nv)
    if blue<150: raise RuntimeError(f'{stem}: normal map is not predominantly +Z (B={blue:.1f})')
    if rgdev<5: raise RuntimeError(f'{stem}: normal map is effectively flat (RG deviation={rgdev:.1f})')
    np=normal_proxy_values(normal)
    proxy_blue=mean(v[2] for v in np); proxy_rgdev=mean(abs(v[0]-128)+abs(v[1]-128) for v in np)
    if proxy_blue<150: raise RuntimeError(f'{stem}: normal direction degrades at 64px combat scale (B={proxy_blue:.1f})')
    if proxy_rgdev<2.0: raise RuntimeError(f'{stem}: normal relief vanishes at 64px combat scale (RG deviation={proxy_rgdev:.2f})')

for stem in ('gill','spore-sac'):
    em=load(f'{stem}-emission.png','L'); ev=sampled(em)
    coverage=sum(v>24 for v in ev)/len(ev); hot=sum(v>128 for v in ev)/len(ev)
    if coverage<.02: raise RuntimeError(f'{stem}: emission is visually absent ({coverage:.1%} coverage)')
    if coverage>.42: raise RuntimeError(f'{stem}: emission is too broad ({coverage:.1%} coverage)')
    if hot<.002: raise RuntimeError(f'{stem}: emission has no readable focal highlights ({hot:.2%} hot)')
    ep=proxy_values(em); proxy_hot=sum(v>72 for v in ep)/len(ep)
    if proxy_hot<.002: raise RuntimeError(f'{stem}: emission disappears at 64px combat scale')
    if proxy_hot>.30: raise RuntimeError(f'{stem}: emission blooms across too much of the 64px combat proxy ({proxy_hot:.1%})')

albedos={stem:load(f'{stem}-albedo.png','RGB') for stem in STEMS}
signatures={stem:proxy_values(albedos[stem]) for stem in STEMS}
chromas={stem:chroma_signature(albedos[stem]) for stem in STEMS}
for i,a in enumerate(STEMS):
    for b in STEMS[i+1:]:
        delta=mean(abs(x-y) for x,y in zip(signatures[a],signatures[b]))
        if delta<3.0: raise RuntimeError(f'{a}/{b}: albedo families are visually redundant at combat scale (mean delta={delta:.2f})')
        cd=sum(abs(x-y) for x,y in zip(chromas[a],chromas[b]))
        if cd<.025: raise RuntimeError(f'{a}/{b}: combat-scale color identity is redundant (normalized RGB delta={cd:.3f})')

# Gameplay hierarchy: luminous anatomy must remain more green-forward than flesh/joints,
# while the spore sac remains visibly magenta-forward. These are identity cues, not decoration.
for stem in ('gill','cap-chitin'):
    r,g,b=channel_means(albedos[stem])
    if g<=r*1.18: raise RuntimeError(f'{stem}: loses green/cyan identity at combat scale (RGB={r:.1f},{g:.1f},{b:.1f})')
r,g,b=channel_means(albedos['spore-sac'])
if r<=g*1.12 or b<=g*1.15: raise RuntimeError(f'spore-sac: loses magenta identity at combat scale (RGB={r:.1f},{g:.1f},{b:.1f})')

print('VERIFIED Sporeling texture fidelity: 17 maps, 1024px source, 64px combat albedo/chroma/roughness/normal/emission readability, localized emission, distinct material families',flush=True)
