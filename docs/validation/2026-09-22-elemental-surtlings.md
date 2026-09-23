# Elemental Surtlings — 2026-09-22

## Reconciled authority

`main` at `d12222c` (0.0.98 pushed and read back). Design authority:
`docs/UNDERWORLD_ELEMENTAL_SURTLINGS_DESIGN.md` from `54592de`. Production scope follows the
2026-09-21 correction in `UNDERWORLD_CREATURE_ART_RIG_STANDARD.md`: donor skeletons, not new
animation systems.

## What was built

- **Twelve models** — two shared bodies (feminine, masculine) × six elemental kits (Fire, Water,
  Earth, Wind, Radiance, Umbral) — authored by `tools/author-underworld-surtlings.py`, registered in
  `assets/generated.manifest.json` as `underworld-surtling-models`. 3.9k–5.8k triangles, three
  material families each (body, regalia, localized glow), one baked 512px atlas each.
- **One generic binder**, `HumanoidSegmentBinder`. Every part is authored against a canonical
  humanoid skeleton that travels in its payload (`rig`), and at registration is retargeted onto the
  matching bone of a donor with a Humanoid avatar. Bones are resolved by humanoid role through the
  avatar's own bone map, not by guessed names. Limbs stretch to the donor's bone length; torso,
  head, hands and feet keep their authored proportion. A role the donor does not map rides its
  nearest mapped canonical ancestor (Draugr map 19 roles and have no Neck).
- **One data-driven registrar**, `UnderworldSurtlingRegistrar`, with the twelve entries in
  `UnderworldSurtlings`. The donor keeps its skeleton, animator, attacks, AI, faction, networking,
  saving and loot. Donor armour visuals are removed from the clone; weapons are kept. Donor death
  ragdolls are cloned and re-bodied so a corpse shows the Surtling.
- Umbral is a Surtling phenotype only; no ElementalAlignment was added. Radiance uses the existing
  name.

## Supporting changes

- `tools/magenheim_blender_kit.py` — geometry and bake machinery extracted from the weapon author
  so both tools share one implementation. Proven behaviour-preserving: regenerating the weapons
  through it reproduced every runtime payload, GLB and baked texture byte-for-byte.
- `verify-generated-freshness.py` gained optional `inputs`, so editing a shared module marks every
  generator built on it stale.
- `export-model-assets.py` passes a `runtime_rig` scene property into the payload; models without
  one are unchanged.
- `verify-model-assets.py` coverage pin raised 300 -> 312 for the twelve new models.

## Donor survey (live, 0.0.99)

Every candidate except `BogWitchKvastur` has a humanoid avatar. Chosen donors:

| element | donor | head-joint height | notes |
|---|---|---|---|
| Fire | Charred_Melee (scale 0.70/0.74) | 1.63 / 1.72m | greatsword attacks; hip cloth stripped |
| Water | Draugr | 1.71 / 1.80m | axe/bow; ragdoll re-bodied |
| Earth | Draugr_Elite (1.10/1.18) | 2.05 / 2.20m | sword; ragdoll re-bodied |
| Wind | Skeleton | 1.63 / 1.70m | sword/bow |
| Radiance | Draugr | 1.78 / 1.85m | axe/bow; ragdoll re-bodied |
| Umbral | Skeleton | 1.70 / 1.76m | sword/bow |

The first live run registered 6/12: Draugr's missing Neck failed Water, Earth and Radiance closed,
with the reason named. The ancestor fallback fixed it; the second run registered **12/12** with no
exception or warning from Magenheim.

## Verification boundary

Static gates and `closeout.ps1 -Offline` pass; 0.0.99 installed (DLL `6FA8485D...F27F0F`). The game
was launched with the profile and registration read from `LogOutput.log`: 12/12, all segments
bound, limb stretch 1.11–1.14.

**Not observed:** a Surtling spawned in a world. Unverified in order of risk: that segments follow
the donor's animation without visible gaps or inverted parts at extreme poses; attack, stagger and
death playback; the donor weapon read in the Surtling's hand.

## Known limitations

- Skeleton and Charred donors have no ragdoll: they die through a shatter effect, so Wind, Umbral
  and Fire deaths still show donor debris. Draugr-based deaths show the Surtling.
- Factions and temperament are the donors' (Demon, Undead). The design does not yet set them.
- No LOD1/LOD2, no idle overlays, no VFX (the design sequences those after silhouette proof).
- Console spawn only: `spawn Magenheim_Underworld_Surtling_<Element>_<Feminine|Masculine>`.
  No natural spawning exists for any Underworld creature yet.
