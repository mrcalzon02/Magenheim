# Underworld initial map loading and native portal presentation — 0.0.170

The fresh gelheim session successfully admitted the child instance and entered it.
Its Underworld Minimap was generated on first entry, taking 22,887 ms and blocking
play. Local player destruction was logged only during application shutdown, not
immediately after transit. The reported black view remains a live visual failure.

The native Minimap.LoadMapData completion now prepares both layer maps during
initial loading. Admission also prepares the child map if map loading completed
before the child context existed. Valheim's own GenerateWorldMap still generates
the pixels; its map payload, exploration and pins remain authoritative. A pending
asynchronous generator defers native initial respawn until its existing completion
poll finishes or times out. The maps remain separate payloads on one Minimap.

Every Deep Gate transport now calls Player.TeleportTo with distantTeleport=true.
Inspection of the installed Valheim assembly confirmed ShowTeleportAnimation
returns m_teleporting && m_distantTeleport, and Hud.UpdateBlackScreen selects the
native teleport presentation from that method, fading away once native transit
finishes. Gate travel hides the independent Magenheim loading overlay rather than
showing it for a fixed three frames. Native destination readiness and floor checks
remain untouched. No player recovery controller or forced fade reset was added.

The shared GameCamera remains in the Surface scene. Its callbacks previously
queried Surface terrain/physics while following an Underworld player. Its existing
callback scope now selects the local player's actual scene. Regression exercises
Underworld camera queries, return to Surface, and restoration of native globals.
This is an identified routing defect, not proof that it explains every black view.

Native portal completion now logs player scene/position, dead/sleeping state and
camera presence/position/enabled state, without changing the native player state.

Offline validation: runtime compile, world-scope regression, core checks and
native patch target verification. Live acceptance remains pending: fresh load must
finish both map generations before gameplay; entry and return must display the
portal screen and restore usable world visibility; map tabs and exploration must
survive saving/reloading. Initial loading is longer; this change moves the work
out of gate travel rather than claiming parallel or cheaper world generation.

Closeout completed: runtime build had zero warnings/errors, the core executable
reported 114,231 assertions plus module suites, camera/world-scope regression
passed, and 23 dynamic resolver families resolved 676 installed native targets.
The new Game.UpdateRespawn(float) and Player.UpdateTeleport(float) bindings and
the native distantTeleport animation selector were verified against game metadata.
Installed 0.0.170 into Central Fuckery; package files and enabled launcher
version/description were verified. Prior installation/catalog were backed up.
Installed Magenheim.dll SHA-256:
EF42A798B81AEC3E1A7FDA40EF3462E54AA6A8646651B7786DD774053BB6A507.
