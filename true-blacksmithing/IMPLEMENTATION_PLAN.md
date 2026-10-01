# True Blacksmithing — Full-System Staged Implementation Plan

## Objective

Implement True Blacksmithing as a complete optional Magenheim subcomponent that replaces abstract equipment recipes with physical manufacturing processes while normally preserving the existing finished Valheim or Magenheim item prefab.

The target is a full-system production pass followed by an integrated test campaign, not a chain of tiny one-item gameplay proofs.

## Gate 0 — Authoritative catalog

Extract a machine-readable catalog of every targeted current vanilla:

- weapon;
- tool;
- shield;
- ammunition family;
- armor piece;
- upgrade recipe;
- crafting station;
- station level;
- repair station;
- metal;
- wood;
- hide;
- textile;
- exotic equipment material.

Classify each target as KEEP, REPLACE, ADAPT, PROCESS, or EXEMPT.

Independently inventory current Magenheim equipment and crafting dependencies so compatibility work uses authoritative current source rather than memory.

**Gate:** no targeted recipe remains unclassified.

## Gate 1 — Manufacturing grammar

Freeze the operation families before mass recipe authoring:

- hand work;
- knapping;
- carving;
- leatherworking;
- textile work;
- casting;
- heating;
- forging;
- armor forming;
- quenching;
- tempering where justified;
- grinding;
- polishing/finishing where justified;
- fitting;
- assembly;
- exotic treatment.

A station exists only when it represents a distinct physical transformation.

Not every item visits every operation.

## Gate 2 — Station architecture

Define and author the required workshop:

- hand crafting;
- knapping block;
- carving bench/shaving horse;
- workbench assembly;
- leatherworking/tanning capability;
- forge/hearth;
- casting setup;
- smithing anvil;
- armorer tooling where justified;
- quench trough;
- grinding wheel;
- existing textile infrastructure;
- artisan/advanced machinery;
- Black Forge;
- magical stations only where materially appropriate.

Choice operations may use recipe menus. Deterministic process operations such as quenching should prefer direct interaction rather than another menu click.

**Gate:** every planned station has a distinct job and every job has a station/process owner.

## Gate 3 — Component taxonomy

Create reusable component families rather than item-specific clutter.

### Primitive
Stone/flint flakes, primitive blades, axe heads, spear points, bindings.

### Wood
Short handles, tool hafts, heavy hafts, long shafts, polearm shafts, grip cores, bow staves, crossbow stocks, shield bodies.

### Leather/textile
Cured hide, straps, grip wraps, harnesses, armor lining, reinforced panels, cloth/padded components.

### Metal
Stock/billets where justified, rivets, fasteners, rings, guards, pommels, sockets, bosses, rims/fittings.

### Weapon forms
Knife blades, blade blanks, forged blades, hardened blades, finished blades, axe heads, pick heads, mace/hammer heads, spearheads, polearm heads.

### Armor forms
Plate blanks, formed plates, helmet forms, mail rings/sections, reinforcement plates, linings, fastening assemblies.

### Ammunition
Arrow shafts, fletching, arrowheads, bolt bodies and bolt heads, with batch operations.

An intermediate item is admitted only if it changes physical process, is reused, represents a real technology gate, or materially communicates construction.

## Gate 4 — Material ledger

For every replaced vanilla recipe record:

- original material budget;
- redistributed True Blacksmithing budget;
- secondary material additions;
- fuel/process costs;
- upgrade costs.

De-abstraction must not accidentally become arbitrary ore inflation. Manufacturing complexity is paid mainly through infrastructure and process, not strip-mining.

**Gate:** no catastrophic cost inflation and no free-resource loops.

## Gate 5 — Data architecture

Implement strongly typed definitions for:

- manufacturing stations;
- processes;
- components;
- recipe replacements;
- upgrade replacements;
- direct processes;
- material budgets;
- equipment families;
- compatibility rules;
- asset references.

No tuples or `System.ValueTuple`.

Production definitions should ship as stable authored data. Runtime code registers/applies them; it should not regenerate the design on every boot.

## Gate 6 — Asset production

Produce the complete required asset family before gameplay testing:

- station models;
- UVs/materials/textures;
- colliders and interaction points;
- build ghosts/icons;
- intermediate item models;
- inventory icons;
- drop visuals;
- localization.

