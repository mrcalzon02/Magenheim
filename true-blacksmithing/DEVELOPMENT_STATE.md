# True Blacksmithing development — 2026-10-02 / 0.0.167

The internal full-system plan remains authoritative. This development pass begins
the catalog/data foundation; it does not claim manufacturing implementation or
activate a partial replacement graph. All 0.0.166 repairs remain included.

## Implemented foundation

- Installed vanilla asset extraction captures 344 equipment recipes, 1,519 item
  and ingredient prefabs and ten native crafting stations. Each candidate is
  classified with a reason: 269 recipe replacements, 27 adaptations and 48 exempt
  disabled recipes. Existing finished prefab identities remain authoritative.
- The catalog includes baseline and per-quality material budgets, special
  upgrader resources, alternative-ingredient flags, batch amounts, quality output
  multipliers, craft/repair stations, required levels, upgrade-only flags and DLC
  identity. This is an original-budget ledger, not yet a redistributed budget.
- Source SHA-256s identify the installed assembly, extended manifest and recipe
  bundle. `tools/extract-blacksmithing-catalog.py` is development tooling; startup
  consumes the shipped JSON rather than extracting or inventing recipe design.
- Typed core definitions cover equipment, material ledgers, station baselines,
  workshop operations, reusable component families and material-dependent routes.
- The authored grammar defines sixteen physical jobs, thirteen reusable component
  families and nine route families. Bronze casting, ferrous heat treatment,
  striking heads, wood composites, garments, formed armor, batch ammunition and
  exotic equipment retain distinct processes. Quenching/tempering specify direct
  interaction; this interaction is planned, not implemented yet.
- Integrity checks reject duplicate identities, unresolved stations/resources,
  incorrect quality ledgers, missing operation owners, undefined/orphan component
  families and uncovered equipment families. JSON loading rejects unknown fields,
  unknown/integer enum values and duplicate properties.
- `magenheim_blacksmithing status` inspects the shipped definitions and actual
  optional module state. `magenheim_blacksmithing catalog`, after normal world
  registration, captures observed vanilla/Magenheim equipment recipes and their
  actual registered ingredients into a new timestamped config file. It performs
  no recipe, inventory, world or save mutation and does not activate the module.
  Valheim's existing devcommands gate applies.

## Evidence boundaries and next work

The offline vanilla catalog is a **serialized candidate census**, not proof that
every asset is in the live ObjectDB. Disabled assets are explicitly exempt; items
without an authored recipe retain their existing acquisition route. Enemy/internal
item prefabs can appear in the broader ingredient census and are not silently
promoted into craftable equipment.

Gate 0 still needs the registered-session census and reconciliation of current
Magenheim outputs, dynamically resolved donors, costs, stations and dependencies.
The read-only command captures those facts from actual registrations rather than
guessing them from recipe-building source. Foreign ingredients are observed, and
foreign equipment/recipe authority is never inferred from the output name.
Do not use this observation snapshot directly as a replacement plan.

Gate 1 has its initial authored grammar. Gate 2 has physical job ownership, but
station models, placement, interactions and unlocks remain to be implemented.
Gate 3 has family taxonomy, not final per-material component prefabs. Gate 4 has
native budgets, not redistributed costs. Gate 5 now has typed baseline/grammar
data, but concrete transformations, replacement/upgrade definitions and asset
references must follow.

Next: reconcile the complete registered vanilla/Magenheim target set; author the
full material-specific transformation graph and conserved material ledgers; build
the coherent workshop and component asset family; connect native crafting,
interaction and server authority through the existing fail-safe journal. Preserve
all original progression stations and prior crystal-equipment prerequisites.

The subsystem stays default-disabled and does not convert recipes even if its
startup switch is enabled at this stage. Full preflight, complete production
assets/graph, authoritative peer agreement and reversible mutation coverage are
required before activation. The first deliberate gameplay campaign remains after
the full-system ready-to-test gate, as the implementation plan requires.

## Offline validation

Final `build.ps1 -Offline` passed with zero runtime compile warnings/errors,
114,218 core assertions plus module suites, including twenty new manufacturing
catalog/grammar and malformed-data checks. Existing production checks also pass:
531 model sets, 224 icon checks, 974 generated files across 28 generators,
94 explicit Harmony targets, 23 dynamic resolvers covering 676 targets and
83 direct/46 helper reflection contracts. Online vulnerability auditing was
unavailable in the offline build. The ignored build evidence is
`dist/blacksmithing-167-final-build.log`.

No gameplay manufacturing test or live catalog capture was performed. Registered
Magenheim reconciliation remains pending and is not inferred from compilation.

Active Central Fuckery profile closeout installed and enabled 0.0.167 with verified
payload and launcher catalog. Installed runtime SHA-256:
`AE47F136779AFD9B7A31BB1AFEA214914E1C9F020439311DCB4289AC7C6801A9`.
Prior installation/catalog backups are
`backups/Local-Magenheim-20261002-095523.zip` and
`backups/mods-20261002-095548-655.yml`. Both installed manufacturing JSON files
match their source hashes. Existing adapter 1.0.13 runtime hash still matches its
receipt. Install evidence is ignored `dist/blacksmithing-167-install.log`.

Verified launcher description: “0.0.167: Begins True Blacksmithing with the
installed vanilla recipe/material catalog, typed workshop grammar, integrity
checks and read-only registered recipe capture. Manufacturing stays inactive
until the complete production graph is ready.”
