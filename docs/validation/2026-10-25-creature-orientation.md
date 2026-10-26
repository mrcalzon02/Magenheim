# Creature orientation correction — 0.0.178

The user correctly identified rear views labeled as fronts in the October 25
entity screening. The first comparison renderer assumed every stored model
faced Unity +Z. That assumption was wrong for the 42 older ordinary creatures
and for White Silence/Furnace Heart, which currently reuse ordinary apex models.
Those screening images were replaced, and no art acceptance was assigned from them.

`export-model-assets.py` maps Blender `[x,y,z]` to Unity `[x,z,-y]`.
The older creature sources place their anatomical front toward Blender +Y,
so their stored runtime front is **-Z**. Actual Glacier Stalker jaw coordinates
are z=-0.83 m while its distal tail is z=+1.155 m. Cinder Hound has the same sign
relationship. `export-refined-boss-runtime.py` uses `[-x,z,y]`, so First Bloom,
Rift Titan and Carrion Crown refined weighted models already face **+Z**.

The comparison renderer now inverts the relevant export basis by versioned rig
kind before choosing an anatomical camera. It captures explicit **front, side,
three-quarter and rear** views. Every render record includes the source hash,
recipe hash, stored forward sign, exporter and full review basis. A render is
invalidated if either the source or recipe changes. The 47 unpaused entities
are rendered; Blackwater Maw's paused art is untouched.

The runtime loader previously parented rigid visuals at identity. Version
0.0.178 rotates the complete rigid visual root and its skeleton 180 degrees
around Unity Y after building the original rest binding. This puts the legacy
anatomical front toward the native character's +Z movement/attack direction.
It preserves model vertices, handedness, scale, height, bone-local frames,
clips and native gameplay authority. Weighted boss presentation retains its
existing +Z orientation. No historical source/export was regenerated for this fix.

`CreatureVisualOrientation` is a direct coordinate adapter used by the existing
binder. ModelAssetTests checks actual exported Glacier Stalker/Cinder Hound
jaw and tail landmarks, their corrected forward signs, unchanged height/scale,
coverage of all 42 legacy rigid assets, the weighted convention and rejection of
unknown rig conventions. Compilation and offline tests are separate from a live
Valheim walking/turning/attack-direction check.

New Carrion Crown r4/r5 and Rift Titan r19 sources are separate intermediate art
studies. They are not substituted into this runtime release. First Bloom remains
the sole explicitly accepted boss (1/6); Blackwater Maw remains paused.

Installed using `closeout.ps1 -Offline` on October 25. Package integrity, all
installed file hashes and the enabled Central Fuckery launcher entry were verified.
The installed `Magenheim.dll` SHA-256 is
`A7F53E693797A591174EFE04571047C9D252F820DE18738D40778E7E4D243007`.
The launcher catalog says **Magenheim v0.0.178 by Local**, with description:

> 0.0.178: Corrects the forward orientation of legacy custom creature visuals so heads face native movement and attack direction. Refined weighted bosses retain their existing orientation. New Rift Titan and Carrion Crown art studies remain separate review candidates.

Previous plugin backup: `backups/Local-Magenheim-20261025-183131.zip`.
Launcher catalog backup: `backups/mods-20261025-183155-551.yml`.
Full closeout log: `dist/20261025-orientation-closeout.log`.
The launcher was verified from catalog data; it was not visually opened, and
Valheim startup/walking/turning/attack-direction checks were not performed.
