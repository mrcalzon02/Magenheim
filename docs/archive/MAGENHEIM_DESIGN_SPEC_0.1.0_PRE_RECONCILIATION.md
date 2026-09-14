<<<<<<< HEAD
# MAGENHEIM
## Authoritative Design & Implementation Specification
**Document:** `MAGENHEIM_DESIGN_SPEC.md`  
**Version:** 0.1.0  
**Status:** Pre-implementation architecture / authoritative planning baseline  
**Target:** Valheim 1.0-era mod stack  
**Primary runtime:** BepInEx + Jötunn  
**Primary assembly:** `Magenheim.dll`

---

# 1. PROJECT DEFINITION

Magenheim is a Valheim magic and lapidary overhaul built around naturally occurring elemental geodes, a trainable Crystal Shaping skill, risky crystal refinement, adaptive equipment socketing, tiered magical staves, runecarving, Galdr, Seiðr rituals, settlement wards, magical travel, ship attunement, elemental resonance, battlefield totems, spirit binding, Fate magic, and boss-resonant capstone artifacts.

The central design rule is that magic begins as geology.

The player does not open a menu and suddenly become a wizard. The player finds unusual stone in the world, learns how to crack it, learns how to shape what is inside without destroying it, learns how to bind the resulting crystals into mundane equipment, and only much later learns to make those crystals perform deliberate supernatural work.

The complete progression is:

`Explore -> Find Geode -> Mine Geode -> Crack Geode -> Receive Rough Crystal -> Shape Crystal -> Risk Refinement -> Socket Equipment OR Build Staff -> Learn Runecraft -> Invoke Galdr -> Perform Seiðr -> Build Wards/Waystones/Totems -> Bind Spirits -> Create Fate Crystals -> Create Boss Resonance Artifacts`

Magenheim must feel like an extension of Valheim's survival progression, not a disconnected spell mod.

---

# 2. NON-NEGOTIABLE DESIGN RULES

1. **Crystal Shaping is a real Valheim skill.**
   - Display name: `Crystal Shaping`
   - Stable internal skill identifier: `magenheim.crystal_shaping`
   - This identifier is permanent after first public release because skill persistence depends upon it.

2. **Every terrestrial biome receives its own naturally spawning geode.**
   - Meadows
   - Black Forest
   - Swamp
   - Mountain
   - Plains
   - Mistlands
   - Ashlands
   - Deep North

3. **World geodes are physical mineable objects.**
   - Larger than ordinary ground stones.
   - Smaller than the player.
   - Visually recognizable from local stone by cracks, mineral banding, and subtle elemental coloration.
   - Destroyed by one valid mining strike by default.
   - Destruction drops the intact geode item.
   - Mining does not directly produce crystals.

4. **All geode cracking and crystal-shaping operations occur at the Geologist's Workstation.**

5. **Crystal quality progression is fixed conceptually:**
   - Rough
   - Simple
   - Refined
   - Advanced
   - Master

6. **Refinement is risky.**
   - Rough -> Simple: 10% base failure.
   - Simple -> Refined: 20% base failure.
   - Refined -> Advanced: 30% base failure.
   - Advanced -> Master: 40% base failure.
   - Failure destroys the source crystal and returns element-matched crystal shards.

7. **Crystal Shaping reduces refinement failure.**
   - Default maximum reduction at skill 100: 75%.
   - Configurable range: 50% to 100%.
   - At 100% configured reduction and skill 100, refinement cannot fail.

8. **Socketed effects are stored per item instance.**
   - Never mutate a shared `ItemDrop`/shared prefab definition to represent an individual socket.
   - One socketed Iron Sword must not alter every Iron Sword.

9. **Equipment is classified into exactly three broad Magenheim equipment categories:**
   - Weapon
   - Armor
   - Utility
   - Configuration overrides may explicitly reclassify or exclude any prefab.

10. **Every normal elemental crystal has three equipment behaviors.**
    - Weapon behavior
    - Armor behavior
    - Utility behavior

11. **Staff progression changes spell behavior, not merely damage numbers.**
    - Simple spell
    - Refined spell
    - Advanced spell
    - Master spell
    - Master spells are visually and mechanically dramatic capstones.

12. **Server gameplay rules are authoritative in multiplayer.**
    - Element definitions, geode weights, socket rules, refinement chances, spell values, ritual values, and balance data must not diverge between clients.

13. **Content definitions are data-driven.**
    - Code implements systems.
    - JSON/config implements content and balance.
    - Hard-coded elemental switch statements should be avoided where a definition-driven handler can be used.

14. **Magenheim must remain usable without unrelated magic frameworks.**
    - BepInEx and Jötunn are the intended hard dependencies.
    - Compatibility integrations are optional adapters.

15. **Every later magic system consumes or extends primitives already established by the crystal system.**
    - No parallel magic currency should be invented unless it solves a specific design problem.

---

# 3. TECHNICAL FOUNDATION

## 3.1 Runtime dependencies

Required:
- BepInEx
- Jötunn

Magenheim should declare network compatibility such that all participants in a multiplayer world must have the mod. Version strictness should initially use minor-version compatibility because new items, RPCs, prefabs, and persistent data structures can change between feature releases.

Recommended plugin declaration concept:

`CompatibilityLevel.EveryoneMustHaveMod`

Recommended version policy:

`VersionStrictness.Minor`

Exact API names must be compiled against the current Jötunn release used by the project.

## 3.2 Repository identity

Recommended repository:

`mrcalzon02/Magenheim`

Single authoritative development branch:

`main`

Do not build the project around generated runtime scripts. Content files are static definitions loaded by the plugin.

## 3.3 Build outputs

Primary build products:

- `Magenheim.dll`
- `magenheim_assets`
- `Translations/English.json`
- optional additional translation files
- default data files under `config/Magenheim/`

Recommended Thunderstore-style package root:

```text
Magenheim/
├── BepInEx/
│   ├── plugins/
│   │   └── Magenheim/
│   │       ├── Magenheim.dll
│   │       ├── magenheim_assets
│   │       └── Translations/
│   │           └── English.json
│   └── config/
│       └── Magenheim/
│           └── default-data/
├── icon.png
├── manifest.json
├── README.md
└── CHANGELOG.md
```

---

# 4. REPOSITORY FILE STRUCTURE

