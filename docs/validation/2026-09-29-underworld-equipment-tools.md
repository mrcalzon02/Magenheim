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

Armour is intentionally not runtime-admitted in this slice. Valheim wearable chest/leg/cape assets
must be correctly skinned to the character hierarchy. The next armour slice must establish one
verified host attach_skin armature, male/female fit checks and bone-reorder acceptance before the
twenty-four owned wearable meshes are bound. A recoloured vanilla armour donor is not accepted as
completion.

Local Blender export, runtime compile and in-game acceptance remain unclaimed from this remote
source-authoring session.
