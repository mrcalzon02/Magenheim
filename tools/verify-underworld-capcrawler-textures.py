#!/usr/bin/env python3
"""Verify committed Fungal Forest Capcrawler PBR maps without regenerating them.

The authoring generator validates maps while it runs, but builds consume committed PNGs.
This gate therefore repeats the gameplay-relevant contract independently so a missing,
replaced, flattened, or over-broad map cannot survive merely because regeneration was skipped.
"""
from pathlib import Path
from statistics import pstdev
from PIL import Image, ImageStat

ROOT = Path(__file__).resolve().parents[1]
TEX = ROOT / 'assets/textures/underworld/creatures/capcrawler'
SIZE = (1024, 1024)
PROXY = (64, 64)
STEMS = ('cap-carapace', 'underside-flesh', 'leg-plate', 'mandible', 'gill')


def flat(im):
    reader = getattr(im, 'get_flattened_data', None)
    return list(reader() if reader else im.getdata())


def load(name, mode):
    path = TEX / name
    if not path.exists():
        raise RuntimeError(f'Missing Capcrawler texture: {path}')
    with Image.open(path) as source:
        if source.size != SIZE:
            raise RuntimeError(f'{name}: expected {SIZE}, got {source.size}')
        return source.convert(mode)


expected = set()
for stem in STEMS:
    expected.update({f'{stem}-albedo.png', f'{stem}-roughness.png', f'{stem}-normal.png'})
expected.add('gill-emission.png')
actual = {p.name for p in TEX.glob('*.png')}
if actual != expected:
    missing = sorted(expected - actual)
    extra = sorted(actual - expected)
    raise RuntimeError(f'Capcrawler texture set mismatch: missing={missing}, extra={extra}')

for stem in STEMS:
    for suffix, mode in (('albedo', 'RGB'), ('roughness', 'L'), ('normal', 'RGB')):
        name = f'{stem}-{suffix}.png'
        image = load(name, mode)
        proxy = image.resize(PROXY, Image.Resampling.LANCZOS)
        luma = proxy.convert('L')
        lo, hi = luma.getextrema()
        sigma = ImageStat.Stat(luma).stddev[0]
        if hi - lo < 8 or sigma < 2.0:
            raise RuntimeError(f'{name}: detail collapses at combat scale range={hi-lo}, sigma={sigma:.2f}')
        if suffix == 'roughness' and (hi - lo < 12 or sigma < 3.0):
            raise RuntimeError(f'{name}: roughness response collapses at combat scale range={hi-lo}, sigma={sigma:.2f}')
        if suffix == 'normal':
            stat = ImageStat.Stat(proxy)
            xy_dev = (stat.stddev[0] ** 2 + stat.stddev[1] ** 2) ** 0.5
            if xy_dev < 2.2:
                raise RuntimeError(f'{name}: normal relief collapses at combat scale xy-dev={xy_dev:.2f}')
            if stat.mean[2] < 150:
                raise RuntimeError(f'{name}: normal map loses +Z orientation at combat scale z={stat.mean[2]:.1f}')

emission = load('gill-emission.png', 'L').resize(PROXY, Image.Resampling.LANCZOS)
ev = flat(emission)
coverage = sum(v >= 8 for v in ev) / len(ev)
if not .01 <= coverage <= .55:
    raise RuntimeError(f'gill-emission.png: emission coverage {coverage:.1%} is not localized')
if pstdev(ev) < 2.0:
    raise RuntimeError(f'gill-emission.png: emission becomes visually flat at combat scale (sd={pstdev(ev):.2f})')

print('VERIFIED Capcrawler texture fidelity: 16 exact 1024px PBR maps; 64px albedo/roughness/normal detail survives and gill emission remains localized', flush=True)