```text
Magenheim/
├── README.md
├── INSTRUCTIONS.md
├── CHANGELOG.md
├── BACKLOG.md
├── IMPLEMENTATION_PLAN.md
├── VALIDATION.md
├── Magenheim.sln
├── Directory.Build.props
├── src/
│   └── Magenheim/
│       ├── Magenheim.csproj
│       ├── MagenheimPlugin.cs
│       ├── Bootstrap/
│       │   ├── PluginConstants.cs
│       │   ├── RegistrationPipeline.cs
│       │   ├── RuntimeState.cs
│       │   └── FeatureFlags.cs
│       ├── Assets/
│       │   ├── AssetBundleLoader.cs
│       │   ├── AssetCatalog.cs
│       │   └── PrefabReferenceResolver.cs
│       ├── Config/
│       │   ├── MagenheimConfig.cs
│       │   ├── ServerConfig.cs
│       │   ├── ClientConfig.cs
│       │   └── ConfigValidator.cs
│       ├── Data/
│       │   ├── MagenheimDataLoader.cs
│       │   ├── DataManifest.cs
│       │   ├── DataSchemaVersion.cs
│       │   ├── Definitions/
│       │   │   ├── ElementDefinition.cs
│       │   │   ├── GeodeDefinition.cs
│       │   │   ├── CrystalTierDefinition.cs
│       │   │   ├── SocketEffectDefinition.cs
│       │   │   ├── StaffDefinition.cs
│       │   │   ├── RuneDefinition.cs
│       │   │   ├── GaldrDefinition.cs
│       │   │   ├── RitualDefinition.cs
│       │   │   ├── WardDefinition.cs
│       │   │   ├── ResonanceDefinition.cs
│       │   │   ├── TotemDefinition.cs
│       │   │   ├── SpiritDefinition.cs
│       │   │   ├── FateDefinition.cs
│       │   │   └── BossResonanceDefinition.cs
│       │   └── Validation/
│       │       ├── DefinitionValidator.cs
│       │       ├── ReferenceValidator.cs
│       │       └── DataHashService.cs
│       ├── Localization/
│       │   ├── LocalizationRegistrar.cs
│       │   └── LocalizationKeys.cs
│       ├── Items/
│       │   ├── ItemRegistrar.cs
│       │   ├── CrystalCatalog.cs
│       │   ├── GeodeItemRegistrar.cs
│       │   ├── CrystalItemRegistrar.cs
│       │   ├── StaffItemRegistrar.cs
│       │   └── SpecialItemRegistrar.cs
│       ├── World/
│       │   ├── GeodeWorldRegistrar.cs
│       │   ├── GeodeSpawnService.cs
│       │   ├── GeodeDropController.cs
│       │   └── WorldGenerationCompatibility.cs
│       ├── Skills/
│       │   ├── SkillRegistrar.cs
│       │   ├── CrystalShapingSkill.cs
│       │   ├── RunecraftSkill.cs
│       │   ├── GaldrSkill.cs
│       │   ├── SeidrSkill.cs
│       │   └── SkillExperienceService.cs
│       ├── Crafting/
│       │   ├── GeologistStationRegistrar.cs
│       │   ├── StationUpgradeRegistrar.cs
│       │   ├── CrystalCrackingService.cs
│       │   ├── CrystalRefinementService.cs
│       │   ├── RefinementRoller.cs
│       │   ├── ShardRecoveryService.cs
│       │   ├── CrystalExtractionService.cs
│       │   └── RecipeRegistrar.cs
│       ├── Socketing/
│       │   ├── EquipmentCategory.cs
│       │   ├── EquipmentClassifier.cs
│       │   ├── SocketMetadata.cs
│       │   ├── SocketMetadataStore.cs
│       │   ├── SocketService.cs
│       │   ├── SocketValidationService.cs
│       │   ├── SocketEffectService.cs
│       │   ├── SocketTooltipService.cs
│       │   └── Effects/
│       │       ├── WeaponCrystalEffects.cs
│       │       ├── ArmorCrystalEffects.cs
│       │       └── UtilityCrystalEffects.cs
│       ├── Magic/
│       │   ├── Staff/
│       │   │   ├── StaffAttackController.cs
│       │   │   ├── StaffResourceService.cs
│       │   │   ├── StaffProjectileFactory.cs
│       │   │   ├── StaffTargetingService.cs
│       │   │   └── Spells/
│       │   │       ├── FireSpells.cs
│       │   │       ├── FrostSpells.cs
│       │   │       ├── StormSpells.cs
│       │   │       ├── EarthSpells.cs
│       │   │       ├── VenomSpells.cs
│       │   │       ├── RadianceSpells.cs
│       │   │       ├── SeidrSpells.cs
│       │   │       └── SpiritSpells.cs
│       │   ├── Runes/
│       │   │   ├── RunecarvingService.cs
│       │   │   ├── RuneMetadata.cs
│       │   │   └── RuneEffectService.cs
│       │   ├── Galdr/
│       │   │   ├── GaldrService.cs
│       │   │   ├── GaldrCooldownService.cs
│       │   │   └── GaldrEffectService.cs
│       │   ├── Seidr/
│       │   │   ├── RitualCircleRegistrar.cs
│       │   │   ├── RitualService.cs
│       │   │   ├── RitualCostService.cs
│       │   │   └── RitualEffectService.cs
│       │   ├── Wards/
│       │   │   ├── WardstoneRegistrar.cs
│       │   │   ├── WardFuelService.cs
│       │   │   └── WardEffectService.cs
│       │   ├── Passage/
│       │   │   ├── PassageStoneRegistrar.cs
│       │   │   ├── PassagePairingService.cs
│       │   │   └── PassageRuleService.cs
│       │   ├── Ships/
│       │   │   ├── ShipAttunementService.cs
│       │   │   ├── ShipCrystalMetadata.cs
│       │   │   └── ShipEffectService.cs
│       │   ├── Lighting/
│       │   │   ├── CrystalLightRegistrar.cs
│       │   │   └── CrystalLightService.cs
│       │   ├── Resonance/
│       │   │   ├── ResonanceService.cs
│       │   │   ├── ResonanceResolver.cs
│       │   │   └── ResonanceStatusEffectService.cs
│       │   ├── Totems/
│       │   │   ├── RuneTotemRegistrar.cs
│       │   │   ├── RuneTotemService.cs
│       │   │   └── RuneTotemEffectService.cs
│       │   ├── Spirits/
│       │   │   ├── SpiritBindingService.cs
│       │   │   ├── SpiritSummonController.cs
│       │   │   └── SpiritLifetimeService.cs
│       │   ├── Fate/
│       │   │   ├── FateCrystalService.cs
│       │   │   ├── FateInterventionService.cs
│       │   │   └── FateDivinationService.cs
│       │   └── BossResonance/
│       │       ├── BossResonanceService.cs
│       │       └── BossResonanceEffectService.cs
│       ├── Pieces/
│       │   ├── PieceRegistrar.cs
│       │   └── CrystalSocketPieceController.cs
│       ├── UI/
│       │   ├── MagenheimUIRegistrar.cs
│       │   ├── GeologistStationPanel.cs
│       │   ├── CrystalRefinementPanel.cs
│       │   ├── SocketingPanel.cs
│       │   ├── RunecarvingPanel.cs
│       │   ├── GaldrRadialMenu.cs
│       │   ├── RitualPanel.cs
│       │   └── TooltipFormatter.cs
│       ├── Networking/
│       │   ├── NetworkRegistrar.cs
│       │   ├── DataSyncService.cs
│       │   ├── DataManifestRpc.cs
│       │   ├── DataPayloadRpc.cs
│       │   └── NetworkValidationService.cs
│       ├── Persistence/
│       │   ├── ItemMetadataKeys.cs
│       │   ├── MetadataMigrationService.cs
│       │   ├── WorldMetadataService.cs
│       │   └── ZdoMetadataKeys.cs
│       ├── Compatibility/
│       │   ├── CompatibilityManager.cs
│       │   ├── ModdedItemAdapter.cs
│       │   └── CompatibilityRuleLoader.cs
│       ├── Patches/
│       │   ├── AttackDamagePatches.cs
│       │   ├── PlayerStatPatches.cs
│       │   ├── ItemTooltipPatches.cs
│       │   ├── InventorySerializationPatches.cs
│       │   ├── ShipPatches.cs
│       │   └── PortalPatches.cs
│       ├── Console/
│       │   ├── MagenheimConsoleCommands.cs
│       │   └── DebugCommands.cs
│       └── Utility/
│           ├── DeterministicRandom.cs
│           ├── WeightedRoll.cs
│           ├── StableHash.cs
│           └── MathUtil.cs
├── assets/
│   ├── UnityProject/
│   └── Source/
│       ├── Geodes/
│       ├── Crystals/
│       ├── Workstations/
│       ├── Staves/
│       ├── Runes/
│       ├── Rituals/
│       ├── Wards/
│       ├── Totems/
│       ├── Spirits/
│       └── VFX/
├── config/
│   └── defaults/
│       ├── elements.json
│       ├── crystal-tiers.json
│       ├── geodes.json
│       ├── socket-rules.json
│       ├── socket-effects.json
│       ├── staffs.json
│       ├── runes.json
│       ├── galdr.json
│       ├── rituals.json
│       ├── wards.json
│       ├── passage.json
│       ├── ships.json
│       ├── resonance.json
│       ├── totems.json
│       ├── spirits.json
│       ├── fate.json
│       └── boss-resonance.json
├── schemas/
│   ├── elements.schema.json
│   ├── geodes.schema.json
│   ├── socket-rules.schema.json
│   ├── staffs.schema.json
│   ├── rituals.schema.json
│   └── resonance.schema.json
├── Translations/
│   └── English.json
└── tests/
    └── Magenheim.Tests/
        ├── Magenheim.Tests.csproj
        ├── RefinementFormulaTests.cs
        ├── WeightedRollTests.cs
        ├── EquipmentClassifierTests.cs
        ├── MetadataMigrationTests.cs
        ├── ResonanceResolverTests.cs
        └── DataValidationTests.cs
```

---

# 5. STABLE INTERNAL NAMING CONVENTIONS

All runtime prefab names use the `Magenheim_` prefix.

All localization tokens use the `magenheim_` namespace.

All persistent metadata keys use the `magenheim.` prefix.

Internal element IDs are lowercase and permanent:

- `fire`
- `frost`
- `storm`
- `earth`
- `venom`
- `radiance`
- `seidr`
- `spirit`

Special non-elemental systems:

- `fate`
- `boss_resonance`

Crystal tier IDs:

- `rough`
- `simple`
- `refined`
- `advanced`
- `master`

Do not rename these IDs after release. Display names may be localized or changed without altering persistent identifiers.

---

# 6. ELEMENT CATALOG

| Internal ID | Display Family | Color | Primary Theme | Vanilla-compatible damage basis |
|---|---|---|---|---|
| `fire` | Fire | Red | heat, flame, combustion | Fire |
| `frost` | Frost | Blue | water, cold, ice | Frost |
| `storm` | Storm | Yellow | lightning, speed, impact | Lightning |
| `earth` | Earth | Green | stone, growth, stability | Blunt / physical |
| `venom` | Venom | Dark Green | poison, rot, decay | Poison |
| `radiance` | Radiance | Gold | sunlight, protection, purification | Spirit / Fire mix |
| `seidr` | Seiðr | Violet | arcane force, Eitr, veils | Spirit / Lightning mix |
| `spirit` | Spirit | White | ancestors, winter spirits, spectral force | Spirit / Frost mix |

Magenheim should prefer existing Valheim damage channels where possible. A custom "arcane" damage type should not be introduced until there is a demonstrated need, because custom damage channels create substantially broader compatibility requirements.

---

# 7. GEODE SYSTEM

## 7.1 World geode display names and prefab names

| Biome | Display item | World prefab | Inventory prefab |
|---|---|---|---|
| Meadows | Meadows Geode | `Magenheim_GeodeWorld_Meadows` | `Magenheim_Geode_Meadows` |
| Black Forest | Black Forest Geode | `Magenheim_GeodeWorld_BlackForest` | `Magenheim_Geode_BlackForest` |
| Swamp | Swamp Geode | `Magenheim_GeodeWorld_Swamp` | `Magenheim_Geode_Swamp` |
| Mountain | Mountain Geode | `Magenheim_GeodeWorld_Mountain` | `Magenheim_Geode_Mountain` |
| Plains | Plains Geode | `Magenheim_GeodeWorld_Plains` | `Magenheim_Geode_Plains` |
| Mistlands | Mistlands Geode | `Magenheim_GeodeWorld_Mistlands` | `Magenheim_Geode_Mistlands` |
| Ashlands | Ashlands Geode | `Magenheim_GeodeWorld_Ashlands` | `Magenheim_Geode_Ashlands` |
| Deep North | Deep North Geode | `Magenheim_GeodeWorld_DeepNorth` | `Magenheim_Geode_DeepNorth` |

## 7.2 Physical design

Approximate physical dimensions:

