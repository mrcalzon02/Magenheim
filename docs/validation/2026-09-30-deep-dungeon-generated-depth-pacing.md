# Deep Dungeon Expansion — generated-depth pacing correction

**Date:** 2026-09-30  
**Authority:** `docs/DEEP_DUNGEON_EXPANSION.md`

## Root cause corrected

The original ordinary-dungeon ecology/reward policy assigned Outer/Mid/Deep/Lair at cloned-room
prefab time using donor metadata plus deterministic variation. That was deterministic, but it was not
the same thing as physical traversal depth after the dungeon generator placed the graph. A room
prefab carrying high pressure could therefore appear near the entrance while a nominally low-risk
prefab appeared at the deepest dead end.

The encounter policy also forced one active CreatureSpawner whenever an admitted donor combat room
rolled all of its individual sockets inactive. Because the expanded dungeons target roughly 3.5x
the donor room count, that fallback could make expedition length scale mandatory fight count too
aggressively.

## Implemented correction

After `DungeonGenerator.Generate` completes for an expanded ordinary dungeon:

1. placed rooms are connected from their actual matching `RoomConnection` endpoints;
2. shortest-path room depth is calculated from the generated entrance room;
3. each placed room receives a generated Outer/Mid/Deep/Lair band;
4. generated depth and band are recorded on the cloned-room metadata;
5. ecology is rebound on the placed room instance using the generated band;
6. reward tables are rebound using the generated band and final ecology pressure;
7. the dynamic biome layer is rebuilt from generated depth:
   - local biome light;
   - Fungal/Blackwater/Sulfur atmosphere;
   - Blackwater native water-pool eligibility/depth;
   - Sulfur thermal-pocket eligibility/intensity;
   - Frozen Rime exposure;
   - Great Decay contamination.

Static material treatment, structural overlays and authored room props remain stable prefab art and
are not rebuilt during generation.

## Encounter normalization

Combat occupancy now has a room-level deterministic admission roll normalized against the configured
room-count multiplier. Individual CreatureSpawner/SpawnArea rolls only run inside an admitted
encounter room. If an admitted room contains CreatureSpawners and every socket misses, one spawner
is retained so an intended encounter does not disappear. A room intentionally suppressed by the
room-level pacing roll stays quiet.

This preserves meaningful additional combat in a much larger dungeon while deliberately creating
quiet traversal/resource rooms instead of multiplying mandatory encounters by the full room-count
factor.

## Transport integrity strengthened

Registration now compares the donor and clone teleport topology, not only endpoint count. Endpoint
keys are built from exterior/interior hierarchy paths and component indices; each clone endpoint must
target the same corresponding endpoint topology as its donor. Clone targets must also remain inside
the cloned location/interior graph.

This is source protection for DDE-10. Exact return behavior still requires installed-game evidence.

## Evidence expansion

Automatic generation evidence and runtime snapshots now record:

- generated Outer room count;
- generated Mid room count;
- generated Deep room count;
- generated Lair room count;
- unclassified room count;
- donor combat-socket room count;
- active encounter-room count;
- deliberately quiet combat-room count;
- existing per-socket encounter, reward, network, timing, memory and generation fingerprint data.

Installed acceptance should show zero unclassified generated rooms, entrance rooms in Outer pressure,
deeper pressure trending through the graph, and deliberate quiet combat rooms across the 3.5x
expedition.

## Additional defect repaired during this pass

The Frozen/Great Decay dedicated-hazard guard had been attached to local-light creation rather than
generic-atmosphere creation. That incorrectly removed their local dungeon lights while allowing a
generic atmosphere volume to coexist with the canonical Rime/Carrion hazard volume.

The guard now sits on generic atmosphere creation. Frozen/Great Decay retain local lighting and use
only their canonical exposure/contamination authorities.

## Source integrity readback

Immediately before this checkpoint, live `main` readback confirmed:

- no C# tuple syntax / `System.ValueTuple` forms in the changed runtime files;
- no newly introduced `Math.Clamp`, `IReadOnlySet<T>`, or
  `string.Contains(..., StringComparison)` net462 compatibility traps;
- donor room index, generated depth and generated risk band metadata are present;
- generated-depth pacing is invoked from the normal dungeon-generation postfix;
- dynamic dressing rebinding is invoked by generated-depth pacing;
- donor teleport topology preservation is enforced during registration;
- Frozen/Great Decay generic atmosphere duplication is excluded.

## Remaining boundary

This remains source implementation, not installed-game acceptance. Blender-generated creature
payload execution, local build/Valheim compile, fresh-world generation, navigation, save/reload,
exact return behavior, economy measurements, frame-time measurements and multiplayer evidence remain
runtime gates.
