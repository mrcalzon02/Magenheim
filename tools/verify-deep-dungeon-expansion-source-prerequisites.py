#!/usr/bin/env python3
"""Static prerequisites for DDE Gates 2-12. Live gate completion still requires installed Valheim."""
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
RUNTIME=ROOT/"src/Magenheim.Runtime"
CORE=ROOT/"src/Magenheim.Core"/"Underworld"
fail=[]

def require(ok,msg):
    if not ok:
        fail.append(msg)

registrar=(RUNTIME/"UnderworldVanillaDungeonRegistrar.cs").read_text()
diagnostics=(RUNTIME/"UnderworldVanillaDungeonGenerationDiagnostics.cs").read_text()
dev_commands=(RUNTIME/"UnderworldDevCommands.cs").read_text()
candidate_audit=(RUNTIME/"UnderworldVanillaDungeonCandidateAudit.cs").read_text()
room_audit_metadata=(RUNTIME/"UnderworldVanillaDungeonRoomAuditMetadata.cs").read_text()
room_policy=(RUNTIME/"UnderworldVanillaDungeonRoomPolicy.cs").read_text()
ecology=(RUNTIME/"UnderworldVanillaDungeonEcologyPolicy.cs").read_text()
rewards=(RUNTIME/"UnderworldVanillaDungeonRewardPolicy.cs").read_text()
mechanics=(RUNTIME/"UnderworldVanillaDungeonMechanicsPolicy.cs").read_text()
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
require("ScaleRoomHierarchy(clone, room, scale, sourceName)" in registrar,
        "DDE-03 room clone is not routed through connection-safe scaling")
require('new GameObject("Magenheim_DDE_ScaleRoot")' in registrar and
        "scaleRoot.localScale = Vector3.one * scale" in registrar,
        "DDE-03 room structural content is not uniformly scaled beneath a private scale root")
require("connection.transform.localPosition *= scale" in registrar,
        "DDE-03 RoomConnection raw positions are not expanded with visible geometry")
require("connection.transform.parent != clone.transform" in registrar,
        "DDE-03 nested RoomConnection topology is not rejected")
require("room.m_size = new Vector3Int(" in registrar,
        "DDE-03 Room.m_size is not scaled with visible geometry")
require("clone.transform.localScale *= scale" not in registrar,
        "DDE-03 incorrectly scales the Room root and would desynchronize PlaceRoom connection math")
for token,label in (
    ("DonorRoomSize","donor room-size provenance"),
    ("DonorConnectionLocalPositions","donor connection provenance"),
    ("LinearScale","donor scale provenance"),
):
    require(token in room_audit_metadata,"DDE-03 live audit metadata missing "+label)
require("auditMetadata.DonorRoomSize = room.m_size" in registrar and
        "auditMetadata.DonorConnectionLocalPositions = room.GetConnections()" in registrar,
        "DDE-03 donor provenance is not captured before scaling")
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

for token,label in (
    ("nameof(DungeonGenerator.Generate)","generation completion hook"),
    ("generated_rooms=","placed-room evidence"),
    ("graph_maximum_depth=","branch-depth evidence"),
    ("missing_required_rooms=","required-room evidence"),
    ("creature_spawners=","encounter-socket evidence"),
    ("containers=","container evidence"),
    ("mineables=","mineable evidence"),
):
    require(token in diagnostics,"DDE runtime diagnostics missing "+label)

# Gate 5 — required rooms / doors fail closed.
require("required room(s) did not resolve to private clones" in registrar,
        "DDE-05 missing-required-room failure is not fail-closed")
require("requires at least" in registrar and "required-room identities exist" in registrar,
        "DDE-05 minimum required-room satisfiability is not enforced")
require("doors-cloned=" in registrar,
        "DDE-05 cloned door diagnostics are absent")

# Gate 6 — creature ecology replacement and pacing.
for token,label in (
    ("UnderworldVanillaDungeonEcologyPolicy.Rebind","registrar ecology-policy bind"),
):
    require(token in registrar,"DDE-06 missing "+label)
for token,label in (
    ("GetComponentsInChildren<CreatureSpawner>","CreatureSpawner replacement"),
    ("m_creaturePrefab = prefab","CreatureSpawner Magenheim binding"),
    ("GetComponentsInChildren<SpawnArea>","SpawnArea replacement"),
    ("area.m_prefabs.Clear()","donor SpawnArea removal"),
    ("spawner.enabled = active","deterministic CreatureSpawner suppression"),
    ("area.enabled = active","deterministic SpawnArea suppression"),
    ("room.m_entrance) return false","safe entrance-room pacing"),
    ("UnderworldCreatureCombatBalance.RoleFor","combat-role authority"),
    ("Role.Apex => .35f","rare apex weighting"),
    ('string.Equals(entry.Donor, "Serpent"',"Blackwater Serpent exclusion"),
):
    require(token in ecology,"DDE-06 missing "+label)
