"""Development-only extraction of installed vanilla manufacturing baselines.

Reads serialized data, never boots the game or changes assets/saves. The resulting
catalog is shipped data, not a design generator run during plugin startup.
"""
import argparse
import hashlib
import json
import math
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "dist/toolchain/unitypy"))
import UnityPy
from UnityPy.classes import PPtr

ITEM_TYPES = {1: "Material", 3: "OneHandedWeapon", 4: "Bow", 5: "Shield",
              6: "Helmet", 7: "Chest", 9: "Ammo", 11: "Legs", 12: "Hands",
              14: "TwoHandedWeapon", 15: "Torch", 17: "Shoulder", 18: "Utility",
              19: "Tool", 20: "Attach_Atgeir", 22: "TwoHandedWeaponLeft",
              23: "AmmoNonEquipable", 24: "Trinket"}
EQUIPMENT = set(ITEM_TYPES) - {1}
ITEM_TYPES.update({0: "None", 2: "Consumable", 10: "Customization", 13: "Trophy", 16: "Misc", 21: "Fish"})
SKILLS = {1: "Sword", 2: "Knife", 3: "Mace", 4: "Polearm", 5: "Spear",
          7: "Axe", 8: "Bow", 9: "Magical", 10: "Magical", 12: "Pickaxe", 14: "Crossbow"}


def family(item_type, skill, animation):
    if item_type not in EQUIPMENT: return "Material"
    if item_type in (6, 7, 11, 12, 17): return "Armour"
    if item_type in (9, 23): return "Ammunition"
    if item_type == 5: return "Shield"
    if item_type in (18, 24): return "Utility"
    if item_type in (15, 19): return "Tool"
    if animation == 10: return "Crossbow"
    if animation == 2: return "Hammer"
    return SKILLS.get(skill, "Tool")


def native_amount(base, per_level, upgrade_resource, quality):
    # Installed Piece.Requirement.GetAmount IL: quality 4 uses multiplier 4,
    # then +0.5 per quality. Upgrade-only resources also retain their base cost.
    if quality <= 1: return base
    multiplier = quality - 1 if quality < 4 else 4 + (quality - 4) / 2
    return math.floor(multiplier * per_level + (base if upgrade_resource else 0))


