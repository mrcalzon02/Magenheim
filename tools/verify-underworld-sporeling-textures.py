#!/usr/bin/env python3
"""Quantitative fidelity gate for the Fungal Forest Sporeling texture set.

Runs without Blender so local handoff can reject flat, clipped, noisy or over-emissive
maps before creature authoring. This is deliberately stricter than file-existence checks.
"""
from pathlib import Path
from statistics import mean, pstdev
from PIL import Image, ImageStat

ROOT=Path(__file__).resolve().parents[1]
TEX=ROOT/'assets/textures/underworld/creatures/sporeling'
SIZE=(1024,1024)
STEMS=('flesh','cap-chitin','joint','gill','spore-sac')


def load(name,mode=None):
    p=TEX/name
    if not p.exists(): raise RuntimeError(f'Missing texture: {p}')
    im=Image.open(p)
    if im.size!=SIZE: raise RuntimeError(f'{name}: expected {SIZE}, got {im.size}')
    return im.convert(mode) if mode else im


def luminance(im):
    return im.convert('L')


def percentile(values,q):
    data=sorted(values); return data[min(len(data)-1,max(0,int((len(data)-1)*q)))]


def sampled(im,step=8):
    p=im.load(); return [p[x,y] for y in range(0,im.height,step) for x in range(0,im.width,step)]

for stem in STEMS:
    alb=load(f'{stem}-albedo.png','RGB'); lum=luminance(alb); vals=sampled(lum)
    p05,p95=percentile(vals,.05),percentile(vals,.95)
    if p95-p05<24: raise RuntimeError(f'{stem}: albedo dynamic range too flat ({p05}..{p95})')
    if p05<4 or p95>251: raise RuntimeError(f'{stem}: albedo clips excessively ({p05}..{p95})')

    rough=load(f'{stem}-roughness.png','L'); rv=sampled(rough)
    if pstdev(rv)<5: raise RuntimeError(f'{stem}: roughness lacks material variation (sd={pstdev(rv):.2f})')
    if not 45<=mean(rv)<=235: raise RuntimeError(f'{stem}: implausible mean roughness ({mean(rv):.1f})')

    normal=load(f'{stem}-normal.png','RGB'); nv=sampled(normal)
    blue=mean(v[2] for v in nv); rgdev=mean(abs(v[0]-128)+abs(v[1]-128) for v in nv)
    if blue<150: raise RuntimeError(f'{stem}: normal map is not predominantly +Z (B={blue:.1f})')
    if rgdev<5: raise RuntimeError(f'{stem}: normal map is effectively flat (RG deviation={rgdev:.1f})')

# Emission is anatomical punctuation, never whole-creature glow. Require a useful bright
# signal while limiting luminous coverage to a minority of texels.
for stem in ('gill','spore-sac'):
    em=load(f'{stem}-emission.png','L'); ev=sampled(em)
    coverage=sum(v>24 for v in ev)/len(ev); hot=sum(v>128 for v in ev)/len(ev)
    if coverage<.02: raise RuntimeError(f'{stem}: emission is visually absent ({coverage:.1%} coverage)')
    if coverage>.42: raise RuntimeError(f'{stem}: emission is too broad ({coverage:.1%} coverage)')
    if hot<.002: raise RuntimeError(f'{stem}: emission has no readable focal highlights ({hot:.2%} hot)')

# Material families must remain distinguishable at combat distance. Reject accidental
# duplicate albedo maps by comparing downsampled luminance signatures.
signatures={}
for stem in STEMS:
    im=luminance(load(f'{stem}-albedo.png','RGB')).resize((32,32))
    signatures[stem]=list(im.getdata())
for i,a in enumerate(STEMS):
    for b in STEMS[i+1:]:
        delta=mean(abs(x-y) for x,y in zip(signatures[a],signatures[b]))
        if delta<3.0: raise RuntimeError(f'{a}/{b}: albedo families are visually redundant (mean delta={delta:.2f})')

print('VERIFIED Sporeling texture fidelity: 17 maps, 1024px, PBR variation, localized emission, distinct material families',flush=True)
