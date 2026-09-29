# Underworld creature combat balance — 2026-09-29

Status: **source implemented and remote-read-back verified on main; live combat acceptance still required.**

## Defect found

The 42 ordinary Underworld fauna had explicit biome skins, resistances, VFX, spawn ecology and AI
temperament, but still inherited donor health wholesale. Their outgoing donor damage also had no
single Magenheim progression envelope. That meant a reused donor could accidentally determine
difficulty more strongly than the creature's intended biome or role.

The twelve elemental Surtlings had the same balance gap: authored bodies and spawn homes, but donor
health/damage pacing.

Deep Fracture creatures are not part of this defect. Their registrar already assigns explicit health
and movement per creature, ranging from light fast forms to slow 780/1200-HP wardens/colossi.
Nowhere King likewise owns a separate boss profile and is not routed through the ordinary-fauna
policy.

## Governing design

Difficulty is not allowed to scale every axis upward at once.

- Fast enemies trade durability and per-hit punishment for mobility.
- Heavy enemies gain health and damage but lose movement speed.
- Heavy/Apex enemies also gain a longer native MonsterAI minimum attack interval, creating recovery
  windows rather than fake player-style stamina.
- Biome progression increases health and damage envelope, not role mobility.
- Extreme donor attacks are capped by the Magenheim role/biome envelope so donor choice cannot
  bypass balance.
- Aggression/perception remains separate from mobility. A creature may notice and pursue the player
  aggressively without also receiving a speed bonus.

## Biome progression

| Biome | Base HP | Damage factor | Base hit cap |
|---|---:|---:|---:|
| Fungal Forest | 180 | 0.90x | 36 |
| Blackwater Deep | 220 | 1.00x | 44 |
| Sulfurous Wastes | 270 | 1.10x | 54 |
| Frozen Caverns | 330 | 1.20x | 66 |
| Fracture Zones | 410 | 1.32x | 80 |
| Great Decay | 500 | 1.45x | 96 |

## Role tradeoff

| Role | HP | Role damage | Hit-cap scale | Movement | Min attack interval |
|---|---:|---:|---:|---:|---:|
| Swarm | 0.55x | 0.45x | 0.55x | 1.15x | 0.75 s |
| Skirmisher | 0.80x | 0.65x | 0.80x | 1.08x | 1.00 s |
| Hunter | 1.10x | 0.85x | 1.00x | 1.00x | 1.40 s |
| Bruiser | 1.55x | 1.05x | 1.25x | 0.88x | 2.00 s |
| Heavy | 2.15x | 1.25x | 1.55x | 0.72x | 2.80 s |
| Apex | 3.00x | 1.45x | 1.90x | 0.60x | 3.60 s |

Final ordinary-fauna HP is biome base HP × role HP scale. Final damage multiplier is biome factor ×
role damage factor. The per-hit cap is biome base cap × role cap scale.

## Explicit classification

All 42 ordinary fauna are classified fail-closed. A new roster name without a role throws during
registration rather than silently inheriting arbitrary donor balance.

Swarm: Lantern Moth, Sporeling, Cave Ray, Ashmite, Rime Moth, Frost Tick, Fracture Wisp, Rift
Skitter, Rotling.

Skirmisher: Capcrawler, Gloomfin, Shoreclaw, Vent Spitter, Pale Burrower, Shardwing, Gravity Leech,
Carrion Bloom, Marrow Creeper.

Hunter: Mycelial Stalker, Shelf Lurker, Lantern Angler, Cinder Hound, Fume Wraith, Magma Leaper,
Iceblind, Rimewing, Glacier Stalker, Chasm Stalker, Spore Husk, Decay Hound.

Bruiser: Puffback, Blackwater Lamprey, Basalt Crawler.

Heavy: Abyss Shellback, Stonebound, Graft Warden, Corpse Orchard.

Apex: Crowncap Brute, Deep Hunter, Furnace Golem, Cryolith Guardian, Rift Colossus.

The classification intentionally keeps Glacier Stalker mobile as a Hunter and makes Graft Warden a
slow Heavy. HuntPlayer is an aggression flag, not a weight-class synonym.

## Surtlings

The same policy now applies to all twelve elemental Surtlings:

- Wind: Skirmisher
- Fire: Hunter
- Water: Hunter
- Earth: Heavy
- Radiance: Bruiser
- Umbral: Hunter

Their progression band is derived from their canonical home; site-only Radiance uses the Fracture
band and low-luminosity Umbral uses the Great Decay band.

## Damage-boundary safety

`MagenheimCreatureCombatScaling` is attached to the custom creature prefab only. Vanilla donor
attack prefabs are not modified.

The shared `Character.Damage` prefix clones the incoming `HitData` before Magenheim applies
outgoing scaling, biome elemental riders and the final per-hit cap. This prevents a reusable AoE hit
object from accumulating scaling as it passes from target one to target two.

## Validation boundary

Static source/read-back can prove policy coverage and values, but not combat feel. Complete
`TESTING.md -> Underworld creature combat-balance acceptance` in the installed game, including
same-role cross-biome comparisons, Swarm-vs-Apex comparisons, AoE two-target checks, Surtling role
checks and multiplayer authority.