def extract(game_root):
    softref = game_root / "valheim_Data/StreamingAssets/SoftRef"
    manifest = (softref / "manifest_extended").read_text(encoding="utf-8")
    entries = re.findall(r"- asset ID: (\w+)\s+bundle: (\w+)\s+path in bundle: ([^\r\n]+)", manifest)
    bundles = sorted({bundle for _, bundle, path in entries if "/recipes/" in path.lower()})
    if not bundles: raise ValueError("Installed manifest contains no recipe bundles")
    env = UnityPy.load(*(str(softref / "Bundles" / bundle) for bundle in bundles))
    print(f"Loaded {len(bundles)} recipe bundle(s); inspecting {len(env.container)} serialized assets", flush=True)
    cache = {}

    def tree(obj):
        key = (obj.assets_file.name, obj.path_id)
        if key not in cache: cache[key] = obj.read_typetree()
        return cache[key]

    def resolve(obj, pointer):
        if not pointer["m_PathID"]: return None
        return PPtr(**pointer, assetsfile=obj.assets_file).deref()

    def reference_name(obj, pointer):
        target = resolve(obj, pointer)
        if target is None: return ""
        t = tree(target)
        if t.get("m_Name"): return t["m_Name"]
        if "m_GameObject" in t: return tree(resolve(target, t["m_GameObject"]))["m_Name"]
        raise ValueError(f"Unnamed required reference in {obj.path_id}: {pointer}")

    items = {}
    stations = {}
    recipes = []
    for path, obj in sorted(dict(env.container.items()).items()):
        obj = obj.deref()
        if obj.type.name != "GameObject" or not path.lower().endswith(".prefab"): continue
        # Inspect root components only: weapons inside enemies are not item prefabs.
        go = obj.read()
        for entry in go.m_Component:
            component = entry.component
            if component.type.name != "MonoBehaviour": continue
            t = tree(component.deref())
            if "m_itemData" in t:
                shared = t["m_itemData"]["m_shared"]
                kind = shared["m_itemType"]
                if kind not in ITEM_TYPES: continue
                if go.m_Name in items: raise ValueError(f"Duplicate item identity: {go.m_Name}")
                items[go.m_Name] = dict(prefab=go.m_Name, sourcePath=path, itemType=kind,
                    family=family(kind, shared["m_skillType"], shared["m_animationState"]),
                    nameKey=shared["m_name"], maxQuality=shared["m_maxQuality"],
                    maxStack=shared["m_maxStackSize"], dlc=shared["m_dlc"],
                    disposition="Process" if kind == 1 else "Exempt",
                    reason="Material stock; existing resource identity is preserved" if kind == 1 else "No serialized equipment recipe; preserve acquisition route")
            if "m_craftRequireRoof" in t:
                stations[go.m_Name] = dict(prefab=go.m_Name, nameKey=t["m_name"],
                    sourcePath=path, baseLevel=1,
                    requiresRoof=bool(t["m_craftRequireRoof"]),
                    requiresFire=bool(t.get("m_craftRequireFire", False)), disposition="Keep")

    for path, obj in sorted(dict(env.container.items()).items()):
        obj = obj.deref()
        if "/recipes/" not in path.lower() or obj.type.name != "MonoBehaviour": continue
        t = tree(obj)
        if "m_resources" not in t or "m_item" not in t: raise ValueError(f"Unexpected recipe asset: {path}")
        output = reference_name(obj, t["m_item"])
        item = items.get(output)
        if item is None or item["itemType"] not in EQUIPMENT: continue
        active = bool(t["m_enabled"])
        disposition = "Exempt" if not active else "Adapt" if item["family"] in ("Magical", "Utility") else "Replace"
        reason = "Disabled vanilla asset; never enable implicitly" if not active else "Preserve magical/utility identity and special dependencies" if disposition == "Adapt" else "Replace production route; retain original finished prefab and quality mechanics"
        if active: item.update(disposition=disposition, reason=reason)
        costs = []
        for requirement in t["m_resources"]:
            resource = reference_name(obj, requirement["m_resItem"])
            if not resource: raise ValueError(f"Missing ingredient in {path}")
            costs.append(dict(prefab=resource, amount=requirement["m_amount"],
                amountPerLevel=requirement["m_amountPerLevel"],
                upgraderResource=bool(requirement["m_upgraderResource"]),
                extraAmountOnlyOneIngredient=requirement["m_extraAmountOnlyOneIngredient"],
                recover=bool(requirement["m_recover"]),
                qualityCosts=[native_amount(requirement["m_amount"], requirement["m_amountPerLevel"],
                    requirement["m_upgraderResource"], q) for q in range(1, item["maxQuality"] + 1)]))
        recipes.append(dict(id=t["m_Name"], outputPrefab=output, sourcePath=path,
            family=item["family"], disposition=disposition, reason=reason,
            enabled=active, outputAmount=t["m_amount"],
            qualityResultAmountMultiplier=t["m_qualityResultAmountMultiplier"],
            upgradeOnly=bool(t["m_noCraftOnlyUpgrade"]),
            requireOnlyOneIngredient=bool(t["m_requireOnlyOneIngredient"]),
            craftingStation=reference_name(obj, t["m_craftingStation"]),
            repairStation=reference_name(obj, t["m_repairStation"]),
            minStationLevel=t["m_minStationLevel"], costs=costs))
    if not recipes or not stations: raise ValueError(f"Equipment or station extraction was empty: items={len(items)}, recipes={len(recipes)}, stations={len(stations)}")
    return dict(schemaVersion=1, scope="SerializedVanillaCandidates",
        sourceHashes={str(p.relative_to(game_root)).replace('\\', '/'): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in [softref / "manifest_extended", game_root / "valheim_Data/Managed/assembly_valheim.dll"] + [softref / "Bundles" / b for b in bundles]},
        items=sorted(items.values(), key=lambda x: x["prefab"]),
        recipes=sorted(recipes, key=lambda x: x["id"]),
        stations=sorted(stations.values(), key=lambda x: x["prefab"]))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-root", type=Path, default=ROOT.parent)
    parser.add_argument("--output", type=Path, default=ROOT / "true-blacksmithing/data/vanilla-catalog.json")
    args = parser.parse_args()
    result = extract(args.game_root)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(f"Captured {len(result['items'])} items, {len(result['recipes'])} equipment recipes, {len(result['stations'])} stations")
