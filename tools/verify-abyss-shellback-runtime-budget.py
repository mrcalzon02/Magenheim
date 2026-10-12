#!/usr/bin/env python3
"""Fail closed on the existing Abyss Shellback elite runtime triangle overrun."""
import json
from pathlib import Path

CAP=24000
MINIMUM=14000
REQUIRED={f"ShellPlate_{i}" for i in range(1,6)}
REQUIRED|={f"Leg_{s}_{i}_Paddle" for s in ("L","R") for i in range(1,5)}
REQUIRED|={"BodyMass","VulnerableUnderside","Claw_L_Palm","Claw_R_Palm","Eye_L","Eye_R"}

def validate(doc):
    parts=doc.get("parts",[])
    missing=REQUIRED-{p["name"] for p in parts}
    if missing:raise ValueError("Shellback missing anatomy: "+repr(sorted(missing)))
    triangles=sum(len(p["triangles"])//3 for p in parts)
    if not MINIMUM<=triangles<=CAP:
        raise ValueError(f"Shellback LOD0 {triangles} triangles outside elite {MINIMUM}-{CAP}")
    if doc.get("creatureRig",{}).get("kind")!="rigid-segment-creature-v1":
        raise ValueError("Shellback rig missing")
    for p in parts:
        if not p.get("path","").startswith("creaturebone:"):
            raise ValueError("Shellback unbound part: "+p["name"])
        if not p.get("uv") or len(p["uv"])!=len(p["vertices"]):
            raise ValueError("Shellback UV failure: "+p["name"])
    return triangles

if __name__=="__main__":
    path=Path(__file__).resolve().parents[1]/"assets/models/runtime/underworld-creature-abyss-shellback.model.json"
    print("PASS Abyss Shellback LOD0:",validate(json.loads(path.read_text())))
