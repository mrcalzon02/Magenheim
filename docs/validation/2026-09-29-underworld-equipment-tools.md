# 2026-09-29 — Underworld equipment authority and tool authoring

The six biome stations now have a concrete equipment program rather than being empty progression
props. `UnderworldEquipmentCatalog` establishes thirty stable identities: six biome-opening tools
and six four-piece armour sets (helmet, chest, legs and cape).

The six tool identities are Sporelight Lantern, Diving Bell Hood, Slag Pick, Rime Chisel, Anchor
Spike and Defiant Censer. Their station dependencies follow the station ladder exactly. The armour
families are Sporeweave, Palewater, Emberiron, Rimeward, Stoneanchor and Defiant.

The Fungal Forest five-item equipment slice deliberately consumes the existing Mycelial Bench
refinements. Later tiers currently consume their canonical raw biome resources until their
corresponding refinement catalogs exist; their station identity and material ancestry are already
stable, but recipe admission remains future work.

`tools/author-underworld-tools.py` creates six independent owned tool models. It does not open or
recolour vanilla tools. Each source has explicit ToolUV data and an 18-part minimum detail gate.
The Diving Bell Hood source is currently the owned inventory/display shell only; its worn form must
use the same attach_skin discipline as armour rather than pretending a static mesh is a wearable.

`tools/rebuild-underworld-tools.ps1` is the local production path: author -> export -> model gate ->
icon render -> icon gate.

Armour now has a runtime skin path rather than stopping at Blender/GLB. The exporter preserves the
common bind space and serializes normalized canonical player-bone weights for each exported vertex.
ModelAssets rebuilds those streams as SkinnedMeshRenderers under a cloned vanilla donor's native
attach_skin hierarchy, and plugin bootstrap enables Jotunn's BoneReorder equipment hook. The slot
donors are HelmetCarapace, ArmorCarapaceChest, ArmorCarapaceLegs and CapeFeather; their equipped
renderers are disabled while their native item/equipment/network semantics are retained.

UnderworldArmourRegistrar registers all twenty-four armour identities, owned icons and recipes at
their canonical Underworld stations. Production admission now requires the 53-bone contract and a
normalized weight row matching every exported armour vertex.

Male/female live fit, clipping through the full animation set, actual Valheim shader appearance,
multiplayer equip propagation and save/reload acceptance remain runtime gates; source/serialization
support is no longer the blocker.
