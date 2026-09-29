#!/usr/bin/env python3
"""Surgical verification for the generated Nowhere King Last Argument pair."""
import json
import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / "assets" / "models"
ICONS = ROOT / "assets" / "earth"
IDS = ("nowhere-king-sword-firmament", "nowhere-king-sword-null-gate")

def verify_png(path: Path):
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise RuntimeError(f"{path}: not a PNG")
    off, ihdr, idat = 8, None, bytearray()
    while off < len(data):
        n = struct.unpack(">I", data[off:off+4])[0]
        kind = data[off+4:off+8]
        body = data[off+8:off+8+n]
        stored = struct.unpack(">I", data[off+8+n:off+12+n])[0]
        if stored != zlib.crc32(kind + body) & 0xffffffff:
            raise RuntimeError(f"{path}: bad {kind!r} CRC")
        if kind == b"IHDR":
            ihdr = struct.unpack(">IIBB", body[:10])
        elif kind == b"IDAT":
            idat += body
        elif kind == b"IEND":
            break
        off += 12 + n
    if ihdr is None:
        raise RuntimeError(f"{path}: no IHDR")
    width, height, depth, colour = ihdr
    if (width, height, depth, colour) != (256, 256, 8, 6):
        raise RuntimeError(f"{path}: expected 256x256 8-bit RGBA, got {width}x{height} depth={depth} colour={colour}")
    raw = zlib.decompress(bytes(idat))
    if len(raw) != height * (1 + width * 4):
        raise RuntimeError(f"{path}: truncated/corrupt decoded pixels")

catalog = json.loads((MODELS / "catalog.json").read_text(encoding="utf-8"))
entries = {entry["id"]: entry for entry in catalog}
for model_id in IDS:
    source = MODELS / "source" / f"{model_id}.blend"
    glb = MODELS / "glb" / f"{model_id}.glb"
    runtime = MODELS / "runtime" / f"{model_id}.model.json"
    icon = ICONS / f"{model_id}.icon.png"
    for path in (source, glb, runtime, icon):
        if not path.is_file() or path.stat().st_size <= 64:
            raise RuntimeError(f"{model_id}: missing/empty {path.relative_to(ROOT)}")
    entry = entries.get(model_id)
    if entry is None:
        raise RuntimeError(f"{model_id}: absent from catalog")
    if entry.get("source") != f"source/{model_id}.blend" or entry.get("glb") != f"glb/{model_id}.glb":
        raise RuntimeError(f"{model_id}: catalog path mismatch")
    doc = json.loads(runtime.read_text(encoding="utf-8"))
    parts = doc.get("parts")
    minimum = 18 if model_id.endswith("firmament") else 22
    if not isinstance(parts, list) or len(parts) < minimum:
        raise RuntimeError(f"{model_id}: insufficient runtime parts")
    names = {str(part.get("name","")) for part in parts}
    paths = [str(part.get("path","")) for part in parts]
    expected_prefix = f"attach/magenheim.{model_id}.visual/"
    if not paths or any(not path.startswith(expected_prefix) for path in paths):
        raise RuntimeError(f"{model_id}: runtime part path identity mismatch")
    required = ({"firmament-frame","firmament-cosmos","firmament-heart"}
                if model_id.endswith("firmament")
                else {"null-gate-absence","null-gate-filament","null-gate-ring-0","null-gate-ring-1","null-gate-ring-2"})
    if required - names:
        raise RuntimeError(f"{model_id}: missing identity geometry {sorted(required-names)}")
    verify_png(icon)
    print("VERIFIED", model_id, len(parts), flush=True)
print("PASS: Nowhere King twin-sword generated assets are complete and internally consistent.", flush=True)
