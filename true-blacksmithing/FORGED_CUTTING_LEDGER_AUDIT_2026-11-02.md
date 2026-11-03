# True Blacksmithing — forged-cutting material-ledger audit (2026-11-02)

**Scope:** offline, read-only Gate 4/5 integrity investigation. **Status: BLOCKED — conservation cannot yet be proved.** This is not an activation approval, a gameplay manufacturing implementation, or a replacement for the complete registered-session Gate 0 census.

## Verified source and grammar

- Source on `main` before this audit: `0e10b05898f909c19405d46baf225bf6c8f4abcc`.
- Authored grammar blob: `995bf9be3146759643cc9a614cdc52fd027bf5cb`; native census blob: `d83a12a0dcd2d71ef028a12d14e90689200db51f`.
- The actual authored grammar contains **16 workshop owners, 13 component families and 9 route families**.
- `forged-cutting` declares **7 equipment families** (Axe, Pickaxe, Knife, Sword, Spear, Polearm, Tool), **7 operations** and **7 component families**. Every listed operation has an owner and every listed component resolves.
- Two referenced reusable components are produced outside the route's own operation list: `wood-forms` at `carving` and `hide-forms` at `leather`. This is **not itself a defect**: upstream fabrication may be intentional. The route has no quantified producer/consumer edges to establish their material cost.
- The existing `ManufacturingCatalog` validates native per-quality input amounts, including the quality-four discontinuity; the `Recipe_SwordIron` test anchors 20 Iron base and 10/20/40 quality costs. **These are original vanilla budgets, not redistributed manufacturing budgets.**

## Repeatable diagnostic

`python tools/audit-blacksmithing-ledger.py` reads the checked-in JSON catalog and grammar without mutating either, inventories all enabled Replace/Adapt candidates whose equipment families overlap forged-cutting, and checks native ingredient identity, duplicate resources, nonnegative costs, quality-by-quality native arithmetic, accidental zero-input base outputs, route operation ownership, and referenced component ownership. It reports counts and source SHA-256s in JSON.

**Important:** family overlap is *not* proof of route ownership. Bronze/cast and exotic variants can share the same equipment family. The tool deliberately does not assign those candidates to forged-cutting or invent transformation costs. Its conservation result is always `false` and its exit status is **2** until the missing authored inputs exist. Do not add it as a release/build gate while the optional manufacturing module remains default-disabled. The diagnostic detects malformed native data separately from missing authored conservation data.

`python -m unittest discover -s tests -p test_blacksmithing_ledger_audit.py` exercises the **real authored forged-cutting grammar** against a deliberately small, known native Iron Sword fixture, then tests quality-four corruption, zero-cost output, duplicate resource, unknown component and duplicate JSON properties. The fixture is not a substitute for a full native-catalog run.

## Missing authoritative inputs — no guessed amounts

1. Explicit per-recipe route selection tied to material condition; an equipment family alone admits multiple possible routes.
2. Per-recipe redistributed material, secondary input, fuel/process and upgrade/quality budgets, with an explicit comparison to each native budget.
3. Quantified input/output yields and upstream producer dependencies for intermediate component forms, including carved wood and leather forms.
4. A registered-session vanilla/Magenheim recipe/resource reconciliation that identifies actual live targets, ownership and station dependencies.

The repository's existing development state explicitly says Gate 4 has **native budgets only**, Gate 5 lacks concrete transformations, and the registered-session Gate 0 census is outstanding. The large native JSON file's contents could not be retrieved into this executor's local runtime, so **no full-catalog execution count or full Core/.NET build pass is claimed** here. The authored grammar was independently read and analyzed, and synthetic audit assertions were executed separately.

**Next:** obtain the complete checked-in catalog in a capable development runner, execute the new Python tests and audit, record actual candidate/cost/quality counts and any native-data defects, then author the missing route bindings and conserved budgets before enabling manufacturing. Preserve the default-disabled switch and existing Valheim/Magenheim recipes.

## 2026-11-03 — complete checked-in census readback and pickaxe classification gap

