"""Gate the shipped inventory icons.

Two defect classes reached players because nothing checked icons at build time:

  * three committed staff icons had corrupt IDAT chunks, and EarthAssets.Texture throws
    InvalidDataException when Unity cannot decode a file;
  * the Earth staff family asked for icons that were never authored, so
    EarthAssets.Icon raised FileNotFoundException.

Both only fail at registration time, inside PrefabManager.OnVanillaPrefabsAvailable.
That event is a multicast delegate, so the first registrar that throws also stops every
registrar subscribed after it -- a missing icon silently removes unrelated content.

Exits non-zero on any failure so build.ps1 fails the build.
"""
import json
import re
import struct
import sys
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ICONS = ROOT / 'assets/earth'
RUNTIME = ROOT / 'src/Magenheim.Runtime'
CATALOG = ROOT / 'assets/models/catalog.json'

failures: list[str] = []


def decode_png(path: Path) -> tuple[int, int, int]:
    """Walk the PNG chunk stream, verifying every CRC. Returns (width, height, colour_type)."""
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
    # Decompressing proves the pixel stream is complete, not merely CRC-clean.
    width, height, depth, colour = size
    channels = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[colour]
    expected = height * (1 + width * channels * depth // 8)
    raw = zlib.decompress(bytes(idat))
    if len(raw) != expected:
        raise ValueError(f'pixel stream is {len(raw)} bytes, expected {expected}')
    return width, height, colour


# 1. Every shipped icon must decode completely and carry a real alpha channel.
icon_files = sorted(ICONS.glob('*.icon.png'))
if not icon_files:
    failures.append(f'No icons found under {ICONS}.')
for path in icon_files:
    try:
        width, height, colour = decode_png(path)
    except Exception as error:  # noqa: BLE001 - report the exact decode failure
        failures.append(f'{path.name}: undecodable ({error})')
        continue
    if colour != 6:
        failures.append(f'{path.name}: colour type {colour}, expected 6 (RGBA)')
    if width != height:
        failures.append(f'{path.name}: {width}x{height} is not square')
    if width < 128:
        failures.append(f'{path.name}: {width}px is below the 128px icon floor')

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

print(f'PASS: {len(icon_files)} icons decode as square RGBA >=128px; '
      f'{len(staff_models)} staff models carry a matching icon; '
      f'all literal EarthAssets.Icon references resolve.')
