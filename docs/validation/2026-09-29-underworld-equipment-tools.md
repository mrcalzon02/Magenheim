# 2026-09-29 — Underworld equipment authority and tool authoring

The six biome stations now have a concrete equipment program rather than being empty progression
props. `UnderworldEquipmentCatalog` establishes thirty stable identities: six biome-opening tools
and six four-piece armour sets (helmet, chest, legs and cape).

The six tool identities are Sporelight Lantern, Diving Bell Hood, Slag Pick, Rime Chisel, Anchor
Spike and Defiant Censer. Their station dependencies follow the station ladder exactly. The armour
families are Sporeweave, Palewater, Emberiron, Rimeward, Stoneanchor and Defiant.

The Fungal Forest five-item equipment slice consumes the existing Mycelial Bench refinements.
The other five biomes now own fifteen additional processed materials—three per station—and every
post-Fungal tool and armour recipe consumes those processed outputs rather than skipping directly
from raw pickups to end-game equipment. Station construction remains raw-resource based so the
player can establish the workshop before using it to manufacture refined stock.

`tools/author-underworld-tools.py` creates six independent owned tool models. It does not open or
recolour vanilla tools. Each source has explicit ToolUV data and an 18-part minimum detail gate.
The Diving Bell Hood is no longer treated as a fake held Tool-slot item. Its catalog slot is Helmet,
and the existing owned shell is now authored on the same canonical 53-bone Valheim player rig as
the armour family. Head geometry is rigidly weighted to Head, the neck seal/harness to Neck, and
production admission requires a normalized attach_skin stream for the model before registration.

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


## Runtime tool mechanics

All six biome tools are now admitted rather than existing only as art/catalog authority. Sporelight
is a fuel-free carried/placed light whose output follows the resolved Fungal atmosphere density.
The Diving Bell Hood is a true skinned helmet and reduces Blackwater swimming stamina use by 35%.
Slag Pick gates Fracture Crystal seams. Rime Chisel adds one Clear Ice through the network-owner
Pickable RPC bonus path. Anchor Spike is a one-use private build tool whose deployed networked spike
can stabilize an Anchor Forge site without waiving level-ground requirements. Defiant Censer remains
under the synchronized Great-Decay weather/mitigation authority.

All twenty-four armour recipes and all six tool recipes are registered at their canonical stations.
Live male/female fit, full-animation clipping, multiplayer propagation, placed-piece refund behavior
and save/reload acceptance remain runtime acceptance gates; they are not inferred from source code.
