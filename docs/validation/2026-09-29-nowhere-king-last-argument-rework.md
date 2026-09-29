# Nowhere King Last Argument rework — source-authoring gate

Date: 2026-09-29

The Nowhere King fight is now defined as exactly three combat phases at 70% and 35% boundaries.
Phase One is the twin-sword duel, Phase Two is the gravity-control phase, and Phase Three is the
leap/ground-pound phase. The former 10% fourth-state behavior is no longer a separate combat phase.

The old single two-handed Last Argument specification has been replaced by a paired royal armament.
The collective name remains **The Last Argument** so existing lore is preserved. The individual
working names are **Firmament** (contained-galaxy blade) and **Null Gate** (void/event-horizon blade).

Source authority is `tools/author-nowhere-king-swords.py`. It derives both weapons from the proven
`crystal-weapon-sword.blend` grip/guard ancestry, removes the old blade, and authors distinct blade
construction instead of recolouring one mesh. Firmament receives a star-field/nebula surface,
embedded starlight and dual nebular ribbons. Null Gate receives a framed near-black aperture,
event-horizon edge rails, three containment rings and a helical violet filament. Both keep the
existing held-axis convention and ordinary sword envelope so the King's 1.82x body scale produces
the intended roughly 2.8m royal weapon length.

`tools/rebuild-nowhere-king-swords.ps1` is the single production command:

1. author both Blender sources;
2. verify identity geometry, UVs, non-collision contract, crystal-null contract and held envelope;
3. export the normal GLB/runtime model payloads through the existing model exporter.

This commit does **not** claim generated `.blend`, GLB, runtime JSON or texture payloads, because the
connected repository environment has no Blender executable and there is no pre-existing approved
Blender workflow in the repository. Runtime hand-binding and the Phase One combat rewrite should
consume the exported assets only after the above production command has succeeded and the resulting
files have passed the normal model gates.