- Width: 1.1-1.6 m
- Height: 0.8-1.4 m
- Irregular silhouette
- Local-biome stone exterior
- Visible crystal/mineral seams
- No large emissive glow at long range
- Subtle color cue at close range
- Distinct mining hover name: `Geode`

World geodes should use a normal networked destructible/mining interaction. Default health must be low enough for one valid pickaxe strike to destroy the object. The exact Valheim component and field names must be verified against the current 1.0 assemblies during implementation rather than guessed in the design document.

Default behavior:

- Requires mining damage.
- One valid mining hit.
- Drops exactly one intact biome geode.
- Drops no stone.
- Does not directly roll crystals.
- Respawn behavior: none by default.
- Spawn only through world generation unless a server enables replenishment.

## 7.3 Default biome crystal weights

Each geode cracks into at least one Rough Crystal. Element result is selected independently from the biome weight table.

| Biome | Default weighted crystal distribution |
|---|---|
| Meadows | Earth 60, Radiance 15, Storm 10, Frost 5, Fire 5, Spirit 5 |
| Black Forest | Storm 45, Earth 20, Venom 15, Spirit 10, Fire 5, Radiance 5 |
| Swamp | Venom 60, Spirit 15, Earth 10, Seiðr 10, Frost 5 |
| Mountain | Frost 60, Spirit 15, Storm 15, Radiance 5, Earth 5 |
| Plains | Radiance 50, Storm 20, Fire 15, Earth 10, Spirit 5 |
| Mistlands | Seiðr 60, Storm 10, Spirit 10, Venom 10, Frost 5, Radiance 5 |
| Ashlands | Fire 65, Radiance 10, Seiðr 10, Earth 5, Storm 5, Spirit 5 |
| Deep North | Spirit 40, Frost 40, Storm 10, Radiance 5, Seiðr 5 |

Every table is configurable in `geodes.json`.

## 7.4 Geode cracking yield

Default cracking:

- 1 guaranteed Rough Crystal roll.
- 35% chance for a second independent Rough Crystal roll.
- 10% chance for a third independent Rough Crystal roll.
- No failure.
- Default Crystal Shaping skill does not affect quantity.
- Optional config may enable skill-based bonus yield later.

Cracking awards a small amount of Crystal Shaping experience because it is a lapidary operation.

---

# 8. CRYSTAL ITEM CATALOG

Every element receives the same five quality stages.

Prefab convention:

`Magenheim_Crystal_{Element}_{Tier}`

Examples:

- `Magenheim_Crystal_Fire_Rough`
- `Magenheim_Crystal_Fire_Simple`
- `Magenheim_Crystal_Fire_Refined`
- `Magenheim_Crystal_Fire_Advanced`
- `Magenheim_Crystal_Fire_Master`

Display names:

| Element | Rough | Simple | Refined | Advanced | Master |
|---|---|---|---|---|---|
| Fire | Rough Fire Crystal | Simple Fire Crystal | Refined Fire Crystal | Advanced Fire Crystal | Master Fire Crystal |
| Frost | Rough Frost Crystal | Simple Frost Crystal | Refined Frost Crystal | Advanced Frost Crystal | Master Frost Crystal |
| Storm | Rough Storm Crystal | Simple Storm Crystal | Refined Storm Crystal | Advanced Storm Crystal | Master Storm Crystal |
| Earth | Rough Earth Crystal | Simple Earth Crystal | Refined Earth Crystal | Advanced Earth Crystal | Master Earth Crystal |
| Venom | Rough Venom Crystal | Simple Venom Crystal | Refined Venom Crystal | Advanced Venom Crystal | Master Venom Crystal |
| Radiance | Rough Radiance Crystal | Simple Radiance Crystal | Refined Radiance Crystal | Advanced Radiance Crystal | Master Radiance Crystal |
| Seiðr | Rough Seiðr Crystal | Simple Seiðr Crystal | Refined Seiðr Crystal | Advanced Seiðr Crystal | Master Seiðr Crystal |
| Spirit | Rough Spirit Crystal | Simple Spirit Crystal | Refined Spirit Crystal | Advanced Spirit Crystal | Master Spirit Crystal |

Shard prefab convention:

`Magenheim_CrystalShard_{Element}`

Display names:

- Fire Crystal Shard
- Frost Crystal Shard
- Storm Crystal Shard
- Earth Crystal Shard
- Venom Crystal Shard
- Radiance Crystal Shard
- Seiðr Crystal Shard
- Spirit Crystal Shard

Rough crystals cannot be socketed and cannot power a staff.

---

# 9. CRYSTAL SHAPING SKILL

## 9.1 Stable skill identity

Display name:

`Crystal Shaping`

Internal identifier:

`magenheim.crystal_shaping`

This identifier must never be changed after player saves exist.

## 9.2 Experience sources

Initial tuning targets:

| Action | Skill experience weight |
|---|---:|
| Crack geode | 0.15 |
| Rough -> Simple attempt | 0.50 |
| Simple -> Refined attempt | 1.00 |
| Refined -> Advanced attempt | 2.00 |
| Advanced -> Master attempt | 4.00 |
| Install crystal socket | 0.25 |
| Extract crystal | 0.50 |
| Craft staff focus | 0.50 |

Experience is awarded on refinement attempt, not only success. Failure is still practice.

The actual progression curve must use Valheim's skill progression mechanics rather than invent a parallel XP level.

## 9.3 Refinement formula

Base failure:

- Rough -> Simple = 0.10
- Simple -> Refined = 0.20
- Refined -> Advanced = 0.30
- Advanced -> Master = 0.40

Config:

`MaximumFailureReduction = 0.75`

Allowed:

`0.50 .. 1.00`

Formula:

`skillReduction = (CrystalShapingSkill / 100) * MaximumFailureReduction`

`effectiveFailure = BaseFailure * (1 - skillReduction)`

At default 75% maximum reduction:

| Refinement | Skill 0 | Skill 25 | Skill 50 | Skill 75 | Skill 100 |
|---|---:|---:|---:|---:|---:|
| Rough -> Simple | 10.00% | 8.125% | 6.25% | 4.375% | 2.50% |
| Simple -> Refined | 20.00% | 16.25% | 12.50% | 8.75% | 5.00% |
| Refined -> Advanced | 30.00% | 24.375% | 18.75% | 13.125% | 7.50% |
| Advanced -> Master | 40.00% | 32.50% | 25.00% | 17.50% | 10.00% |

At a configured maximum reduction of 100%, skill 100 results in 0% failure.

## 9.4 Failure recovery

Default shard returns:

| Failed operation | Shards returned |
|---|---:|
| Rough -> Simple | 1 |
| Simple -> Refined | 2 |
| Refined -> Advanced | 3 |
| Advanced -> Master | 5 |

Default shard recombination:

`5 matching Crystal Shards -> 1 Simple Crystal`

Config:

`ShardsRequiredForSimpleCrystal = 5`

Shard recipes are element-specific and executed at the Geologist's Workstation.

---

# 10. GEOLOGIST'S WORKSTATION

## 10.1 Base piece

Display name:

`Geologist's Workstation`

Prefab:

`Magenheim_GeologistWorkstation`

Localization token:

`$piece_magenheim_geologist_workstation`

Default recipe target:

- Wood x10
- Stone x8
- Flint x4
- Hard Antler x1

Build requirement:

- Workbench nearby

This naturally places the workstation after Eikthyr and the acquisition of mining capability without making it a late-game system.

## 10.2 Visual language

The station should resemble a rugged Viking workbench modified for stone and crystal work:

- Timber frame
- Heavy stone slab
- Small clamp
- Hammer and chisels
- Stone fragments
- Crystal shard tray
- primitive inspection lens or framed polished glass
- leather wraps
- later upgrades add rotating wheel, metal braces, rune plates, and suspended crystal resonance structures

It must not resemble a modern jewelers' desk or laboratory.

## 10.3 Station upgrades

### Level 1: Geologist's Workstation
Allows:
- Crack geodes
- Rough -> Simple
- Shards -> Simple
- Install Simple crystals
- Craft Simple staves

### Level 2: Fracturing Block
Display:

`Fracturing Block`

Prefab:

`Magenheim_StationUpgrade_FracturingBlock`

Suggested recipe:
- Core Wood x5
- Bronze x2
- Flint x8

Unlocks:
- Simple -> Refined
- Refined crystal socketing
- Refined staff crafting

### Level 3: Faceting Wheel
Display:

`Faceting Wheel`

Prefab:

`Magenheim_StationUpgrade_FacetingWheel`

Suggested recipe:
- Fine Wood x10
- Iron x3
- Sharpening Stone x1

Unlocks:
- Refined -> Advanced
- Advanced crystal socketing
- Advanced staff crafting
- Basic crystal extraction

### Level 4: Resonance Frame
Display:

`Resonance Frame`

Prefab:

`Magenheim_StationUpgrade_ResonanceFrame`

Suggested recipe:
- Silver x4
- Black Metal x2
- vanilla Crystal x4
- Fine Wood x8

Unlocks:
- Advanced -> Master
- Master sockets
- Master staves
- elemental resonance inspection

### Level 5: Rune Engraver
Display:

`Rune Engraver`

Prefab:

`Magenheim_StationUpgrade_RuneEngraver`

Suggested recipe:
- Yggdrasil Wood x10
- Black Marble x5
- Refined Eitr x2
- Black Metal x2

Unlocks:
- Runecarving
- high-grade crystal extraction
- Galdr focus recipes
- ritual components
- ward cores
- passage stones
- spirit-binding components
- boss resonance recipes

---

# 11. SOCKETING SYSTEM

## 11.1 Default socket limits

Server-configurable defaults:

- Weapons: 1 socket
- Armor: 1 socket
- Utility: 1 socket

Allowed configured range:

`0 .. 3`