require(".42d" in ecology and ".72d" in ecology,
        "DDE-06 active socket fractions are not bounded from outer to lair pressure")
require("RiskFor(" in room_policy and "m_minPlaceOrder" in room_policy and "m_endCap" in room_policy,
        "DDE-06 room-risk authority does not use donor depth/room hints")

# Gate 7 — resource / reward replacement with rare-yield normalization.
require("UnderworldVanillaDungeonRewardPolicy.Rebind" in registrar,
        "DDE-07 registrar is not bound to reward policy")
for token,label in (
    ("GetComponentsInChildren<Container>","container reward replacement"),
    ("m_defaultItems = ResourceTable","container biome-resource table"),
    ("GetComponentsInChildren<Pickable>","pickable reward replacement"),
    ("GetComponentsInChildren<MineRock>","MineRock reward replacement"),
    ("GetComponentsInChildren<MineRock5>","MineRock5 reward replacement"),
    ("GetComponentsInChildren<DropOnDestroyed>","destructible reward replacement"),
    ("1d / roomCountMultiplier","inverse bottleneck normalization"),
    ("baselineShare / profile.RoomCountMultiplier","direct-pickable bottleneck normalization"),
    ('"Magenheim_Underworld_Resource_BlackwaterPearl"',"Blackwater bottleneck"),
    ('"Magenheim_Underworld_Resource_Emberiron"',"Sulfur bottleneck"),
    ('"Magenheim_Underworld_Resource_Rimesilver"',"Frozen bottleneck"),
    ('"Magenheim_Underworld_Resource_CarrionAmber"',"Decay bottleneck"),
):
    require(token in rewards,"DDE-07 missing "+label)
require("fixture == Fixture.Destructible && rarity == Rarity.Bottleneck" in rewards,
        "DDE-07 generic destructibles may still spill bottleneck resources")

# Gate 8 — retain donor mechanics while stripping donor lore/progression.
require("UnderworldVanillaDungeonMechanicsPolicy.Apply" in registrar,
        "DDE-08 registrar is not bound to donor-mechanics policy")
for token,label in (
    ("GetComponentsInChildren<Vegvisir>","Vegvisir lore stripping"),
    ("GetComponentsInChildren<RuneStone>","RuneStone lore stripping"),
    ('Count(counts, "Door")',"door preservation inventory"),
    ('Count(counts, "Teleport")',"teleport preservation inventory"),
    ('Count(counts, "RandomSpawn")',"hidden/random mechanic preservation inventory"),
    ('Count(counts, "Destructible")',"destructible preservation inventory"),
    ("unknown donor mechanics are deliberately preserved","unknown donor mechanic preservation"),
):
    require(token in mechanics,"DDE-08 missing "+label)
require("DestroyImmediate" in mechanics,
        "DDE-08 known donor lore markers are not actually removed")
for forbidden in ("GetComponentsInChildren<Door>", "GetComponentsInChildren<Teleport>",
                  "GetComponentsInChildren<RandomSpawn>", "GetComponentsInChildren<Destructible>"):
    require(forbidden not in mechanics or "DestroyImmediate" not in mechanics.split(forbidden,1)[-1][:220],
            "DDE-08 structural donor mechanic appears to be destroyed: "+forbidden)
require("CreateClonedLocation(definition.PrefabName, donorEntrance)" in registrar,
        "DDE-08 donor entrance/mechanics are not inherited by clone")
require("GetComponentInChildren<DungeonGenerator>(true)" in registrar,
        "DDE-08 clone does not preserve donor DungeonGenerator mechanics")

# Gate 9 — exact Underworld confinement and all-family RuntimeReady admission.
require("zone.m_biome = UnderworldTerrainRuntime.ToNativeBiome(profile.Biome)" in registrar,
        "DDE-09 clone is not bound to exact owning Underworld biome")
for token,label in (
    ("zone.m_exteriorRadius = donorExteriorRadius","donor exterior-radius preservation"),
    ("zone.m_minTerrainDelta = donorMinTerrainDelta","donor minimum terrain-delta preservation"),
    ("zone.m_maxTerrainDelta = donorMaxTerrainDelta","donor maximum terrain-delta preservation"),
    ("zone.m_minAltitude = donorMinAltitude","donor minimum-altitude preservation"),
    ("zone.m_maxAltitude = donorMaxAltitude","donor maximum-altitude preservation"),
    ("zone.m_slopeRotation = donorSlopeRotation","donor slope-rotation preservation"),
    ("zone.m_snapToWater = donorSnapToWater","donor water-snap preservation"),
):
    require(token in registrar,"DDE-09 missing "+label)
