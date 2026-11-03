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
