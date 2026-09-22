# Rootforged placeables — 0.0.91

The user explicitly requested root-based placeables after the natural-scenery scope correction,
and then requested further detail plus full live-profile closeout.

## Content

Eleven pieces from the existing UnderworldArchitecture catalog, without a duplicate recipe catalog:
2/4/8m beams and pillars; 4m iron-banded beam; 4/8m arch ribs; 2x2m foundation and great column plinth.
Each has an editable Blender source, GLB, runtime mesh payload and 256px rendered icon. The wood
forms combine three braided roots and two raised surface roots. Reinforced members carry three
modeled forged collars. Bases have stepped stone seating and root inlays. Packed 256px authored
texture maps use longitudinal root UVs and a neutral exported tint to avoid multiplying albedo twice.

Runtime imports models through ModelAssets, uses Jotunn CustomPiece in Hammer > Rootforged and
preserves native Piece/WearNTear/ZNetView behavior. Static collision is copied into a separate
always-active child, retaining the donor collider layer; wear-state visuals carry no collision.
Native HardWood, Stone and Iron support rules apply. Recipes preserve catalog amounts and stations
but explicitly bind unimplemented Worldroot Timber harvesting to RoundLog and Understone to Stone.
No new resource items, harvesting, save, map or support system is claimed.

Installed Valheim IL was inspected: Piece.GetSnapPoints checks direct children tagged `snappoint`.
New anchors follow that contract; inherited anchors are untagged. This is source/API verification,
not proof that placement feels correct in play.

## Checks

Runtime build passed with zero warnings/errors before full closeout. Added a permanent Rootforged
asset gate to build.ps1: all eleven identities, catalog dimensions/ground contact, structural
collision, open arch passages, three physical collars on reinforced pieces, neutral texture tint
and icon files. Generic geometry gate passed winding on all eleven assets. Generated freshness
records cover 33 source/export assets and eleven icons. Source-art contact sheet was viewed.

Full closeout results and installed hashes are recorded in PROJECT_STATE.md after installation.
In-game registration, snap placement, load-bearing support, weathering, demolition/refunds and
multiplayer remain untested. Offline art and static/runtime compilation checks are not live acceptance.

Closeout completed successfully: 0.0.91 installed in Central Fuckery, all payload hashes and enabled launcher entry verified. DLL hash `A43BC67EBF589A4548C70454AC065677330E958089ECC919EB1852AAD52B819F`; catalog backup `backups/mods-20260921-151752-051.yml`. No game was launched.