Additional sockets must not be added until multi-socket UI and balance are verified. Version 1.0 gameplay should ship with one socket per item by default even if the engine supports more.

## 11.2 Equipment classification

Enum:

```text
Weapon
Armor
Utility
Excluded
Unknown
```

Classification priority:

1. Explicit config override by prefab name.
2. Magenheim explicit registration.
3. Known Valheim item type mapping.
4. Compatibility adapter.
5. Unknown -> Excluded with diagnostic log.

Default conceptual mapping:

Weapon:
- one-handed weapons
- two-handed weapons
- bows
- crossbows
- polearms
- knives
- magic weapons
- offensive tools when explicitly overridden

Armor:
- helmets
- chest
- legs
- shoulder/cloak
- shields

Utility:
- utility-slot items
- tools
- selected noncombat equipment

## 11.3 Persistent item metadata

Persistent keys:

```text
magenheim.data_version
magenheim.socket.count
magenheim.socket.0.element
magenheim.socket.0.tier
magenheim.socket.0.definition_version
magenheim.rune.id
magenheim.rune.version
```

Never store localized names as authoritative metadata.

The item metadata adapter must isolate Magenheim from future changes in Valheim's raw item-data implementation.

## 11.4 Socket operation

At Geologist's Workstation:

1. Player selects equipment item.
2. System classifies equipment.
3. UI displays current socket state.
4. Player selects eligible crystal.
5. Server validates:
   - item exists
   - crystal exists
   - crystal tier is Simple or better
   - socket capacity is available
   - item is not excluded
   - operation is allowed by synchronized server rules
6. Crystal item is consumed.
7. Element/tier metadata is written to that item instance.
8. tooltip/state is refreshed.
9. Crystal Shaping experience is awarded.
10. persistence is verified through inventory serialization.

## 11.5 Extraction

Crystal extraction becomes available after the Faceting Wheel.

Default extraction uses the same base risk ladder as refinement:

- Simple: 10% break
- Refined: 20%
- Advanced: 30%
- Master: 40%

Crystal Shaping applies the same configured failure reduction.

Success:
- returns original crystal
- clears socket metadata

Failure:
- destroys installed crystal
- returns normal failure shards for that tier
- clears socket metadata

Replacing a crystal without extraction destroys the existing crystal by default.

Config may permit free replacement, but it is not the default.

---

# 12. DEFAULT SOCKET EFFECTS

All magnitudes are initial balance targets and are data-driven.

Tier scalar:

- Simple = 1.00
- Refined = 1.50
- Advanced = 2.25
- Master = 3.50

## Fire

Weapon:
- additional fire damage
- Master adds a configurable chance to cause a small flame burst

Armor:
- improves fire resistance
- attackers may receive minor burn retaliation at high tier

Utility:
- warmth effect
- reduced environmental heat/cold penalties as appropriate to implementation
- Master provides the strongest environmental protection

## Frost

Weapon:
- frost damage
- slow intensity/duration scaling by tier

Armor:
- frost/cold resistance

Utility:
- reduced stamina penalty from swimming and cold travel
- Master improves movement in snow/harsh cold

## Storm

Weapon:
- lightning damage
- increased stagger contribution at higher tier

Armor:
- stagger resistance
- Master may discharge a short retaliation arc

Utility:
- movement speed and stamina regeneration bonuses kept deliberately modest

## Earth

Weapon:
- additional physical/blunt impact
- increased knockback/stagger

Armor:
- flat armor contribution or physical damage mitigation
- increased knockback resistance

Utility:
- carry weight and/or mining efficiency
- values capped to avoid replacing Megingjord or mining progression

## Venom

Weapon:
- poison damage over time

Armor:
- poison resistance

Utility:
- improved gathering/foraging yield chance or poison-environment protection
- no multiplicative infinite-yield behavior

## Radiance

Weapon:
- spirit damage
- increased effect against undead/hostile spirit-tagged enemies where available

Armor:
- minor health regeneration or spirit/fire protection

Utility:
- soft illumination
- minor recovery/comfort support

## Seiðr

Weapon:
- improves magical weapon damage or adds a small spirit/lightning component

Armor:
- magical damage protection and/or Eitr reserve support

Utility:
- Eitr regeneration
- tier scaling must remain below dedicated late-game Eitr food/equipment replacement levels

## Spirit

Weapon:
- spirit damage with a small frost component
- stronger against spectral/undead enemies

Armor:
- spirit/frost protection
- high tiers improve resistance to fear/hostile spirit effects if such effects are present

Utility:
- nearby hostile-spirit awareness
- corpse/death-site support
- high-tier effects interact with Spirit Binding

---

# 13. STAFF SYSTEM

## 13.1 Staff naming

Prefab:

`Magenheim_Staff_{Element}_{Tier}`

Example:

`Magenheim_Staff_Fire_Master`

Display convention:

`{Tier} Staff of {Element}`

Examples:

- Simple Staff of Fire
- Refined Staff of Fire
- Advanced Staff of Fire
- Master Staff of Fire

## 13.2 Resource progression

Default:

- Simple: stamina
- Refined: stamina
- Advanced: stamina + Eitr
- Master: primarily Eitr

This prevents the entire early crystal system from becoming unusable until Mistlands.

Config may force all staves to use Eitr.

## 13.3 Suggested staff frame recipes

Simple:
- Fine Wood x10
- Bronze x2
- matching Simple Crystal x1

Refined:
- Ancient Bark x10
- Iron x2
- matching Refined Crystal x1

Advanced:
- Fine Wood x12
- Silver x3
- Black Metal x2
- matching Advanced Crystal x1

Master:
- Yggdrasil Wood x15
- Black Metal x4
- Refined Eitr x10
- matching Master Crystal x1

These values are balance starting points, not immutable lore.

## 13.4 Complete spell matrix

| Element | Simple | Refined | Advanced | Master |
|---|---|---|---|---|
| Fire | Ember Dart | Firebolt | Flameburst | Meteorfall |
| Frost | Water Dart | Frost Lance | Ice Volley | Rime Torrent |
| Storm | Spark | Chain Bolt | Thunder Spear | Stormcall |
| Earth | Stone Splinter | Crag Shot | Fault Line | Mountainfall |
| Venom | Venom Needle | Bile Orb | Miasma Burst | Plague Mire |
| Radiance | Gleam Bolt | Sun Lance | Radiant Wave | Sunfall |
| Seiðr | Seiðr Bolt | Seeking Hex | Arcane Rupture | Veilstorm |
| Spirit | Wisp Dart | Spirit Lance | Spectral Host | Howling Dead |

### Fire
Simple: small direct fire projectile.  
Refined: faster projectile with modest impact explosion.  
Advanced: projectile creates a short-lived flame eruption.  
Master: target point is marked, then one or more flaming meteors descend from above for large AoE fire/blunt damage.

### Frost
Simple: fast water/frost dart.  
Refined: piercing frost lance.  
Advanced: spread or cone of ice shards.  
Master: sustained torrent of razor ice into the target area with heavy frost buildup and crowd control.

### Storm
Simple: spark projectile.  
Refined: bolt can jump once to a nearby hostile.  
Advanced: high-impact lightning spear with stagger.  
Master: target zone receives repeated lightning strikes for a short duration.

### Earth
Simple: stone splinter.  
Refined: heavy rock projectile with knockback.  
Advanced: line of erupting ground impacts.  
Master: massive rockfall/earth rupture over the target zone.

### Venom
Simple: poison needle.  
Refined: toxic glob with splash.  
Advanced: expanding poison burst.  
Master: persistent corrosive mire/miasma field.

### Radiance
Simple: spirit-light bolt.  
Refined: piercing sun lance.  
Advanced: expanding radiant wave.  
Master: descending solar pillar with strong spirit/fire behavior.

### Seiðr
Simple: violet magical bolt.  
Refined: weakly seeking hex missiles.  
Advanced: unstable arcane detonation using existing compatible damage channels.  
Master: Veilstorm repeatedly strikes a target area with supernatural projectiles.

### Spirit
Simple: spectral wisp dart.  
Refined: piercing spirit lance.  
Advanced: several spectral attackers/projectiles sweep the target.  
Master: Howling Dead creates a temporary storm of ancestral/winter spirits.

---

# 14. RUNECARVING

Runecarving is the first expansion layer after Master crystals.

Skill:

- Display: `Runecraft`
- Stable ID: `magenheim.runecraft`

Runecraft is intentionally separate from Crystal Shaping:
- Crystal Shaping determines ability to create reliable magical material.
- Runecraft determines ability to define what that material does persistently.

## 14.1 Core rune items

`Magenheim_BlankRunePlate`
Display: `Blank Rune Plate`

`Magenheim_RuneChisel`
Display: `Rune Chisel`

## 14.2 Initial rune vocabulary

Runes describe behavior, not element.

### Rune of Binding
ID: `binding`
Effect:
- increases effectiveness of the item's installed crystal
- intended as straightforward amplification

### Rune of the Edge
ID: `edge`
Effect:
- weapon-oriented offensive modifier
- element changes resulting damage behavior

### Rune of Shelter
ID: `shelter`
Effect:
- armor-oriented defensive modifier

### Rune of the Way
ID: `way`
Effect:
- utility, travel, passage, and ship interactions

### Rune of Echoes
ID: `echoes`
Effect:
- Galdr and staff interactions
- may repeat or extend selected effects

### Rune of the Vessel
ID: `vessel`
Effect:
- improves capacity, duration, charge, or sustained magical structures

One rune slot per compatible item by default.

Rune metadata is independent of crystal metadata.

---

# 15. GALDR

Skill:

- Display: `Galdr`
- Stable ID: `magenheim.galdr`

Galdr is active personal magic produced by resonating equipped crystals through voice/rune technique.

