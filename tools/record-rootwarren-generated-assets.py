#!/usr/bin/env python3
"""Record a successfully forged Rootwarren family into generated.manifest.json.

This is deliberately separate from the pre-forge manifest: normal builds must pass while Rootwarren
is still Planned with zero generated payloads. Once all 17 source/GLB/runtime models exist, the
production forge runs this recorder and from that point normal generated-freshness validation owns
the family exactly like the other Blender-generated Magenheim models.
"""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "assets/generated.manifest.json"
PREFIX = "underworld-dungeon-fungal-rootwarren-"
GENERATOR_ID = "rootwarren-dungeon-models"
GENERATOR = "tools/author-rootwarren-dungeon.py"
INPUTS = [
    "src/Magenheim.Core/Underworld/UnderworldFungalRootwarrenCatalog.cs",
    "tools/magenheim_blender_kit.py",
    "tools/magenheim_flora_kit.py",
    "tools/verify-rootwarren-dungeon.py",
    "tools/verify-rootwarren-production-contract.py",
    "tools/rebuild-rootwarren-dungeon.ps1",
    "tools/export-model-assets.py",
    "tools/model_surface_authoring.py",
]
OUTPUT_PATTERNS = [
    "assets/models/source/underworld-dungeon-fungal-rootwarren-*.blend",
    "assets/models/glb/underworld-dungeon-fungal-rootwarren-*.glb",
    "assets/models/runtime/underworld-dungeon-fungal-rootwarren-*.model.json",
]


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def input_hash() -> str:
    digest = hashlib.sha256()
    for name in INPUTS:
        path = ROOT / name
        if not path.is_file():
            raise SystemExit("Rootwarren freshness input is missing: " + name)
        digest.update(name.encode("utf-8") + b"|" + sha256(path).encode("ascii") + b"|")
    return digest.hexdigest()


def resolve_outputs():
    outputs = set()
    for pattern in OUTPUT_PATTERNS:
        found = [path for path in ROOT.glob(pattern) if path.is_file()]
        if len(found) != 17:
            raise SystemExit(
                f"Rootwarren forge must produce 17 files for {pattern!r}; found {len(found)}")
        outputs.update(path.relative_to(ROOT).as_posix() for path in found)
    if len(outputs) != 51:
        raise SystemExit(
            f"Rootwarren freshness expects 51 source/GLB/runtime outputs, found {len(outputs)}")
    return sorted(outputs)


def main():
    generator = ROOT / GENERATOR
    if not generator.is_file():
        raise SystemExit("Rootwarren generator is missing: " + GENERATOR)

    outputs = resolve_outputs()
    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    entries = data["generators"]

    # Reject another generator already claiming any exact Rootwarren output.
    output_set = set(outputs)
    for entry in entries:
        if entry.get("id") == GENERATOR_ID:
            continue
        recorded = set((entry.get("output_sha256") or {}).keys())
        collision = sorted(recorded & output_set)
        if collision:
            raise SystemExit(
                f"Rootwarren output already belongs to {entry.get('id')}: {collision[0]}")

    replacement = {
        "id": GENERATOR_ID,
        "generator": GENERATOR,
        "command": [
            "pwsh",
            "-NoProfile",
            "-File",
            "tools/rebuild-rootwarren-dungeon.ps1",
        ],
        "outputs": OUTPUT_PATTERNS,
        "hash": "generator-only",
        "generator_sha256": sha256(generator),
        "output_sha256": {name: None for name in outputs},
        "inputs": INPUTS,
        "inputs_sha256": input_hash(),
    }

    existing = next(
        (index for index, entry in enumerate(entries)
         if entry.get("id") == GENERATOR_ID),
        None,
    )
    if existing is None:
        entries.append(replacement)
    else:
        entries[existing] = replacement

    MANIFEST.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
    print(
        "RECORDED Rootwarren generated freshness:",
        f"{len(outputs)} outputs; generator={replacement['generator_sha256'][:12]};",
        f"inputs={replacement['inputs_sha256'][:12]}",
    )


if __name__ == "__main__":
    main()
