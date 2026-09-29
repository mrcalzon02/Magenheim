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
    "Underworld raw materials":(ids_with("underworld-resource-"),22),
    "Underworld refined materials":(ids_with("underworld-refined-"),18),
    "crystal weapons":(ids_with("crystal-weapon-"),10),
    "Underworld weapons":(ids_with("underworld-weapon-"),12),
    "geothermal vents":(ids_with("underworld-geothermal-vent-"),3),
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
if len(production_ids)!=150:
    raise SystemExit(f"Production review universe must contain 150 unique models, found {len(production_ids)}")

for model_id in production_ids:
    for path in (SOURCE/(model_id+".blend"),GLB/(model_id+".glb"),RUNTIME/(model_id+".model.json")):
        if not path.is_file():
            raise SystemExit(f"{model_id}: missing production representation {path.relative_to(ROOT)}")
    if model_id.startswith(("underworld-resource-","underworld-refined-")):
        icon=ROOT/"assets"/"earth"/(model_id+".icon.png")
        if not icon.is_file():
            raise SystemExit(f"{model_id}: missing owned material inventory icon {icon.relative_to(ROOT)}")

pbr_prefixes=("underworld-geothermal-vent-","rootforged-","underworld-station-","underworld-tool-","underworld-armor-")
pbr_models=[x for x in production_ids if x.startswith(pbr_prefixes)]
material_specs=json.loads((ROOT/"tools"/"underworld-material-item-specs.json").read_text())
new_material_ids=set(material_specs)
if len(new_material_ids)!=32:
    raise SystemExit(f"Underworld material-item art catalog must contain 32 new models, found {len(new_material_ids)}")
pbr_models+=sorted(new_material_ids)
for model_id in pbr_models:
    doc=json.loads((RUNTIME/(model_id+".model.json")).read_text())
    skinned=model_id.startswith("underworld-armor-") or model_id=="underworld-tool-diving-bell-hood"
    if skinned:
        rig=doc.get("skinRig") or {}
        if rig.get("kind")!="valheim-player-attach-skin" or rig.get("root")!="Hips" or len(rig.get("bones") or [])!=53:
            raise SystemExit(f"{model_id}: canonical 53-bone Valheim attach_skin contract missing")
    for index,part in enumerate(doc.get("parts",[])):
        if skinned:
            weights=part.get("skinWeights")
            if not isinstance(weights,list) or len(weights)!=len(part.get("vertices") or []):
                raise SystemExit(f"{model_id}/part {index}: skin weights do not match exported vertices")
            for vertex,row in enumerate(weights):
                if not 1<=len(row)<=4 or abs(sum(float(pair[1]) for pair in row)-1.0)>.002:
                    raise SystemExit(f"{model_id}/part {index}/vertex {vertex}: invalid normalized skin weights")
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

if len(runtime_ids)<449:
    raise SystemExit(f"Full model library must be at least 449 models after complete Underworld material-item admission; found {len(runtime_ids)}")

print("VERIFIED production asset admission: 150 reviewed material/weapon/build/equipment/environment models; "
      "22 raw materials, 18 refined materials, 10 Crystal weapons, 12 Underworld derivatives, 32 staves, 3 geothermal vents, 17 Rootforged, "
      "6 stations, 6 tools and 24 armour pieces with required PBR runtime maps.")
