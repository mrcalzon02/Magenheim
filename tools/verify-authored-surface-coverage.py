#!/usr/bin/env python3
"""Gate the authored fallback that closes the 100-model / 1,303-part texture gap."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageStat

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "assets" / "models" / "runtime"
TEXTURES = ROOT / "assets" / "models" / "textures"
FAMILIES = ("stone", "timber", "metal", "cloth", "bone", "liquid", "leather", "crystal", "generic")
BASELINE_MODELS = 100
BASELINE_PARTS = 1303
CLASSIFIERS = (
    ("liquid", ("water", "liquid", "solution")),
    ("leather", ("hide", "leather", "pelt")),
    ("stone", ("stone", "marble", "rock", "earth", "strata", "slate", "basalt")),
    ("timber", ("wood", "timber", "root", "shaft", "bark")),
    ("metal", ("iron", "bronze", "silver", "gold", "metal", "band", "collar", "brace", "rail", "rim")),
    ("cloth", ("cloth", "banner", "fabric")),
    ("bone", ("bone", "ivory", "antler")),
    ("crystal", ("crystal", "frost", "rime", "ice", "spirit", "radiance", "venom", "seidr",
                 "fate", "eitr", "gem", "shard", "growth", "focus", "core", "light")),
)


def semantic_token(value):
    segments = (value or "").split(".")
    last = len(segments) - 1
    while last > 0 and segments[last].isdigit():
        last -= 1
    return segments[last].lower() if segments else ""


def family_for(value):
    token = semantic_token(value)
    for family, needles in CLASSIFIERS:
        if any(needle in token for needle in needles):
            return family
    return "generic"


digests = {}
for family in FAMILIES:
    path = TEXTURES / f"surface-{family}-authored.png"
    if not path.is_file():
        raise SystemExit(f"Missing authored surface family: {path}")
    data = path.read_bytes()
    digest = hashlib.sha256(data).hexdigest()
    if digest in digests:
        raise SystemExit(f"Authored surface families collapse to identical files: {family} and {digests[digest]}")
    digests[digest] = family
    image = Image.open(path).convert("L")
    if image.size != (256, 256):
        raise SystemExit(f"{family}: authored surface must be 256x256, got {image.size}")
    lo, hi = image.getextrema()
    reduced = image.resize((64, 64), Image.Resampling.LANCZOS)
    rlo, rhi = reduced.getextrema()
    stddev = ImageStat.Stat(reduced).stddev[0]
    if hi - lo < 80 or rhi - rlo < 40 or stddev < 10:
        raise SystemExit(f"{family}: surface collapses at gameplay scale "
                         f"(source range {hi-lo}, 64px range {rhi-rlo}, stddev {stddev:.1f})")

fallback_models = 0
fallback_parts = 0
family_counts = {family: 0 for family in FAMILIES}
missing_explicit = []
for path in sorted(RUNTIME.glob("*.model.json")):
    document = json.loads(path.read_text(encoding="utf-8"))
    model_fallbacks = 0
    for part in document.get("parts", []):
        material = part.get("material") or {}
        texture = material.get("texture")
        if texture:
            if not (TEXTURES / texture).is_file():
                missing_explicit.append(f"{path.stem}/{part.get('name','?')}: {texture}")
            continue
        family = family_for(material.get("name", ""))
        fallback = TEXTURES / f"surface-{family}-authored.png"
        if not fallback.is_file():
            raise SystemExit(f"{path.stem}/{part.get('name','?')}: no authored fallback for {family}")
        family_counts[family] += 1
        fallback_parts += 1
        model_fallbacks += 1
    if model_fallbacks:
        fallback_models += 1

if missing_explicit:
    raise SystemExit("Missing explicit model textures:\n  " + "\n  ".join(missing_explicit[:20]))
if fallback_models > BASELINE_MODELS or fallback_parts > BASELINE_PARTS:
    raise SystemExit(f"Texture fallback regression: {fallback_models} models/{fallback_parts} parts exceed "
                     f"the audited baseline {BASELINE_MODELS}/{BASELINE_PARTS}.")

usage = ", ".join(f"{family}={family_counts[family]}" for family in FAMILIES if family_counts[family])
print(f"VERIFIED authored surfaces: {len(FAMILIES)} distinct readable 256px families; "
      f"{fallback_parts} null exported slot(s) across {fallback_models} model(s) resolve to file-backed art "
      f"(baseline ceiling {BASELINE_PARTS}/{BASELINE_MODELS}).")
if usage:
    print("Fallback family usage: " + usage)
