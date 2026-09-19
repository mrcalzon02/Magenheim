#!/usr/bin/env python3
"""Fail the build if a model payload references one of our own generated surfaces as if it were art.

    python tools/verify-no-baked-surfaces.py

`magenheim.surface.*` textures are produced by GeneratedSurfaceTextures at runtime. At some point
they were baked back into the .blend sources, where they became indistinguishable from authored art.
Those bakes predate the 0.0.66 classifier fix, when Classify matched the model id, so every material
on `architecture-crystal-foundation-2m` -- darkstone and iron included -- was frozen as Crystal. The
runtime repair pass could never undo it, because it only replaces textures of 4px or smaller and
these are 256px and file-backed. 100 of 283 models and 1,303 materials carried one.

tools/export-model-assets.py now skips them, so a material that has only a bake exports with no
texture and the runtime classifies it correctly from its own semantic at load. This gate exists so a
revert of that skip is caught here rather than in a hand. Texture filenames are content hashes, so a
returning bake reproduces its exact filename -- which makes a denylist of those hashes exact rather
than approximate.
"""
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "assets" / "models" / "runtime"

# Content hashes of the generated surfaces found baked into the sources, recorded when they were
# removed. Regenerating any of them yields the same filename, so presence here is proof of a revert.
BAKED_SURFACES = {
    "949a8c48035cd58b.png": "magenheim.surface.crystal",
    "8f57e48cf3cb87f8.png": "magenheim.surface.metal",
    "37ba9ca48f13ce38.png": "magenheim.surface.timber",
    "4e455a71226cdea9.png": "magenheim.surface.stone",
    "8c7680a1552fc2a9.png": "magenheim.surface.cloth",
    "8e352c7f8052d0c2.png": "magenheim.surface.liquid",
    "d42954bbbe07391a.png": "magenheim.surface.bone",
    "e51c6e97d6fc0fd9.png": "magenheim.surface.leather",
}

offenders = []
payloads = sorted(RUNTIME.glob("*.model.json"))
for path in payloads:
    document = json.loads(path.read_text(encoding="utf-8"))
    for part in document.get("parts", []):
        texture = part.get("material", {}).get("texture")
        if texture in BAKED_SURFACES:
            offenders.append((path.stem, part.get("name", "?"), texture, BAKED_SURFACES[texture]))

if offenders:
    print(f"FAILED: {len(offenders)} model part(s) reference a baked generated surface as authored art.")
    for model, part, texture, origin in offenders[:20]:
        print(f"  {model}/{part}: {texture} ({origin})")
    if len(offenders) > 20:
        print(f"  ... and {len(offenders) - 20} more")
    print("tools/export-model-assets.py must skip images named 'magenheim.surface.*'; re-export after restoring it.")
    sys.exit(1)

print(f"VERIFIED no baked generated surfaces across {len(payloads)} model payloads.")