**Source readback:** On `main` baseline `7fc857b48b1df8b7eb5123e1b653c955c230da41`, retrieved the **entire** `vanilla-catalog.json` blob (1,180,441 UTF-8 bytes, Git blob `d83a12a0dcd2d71ef028a12d14e90689200db51f`) and complete `manufacturing-grammar.json` (8,962 UTF-8 bytes, Git blob `995bf9be3146759643cc9a614cdc52fd027bf5cb`) through the connected GitHub read interface. Independent UTF-8 SHA-256 calculations, with a standard `abc` hash self-test, produced:

- Native catalog SHA-256: `4e050cd1655da93a94394e9acc8f8fe79f9e0d965df94c2204b13cf5e8a6698f`.
- Authored grammar SHA-256: `1c9ec9da6a1a87289eb89ad4a481fca2699f61d389e2edd556beb1a07c5cf1fb`.

**Complete-catalog findings (independent in-memory reproduction of the Python audit's native arithmetic and candidate selection; NOT a successful invocation of the repository Python command):** 1,519 item entries; 344 recipe entries; 296 enabled and 48 disabled; 269 Replace, 27 Adapt, 48 Exempt. The forged-cutting **family-overlap candidates** number **101** (21 Axe, 10 Knife, 25 Sword, 13 Spear, 7 Polearm, 25 Tool, **0 Pickaxe**), comprising **384 native cost rows** and **1,370 quality entries**. The mirrored native cost, resource-identity, output-identity, quality-length, native quality-arithmetic and nonzero-base checks reported **zero errors** among those 101 candidates. This is **not** proof of conservation, not a registered-session census, and not a complete validation of all 344 recipes.

**Concrete gap discovered:** Four enabled replacement recipes — `Recipe_PickaxeAntler`, `Recipe_PickaxeBlackMetal`, `Recipe_PickaxeBronze` and `Recipe_PickaxeIron` — have native `family: "Hammer"` despite `Pickaxe*` output prefabs. The disabled/exempt `Recipe_PickaxeStone` also has native family Hammer, but is correctly excluded from enabled candidates. Thus a route-family-only filter misses all four enabled pickaxes, even though the authored forged-cutting grammar names `Pickaxe`. This reflects the checked-in **native classification**, not an instruction to rewrite that source catalog or to assign a manufacturing route. Bronze and antler pickaxes may require different operations from iron, so no inferred forged-cutting membership is approved.

**Bounded diagnostic repair:** `tools/audit-blacksmithing-ledger.py` now emits `native_pickaxe_family_exclusions` with each enabled named pickaxe omitted by the family filter, without changing its candidate count, status, exit code, activation, or conservation claim. `tests/test_blacksmithing_ledger_audit.py` adds a synthetic Hammer-family PickaxeIron case asserting it is reported but **not** silently assigned. A small independent Python execution of the exact exclusion expression with enabled/disabled controls passed. The complete-catalog readback independently found the four named exclusions. Source commits: `a2bb819e8834aacfb54ff87dda008e5924da7520` (diagnostic) and `4228cfbcb6b8560dda23c0a862b799657093e392` (regression).

**Unverified acceptance / blocker:** The complete Git checkout failed in this executor's container because `github.com` DNS resolution was unavailable. Consequently **`python -m unittest discover -s tests -p test_blacksmithing_ledger_audit.py` and `python tools/audit-blacksmithing-ledger.py` were NOT run against the full checked-out catalog**. No Python suite pass, exact command exit code, full .NET build, live Valheim session, or conserved material budget is claimed. The documented diagnostic exit status is intentionally 2 even for a clean native census while authored budgets are missing. The optional module stays disabled, and this diagnostic must not block releases.

**Next:** In a complete checkout, execute both exact Python commands, verify that the new exclusion list contains four enabled pickaxes, reconcile the 101 candidate/384 cost/1,370 quality counts and both SHA-256s, and investigate any divergence. Then author explicit material-conditioned recipe-route bindings and conserved intermediate/upgrade budgets from authoritative data before enabling any manufacturing.
