#!/usr/bin/env python3
"""Build a transient Blender library from runtime-extracted Valheim cinematic donors.

The output is an Actions/local production artifact and MUST NOT be committed to the
Magenheim repository. The repository stores only prefab identities and assembly logic.
"""
from __future__ import annotations
import argparse,json,sys
from pathlib import Path
import bpy

def args():
    raw=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    ap=argparse.ArgumentParser()
    ap.add_argument("--export-dir",type=Path,required=True)
    ap.add_argument("--resolved",type=Path,required=True)
    ap.add_argument("--output",type=Path,required=True)
    return ap.parse_args(raw)

def import_obj(path:Path):
    before=set(bpy.data.objects)
    if hasattr(bpy.ops.wm,"obj_import"):
        bpy.ops.wm.obj_import(filepath=str(path))
    elif hasattr(bpy.ops.import_scene,"obj"):
        bpy.ops.import_scene.obj(filepath=str(path))
    else:
        raise RuntimeError("This Blender build has no OBJ importer.")
    return [o for o in bpy.data.objects if o not in before]

def main():
    ns=args()
    data=json.loads(ns.resolved.read_text(encoding="utf-8"))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene=bpy.context.scene
    scene["magenheim_cinematic_donor_library"]=1
    scene["distribution"]="transient-runtime-extracted-valheim-assets"
    imported=0
    for row in data["resolved"]:
        logical=row["logical"]; prefab=row["prefab"]
        path=ns.export_dir/row["file"]
        if not path.is_file():
            raise RuntimeError(f"Missing resolved OBJ: {path}")
        donor=bpy.data.collections.new("VALHEIM_"+logical)
        scene.collection.children.link(donor)
        objects=import_obj(path)
        if not objects:
            raise RuntimeError(f"OBJ imported no objects: {path}")
        root=bpy.data.objects.new("DONOR_"+logical,None)
        donor.objects.link(root)
        root["logical_role"]=logical
        root["valheim_prefab"]=prefab
        for obj in objects:
            for col in tuple(obj.users_collection):
                col.objects.unlink(obj)
            donor.objects.link(obj)
            if obj.parent is None:
                obj.parent=root
        imported+=1
    ns.output.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(ns.output),compress=True)
    if imported!=len(data["resolved"]):
        raise RuntimeError("Donor library import count mismatch.")
    print(f"WROTE transient Valheim cinematic donor library: {imported} donors -> {ns.output}",flush=True)

if __name__=="__main__":
    main()
