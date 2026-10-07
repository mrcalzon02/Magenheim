# True Blacksmithing Data

This directory is reserved for stable authored manufacturing definitions and catalogs.

Expected production data includes the authoritative vanilla target catalog, manufacturing station/process definitions, component definitions, recipe replacements, upgrade replacements, material ledgers, direct-process definitions, and Magenheim compatibility mappings.

Runtime should consume finished data rather than regenerate the manufacturing design on every boot.

`vanilla-catalog.json` is extracted from the installed vanilla assets and retains
disabled candidates separately. `manufacturing-grammar.json` is the authored
operation/job/component-family design. Neither is a recipe replacement plan.
See `../DEVELOPMENT_STATE.md` for current gate boundaries and reconciliation work.
