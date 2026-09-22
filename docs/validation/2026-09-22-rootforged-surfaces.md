# Rootforged floors, stairs and great-hall span — 0.0.95

Adds a 4 x 4 x 1m Worldroot floor (8 core wood, workbench), a 2m wide / 4m run / 2m rise Understone stair flight (12 stone, stonecutter), and an 8m iron-banded Worldroot beam (8 core wood + 4 iron, stonecutter). Existing fourteen definitions and all other foundation content remain unchanged.

The floor has eight split-root boards and three grown joists. Stone stairs have eight quarter-meter rises and root inlays; they ascend toward local +Z. The long beam has three physical iron collars. Editable Blender sources, GLBs, runtime meshes and matching Hammer icons are included. Floor snaps reuse the top/bottom building grid; stairs expose bottom, midpoint and top anchors. Native HardWood/Stone/Iron support, collision, wear, network behavior and refunds are reused.

Asset regression checks require a level floor surface and eight collidable steps increasing in height toward +Z. Core tests verify the floor/stair grid and reinforced span catalog. The stairs icon is rendered from the approach side so its treads are visible; the shared renderer keeps its default orientation for all existing pieces.

In-game traversal, snapping, support/collapse and multiplayer acceptance remain untested.

Full closeout passed: 300 models, 134 icons, 43,426 Core assertions plus separate suites. Runtime compiled with zero warnings/errors. Installed 0.0.95 with all hashes and launcher metadata verified. DLL SHA-256: 7767F8D832B3DF984A0A83B8F047EDDE1E04734C7B6058E1EB6DDA6687E106AF. Installation backup: backups/Local-Magenheim-20260922-145139.zip; catalog backup: backups/mods-20260922-145152-136.yml. Full local log: dist/rootforged-surfaces-closeout.log.
