# 0.0.164 — latest world-run and Surface crypt repairs

Source evidence: Central Fuckery's latest `BepInEx/LogOutput.log`, running
Magenheim 0.0.162 in Rawheim. Preserved locally as
`dist/world-run-162-errors.log`; runtime logs are not committed source.

## Gate failure and cascading exceptions

The log records successful Deep Gate entry, immediately followed by
`Underworld physical session boundary changed 842248273 -> 7364048498236462650`.
The lifecycle then creates a child of the child world, fails admission because
the supposed Surface scene is unloading, and destroys the local player.
19,056 Unity null-reference errors follow, including Tail, camera, Character,
ZNetScene, ZDOMan, WearNTear and BaseAI callbacks.

The generic Valheim callback patch overlapped specialized ZNetScene/ZoneSystem/
SpawnSystem/Character scope patches. Harmony finalizers do not guarantee reverse
prefix order between patch classes; Surface and local-player Underworld scopes
could restore the child context last. The generic resolver now excludes methods
owned by these specialized scope patches. Session identity reads the explicitly
bound Surface World rather than the temporarily rebound ZNet singleton.

`tools/WorldScopeRegression` links production execution/target-selection code to
small engine-seam stubs. It checks disjoint scope ownership, nested callbacks,
and restoration of World, ZoneSystem, ZDOMan, ZNet's manager and active scene.
It passes the repaired code and rejects the prior committed source with
`Generic/specialized world scopes overlap`. This is an offline regression test,
not a native Unity or multiplayer acceptance test.

## Registration and presentation

- All 42 ordinary authored creatures failed registration with a null reference.
  RigidCreaturePresentationDriver.Bind called Character.GetHealth on inactive
  prototypes. Installed native IL confirms GetHealth dereferences m_nview, which
  Character.Awake has not initialized on those prototypes. Bind now reads m_health;
  live Update retains ordinary native health reads. Registration failures now log
  full stack traces instead of losing the failing call site.
- Burial Chambers retained required-room alternatives 03–05 that have no enabled
  native candidates. Remapping now omits unavailable native alternatives while
  preserving available private clones and the required-room minimum. An enabled
  candidate missing its private clone still fails admission.
- The Last Argument logged 14,560 missing left-hand/forearm exceptions. The live
  attachment now uses Animator.GetBoneTransform with HumanBodyBones.LeftHand and
  LeftLowerArm after animator initialization. No authored weapon mesh or trim changed.
- Companions.BuildUI threw FieldAccessException on InventoryGui.m_containerGrid.
  The prior local2 injection imported a private native field directly. Local3
  reads that field through an injected reflection helper, retaining the player,
  drop-delegate, TMP and eight joystick repairs. Cecil audit checks the helper and
  rejects a remaining direct field read; the untouched vendor is a failing control.

## Acceptance boundary

Build, asset checks, scope regression and Companion IL audit are offline evidence.
Native gate entry/return, stable child-world lifetime, creature spawn/animation,
dungeon generation, Last Argument attachment and Companion inventory interaction
still require another world run. Unidentified missing-prefab hashes and foreign
mod warnings in the original log have not been claimed repaired.

## Surface crypt leakage, enclosure and mushrooms

The same 0.0.162 run placed six `Magenheim_Underworld_Dungeon_FungalForest`
locations on the Surface before gate entry, in zones (7,6), (-5,-9), (-1,-11),
(-1,-18), (3,-29) and (7,-36). Removing Underworld catalog rows alone does not
repair saved native location instances or their LocationProxy ZDO identities.
0.0.164 restores only these owned derivative identities to their catalogued
native entrance donors, both at placement and proxy reconstruction. The saved
location index retains positions and placed-state. Native `LocationInstance`
is a struct: the corrected value is explicitly written back after enumeration.
The installed-native regression gate verifies persistence of that replacement,
unchanged foreign entries, retained position/placed-state and idempotence.

Installed `Location.Awake` creates an `InteriorEnvironmentZoneForce` child at the
native dungeon centre with scale (64,500,64). Asset inspection confirms its
`interior_cube` renderer and EnvZone. Expanded crypt packing is (179.6,179.6,179.6),
so its native darkness enclosure can intersect expanded rooms. Owned derivative
locations now size that native enclosure to the packing volume plus tile margins,
preserving native position/material and existing vertical clearance. Palette
conversion excludes `interior_cube` and `exirt_portal` materials. Recovered Surface
proxies retain a single namespaced original-identity field so their retained
expanded room layout receives adequate clearance after the exterior is restored.
This does not reset room layout, loot, creatures or any saved native object.

The prior decorative visual factory deliberately stripped Pickable/TreeBase/
ZNetView and all collision. Fungal amber-bed, lantern-cap and nursery-cap clusters
now clone the native Glowcap Flesh pickup; larger fungal forms clone native
Glowcap/Spirestalk felling prefabs. Their registered names, native colliders,
picked-state and resource drops are preserved. Other biome decoration is unchanged.
These prefab additions are constructed under inactive authoring parents.

Offline verification: runtime compilation has zero warnings/errors; Core tests
pass 114,198 assertions plus module suites; world-scope regression passes; the
native dynamic gate resolves 23 families/676 targets and exercises saved-site
recovery using actual installed Valheim types. Visual crypt clearance, mushroom
interaction, native gate travel and multiplayer still require a new game run.
