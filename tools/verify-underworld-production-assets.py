#!/usr/bin/env python3
"""Post-generation admission gate for the one-run Magenheim production asset program."""
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
MODELS=ROOT/"assets"/"models"
RUNTIME=MODELS/"runtime"
SOURCE=MODELS/"source"
GLB=MODELS/"glb"
TEXTURES=MODELS/"textures"

runtime_ids={p.name[:-11] for p in RUNTIME.glob("*.model.json")}

def ids_with(prefix):
    return sorted(x for x in runtime_ids if x.startswith(prefix))

staff_ids=sorted(x for x in runtime_ids if x.startswith("staff-") or x.startswith("Magenheim_Staff_"))
expected={
    "crystal weapons":(ids_with("crystal-weapon-"),10),
    "Underworld weapons":(ids_with("underworld-weapon-"),2),
    "Rootforged":(ids_with("rootforged-"),17),
    "Underworld stations":(ids_with("underworld-station-"),6),
    "Underworld tools":(ids_with("underworld-tool-"),6),
    "Underworld armour":(ids_with("underworld-armor-"),24),
    "elemental staves":(staff_ids,32),
}
for label,(ids,count) in expected.items():
    if len(ids)!=count:
        raise SystemExit(f"{label}: expected {count} production models, found {len(ids)}")

production_ids=sorted(set().union(*(set(ids) for ids,_ in expected.values())))
if len(production_ids)!=97:
    raise SystemExit(f"Production review universe must contain 97 unique models, found {len(production_ids)}")

for model_id in production_ids:
    for path in (SOURCE/(model_id+".blend"),GLB/(model_id+".glb"),RUNTIME/(model_id+".model.json")):
        if not path.is_file():
            raise SystemExit(f"{model_id}: missing production representation {path.relative_to(ROOT)}")

pbr_prefixes=("rootforged-","underworld-station-","underworld-tool-","underworld-armor-")
pbr_models=[x for x in production_ids if x.startswith(pbr_prefixes)]
for model_id in pbr_models:
    doc=json.loads((RUNTIME/(model_id+".model.json")).read_text())
    for index,part in enumerate(doc.get("parts",[])):
        material=part.get("material") or {}
        for field in ("texture","normalTexture","metallicGlossTexture"):
            name=material.get(field)
            if not name:
                raise SystemExit(f"{model_id}/part {index}: shared-PBR material missing {field}")
            if Path(name).name!=name or not (TEXTURES/name).is_file():
                raise SystemExit(f"{model_id}/part {index}: invalid or missing runtime {field} {name!r}")
        if float(material.get("normalScale") or 0)<=0:
            raise SystemExit(f"{model_id}/part {index}: invalid normalScale")

# Crystal-derived Underworld weapons intentionally keep the Crystal chassis' painted atlas.
# Require PBR only on the added biome accent parts rather than flattening that stronger source art.
for model_id in ids_with("underworld-weapon-"):
    doc=json.loads((RUNTIME/(model_id+".model.json")).read_text())
    pbr=[p for p in doc.get("parts",[]) if (p.get("material") or {}).get("normalTexture")]
    if not pbr:
        raise SystemExit(f"{model_id}: no PBR biome accent survived Crystal-chassis derivation")
    for part in pbr:
        material=part["material"]
        for field in ("normalTexture","metallicGlossTexture"):
            name=material.get(field)
            if not name or not (TEXTURES/name).is_file():
                raise SystemExit(f"{model_id}: biome accent missing runtime {field}")

if len(runtime_ids)<404:
    raise SystemExit(f"Full model library must be at least 404 models after the 38-model Underworld production admission; found {len(runtime_ids)}")

print("VERIFIED production asset admission: 97 reviewed weapon/build/equipment models; "
      "10 Crystal weapons, 2 Underworld derivatives, 32 staves, 17 Rootforged, "
      "6 stations, 6 tools and 24 armour pieces with required PBR runtime maps.")