Reusable geometry is encouraged. Intermediate states must visually read as unfinished/processed objects rather than recolored generic ingots.

Generation scripts may be used to create the production assets, but are development tools rather than a permanent manufacturing pipeline.

## Gate 7 — Core runtime

Implement:

- subsystem bootstrap;
- startup config;
- fail-safe activation host;
- asset loading;
- item registration;
- station/piece registration;
- manufacturing registry;
- vanilla recipe replacement engine;
- direct-process station handling;
- unlock integration;
- multiplayer authority;
- localization;
- diagnostics.

All recipe mutation must respect `FAIL_SAFE_CONTRACT.md`.

## Gate 8 — Full vanilla conversion

Convert the entire target catalog before the first integrated gameplay test:

- primitive tools;
- axes;
- pickaxes;
- knives;
- swords;
- clubs/maces;
- hammers/sledges;
- spears;
- atgeirs/polearms;
- bows;
- crossbows;
- shields;
- ammunition;
- leather/light armor;
- metal armor;
- textile/padded armor;
- exotic/endgame equipment.

Preserve appropriate differences in manufacturing grammar. Do not force bows, maces, plate armor, and magical gear through the same chain merely for consistency.

## Gate 9 — Upgrades

Replace abstract raw-material quality upgrades with manufactured improvement components or meaningful workshop processes while retaining vanilla quality mechanics.

Examples include improved fittings, reinforcement plates, replacement/refined blade components, improved grips, linings, mail sections, or shield reinforcement.

## Gate 10 — Magenheim integration

When True Blacksmithing is disabled, Magenheim recipes remain normal.

When enabled and validated, adapt appropriate Magenheim crafting so mundane construction and Crystal Shaping intersect without replacing Magenheim's ownership of:

- crystal qualities;
- Crystal Shaping;
- elemental identity;
- custom resources;
- weapon behavior;
- Underworld progression.

Existing dependency rules remain authoritative. Underworld weapon families must continue to build from prior crystal equipment where designed.

## Gate 11 — Pre-boot integrity pass

Before launching the game, require:

- zero compile errors;
- zero prohibited tuple syntax;
- valid prefab references;
- valid station references;
- valid localization references;
- complete asset references;
- no recipe dependency cycles;
- no orphan required components;
- no unreachable final equipment;
- no unintended vanilla shortcut;
- valid upgrades;
- reconciled material ledgers;
- successful Magenheim-present compatibility resolution;
- successful Magenheim-absent isolation;
- fail-safe rollback coverage for every replacement mutation.

This gate is an integrity pass, not a separate shipping verifier product.

## Gate 12 — Ready-to-test package

Package the complete plugin, assets, data, localization, config defaults, and compatibility layer.

At this point the feature may be called implementation-complete but **not yet empirically proven**.

## Gate 13 — First integrated game campaign

Only now perform the deliberate full-system gameplay test.

### Configuration A: Magenheim with True Blacksmithing enabled

Exercise the production graph from primitive equipment through current endgame using developer spawning where appropriate to avoid wasting time on resource gathering.

Validate all station families, equipment families, upgrades, repairs, unlocks, save/reload, and bypass removal.

### Configuration B: True Blacksmithing disabled

Confirm baseline Magenheim/Valheim crafting remains available and no True Blacksmithing mutation leaks into the session.

### Multiplayer

Validate authoritative activation, station use, direct processes, inventory synchronization, config agreement, save/reload, and failure behavior.

## Gate 14 — Consolidated defect harvest

Finish a broad enough test sweep to identify systemic defects before repairing isolated symptoms.

Group failures by:

- registration;
- recipes;
- assets;
- station interaction;
- unlocks;
- material economy;
- networking;
- compatibility;
- visuals;
- performance.

Fix root causes in batches.

## Gate 15 — Regression

Recompile, rerun the pre-boot integrity gate, repackage, and repeat integrated validation with True Blacksmithing enabled and disabled.

Further cycles exist only for actual defects, not ceremonial micro-checkpoints.

## Completion

True Blacksmithing is complete when the entire targeted recipe catalog has a deliberate manufacturing route, the required stations and assets exist, upgrades work, old shortcuts are removed, current endgame is reachable, optional Magenheim integration works, disabled mode preserves baseline crafting, multiplayer behavior is authoritative, and the fail-safe contract survives integrated regression.
