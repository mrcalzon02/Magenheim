"""Build the geode source through the complete topology + surface acceptance pipeline.

This is the authoritative Blender entry point for geode-sample.  The missing-plane repair had
previously been split across several manually ordered scripts, which made it too easy to export
a regenerated but still defective intermediate .blend.  Run this file from repository root:

    blender --background --factory-startup --python tools/build-geode-accepted-asset.py

The pass deliberately executes the existing authorities rather than duplicating their geometry:
  1. generate-geode-models.py              baseline authored shell/cavity/banding/druzy
  2. rebuild-geode-watertight-shell.py     continuous exterior, one intentional mouth
  3. repair-geode-topology.py              manifold backing/core topology
  4. refine-geode-surface-fidelity.py      differentiated authored surface response

Every stage is required.  Blender exits non-zero at the first failed topology/material contract,
so an intermediate geode cannot be mistaken for the accepted source asset.
"""
from pathlib import Path
import runpy
import sys

ROOT = Path(__file__).resolve().parents[1]
TOOLS = ROOT / "tools"
SOURCE = ROOT / "assets/models/source/geode-sample.blend"
STAGES = (
    "generate-geode-models.py",
    "rebuild-geode-watertight-shell.py",
    "repair-geode-topology.py",
    "refine-geode-surface-fidelity.py",
)


def run_stage(filename):
    path = TOOLS / filename
    if not path.is_file():
        raise RuntimeError("Missing required geode build stage: " + str(path))
    print("[Magenheim geode] running " + filename)
    runpy.run_path(str(path), run_name="__main__")
    if not SOURCE.is_file():
        raise RuntimeError(filename + " did not leave the authoritative geode source asset")


for stage in STAGES:
    run_stage(stage)

# The final two stages open/save the same source asset.  Assert Blender's current scene is the
# accepted five-part contract so a stage that silently opened the wrong file cannot pass.
import bpy
expected = {"GeodeShell", "GeodeCore", "GeodeCavity", "GeodeBanding", "GeodeDruzy"}
actual = {obj.name for obj in bpy.context.scene.objects if obj.type == "MESH"}
if actual != expected:
    raise RuntimeError("Final geode source contract drift: expected %s, found %s" %
                       (sorted(expected), sorted(actual)))

for name in expected:
    obj = bpy.data.objects[name]
    if not obj.data.polygons:
        raise RuntimeError("Accepted geode part has no faces: " + name)
    if len(obj.data.materials) != 1 or obj.data.materials[0] is None:
        raise RuntimeError("Accepted geode part lacks its authored material: " + name)

print("GEODE ACCEPTED SOURCE BUILD: topology + five-part material contract complete")
sys.exit(0)
