#!/usr/bin/env python3
"""Build, export, and verify the accepted geode asset in one command.

    python tools/accept-geode-asset.py

This is intentionally a local asset-authoring command, not CI. It closes the gap between the
Blender source repair and the checked-in runtime payload: a successful run means the accepted
source was rebuilt, geode-sample alone was exported, and both backing-core and visible-shell
topology gates passed against that fresh export.
"""
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
TOOLS = ROOT / "tools"
def _resolve_blender():
    """Blender is rarely on PATH on a Windows workstation, and requiring it there meant this
    acceptance pass could never run: the Blender source repair existed while the checked-in
    runtime payload stayed broken, which is exactly the gap this command is supposed to close.
    Honour MAGENHEIM_BLENDER first, then PATH, then the standard install locations."""
    import os

    override = os.environ.get("MAGENHEIM_BLENDER")
    if override and Path(override).is_file():
        return override

    found = shutil.which("blender")
    if found:
        return found

    for root in (Path(r"C:/Program Files/Blender Foundation"),
                 Path(r"C:/Program Files (x86)/Blender Foundation")):
        if not root.is_dir():
            continue
        # Newest install wins, so a machine with several versions uses the current one.
        for candidate in sorted(root.glob("Blender */blender.exe"), reverse=True):
            return str(candidate)
    return None


BLENDER = _resolve_blender()

if BLENDER is None:
    raise SystemExit(
        "GEODE ACCEPTANCE BLOCKED: Blender was not found on PATH, in MAGENHEIM_BLENDER, or in a "
        "standard install location. Set MAGENHEIM_BLENDER to blender.exe and rerun "
        "`python tools/accept-geode-asset.py`; no source or runtime asset was changed."
    )


def run(label, command):
    print(f"[Magenheim geode] {label}", flush=True)
    completed = subprocess.run(command, cwd=ROOT)
    if completed.returncode:
        raise SystemExit(f"GEODE ACCEPTANCE FAILED during {label} (exit {completed.returncode})")


run(
    "rebuilding accepted Blender source",
    [BLENDER, "--background", "--factory-startup", "--python", str(TOOLS / "build-geode-accepted-asset.py")],
)
run(
    "exporting geode-sample runtime payload",
    [BLENDER, "--background", "--factory-startup", "--python", str(TOOLS / "export-model-assets.py"), "--", "geode-sample"],
)
run("verifying backing/core topology", [sys.executable, str(TOOLS / "verify-geode-topology.py")])
run("verifying visible shell topology", [sys.executable, str(TOOLS / "verify-geode-shell-topology.py")])

required = (
    ROOT / "assets/models/source/geode-sample.blend",
    ROOT / "assets/models/runtime/geode-sample.model.json",
    ROOT / "assets/models/glb/geode-sample.glb",
)
missing = [str(path.relative_to(ROOT)) for path in required if not path.is_file() or path.stat().st_size == 0]
if missing:
    raise SystemExit("GEODE ACCEPTANCE FAILED: missing/empty outputs: " + ", ".join(missing))

print("GEODE ASSET ACCEPTED: rebuilt source, fresh runtime/GLB export, core topology PASS, visible shell topology PASS")