It does not require carrying a second set of spellbooks.

Default input:

`G`

The key must be configurable.

UI:

`GaldrRadialMenu`

Default duration target:

20 seconds

Default cooldown target:

120 seconds

Galdr skill:
- reduces resource cost
- modestly improves duration
- modestly reduces cooldown
- never reduces cooldown to zero

## Elemental Galdr

Fire:
`Galdr of Embers`
- weapon hits gain stronger burn behavior
- nearby enemies may ignite

Frost:
`Galdr of Rime`
- nearby hostiles are slowed
- player gains temporary cold protection

Storm:
`Galdr of Thunder`
- movement/stamina benefit
- attacks may arc lightning

Earth:
`Galdr of Stone`
- knockback resistance
- armor/poise increase

Venom:
`Galdr of Rot`
- attacks strengthen poison
- periodic poison aura

Radiance:
`Galdr of Dawn`
- regeneration/support
- damages or repels qualifying undead enemies

Seiðr:
`Galdr of Veils`
- Eitr regeneration
- staff cost reduction
- mild magical protection

Spirit:
`Galdr of Ancestors`
- temporary spectral guardian behavior
- spirit/frost protection

---

# 16. SEIÐR RITUAL MAGIC

Skill:

- Display: `Seiðr`
- Stable ID: `magenheim.seidr`

Seiðr is not ordinary combat casting. It is deliberate large-scale magic requiring structures, offerings, crystals, time, and preparation.

## 16.1 Ritual structures

`Magenheim_SeidrCircle`
Display: `Seiðr Circle`

`Magenheim_OfferingBowl`
Display: `Offering Bowl`

`Magenheim_RunePillar`
Display: `Rune Pillar`

`Magenheim_CrystalBrazier`
Display: `Crystal Brazier`

A valid ritual circle checks required nearby pieces before activation.

## 16.2 Initial ritual catalog

### Ritual of Clear Skies
Purpose:
- temporarily clears hostile weather in a configured radius/region when possible

### Ritual of Fair Wind
Purpose:
- temporary favorable sailing wind or wind assistance

### Ritual of Returning
Purpose:
- identifies or marks the player's most recent death location
- can provide temporary corpse-retrieval support

### Ritual of Warding
Purpose:
- establishes a strong temporary defensive ritual zone

### Ritual of Seeking
Purpose:
- divination
- searches for configured world targets such as boss altars, biome features, or Magenheim geodes
- never reveals the entire map at once

### Ritual of Ancestors
Purpose:
- spirit communication / temporary ancestral aid
- prerequisite path for Spirit Binding

### Ritual of the Norns
Purpose:
- late-game Fate system
- converts rare boss-resonant materials into Fate Shards or Fate Crystals

All ritual costs are defined in `rituals.json`.

---

# 17. CRYSTAL WARDSTONES

Build piece:

`Magenheim_CrystalWardstone`

Display:

`Crystal Wardstone`

Wardstones accept one Simple-or-better crystal.

Crystal remains installed.

Fuel:
- matching element Crystal Shards

This deliberately gives failed refinements and excess shards a continuing late-game use.

Quality controls:
- radius
- pulse strength
- duration per shard
- effect magnitude

Element behavior:

Fire:
- periodic damage to hostile creatures

Frost:
- slows hostile creatures

Storm:
- periodic stagger/lightning pulse

Earth:
- improves structural durability and knockback resistance within area

Venom:
- damages/poisons hostile creatures entering the ward

Radiance:
- bright area
- anti-undead/anti-spirit pressure
- modest restorative support

Seiðr:
- improves Eitr recovery
- reduces selected hostile magical effects

Spirit:
- strengthens Spirit Binding
- detects nearby hostile spirits/undead
- improves death-recovery support

---

# 18. RUNESTONES OF PASSAGE

Build piece:

`Magenheim_RunestoneOfPassage`

Display:

`Runestone of Passage`

The system uses paired tags similar in concept to portals but has its own validation and crystal requirements.

Default rules:

- Minimum crystal: Advanced Seiðr Crystal
- Both linked stones must contain compatible crystals.
- By default, normal Valheim teleport restrictions remain respected.
- Server config can permit restricted-item passage at Master tier.
- Restricted-item bypass is OFF by default.
- Crystal tier controls activation stability, visual intensity, and optional special permissions.

Config file:

`passage.json`

Important server settings:

```text
EnablePassageStones
AllowRestrictedItems
MinimumTier
RequireMatchingElements
RequireMatchingTier
ActivationCost
```

This system must not silently patch vanilla portals globally.

---

# 19. LONGSHIP / SHIP ATTUNEMENT

Core item:

`Magenheim_RunicKeelstone`

Display:

`Runic Keelstone`

A Runic Keelstone is installed into a supported ship through interaction rather than by permanently mutating the ship prefab.

Persistent ship metadata:

```text
magenheim.ship.element
magenheim.ship.tier
magenheim.ship.data_version
```

Default effects:

Fire:
- reduces Ashlands/fire-environment ship penalties where appropriate

Frost:
- improves survival in Deep North/cold seas
- improves resistance to frost-related hazards

Storm:
- improved acceleration/top sailing performance
- effect capped to preserve sailing gameplay

Earth:
- hull damage resistance

Venom:
- hostile creature deterrence or damage aura at close range

Radiance:
- magical navigation light
- improved visibility

Seiðr:
- modest wind-independence / magical propulsion assistance
- expensive enough not to obsolete sailing

Spirit:
- death-site/navigation support
- spectral warning behavior

Ship support begins with Karve and Longship. Additional ship prefabs are supported through compatibility overrides.

---

# 20. CRYSTAL LANTERNS AND BRAZIERS

Pieces:

`Magenheim_CrystalLantern`
Display: `Crystal Lantern`

`Magenheim_CrystalBrazier`
Display: `Crystal Brazier`

Both accept an installed crystal.

The visual light color follows element.

Simple crystals provide illumination only.

Higher tiers may provide small localized secondary effects, deliberately weaker than Wardstones.

Examples:
- Fire: warmth
- Frost: cool blue light
- Storm: intermittent sparks
- Earth: green/golden low light
- Venom: sickly green illumination
- Radiance: strongest clean illumination
- Seiðr: violet magical light
- Spirit: pale spectral light

---

# 21. ELEMENTAL RESONANCE

Resonance counts crystals currently equipped by the player.

Default same-element thresholds:

- 2 matching crystals: Minor Resonance
- 4 matching crystals: Greater Resonance
- 6 matching crystals: Master Resonance

Same-element resonance is intentionally stronger than merely adding individual socket values.

Examples:

Fire:
- Minor: Ember Resonance
- Greater: Flame Resonance
- Master: Inferno Resonance

Frost:
- Minor: Rime Resonance
- Greater: Glacier Resonance
- Master: Winter Resonance

Equivalent names should be data-defined.

## Mixed resonance combinations

Initial mixed combinations:

| Elements | Resonance |
|---|---|
| Fire + Frost | Steam |
| Storm + Frost | Tempest |
| Venom + Frost | Rotfrost |
| Fire + Radiance | Sunfire |
| Earth + Storm | Thunderstone |
| Seiðr + Spirit | Veilborn |
| Earth + Radiance | Hearthstone |
| Venom + Seiðr | Witchrot |

Resolution rule:

1. Determine same-element counts.
2. Apply highest qualifying same-element resonance.
3. Determine two strongest non-identical elemental counts.
4. Resolve at most one mixed resonance by default.
5. Ties resolve deterministically by definition priority.
6. Server config may allow multiple mixed resonances later.

File:

`resonance.json`

---

# 22. RUNE TOTEMS

Core inventory item:

`Magenheim_RunedTotem`

Display:

`Runed Totem`

A Runed Totem can receive a crystal at the Geologist's Workstation.

Using it places:

`Magenheim_RuneTotemWorld`

The world totem:
- is networked
- has finite duration
- applies an elemental field
- expires automatically
- retains element/tier metadata

This provides battlefield preparation without turning every effect into a direct projectile spell.

Default roles:

Fire: offensive damage field  
Frost: slowing field  
Storm: stagger/lightning field  
Earth: defensive stability field  
Venom: poison field  
Radiance: support/anti-undead field  
Seiðr: Eitr/support field  
Spirit: spectral ally/defense field

---

# 23. SPIRIT BINDING

Spirit Binding grows out of Seiðr + Spirit crystals.

Initial bound-spirit items:

`Magenheim_SpiritFetish_Wolf`
Display: `Wolf Spirit Fetish`

`Magenheim_SpiritFetish_Raven`
Display: `Raven Spirit Fetish`

`Magenheim_SpiritFetish_Warrior`
Display: `Fallen Warrior Spirit Fetish`

Recipes consume:
- appropriate creature trophy/material
- Spirit Crystal of required tier
- Runecraft component
- Seiðr ritual component

Summons are temporary.

Default limits:
- one active bound combat spirit per player
- raven may use a separate scouting/noncombat slot if balance permits
- duration configured by spirit type
- summon cannot permanently reproduce
- summon cannot drop normal creature loot
- summon ownership is network-authoritative

Master Spirit crystals create dramatically stronger but still temporary summons.

---

# 24. FATE MAGIC

Fate is deliberately outside the normal eight-element geode table.

Items:

`Magenheim_FateShard`
Display: `Fate Shard`

`Magenheim_FateCrystal`
Display: `Fate Crystal`

Visual:
- dark/opalescent/prismatic
- does not share a single elemental color

Primary source:
- Ritual of the Norns
- requires boss-resonance materials and high-level Seiðr

Fate Crystal uses:

### Thread Unbroken
Consumable intervention:
- guarantees the next Advanced -> Master refinement attempt cannot fail
- crystal consumed when intervention triggers

