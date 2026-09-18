"""Gate the shipped inventory icons.

Two defect classes reached players because nothing checked icons at build time:

  * three committed staff icons had corrupt IDAT chunks, and EarthAssets.Texture throws
    InvalidDataException when Unity cannot decode a file;
  * the Earth staff family asked for icons that were never authored, so
    EarthAssets.Icon raised FileNotFoundException.

Both only fail at registration time, inside PrefabManager.OnVanillaPrefabsAvailable.
That event is a multicast delegate, so the first registrar that throws also stops every
registrar subscribed after it -- a missing icon silently removes unrelated content.

The compatibility floor remains 128px for legacy checked-in assets, but newly authored or
regenerated icons are expected to meet the 256px readability target. Set
MAGENHEIM_ICON_TARGET_SIZE to a larger square size for higher-resolution review/export passes.
Set MAGENHEIM_ENFORCE_ICON_TARGET=1 for asset-release/acceptance passes: every shipped icon
below the configured target then becomes a hard failure. Normal compatibility builds continue
to enumerate legacy-resolution debt without destructively blocking unrelated development.
An icon is judged on three measures, not one. A single "fraction of the frame with alpha" floor
cannot express what this check is for: it conflates how a subject is placed with how thick the
subject is. Every one of the 32 staff icons is framed the same way -- visible bounding box 78.5%
to 84.4% of the icon wide, 64.5% to 69.9% tall -- yet frame coverage runs from 3.6% to 9.9%,
because a staff is a hairline shaft and the tiers differ only in how much mass the head carries.
A flat 6% floor therefore rejected the six thinnest correctly-rendered staves, and no change to
render-staff-icons.py could have satisfied it: raising spirit-simple past 6% needs roughly 1.65x
linear scale, which crops the staff out of frame. The three measures separate the defects:

  * frame coverage -- catches a blank or near-blank render, and (at the top) an icon that is
    effectively a filled square with no silhouette;
  * visible bounding-box area -- catches a subject rendered tiny or pushed into a corner, which
    frame coverage alone cannot distinguish from a correctly framed thin one;
  * ink density inside that bounding box -- catches an outline-only or ghost render whose box is
    large but whose subject is not actually there.

Review environments may tighten any of them with MAGENHEIM_ICON_MIN_COVERAGE,
MAGENHEIM_ICON_MAX_COVERAGE, MAGENHEIM_ICON_MIN_BOX_FILL and MAGENHEIM_ICON_MIN_INK_DENSITY
without changing checked-in policy. Note that low ink density across the whole staff family
(6.4%-18.8%, against 47%-85% for every other icon) is real readability debt recorded in
BACKLOG.md; it is an icon-composition problem, not something a threshold can decide.
Exits non-zero on structural failures so build.ps1 fails the build.
"""
import json
import os
import re
import struct
import sys
import zlib
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ICONS = ROOT / 'assets/earth'
RUNTIME = ROOT / 'src/Magenheim.Runtime'
CATALOG = ROOT / 'assets/models/catalog.json'
LEGACY_ICON_FLOOR = 128
ICON_TARGET_SIZE = max(256, int(os.environ.get('MAGENHEIM_ICON_TARGET_SIZE', '256')))
ENFORCE_ICON_TARGET = os.environ.get('MAGENHEIM_ENFORCE_ICON_TARGET', '').strip().lower() in {
    '1', 'true', 'yes', 'on'
}


def coverage_setting(name: str, default: float) -> float:
    raw = os.environ.get(name)
    if raw is None or not raw.strip():
        return default
    try:
        value = float(raw)
    except ValueError as error:
        raise SystemExit(f'{name} must be a decimal fraction between 0 and 1, got {raw!r}') from error
    if not 0.0 < value < 1.0:
        raise SystemExit(f'{name} must be strictly between 0 and 1, got {value}')
    return value


# Floors sit below the measured minimum of the shipped library with real margin, so they reject
# a broken render rather than a thin one. Observed today across 44 icons: frame coverage 3.64%
# minimum, bounding-box area 30.5% minimum, ink density inside the box 6.4% minimum.
MIN_VISIBLE_ALPHA_COVERAGE = coverage_setting('MAGENHEIM_ICON_MIN_COVERAGE', 0.015)
MAX_VISIBLE_ALPHA_COVERAGE = coverage_setting('MAGENHEIM_ICON_MAX_COVERAGE', 0.94)
MIN_VISIBLE_BOX_FILL = coverage_setting('MAGENHEIM_ICON_MIN_BOX_FILL', 0.22)
MIN_VISIBLE_INK_DENSITY = coverage_setting('MAGENHEIM_ICON_MIN_INK_DENSITY', 0.04)
if MIN_VISIBLE_ALPHA_COVERAGE >= MAX_VISIBLE_ALPHA_COVERAGE:
    raise SystemExit(
        'MAGENHEIM_ICON_MIN_COVERAGE must be lower than MAGENHEIM_ICON_MAX_COVERAGE '
        f'(got {MIN_VISIBLE_ALPHA_COVERAGE} >= {MAX_VISIBLE_ALPHA_COVERAGE})'
    )

