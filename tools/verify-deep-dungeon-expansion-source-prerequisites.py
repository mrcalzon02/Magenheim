#!/usr/bin/env python3
"""Static prerequisites for DDE Gates 2-10. Live gate completion still requires installed Valheim."""
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
RUNTIME=ROOT/"src/Magenheim.Runtime"
CORE=ROOT/"src/Magenheim.Core"/"Underworld"
fail=[]

def require(ok,msg):
    if not ok:
        fail.append(msg)

registrar=(RUNTIME/"UnderworldVanillaDungeonRegistrar.cs").read_text()
candidate=(RUNTIME/"UnderworldVanillaDungeonCandidatePolicy.cs").read_text()
bridge=(RUNTIME/"UnderworldWorldgenContentBridge.cs").read_text()
placement=(RUNTIME/"UnderworldDungeonPlacementRuntime.cs").read_text()
reuse=(CORE/"UnderworldVanillaDungeonReuseCatalog.cs").read_text()
catalog=(CORE/"UnderworldDungeonCatalog.cs").read_text()

# Gate 2 — vanilla isolation.
for token,label in (
    ("UnityEngine.Object.Instantiate(source)","private donor-room clone"),
    ("new CustomRoom(","Magenheim-owned room registration"),
    ("CreateClonedLocation(","Magenheim-owned location clone"),
    ("CreateClonedPrefab(","Magenheim-owned door clone"),
    ("RegisterDungeonTheme","private dungeon theme"),
    ("generator.m_themes = Room.Theme.None","vanilla theme exclusion"),
):
    require(token in registrar,"DDE-02 missing "+label)

# Gate 3 — physical scale integrity source prerequisites.
require("clone.transform.localScale *= scale" in registrar,
        "DDE-03 room root is not scaled")
require("room.m_size = new Vector3Int(" in registrar,
        "DDE-03 Room.m_size is not scaled with visible geometry")
require("clone.transform.localScale *= (float)profile.LinearRoomScale" in registrar,
        "DDE-03 donor door clone is not scaled")
require("generator.m_tileWidth *= (float)profile.LinearRoomScale" in registrar,
        "DDE-03 generator physical tile width is not scaled")
require("MinimumLinearRoomScale = 1.5d" in reuse,
        "DDE-03 1.5x scale floor is absent")

# Gate 4 — 3.5x room count and packing volume.
require("profile.ExpandedMinimumRooms(vanillaMin)" in registrar and
        "profile.ExpandedMaximumRooms(vanillaMax)" in registrar,
        "DDE-04 clone room bounds are not derived from live donor values")
require("profile.LinearRoomScale * Math.Sqrt(profile.RoomCountMultiplier)" in registrar,
        "DDE-04 generator packing span is not derived from room scale/count")
require("generator.m_zoneSize *= zoneScale" in registrar,
        "DDE-04 generator legal zone is not expanded")
require("MinimumRoomCountMultiplier = 3.5d" in reuse,
        "DDE-04 3.5x room-count floor is absent")

# Gate 5 — required rooms / doors fail closed.
require("required room(s) did not resolve to private clones" in registrar,
        "DDE-05 missing-required-room failure is not fail-closed")
require("requires at least" in registrar and "required-room identities exist" in registrar,
        "DDE-05 minimum required-room satisfiability is not enforced")
require("doors-cloned=" in registrar,
        "DDE-05 cloned door diagnostics are absent")

# Gate 6 — creature ecology replacement.
for token,label in (
    ("GetComponentsInChildren<CreatureSpawner>","CreatureSpawner replacement"),
    ("m_creaturePrefab = prefab","CreatureSpawner Magenheim binding"),
    ("GetComponentsInChildren<SpawnArea>","SpawnArea replacement"),
    ("area.m_prefabs.Clear()","donor SpawnArea removal"),
    ("Creatures(profile.Biome)","owning-biome creature vocabulary"),
):
    require(token in registrar,"DDE-06 missing "+label)

# Gate 7 — resource / reward replacement.
for token,label in (
    ("GetComponentsInChildren<Container>","container reward replacement"),
    ("m_defaultItems = ResourceTable","container biome-resource table"),
    ("GetComponentsInChildren<Pickable>","pickable reward replacement"),
    ("GetComponentsInChildren<MineRock>","MineRock reward replacement"),
    ("GetComponentsInChildren<MineRock5>","MineRock5 reward replacement"),
    ("GetComponentsInChildren<DropOnDestroyed>","destructible reward replacement"),
    ("Resources(profile.Biome)","owning-biome resource vocabulary"),
):
    require(token in registrar,"DDE-07 missing "+label)

# Gate 8 — retain donor mechanics while stripping donor lore/progression.
require("GetComponentsInChildren<Vegvisir>" in registrar and
        "GetComponentsInChildren<Runestone>" in registrar,
        "DDE-08 donor lore stripping is absent")
require("CreateClonedLocation(definition.PrefabName, donorEntrance)" in registrar,
        "DDE-08 donor entrance/mechanics are not inherited by clone")
require("GetComponentInChildren<DungeonGenerator>(true)" in registrar,
        "DDE-08 clone does not preserve donor DungeonGenerator mechanics")

# Gate 9 — exact Underworld confinement and Planned fail-close.
require("zone.m_biome = UnderworldTerrainRuntime.ToNativeBiome(profile.Biome)" in registrar,
        "DDE-09 clone is not bound to exact owning Underworld biome")
require("UnderworldVanillaDungeonCandidatePolicy.IsWorldgenAdmitted(dungeon)" in bridge,
        "DDE-09 detached catalog does not use admitted-family predicate")
require("without candidate admission" in bridge and "without candidate admission" in placement,
        "DDE-09 non-candidate Planned rows are not rejected")
require('"EnablePlannedCandidateWorldgen",\n            false' in candidate,
        "DDE-09 candidate override is not disabled by default")

# Gate 10 source prerequisite — preserve donor location/interior mechanics, no custom return system.
require("CreateClonedLocation(definition.PrefabName, donorEntrance)" in registrar,
        "DDE-10 derivative does not inherit donor entrance/interior transport")
for forbidden in ("UnderworldGateTransitRuntime","ReturnToSurface","DeepFractureReturn"):
    require(forbidden not in registrar,
            "DDE-10 ordinary donor registrar wrongly owns non-donor return mechanism: "+forbidden)

# Catalog may remain Planned until live gates are complete.
for name in ("FungalForest","BlackwaterDeep","SulfurousWastes","FrozenCaverns","GreatDecay"):
    require(f"public static UnderworldDungeonDefinition {name}" in catalog,
            "DDE catalog lost ordinary family "+name)

if fail:
    print("FAIL DDE Gates 2-10 source prerequisites")
    for item in fail:
        print(" - "+item)
    raise SystemExit(1)

print("PASS DDE Gates 2-10 source prerequisites")
print(" - this is static admission only; no live DDE gate is claimed passed")