### Marked Return
Optional/server-configurable death protection:
- reduces a defined part of death penalty or protects a configured subset of carried equipment
- must be conservative by default
- never duplicates inventory

### Farseeing
Divination:
- reveals a limited target region, boss marker, rare feature, or unexplored point according to ritual configuration

### Second Thread
High-cost reroll:
- when an eligible refinement fails, immediately rerolls that specific failure once
- cannot recursively trigger
- Fate Crystal is consumed

Fate effects require strict transactional handling so a crash/disconnect cannot duplicate or repeatedly trigger a crystal.

---

# 25. BOSS RESONANCE

Boss Resonance is the capstone system.

It consumes a Forsaken trophy and a matching Master Crystal to produce a unique resonant artifact/crystal.

Initial named artifacts:

| Boss | Element | Result |
|---|---|---|
| Eikthyr | Storm | Eikthyr's Stormheart |
| The Elder | Earth | Elderroot Heart |
| Bonemass | Venom | Bonemass Rotheart |
| Moder | Frost | Moder Rimeheart |
| Yagluth | Radiance/Fire | Yagluth Sunheart |
| The Queen | Seiðr | Queen Veilheart |
| Fader | Fire | Fader Ashheart |
| Kall Fimbulbringer | Spirit/Frost | Kall Winterheart |

Prefab convention:

`Magenheim_BossResonance_{BossId}`

Examples:

- `Magenheim_BossResonance_Eikthyr`
- `Magenheim_BossResonance_Kall`

These are not ordinary quality tiers. They are named capstone artifacts.

Each receives one unique behavior plus a strong elemental identity.

Examples:

Eikthyr's Stormheart:
- major Storm resonance contribution
- improves Storm Galdr
- lightning chain behavior

Elderroot Heart:
- structure/earth stability
- strong Earth resonance
- root/ground interaction

Bonemass Rotheart:
- poison resistance/offense interaction
- Venom resonance capstone

Moder Rimeheart:
- Frost mastery
- improves ship wind/cold interactions

Yagluth Sunheart:
- Radiance + Fire hybrid
- strong anti-undead and solar behavior

Queen Veilheart:
- Seiðr/Eitr capstone
- ritual and staff efficiency

Fader Ashheart:
- extreme fire behavior
- Ashlands-oriented ward/staff enhancements

Kall Winterheart:
- Spirit/Frost hybrid
- final progression resonance
- Spirit Binding and Deep North effects

Boss trophies are consumed by default. Server config can disable trophy consumption.

---

# 26. DATA FILES

Runtime editable server data lives under:

`BepInEx/config/Magenheim/`

## `elements.json`
Defines:
- element ID
- display/localization keys
- color
- VFX references
- damage mapping
- socket behavior references
- staff behavior reference

## `crystal-tiers.json`
Defines:
- tiers
- quality order
- base failure
- shard return
- visual scale
- socket scalar
- staff scalar
- station requirement

## `geodes.json`
Defines:
- biome geode
- spawn density
- altitude rules
- vegetation/world-generation parameters
- element weight table
- bonus roll chances

## `socket-rules.json`
Defines:
- prefab overrides
- excluded prefabs
- category overrides
- default socket counts
- maximum sockets

Example:

```json
{
  "overrides": {
    "SwordIron": "Weapon",
    "ShieldBlackmetal": "Armor",
    "Wishbone": "Utility",
    "SomeMod_DebugItem": "Excluded"
  }
}
```

## `socket-effects.json`
Defines all element/category/tier effect magnitudes.

## `staffs.json`
Defines:
- staff recipes
- cost type
- stamina/Eitr cost
- projectile definition
- damage
- AoE
- cooldown
- VFX
- tier behavior

## `runes.json`
Defines rune IDs and rules.

## `galdr.json`
Defines duration, cooldown, resource cost, effect.

## `rituals.json`
Defines ritual requirements, offerings, timing, radius, and outputs.

## `wards.json`
Defines ward effects, fuel use, radius, tick interval.

## `passage.json`
Defines passage restrictions.

## `ships.json`
Defines supported ship prefabs and element effects.

## `resonance.json`
Defines threshold and mixed resonance behavior.

## `totems.json`
Defines duration/radius/effects.

## `spirits.json`
Defines summon types and lifetime.

## `fate.json`
Defines Fate costs and intervention permissions.

## `boss-resonance.json`
Defines boss artifact recipes and effects.

All files carry:

```text
schemaVersion
contentVersion
```

Unknown schema versions cause a clear startup error rather than silent partial loading.

---

# 27. CONFIGURATION FILE

Primary BepInEx config:

`com.mrcalzon02.Magenheim.cfg`

Recommended sections:

```text
[General]
Enabled
DebugLogging

[Networking]
RequireMatchingMinorVersion
ServerAuthoritativeData

[Crystal Shaping]
MaximumFailureReduction
ShardsRequiredForSimpleCrystal
SkillGainMultiplier

[Geodes]
EnableWorldGeodes
EnableExistingWorldRegeneration
GlobalSpawnMultiplier

[Socketing]
WeaponSockets
ArmorSockets
UtilitySockets
AllowExtraction
AllowFreeReplacement

[Staves]
EnableStaves
SimpleUsesEitr
RefinedUsesEitr
GlobalStaffDamageMultiplier

[Runecraft]
Enabled

[Galdr]
Enabled
CooldownMultiplier

[Seidr]
Enabled

[Passage]
Enabled
AllowRestrictedItems

[Ships]
Enabled

[Fate]
Enabled
EnableDeathProtection

[Compatibility]
UnknownItemsDefaultToExcluded
```

Server gameplay entries must be admin-controlled/synchronized.

Purely local entries may include:
- UI scale
- VFX density
- audio volume
- Galdr hotkey
- tooltip verbosity

---

# 28. SERVER DATA SYNCHRONIZATION

JSON files are server-authoritative gameplay data.

On connection:

1. Client and server pass Jötunn mod/version compatibility.
2. Server calculates a normalized data-manifest hash.
3. Server sends `DataManifestRpc`.
4. Client compares local/in-memory definitions.
5. If hashes match, normal connection continues.
6. If hashes differ, server sends compressed authoritative data payload.
7. Client validates schema and references.
8. Client installs server definitions in memory for the session.
9. Client does not overwrite its local disk configuration.
10. Connection completes only after authoritative gameplay data is valid.

Files included in server manifest:
- elements
- tiers
- geodes
- socket rules/effects
- staffs
- runes
- galdr
- rituals
- wards
- passage
- ships
- resonance
- totems
- spirits
- fate
- boss resonance

This prevents damage, refinement, or recipe disagreement among multiplayer participants.

---

# 29. RUNTIME INITIALIZATION ORDER

`MagenheimPlugin.Awake()` should orchestrate registration through `RegistrationPipeline`.

Required order:

1. Initialize logger.
2. Load plugin constants/version.
3. Bind local and server config.
4. Validate config ranges.
5. Register network compatibility.
6. Load static JSON definitions.
7. Validate schema.
8. Build normalized data manifest.
9. Load `magenheim_assets`.
10. Register RPC/network handlers.
11. Register stable custom skills.
12. Register base custom prefabs/items.
13. Register geode inventory items.
14. Register crystal items and shards.
15. Register staff items.
16. Register special items.
17. Register Geologist's Workstation.
18. Register workstation upgrades.
19. Register ritual/ward/light/passage/totem build pieces.
20. Register recipes after all referenced items exist.
21. Register custom status effects.
22. Register world geode vegetation/world-generation entries.
23. Register UI hooks.
24. Register minimal Harmony patches required for per-instance socket effects.
25. Register console/debug commands.
26. Run cross-reference validation.
27. Log concise content totals and schema hash.
28. On multiplayer connection, synchronize authoritative data before gameplay.
29. On world load, validate world-generation registration.
30. On player load, migrate old Magenheim item metadata if necessary.

Do not use Harmony patches where a Jötunn event/manager can perform the same job cleanly.

---

# 30. USER GAMEPLAY ORDER

The intended player path is:

1. Begin normal Meadows progression.
2. See geodes in the world but cannot meaningfully exploit them without mining.
3. Defeat Eikthyr and obtain mining capability.
4. Mine first geode.
5. Build Geologist's Workstation.
6. Crack geode.
7. Obtain first Rough Crystal.
8. Attempt Rough -> Simple shaping.
9. Learn Crystal Shaping.
10. Socket first Simple Crystal or save it for a staff.
11. Enter Black Forest and find different geode weighting.
12. Build Fracturing Block.
13. Begin Refined crystals.
14. Enter Swamp/Mountain progression.
15. Build Faceting Wheel.
16. Reach Advanced crystals.
17. Enter Plains progression.
18. Build Resonance Frame.
19. Reach Master crystals.
20. Build Master staff and/or complete Master socket build.
21. Reach Mistlands materials.
22. Build Rune Engraver.
23. Begin Runecraft.
24. Unlock Galdr.
25. Build Seiðr ritual structures.
26. Build wardstones, passage stones, lights, and totems.
27. Attune ships.
28. Unlock Spirit Binding.
29. Build elemental resonance sets.
30. Defeat bosses / consume trophies into Boss Resonance.
31. Perform Ritual of the Norns.
32. Create Fate Crystals.
33. Construct final boss-resonant and Fate-enhanced builds.

---

# 31. IMPLEMENTATION ROADMAP

## PHASE 0 - Repository and authority documents

Create:
- repository
- solution/project
- `README.md`
- `INSTRUCTIONS.md`
- this design specification
- `IMPLEMENTATION_PLAN.md`
- `BACKLOG.md`
- `CHANGELOG.md`
- `VALIDATION.md`

Acceptance:
- clean build skeleton
- Jötunn dependency resolves
- plugin loads and logs version
- no gameplay content yet