require("UnderworldVanillaDungeonCandidatePolicy.IsWorldgenAdmitted(dungeon)" in bridge,
        "DDE-09 detached catalog does not use RuntimeReady admission predicate")
require("definition.Status == UnderworldDungeonStatus.RuntimeReady" in candidate,
        "DDE-09 runtime admission predicate is not RuntimeReady-only")
require("internal static bool Enabled => false" in candidate,
        "DDE-09 retired one-at-a-time candidate switch can still activate")
require("active.Length != UnderworldVanillaDungeonReuseCatalog.All.Count" in registrar,
        "DDE-09 registrar does not require all five ordinary families together")

# Gate 10 source prerequisite — preserve donor location/interior mechanics, no custom return system.
require("CreateClonedLocation(definition.PrefabName, donorEntrance)" in registrar,
        "DDE-10 derivative does not inherit donor entrance/interior transport")
for token,label in (
    ("ValidateInteriorTransport(profile, donorZone, custom.Prefab, generator)","transport parity admission"),
    ("cloneLocation.m_useCustomInteriorTransform != donorLocation.m_useCustomInteriorTransform","Location custom-interior parity"),
    ("cloneGenerator.m_useCustomInteriorTransform != donorGenerator.m_useCustomInteriorTransform","generator custom-interior parity"),
    ("cloneLocation.m_generator != cloneGenerator","Location-to-generator pairing"),
    ("CollectDungeonTeleports","paired Teleport graph audit"),
    ("cloneTeleports.Count != donorTeleports.Count","Teleport endpoint-count parity"),
    ("!cloneTeleports.Contains(teleport.m_targetPoint)","Teleport target containment"),
):
    require(token in registrar,"DDE-10 missing "+label)
for forbidden in ("UnderworldGateTransitRuntime","ReturnToSurface","DeepFractureReturn"):
    require(forbidden not in registrar,
            "DDE-10 ordinary donor registrar wrongly owns non-donor return mechanism: "+forbidden)

# Gates 11-12 source prerequisites — runtime persistence/multiplayer snapshots and
# generation cost evidence. These remain evidence tooling only; live cases still decide pass/fail.
for token,label in (
    ("CaptureRuntimeSnapshot","runtime persistence snapshot capture"),
    ("generation_fingerprint=","stable generated-layout fingerprint"),
    ("network_peer_count=","peer-count evidence"),
    ("valid_znetviews=","network-object validity evidence"),
    ("active_pickables=","harvest-state evidence"),
    ("generation_ms=","generation-time evidence"),
    ("managed_memory_delta_bytes=","managed-memory evidence"),
):
    require(token in diagnostics,"DDE-11/12 missing "+label)
require('case "dde":' in dev_commands and
        "CaptureRuntimeSnapshot(player" in dev_commands,
        "DDE-11/12 developer snapshot command is not wired")

for token,label in (
    ("UnderworldVanillaDungeonCandidateAudit.Capture","developer candidate-audit command"),
    ("Magenheim_DDE_ScaleRoot","live scale-root audit"),
    ("DonorConnectionLocalPositions","live donor-connection audit"),
    ("CreatureSpawner retains non-biome creature prefab","live ecology ownership audit"),
    ("retains non-biome reward prefab","live reward ownership audit"),
    ("Surface Vegvisir components remain","live Surface-lore audit"),
    ("Expanded donor location biome","live owning-biome audit"),
):
    target = dev_commands if token == "UnderworldVanillaDungeonCandidateAudit.Capture" else candidate_audit
    require(token in target,"DDE live candidate audit missing "+label)

# All ordinary catalog families are intentionally RuntimeReady in source; live acceptance evidence
# remains a separate claim boundary.
for name in ("FungalForest","BlackwaterDeep","SulfurousWastes","FrozenCaverns","GreatDecay"):
    require(f"public static UnderworldDungeonDefinition {name}" in catalog,
            "DDE catalog lost ordinary family "+name)

if fail:
    print("FAIL DDE Gates 2-12 source prerequisites")
    for item in fail:
        print(" - "+item)
    raise SystemExit(1)

print("PASS DDE Gates 2-12 source prerequisites")
print(" - this is static admission only; no live DDE gate is claimed passed")
