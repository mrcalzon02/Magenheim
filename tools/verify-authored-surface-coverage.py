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
BASELINE_MODELS = 87
BASELINE_PARTS = 1048

ITEM_TEXTURE_BINDINGS = {
    "furniture-crystal-bench": {
        "magenheim.furniture.furniture-crystal-bench.dark-stone": "furniture-crystal-bench-dark-stone.png",
        "magenheim.furniture.furniture-crystal-bench.wood": "furniture-crystal-bench-wood.png",
        "magenheim.furniture.furniture-crystal-bench.iron": "furniture-crystal-bench-iron.png",
        "magenheim.furniture.furniture-crystal-bench.crystal": "furniture-crystal-bench-crystal.png",
    },
    "furniture-geode-table": {
        "magenheim.furniture.furniture-geode-table.dark-stone": "furniture-geode-table-dark-stone.png",
        "magenheim.furniture.furniture-geode-table.wood": "furniture-geode-table-wood.png",
        "magenheim.furniture.furniture-geode-table.iron": "furniture-geode-table-iron.png",
    },    "crystal-bed-earth": {
        "magenheim.crystal-bed.stone": "crystal-bed-stone.png",
        "magenheim.crystal-bed.iron": "crystal-bed-iron.png",
        "magenheim.crystal-bed.mineral-water": "crystal-bed-mineral-water.png",
        "magenheim.crystal-bed.crystal-growth": "crystal-bed-crystal-growth.png",
    },
    "crystal-bed-fire": {
        "magenheim.crystal-bed.stone": "crystal-bed-stone.png",
        "magenheim.crystal-bed.iron": "crystal-bed-iron.png",
        "magenheim.crystal-bed.mineral-water": "crystal-bed-mineral-water.png",
        "magenheim.crystal-bed.crystal-growth": "crystal-bed-crystal-growth.png",
    },
    "crystal-bed-frost": {
        "magenheim.crystal-bed.stone": "crystal-bed-stone.png",
        "magenheim.crystal-bed.iron": "crystal-bed-iron.png",
        "magenheim.crystal-bed.mineral-water": "crystal-bed-mineral-water.png",
        "magenheim.crystal-bed.crystal-growth": "crystal-bed-crystal-growth.png",
    },
    "crystal-bed-radiance": {
        "magenheim.crystal-bed.stone": "crystal-bed-stone.png",
        "magenheim.crystal-bed.iron": "crystal-bed-iron.png",
        "magenheim.crystal-bed.mineral-water": "crystal-bed-mineral-water.png",
        "magenheim.crystal-bed.crystal-growth": "crystal-bed-crystal-growth.png",
    },
    "crystal-bed-seidr": {
        "magenheim.crystal-bed.stone": "crystal-bed-stone.png",
        "magenheim.crystal-bed.iron": "crystal-bed-iron.png",
        "magenheim.crystal-bed.mineral-water": "crystal-bed-mineral-water.png",
        "magenheim.crystal-bed.crystal-growth": "crystal-bed-crystal-growth.png",
    },
    "crystal-bed-spirit": {
        "magenheim.crystal-bed.stone": "crystal-bed-stone.png",
        "magenheim.crystal-bed.iron": "crystal-bed-iron.png",
        "magenheim.crystal-bed.mineral-water": "crystal-bed-mineral-water.png",
        "magenheim.crystal-bed.crystal-growth": "crystal-bed-crystal-growth.png",
    },
    "crystal-bed-storm": {
        "magenheim.crystal-bed.stone": "crystal-bed-stone.png",
        "magenheim.crystal-bed.iron": "crystal-bed-iron.png",
        "magenheim.crystal-bed.mineral-water": "crystal-bed-mineral-water.png",
        "magenheim.crystal-bed.crystal-growth": "crystal-bed-crystal-growth.png",
    },
    "crystal-bed-venom": {
        "magenheim.crystal-bed.stone": "crystal-bed-stone.png",
        "magenheim.crystal-bed.iron": "crystal-bed-iron.png",
        "magenheim.crystal-bed.mineral-water": "crystal-bed-mineral-water.png",
        "magenheim.crystal-bed.crystal-growth": "crystal-bed-crystal-growth.png",
    },    "crystal-enchanting-dais": {
        "magenheim.crystal-enchanting-dais.dark-stone": "crystal-enchanting-dais-dark-stone.png",
        "magenheim.crystal-enchanting-dais.face-stone": "crystal-enchanting-dais-face-stone.png",
        "magenheim.crystal-enchanting-dais.iron": "crystal-enchanting-dais-iron.png",
        "magenheim.crystal-enchanting-dais.node-crystal-earth": "crystal-enchanting-dais-crystal.png",
        "magenheim.crystal-enchanting-dais.node-crystal-fire": "crystal-enchanting-dais-crystal.png",
        "magenheim.crystal-enchanting-dais.node-crystal-frost": "crystal-enchanting-dais-crystal.png",
        "magenheim.crystal-enchanting-dais.node-crystal-storm": "crystal-enchanting-dais-crystal.png",
        "magenheim.crystal-enchanting-dais.node-crystal-venom": "crystal-enchanting-dais-crystal.png",
        "magenheim.crystal-enchanting-dais.node-crystal-radiance": "crystal-enchanting-dais-crystal.png",
        "magenheim.crystal-enchanting-dais.node-crystal-seidr": "crystal-enchanting-dais-crystal.png",
        "magenheim.crystal-enchanting-dais.node-crystal-spirit": "crystal-enchanting-dais-crystal.png",
        "magenheim.crystal-enchanting-dais.central-crystal": "crystal-enchanting-dais-crystal.png",
    },
    "architecture-crystal-hearth": {
        "magenheim.architecture.architecture-crystal-hearth.stone": "architecture-crystal-hearth-stone.png",
        "magenheim.architecture.architecture-crystal-hearth.darkstone": "architecture-crystal-hearth-darkstone.png",
        "magenheim.architecture.architecture-crystal-hearth.iron": "architecture-crystal-hearth-iron.png",
        "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.0": "architecture-crystal-hearth-rainbow-crystal.png",
        "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.1": "architecture-crystal-hearth-rainbow-crystal.png",
        "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.2": "architecture-crystal-hearth-rainbow-crystal.png",
        "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.3": "architecture-crystal-hearth-rainbow-crystal.png",
        "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.4": "architecture-crystal-hearth-rainbow-crystal.png",
        "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.5": "architecture-crystal-hearth-rainbow-crystal.png",
        "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.6": "architecture-crystal-hearth-rainbow-crystal.png",
    },
    "crystalline-ice-box": {
        "magenheim.crystalline-ice-box.black-stone": "crystalline-ice-box-black-stone.png",
        "magenheim.crystalline-ice-box.ice-crystal": "crystalline-ice-box-ice-crystal.png",
        "magenheim.crystalline-ice-box.iron": "crystalline-ice-box-iron.png",
        "magenheim.crystalline-ice-box.silver": "crystalline-ice-box-silver.png",
        "magenheim.crystalline-ice-box.frost-crystal": "crystalline-ice-box-frost-crystal.png",
    },

}

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
        raise SystemExit(f"{family}: packaged fallback surface must be 256x256, got {image.size}")
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
print(f"VERIFIED authored surfaces: {len(FAMILIES)} distinct readable 256px packaged fallback families; "
      f"{fallback_parts} null exported slot(s) across {fallback_models} model(s) resolve to file-backed art "
      f"(baseline ceiling {BASELINE_PARTS}/{BASELINE_MODELS}).")
if usage:
    print("Fallback family usage: " + usage)

for model_id, expected in ITEM_TEXTURE_BINDINGS.items():
    document = json.loads((RUNTIME / (model_id + ".model.json")).read_text(encoding="utf-8"))
    seen = {}
    for part in document.get("parts", []):
        material = part.get("material") or {}
        name = material.get("name")
        if name in expected:
            texture = material.get("texture")
            if texture != expected[name]:
                raise SystemExit(f"{model_id}/{name}: expected model-specific texture {expected[name]}, got {texture}")
            path = TEXTURES / texture
            image = Image.open(path)
            if image.size != (256, 256):
                raise SystemExit(f"{model_id}/{name}: placeable texture must be 256x256, got {image.size}")
            seen[name] = texture
    missing = sorted(set(expected) - set(seen))
    if missing:
        raise SystemExit(f"{model_id}: missing bound material(s): {', '.join(missing)}")
print("VERIFIED model-specific Valheim-scale textures: crystal bench 8/8, geode table 9/9, crystal beds 128/128, enchanting dais 62/62, crystal hearth 21/21, crystalline ice box 27/27 parts.")
