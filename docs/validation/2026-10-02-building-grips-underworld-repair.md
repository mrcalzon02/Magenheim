# October 2 screenshot repairs — 0.0.168

The user reported Last Argument and Amber Blade hand offsets, checkerboard/uneven
Rootforged foundation textures, crystal support failures, invisible Worldroot pieces,
an inaccessible Tidal Basin, and detached geometric station details. A subsequent
screenshot showed Underworld entry refused because its instance was inactive.

## Underworld admission

The current boot log explicitly refused admission because a child chunk index was
missing while native chunk payloads existed. Inspection of the installed native
assembly confirms that ChunkSaveMapping owns the index codec; ZDOMan.LoadChunks
loads it before reading payloads. The cloud save has since autosaved a matching
child `_main.3.chunks` and `20_20__1_1.chunk` inside `worlds/yester/`.

Missing child indexes now recover through native ChunkSaveMapping.Save, using
strictly parsed native filenames and native SaveFileHelper/ZPackage headers.
Unrecognized filenames, duplicate chunk identities, wrong codecs or invalid counts
remain explicit failures. Original chunk payloads are retained. This is a thin
adapter to Valheim's existing codec, not a replacement persistence format.
Failed loads retain the pending load and suppress child autosave; Surface still
saves normally. No world-save files were directly edited during development.

## Equipment and construction

The actual grip centre is seated after rotation and scaling for Firmament, Null
Gate and Amber Blade. The paired left blade uses that same operation despite
bypassing the normal held-item loader. Six exported-payload checks cover full and
0.43 player scale. Blade size and orientation are unchanged.

The follow-up audit found that all twelve Underworld weapon upgrades clone an
already aligned crystal chassis but remeasure the union of its owned visual and
hidden native meshes. They now inherit the ancestor's rotation and seating,
without applying its authored trim twice. Exported-model regression checks use
an oversized hidden donor to exercise this failure path for every upgrade.
Only the three named sword identities receive the new grip-centre correction.

Crystal beams/foundations use native iron support, matching their iron recipes;
their support COM follows the actual size rather than the short donor. Rootforged
non-stone pieces now use the visible crystal family's plain pole donor, removing
the corewood/iron donor variants; native support tier still follows their recipes.
Donor fade components are removed and Rootforged support COM follows authored height.
Appearance and support propagation require live acceptance.

Tidal Basin collision fits the 2.7m by 0.96m basin body rather than its full 3.6m by
2.4m design envelope. Native use distance is 2.5m, and the collision uses the piece
layer. Other station placement rules remain intact.

## Authored surface and station pass

Rootforged block faces use metres per texture tile instead of stretching a whole
texture independently over every face. Tube circumference/length use the same
1.5m tile scale. Packed workshop surfaces use irregular multiscale variation and
wandering grain instead of the diagonal interference pattern.

Six Underworld stations were regenerated. Mycelial, Tidal, Silence and Crown
drawer arrays now have joined carcasses, rails, recessed thin fronts, visible
reveals and pegged handles. This replaces detached cubes with handles. Station
UVs use the same 1.5m tile scale. Neutral/context renders were inspected; these
are offline authoring evidence, not native shader or in-game acceptance.

Station revision 5 works the existing primary surfaces: shallow board bowing,
unequal hewn edges and chipped upper edges, with explicitly triangulated faces.
Station-only albedo, normal and roughness variation replace immaculate uniform
surfaces; no additional decorative parts were added in this revision.

The user's follow-up rejected props that do not communicate the crafting role.
Station revision 6 removes Mycelial task-pod spheres and spore nodes, replacing
them with a clamped Worldroot billet/drawknife, supported cord spindle and
slatted rack carrying sliced glowcaps. Tidal removes the fountain-like circular
arrangement and gauges. Table boards surround an open rectangular inset:
the closed flowstone bowl has sloping inner walls and a floor 0.545m below its
rim, with brine 0.445m below the rim. A drain/valve, pearl basket, salt tray and
fibre drying bar explain existing refinement ingredients. These props remain
visuals of existing recipe processes; they introduce no new simulation.

The expanded full build passed all twelve inherited weapon poses, six named
grip checks and ten crystal orientation checks, plus 531 imported models,
native patch/reflection checks and the Core suite. Revision 6 art is validated
separately after that build and included in the final repackaging.

Revision 7 extends the recipe-led review to the other four stations. Furnace
shows an open masonry chamber/grate, tuyere-fed bellows, one chimney, accessible
anvil and open quench trough. Silence shows a supported lapping wheel and a lens
workpiece, one mounted inspection lens, and a screw press holding laminate.
Anchor uses guide columns and a ram/die around a Titanbone workpiece, a ratchet
lift and contained prism stock. Crown uses an amber seal stamp/die, lamination
clamp, bone mortar/pestle and a mounted sealed reliquary. Decorative gauges,
floating orb arrays and unused hoops were removed. Original recipes and station
admission contracts remain unchanged.

Compilation passed with zero warnings/errors. Core passed 114,231 assertions plus
module suites. Full asset, model and installed-runtime checks and installation
are recorded in the closeout below once finished.

## Final delivery

The expanded full build passed runtime compilation, Core suites, native patch
and reflection admission, 531-model importer checks, twelve inherited weapon
poses, ten crystal orientations and six named grip seating checks. The subsequent
station-only revisions passed source/export/icon validation and fresh render
inspection; all 974 generated files across 28 generators matched their inputs.
Final local compilation/repackaging passed with zero warnings/errors.

0.0.168 was installed into Central Fuckery after the later installation permission
was granted. Installer backed up the previous plugin and launcher metadata,
verified all replacement package files by SHA-256, and verified the enabled
launcher version and exact release description. Installed Magenheim.dll SHA-256:
EFBBF10429F7D8E48F15E6AA88718B6CDE9725AAFD0FDDA70ECB888E9A6D449C.
No Valheim/r2modman process was terminated. Gameplay acceptance remains outstanding.