## PHASE 1 - Data and asset foundation

Implement:
- asset bundle loader
- JSON loader
- schemas
- definition classes
- validation
- localization loader
- config
- network compatibility declaration

Acceptance:
- startup loads valid definitions
- invalid definitions fail loudly with file/key
- asset bundle resolves
- localization resolves
- data hash deterministic

## PHASE 2 - First complete vertical slice

Only implement:
- Meadows Geode
- Earth crystal family
- Earth shards
- Geologist's Workstation
- Crystal Shaping skill
- complete Rough -> Master refinement
- failure/shard system
- shard recombination
- one Earth weapon socket effect
- Simple/Refined/Advanced/Master Earth staff

This is the most important milestone.

Acceptance:
- world geode spawns
- one mining hit drops intact Meadows Geode
- workstation cracks geode
- all quality transitions work
- skill changes failure chance correctly
- failure returns shards
- shards recombine
- Earth socket persists through save/load/drop/pickup
- Earth staff family functions
- multiplayer player A can hand socketed item to player B without data loss

Do not expand to eight elements until this passes.

## PHASE 3 - Full geode and crystal catalog

Add:
- remaining seven biome geodes
- remaining seven elements
- all 40 normal crystal items
- all 8 shard items
- full weighting tables
- biome visuals

Acceptance:
- each biome spawns correct geode
- 100,000 simulated weighted rolls match configured distributions within tolerance
- no weight table has invalid/negative/unreachable entries

## PHASE 4 - Adaptive socket framework

Implement:
- classifier
- config overrides
- persistent metadata adapter
- weapon/armor/utility effect pipeline
- socket UI
- extraction
- tooltip rendering

Acceptance:
- one vanilla item from every major type classifies correctly
- shield -> Armor
- modded unknown -> Excluded by default
- explicit override works
- effects apply only to socketed instance
- shared prefab remains unchanged
- metadata survives repair, death, drop, pickup, chest storage, world transfer, multiplayer transfer

## PHASE 5 - Complete elemental socket effects

Implement all 24 elemental category behaviors:
- 8 weapon
- 8 armor
- 8 utility

Acceptance:
- values originate in data
- no element requires bespoke metadata format
- effects stack according to defined cap rules
- no runaway multiplicative loops

## PHASE 6 - Full staff system

Implement:
- all 32 staves
- projectile factory
- costs
- targeting
- all spell VFX
- Master capstones

Acceptance:
- damage authority consistent in multiplayer
- meteor does not strike through prohibited interiors unless configured
- projectiles do not damage owner unexpectedly
- AoE respects PvP rules
- Master effects are visually distinct and materially stronger

## PHASE 7 - Runecraft

Implement:
- `magenheim.runecraft`
- Rune Engraver operations
- six initial rune types
- rune metadata
- runecarving UI
- rune effects

Acceptance:
- rune data independent from socket data
- rune + crystal composition behaves deterministically
- no rune can corrupt unsupported equipment

## PHASE 8 - Galdr

Implement:
- `magenheim.galdr`
- radial menu
- configurable input
- eight elemental Galdr
- cooldown/resource/skill scaling

Acceptance:
- server validates activation
- cooldown cannot be bypassed by relog/equip spam
- no effect remains permanently after expiry

## PHASE 9 - Seiðr

Implement:
- `magenheim.seidr`
- ritual circle validation
- ritual offering transactions
- six initial rituals
- ritual networking
- ritual UI

Acceptance:
- offerings consumed exactly once
- interruption has defined behavior
- ritual cannot duplicate outputs on disconnect
- map/divination effects are bounded

## PHASE 10 - Wardstones

Implement:
- ward piece
- crystal installation
- matching shard fuel
- eight elemental effects

Acceptance:
- unloaded ward does not burn fuel incorrectly
- effect radius deterministic
- overlapping wards obey stacking rules
- structures/players outside radius unaffected

## PHASE 11 - Passage Stones

Implement:
- runestone piece
- pairing
- validation
- teleport rules
- optional restricted-item setting

Acceptance:
- no bypass of server policy
- vanilla portals remain unchanged
- mismatch state gives clear user feedback
- no item duplication through interrupted teleport

## PHASE 12 - Ship Attunement

Implement:
- Runic Keelstone
- persistent ship metadata
- supported vanilla ships
- eight effects

Acceptance:
- ship persists attunement after save/load
- ownership/network handling correct
- ship destruction defines crystal recovery/destruction behavior
- unsupported modded ship fails safely

## PHASE 13 - Crystal Lighting

Implement:
- lantern
- brazier
- socket interaction
- elemental VFX

Acceptance:
- no excessive network updates
- light count/performance acceptable
- light secondary effects weaker than wardstone

## PHASE 14 - Resonance

Implement:
- same-element thresholds
- mixed resonance resolver
- status effects
- UI display

Acceptance:
- deterministic resolution
- equipment swap updates promptly
- duplicate equip events cannot stack ghost resonance
- only configured mixed resonance count applies

## PHASE 15 - Rune Totems

Implement:
- socketable totem
- timed world object
- eight fields

Acceptance:
- timer persists/reconciles correctly
- expired totems clean themselves up
- no permanent orphan ZDOs
- PvP rules honored

## PHASE 16 - Spirit Binding

Implement:
- Spirit Fetishes
- three initial spirit types
- summon ownership
- temporary lifetime
- loot suppression

Acceptance:
- one summon cap works
- no permanent tame-state corruption
- summons vanish/resolve correctly on logout/death/world change
- no normal loot farming

## PHASE 17 - Fate

Implement:
- Fate Shard
- Fate Crystal
- Ritual of the Norns
- Thread Unbroken
- Second Thread
- Farseeing
- optional Marked Return

Acceptance:
- no recursive reroll
- no duplication on crash/reconnect
- guarantee state is consumed transactionally
- death protection cannot clone inventory

## PHASE 18 - Boss Resonance

Implement all eight capstones:
- Eikthyr
- Elder
- Bonemass
- Moder
- Yagluth
- Queen
- Fader
- Kall Fimbulbringer

Acceptance:
- trophy consumption obeys config
- artifacts persist correctly
- each has unique effect
- no artifact silently reduces into ordinary Master crystal behavior

## PHASE 19 - Compatibility and hardening

Implement:
- external equipment override examples
- compatibility diagnostics
- migration tests
- old-save tests
- dedicated server soak
- performance profiling
- balance pass

Acceptance:
- no errors in clean dedicated-server boot
- no missing prefab refs
- no divergent data hashes
- no known item metadata loss
- no geode worldgen spam
- stable 4+ player test session
- acceptable FPS and network cost around multiple wards/lights/totems

---

# 32. TEST MATRIX

Every release candidate must cover:

## Persistence
- inventory save/load
- chest save/load
- item drop/pickup
- item repair
- player death
- corpse recovery
- character moved between worlds
- world moved between host/dedicated server
- item transferred to another player

## Multiplayer
- host + client
- dedicated server + 2 clients
- mismatched mod version
- mismatched local JSON
- admin config change
- join in progress
- disconnect during refinement
- disconnect during ritual
- disconnect during Fate intervention

## World generation
- new world
- existing world with unexplored zones
- existing explored biome
- all eight biomes
- geode density multipliers 0, default, high
- geodes do not spawn floating/in inaccessible invalid positions

## Compatibility
- vanilla equipment
- modded weapon
- modded armor
- modded utility
- explicitly excluded item
- item with unknown type
- modded ship override
- unusual tooltip/quality item

## Combat
- PvE
- PvP off
- PvP on
- boss target
- resistant target
- immune target
- friendly/tamed target
- summoned spirit target selection

---

# 33. DEVELOPMENT CONSOLE COMMANDS

Admin/debug-only commands:

```text
magenheim.status
magenheim.data.hash
magenheim.data.reload
magenheim.geode.spawn <biome>
magenheim.crystal.give <element> <tier> [count]
magenheim.shard.give <element> [count]
magenheim.skill.set crystal_shaping <0-100>
magenheim.skill.set runecraft <0-100>
magenheim.skill.set galdr <0-100>
magenheim.skill.set seidr <0-100>
magenheim.socket.inspect
magenheim.socket.clear
magenheim.resonance.inspect
magenheim.ritual.debug
magenheim.ship.inspect
magenheim.fate.inspect
```

Debug commands must not become required gameplay paths.

---

# 34. LOCALIZATION KEY CONVENTIONS

Examples:

```text
$item_magenheim_crystal_fire_rough
$item_magenheim_crystal_fire_rough_desc

$item_magenheim_staff_fire_master
$item_magenheim_staff_fire_master_desc

$piece_magenheim_geologist_workstation
$piece_magenheim_geologist_workstation_desc

$skill_magenheim_crystal_shaping
$skill_magenheim_crystal_shaping_desc

$effect_magenheim_resonance_inferno
$effect_magenheim_resonance_inferno_desc
```

Prefab names and localization keys must never be conflated.

---

# 35. MIGRATION POLICY

Persistent schema version starts at:

`1`

Every item carrying Magenheim metadata also carries:

`magenheim.data_version=1`

Migration rule:

1. Read version.
2. If current, use normally.
3. If known older version, migrate through ordered migration steps.
4. Write current version only after successful migration.
5. If future/unknown version, do not destructively rewrite.
6. Log a clear compatibility error.

Never "fix" unknown item metadata by deleting it.

---

# 36. PERFORMANCE RULES

- No Update-loop scan of every inventory item every frame.
- Recalculate resonance only when equipment/state changes.
- Wardstones/totems use sensible tick intervals rather than per-frame scans.
- Geodes are world-generation objects, not runtime procedural search jobs.
- Lighting VFX has client-side quality options.
- Master spell projectiles have hard count/lifetime caps.
- Ritual searches use bounded queries and cached world data where appropriate.
- Configuration reload validates once and swaps immutable definition snapshots rather than repeatedly reparsing JSON during combat.

