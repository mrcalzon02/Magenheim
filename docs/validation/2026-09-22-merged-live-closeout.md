# Combined Rootforged, weather and native minimap candidate — 0.0.94

Merged origin/main d01dc92 (61 incoming commits) into local main 4b489dc. Resolved four conflicted files: plugin initialization/disposal, assembly version, release metadata and project history. All local authored assets and foundation definitions are unchanged. Both histories remain intact; old rescue branches and stashes were not reapplied.

Preserved fourteen Rootforged pieces and integrated the incoming biome weather, Overworld/Underworld Minimap tabs, instance-layer fixes and supporting tests/docs. Plugin lifetime includes all four Rootforged, atmosphere, weather and map components.

The first runtime compile exposed incoming references to private members and obsolete sector APIs. Private map texture access and GenerateWorldMap now use the established reflection boundary. Layer queries now retain native FindObjects/FindDistantObjects, visited-sector and portal behavior, then filter only newly appended results. Native ReleaseNearbyZDOS retains simulation-distance and ownership handling; its peer-area answer is constrained by physical layer. Removed the incoming replacement sector loops and parallel player ownership cache. Public ZDO position/prefab and ZNet time accessors replace private field access. No game assemblies were modified or publicized.

Focused runtime compilation passed without warnings/errors. Verified 42 Harmony targets, 33 direct reflection bindings and 42 helper field contracts against installed assemblies. Twelve existing dynamic bindings remain outside the static gate's coverage.

Live map tabs, weather visuals, transition behavior, shared-map compatibility and multiplayer/save acceptance still require in-game testing; installation is not evidence of those outcomes.

Full closeout passed: 297 model sets, 131 icons, 43,423 Core assertions plus separate suites. Runtime: zero warnings/errors. Installed 0.0.94 into Central Fuckery with all payload hashes and enabled launcher metadata verified. DLL SHA-256: CF8422CC3B7FF8614172C0C0F159ECEE77CE020C972C54FCBCEAC7ADF9161F5D. Backups: backups/Local-Magenheim-20260922-060803.zip and backups/mods-20260922-060814-064.yml. Local log: dist/merge-0094-closeout.log.