failures: list[str] = []
legacy_resolution: list[str] = []
resolution_counts: Counter[int] = Counter()


def decode_png(path: Path) -> tuple[int, int, int, bytes]:
    """Walk the PNG chunk stream, verifying every CRC. Returns dimensions, colour type and raw scanlines."""
    data = path.read_bytes()
    if data[:8] != b'\x89PNG\r\n\x1a\n':
        raise ValueError('not a PNG')
    offset, size, idat, saw_end = 8, None, bytearray(), False
    while offset < len(data):
        if offset + 8 > len(data):
            raise ValueError('truncated chunk header')
        length, kind = struct.unpack('>I4s', data[offset:offset + 8])
        body = data[offset + 8:offset + 8 + length]
        if len(body) != length:
            raise ValueError(f'truncated {kind.decode("ascii", "replace")} chunk')
        if offset + 12 + length > len(data):
            raise ValueError(f'missing {kind.decode("ascii", "replace")} CRC')
        stored = struct.unpack('>I', data[offset + 8 + length:offset + 12 + length])[0]
        if stored != zlib.crc32(kind + body) & 0xFFFFFFFF:
            raise ValueError(f'bad CRC on {kind.decode("ascii", "replace")} chunk')
        if kind == b'IHDR':
            width, height, depth, colour = struct.unpack('>IIBB', body[:10])
            size = (width, height, depth, colour)
            if depth != 8:
                raise ValueError(f'unsupported bit depth {depth}; shipped icons must be 8-bit RGBA')
        elif kind == b'IDAT':
            idat += body
        elif kind == b'IEND':
            saw_end = True
        offset += 12 + length
        if saw_end:
            break
    if size is None:
        raise ValueError('no IHDR')
    if not saw_end:
        raise ValueError('no IEND')
    width, height, depth, colour = size
    channels = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[colour]
    expected = height * (1 + width * channels * depth // 8)
    raw = zlib.decompress(bytes(idat))
    if len(raw) != expected:
        raise ValueError(f'pixel stream is {len(raw)} bytes, expected {expected}')
    return width, height, colour, raw


def silhouette_metrics(raw: bytes, width: int, height: int) -> tuple[float, float, float]:
    """Decode PNG filters enough to measure the actual visible RGBA silhouette without Pillow.

    Returns (frame coverage, visible bounding-box area as a fraction of the icon, ink density
    inside that bounding box). An icon with no visible pixels at all returns zeros, which fails
    every floor.
    """
    stride = width * 4
    previous = bytearray(stride)
    visible = 0
    min_x, max_x, min_y, max_y = width, -1, height, -1
    cursor = 0
    for y in range(height):
        filter_type = raw[cursor]
        cursor += 1
        encoded = raw[cursor:cursor + stride]
        cursor += stride
        row = bytearray(stride)
        for i, value in enumerate(encoded):
            left = row[i - 4] if i >= 4 else 0
            up = previous[i]
            upper_left = previous[i - 4] if i >= 4 else 0
            if filter_type == 0:
                decoded = value
            elif filter_type == 1:
                decoded = (value + left) & 0xff
            elif filter_type == 2:
                decoded = (value + up) & 0xff
            elif filter_type == 3:
                decoded = (value + ((left + up) // 2)) & 0xff
            elif filter_type == 4:
                p = left + up - upper_left
                pa, pb, pc = abs(p - left), abs(p - up), abs(p - upper_left)
                predictor = left if pa <= pb and pa <= pc else (up if pb <= pc else upper_left)
                decoded = (value + predictor) & 0xff
            else:
                raise ValueError(f'unsupported PNG filter {filter_type}')
            row[i] = decoded
        lit = [i >> 2 for i in range(3, stride, 4) if row[i] >= 24]
        if lit:
            visible += len(lit)
            if lit[0] < min_x:
                min_x = lit[0]
            if lit[-1] > max_x:
                max_x = lit[-1]
            if y < min_y:
                min_y = y
            max_y = y
        previous = row
    if max_x < 0:
        return 0.0, 0.0, 0.0
    box = (max_x - min_x + 1) * (max_y - min_y + 1)
    return visible / (width * height), box / (width * height), visible / box


# 1. Every shipped icon must decode completely, carry real alpha, and occupy a useful inventory silhouette.
icon_files = sorted(ICONS.glob('*.icon.png'))
if not icon_files:
    failures.append(f'No icons found under {ICONS}.')
for path in icon_files:
    try:
        width, height, colour, raw = decode_png(path)
    except Exception as error:  # noqa: BLE001 - report the exact decode failure
        failures.append(f'{path.name}: undecodable ({error})')
        continue
    resolution_counts[width] += 1
    if colour != 6:
        failures.append(f'{path.name}: colour type {colour}, expected 6 (RGBA)')
    elif width == height:
        try:
            coverage, box_fill, ink_density = silhouette_metrics(raw, width, height)
            if coverage < MIN_VISIBLE_ALPHA_COVERAGE:
                failures.append(f'{path.name}: visible silhouette occupies only {coverage:.1%} of the icon; the render is effectively blank')
            elif coverage > MAX_VISIBLE_ALPHA_COVERAGE:
                failures.append(f'{path.name}: visible silhouette occupies {coverage:.1%}; icon is effectively a filled square')
            elif box_fill < MIN_VISIBLE_BOX_FILL:
                failures.append(f'{path.name}: subject occupies only {box_fill:.1%} of the icon area; it is rendered too small or off-centre')
            elif ink_density < MIN_VISIBLE_INK_DENSITY:
                failures.append(f'{path.name}: only {ink_density:.1%} of the subject bounding box is drawn; the render is an outline or a ghost')
        except Exception as error:  # noqa: BLE001
            failures.append(f'{path.name}: cannot evaluate visible silhouette ({error})')
    if width != height:
        failures.append(f'{path.name}: {width}x{height} is not square')
    if width < LEGACY_ICON_FLOOR:
        failures.append(f'{path.name}: {width}px is below the {LEGACY_ICON_FLOOR}px compatibility floor')
    elif width < ICON_TARGET_SIZE:
        item = f'{path.name} ({width}px < {ICON_TARGET_SIZE}px target)'
        legacy_resolution.append(item)
        if ENFORCE_ICON_TARGET:
            failures.append(f'{item}: asset-release target enforcement is enabled')


# 2. Every staff model in the catalog must have an icon named the way the registrars ask.
def asset_name(model_id: str) -> str:
    if model_id.startswith('Magenheim_Staff_'):
        _, _, family, tier = model_id.split('_', 3)
        return f'staff-{family.lower()}-{tier.lower()}'
    return model_id


catalog = json.loads(CATALOG.read_text())
staff_models = [e['id'] for e in catalog if 'staff' in e['id'].lower()]
for model_id in sorted(staff_models):
    name = asset_name(model_id)
    if not (ICONS / f'{name}.icon.png').is_file():
        failures.append(f'{model_id}: staff model has no icon at assets/earth/{name}.icon.png')

# 3. Every literal EarthAssets.Icon("name") call must resolve to a file on disk.
literal = re.compile(r'EarthAssets\.Icon\(\s*"([^"]+)"')
for source in sorted(RUNTIME.rglob('*.cs')):
    if 'bin' in source.parts or 'obj' in source.parts:
        continue
    for name in literal.findall(source.read_text(encoding='utf-8', errors='replace')):
        if not (ICONS / f'{name}.icon.png').is_file():
            failures.append(f'{source.name}: EarthAssets.Icon("{name}") has no assets/earth/{name}.icon.png')

if failures:
    print('FAIL: icon asset verification', file=sys.stderr)
    for failure in failures:
        print('  - ' + failure, file=sys.stderr)
    sys.exit(1)

if resolution_counts:
    distribution = ', '.join(f'{size}px={count}' for size, count in sorted(resolution_counts.items()))
    print(f'ICON RESOLUTION DISTRIBUTION: {distribution}')

if legacy_resolution:
    mode = 'enforced' if ENFORCE_ICON_TARGET else 'notice-only'
    print(f'NOTICE: {len(legacy_resolution)} legacy icons remain below the {ICON_TARGET_SIZE}px readability target ({mode}):')
    for item in legacy_resolution:
        print('  - ' + item)

print(f'PASS: {len(icon_files)} icons decode as square RGBA >={LEGACY_ICON_FLOOR}px with useful alpha silhouettes; '
      f'{len(staff_models)} staff models carry a matching icon; '
      f'all literal EarthAssets.Icon references resolve; target={ICON_TARGET_SIZE}px; '
      f'coverage={MIN_VISIBLE_ALPHA_COVERAGE:.1%}-{MAX_VISIBLE_ALPHA_COVERAGE:.0%}, '
      f'box_fill>={MIN_VISIBLE_BOX_FILL:.0%}, ink_density>={MIN_VISIBLE_INK_DENSITY:.0%}; '
      f'enforce_target={ENFORCE_ICON_TARGET}.')