---

# 37. SAVE-SAFETY RULES

Magenheim must never intentionally delete an unknown item because a definition is missing.

If an element definition disappears:
- keep metadata
- disable effect
- show `Unknown Magenheim Crystal` diagnostic tooltip if necessary
- log missing definition

If the mod is removed entirely, normal modded-content loss risks still apply; documentation must warn players before loading valuable worlds without Magenheim installed.

---

# 38. FIRST PLAYABLE RELEASE SCOPE

The first internal playable build is not "all eight elements."

It is exactly:

- Magenheim plugin boots
- Meadows Geode world object
- Meadows Geode inventory item
- Earth element
- Rough Earth Crystal
- Simple Earth Crystal
- Refined Earth Crystal
- Advanced Earth Crystal
- Master Earth Crystal
- Earth Crystal Shard
- Geologist's Workstation
- Fracturing Block
- Faceting Wheel
- Resonance Frame
- Crystal Shaping skill
- refinement formula
- failure/shards
- shard recombination
- one socketable weapon path
- one armor path
- one utility path
- full Earth staff family
- save/load
- multiplayer transfer
- server data hash/sync

Nothing else should be allowed to distract from proving that vertical slice.

Once that slice passes validation, the rest of Magenheim is expansion of a functioning architecture rather than repeated reinvention.

---

# 39. RELEASE MILESTONES

## 0.1 - Geological Proof
One-biome Earth vertical slice.

## 0.2 - Eightfold Stone
All eight biomes/elements/geodes.

## 0.3 - Bound Steel
Full socket system.

## 0.4 - Crystal Arms
Full staff system.

## 0.5 - Written Power
Runecraft.

## 0.6 - Voices of Power
Galdr.

## 0.7 - Seiðr
Ritual magic.

## 0.8 - Hearth and Horizon
Wards, lights, passage, ship attunement.

## 0.9 - Resonant Dead
Resonance, totems, Spirit Binding.

## 0.10 - Threads of Fate
Fate magic.

## 0.11 - Hearts of the Forsaken
Boss Resonance.

## 1.0 - Magenheim
Hardening, compatibility, balance, migration, dedicated-server validation, final art/audio/localization pass.

---

# 40. DEFINITION OF DONE FOR MAGENHEIM 1.0

Magenheim 1.0 is complete only when:

- all eight biome geodes generate correctly;
- all 40 normal crystal items exist;
- all eight shard types exist;
- Crystal Shaping levels and correctly modifies failure;
- failed refinement produces recoverable shards;
- Geologist's Workstation and all four upgrades function;
- socketing works on vanilla and configurable modded equipment;
- all 24 category-specific elemental effects function;
- all 32 normal elemental staves function;
- Master staves have unique capstone attacks;
- Runecraft works;
- Galdr works;
- Seiðr rituals work;
- Wardstones work;
- Passage Stones work;
- ship attunement works;
- crystal lighting works;
- resonance works;
- Rune Totems work;
- Spirit Binding works;
- Fate works;
- all eight Boss Resonance artifacts work;
- server-authoritative configuration synchronizes;
- item metadata survives all persistence tests;
- no system relies upon mutating shared item prefab state for per-item effects;
- existing-world behavior is documented;
- migration policy is tested;
- dedicated-server multiplayer is validated;
- no known duplication exploit exists in refinement, ritual, Fate, socket extraction, or portal operations;
- content and balance are controlled through static definitions rather than runtime-generated recipe trees;
- Magenheim remains recognizable as a Valheim progression mod rather than a detached spell-selection UI.

---

# 41. IMPLEMENTATION STARTING POINT

The first code to write should be, in order:

1. `MagenheimPlugin.cs`
2. `PluginConstants.cs`
3. `MagenheimConfig.cs`
4. `MagenheimDataLoader.cs`
5. `ElementDefinition.cs`
6. `CrystalTierDefinition.cs`
7. `DefinitionValidator.cs`
8. `AssetBundleLoader.cs`
9. `SkillRegistrar.cs`
10. `CrystalShapingSkill.cs`
11. `CrystalItemRegistrar.cs`
12. `GeodeItemRegistrar.cs`
13. `GeodeWorldRegistrar.cs`
14. `GeologistStationRegistrar.cs`
15. `CrystalCrackingService.cs`
16. `RefinementRoller.cs`
17. `CrystalRefinementService.cs`
18. `ShardRecoveryService.cs`
19. `EquipmentClassifier.cs`
20. `SocketMetadataStore.cs`
21. `SocketService.cs`
22. `WeaponCrystalEffects.cs`
23. `StaffItemRegistrar.cs`
24. `StaffAttackController.cs`
25. `EarthSpells.cs`
26. persistence tests
27. multiplayer synchronization tests

The first asset work should be, in order:

1. Geologist's Workstation blockout
2. Meadows Geode
3. Earth Rough Crystal
4. Earth Simple Crystal
5. Earth Refined Crystal
6. Earth Advanced Crystal
7. Earth Master Crystal
8. Earth Crystal Shard
9. Earth staff model
10. Earth projectile/VFX family
11. workstation upgrade blockouts

Do not model all eight geode sets before the first one actually spawns, mines, cracks, saves, synchronizes, and produces a working socket and staff.

That is the foundation of Magenheim.
=======
# Magenheim Design Specification

## Core fantasy

Magenheim makes magic begin as geology. Biomes surface geodes as discoverable world objects. Geodes crack into elemental Rough Crystals, which are shaped through Simple, Crystal, Advanced, and Master grades. Crystal Shaping is a permanent player skill and refinement becomes progressively more difficult. Equipment can accept crystals through a socket system whose bonuses adapt to the item category rather than requiring destructive replacement of foreign items.

## World-generation invariants

Worldgen integration is additive. Magenheim may add its own locations, vegetation, clutter, prefabs, and metadata, but must not delete, rewrite, disable, reorder, or otherwise mutate vanilla or third-party worldgen entries merely to obtain compatibility.

Every Magenheim worldgen registration uses a stable namespaced key beginning with `magenheim.`. Before an addition is executed, integration code must observe existing registrations and produce a plan. A conflicting key is skipped by default and diagnosed. The planner operates on snapshots and never mutates observed collections.

Biome area is treated as an explicit flags contract. Supported values are `Median`, `Edge`, and `All` (`Median | Edge`). `None`, negative/unknown bits, and values outside the known mask are invalid. Configuration determines whether invalid values are rejected, clamped to known bits, or replaced by `All`; the default is reject. A clamp that produces `None` is still invalid.

Registration timing follows the host API. Additions must be performed once per registration lifecycle and guarded against duplicate injection. Observing a new world load does not grant permission to duplicate already-added custom content. Any future behavior that intentionally modifies vanilla content requires a separately documented feature and must never be smuggled through the compatibility layer.

## Compatibility architecture

Compatibility has three layers. The pure planning layer validates Magenheim definitions and compares them with observed registrations. The adapter layer translates approved additions to Jötunn/Valheim types. The execution layer performs only approved Magenheim additions and records what it attempted. This separation makes conflicts inspectable and keeps foreign data out of mutation paths.

Configuration is conservative by default:

- mode: `additiveOnly`
- duplicate registration: `skip`
- invalid area: `reject`
- mutate vanilla entries: `false`
- mutate foreign entries: `false`
- unknown integration owner: treat as foreign
- diagnostics: enabled

Compatibility configuration may make Magenheim less intrusive, but cannot enable destructive foreign mutation. A future explicit interoperability module may read another mod's public API and add Magenheim data through that API, provided the other mod owns the mutation and the integration remains opt-in.

## Geodes and crystal progression

Each biome can define one or more geode families and weighted elemental results. Meadows begins with an Earth-dominant geode slice. Each cracked geode produces at least one Rough Crystal; additional drops are independent rolls so quantity and element selection remain separable. Weighted tables must sum to a positive total and may be normalized mathematically rather than requiring exactly 100.

Crystal grades are Rough, Simple, Crystal, Advanced, and Master. Refinement attempts use Crystal Shaping skill, station requirements, recipe inputs, and a grade-specific failure chance. Failure increases cumulatively by 10 percentage points for each higher refinement step unless a data definition explicitly supplies a validated replacement curve. Master is the most failure-prone grade and grants the strongest elemental contribution.

Failure behavior must be data-driven and explicit about whether input is destroyed, downgraded, or converted to shards. No integration may silently duplicate output after a failed transaction.

## Adaptive equipment socketing

Sockets are attached through Magenheim-owned metadata or sidecar state rather than by replacing the underlying foreign item definition. The compatibility system classifies an item by observable capabilities and category, then selects an elemental effect profile appropriate to weapons, armor, shields, tools, utility items, or other supported equipment.

Unknown equipment remains usable. If classification cannot be made safely, socketing is refused with a diagnostic rather than mutating or cloning the foreign item. Existing item stats remain authoritative; Magenheim adds calculated modifiers through its own effect layer.

## Networking and authority

Worldgen definitions, refinement outcomes, socket changes, item consumption, item grants, and skill experience are authoritative transactions. Client presentation may predict or preview, but persisted changes require server-approved state. Compatibility fingerprints should cover gameplay-significant definitions so mismatched peers can be rejected or warned before world state diverges.

## Acceptance principle

No feature is complete because a source file exists. Completion requires observed evidence appropriate to the layer: deterministic core tests for pure logic, compilation for adapters, controlled runtime loading for registration, and disposable-world generation checks for worldgen. Valuable saves are never the first validation target.
>>>>>>> origin/master
