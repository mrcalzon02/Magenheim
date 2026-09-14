# Magenheim Validation Record

## 2026-09-13 foundation repair cycle

Observed repository state before repair:

- repository existed but its first commit contained only `README.md`;
- README claimed source, tests, build scripts, data files, BepInEx/Jötunn integration, and 118 assertions that were not present in the commit;
- no authoritative project-state, design, backlog, validation, or source files existed in the repository.

Repairs completed on `main`:

- created authoritative project-state and design records;
- created dependency-ordered backlog;
- created dependency-free `Magenheim.Core` project;
- implemented crystal tier, elemental alignment, refinement request/result, refinement-rule validation, and refinement engine;
- corrected an enum-arithmetic compile defect found during review;
- added a dedicated `NoRule` result rather than misreporting missing progression as a station failure;
- reconciled README claims with files that actually exist.

## 2026-09-13 advanced refinement semantics review

The implementation was compared against the authoritative Magenheim design specification before further runtime expansion. The review found bootstrap semantic drift in the pure-domain authority:

- the stable third tier was implemented as `Crystal` instead of `Refined`;
- elemental identities included Lightning/Poison/Nature/Water/Air/Arcane instead of the canonical Storm/Venom/Radiance/Seidr/Spirit catalog;
- refinement used fixed success chances rather than Crystal Shaping's configured failure reduction;
- bootstrap defaults invented skill-level gates not defined by the progression design;
- failures could preserve the source crystal, contradicting the required destructive-failure/shard loop;
- all refinement tiers used one workstation ID instead of workstation-upgrade gating.

The corrected domain rules now encode:

- tiers Rough, Simple, Refined, Advanced, Master;
- normal alignments Earth, Fire, Frost, Storm, Venom, Radiance, Seidr, Spirit;
- base failure 10%, 20%, 30%, 40%;
- `effectiveFailure = BaseFailure * (1 - (Skill / 100) * MaximumFailureReduction)`;
- maximum failure reduction constrained to 0.50..1.00, default 0.75;
- effective failure reaches zero at skill 100 when maximum reduction is configured to 1.00;
- progression gated by Geologist's Workstation, Fracturing Block, Faceting Wheel, and Resonance Frame respectively;
- failure destroys the source and returns 1/2/3/5 matching shards;
- valid success/failure attempts are experience-eligible; invalid requests are not;
- refinement preserves elemental alignment.

Static review checks:

- enum order supports exactly one-tier progression;
- Master rules are rejected;
- base failure, roll, skill, and maximum-reduction bounds reject NaN/infinity and invalid ranges;
- duplicate source-tier rules are rejected;
- no Unity/Valheim/BepInEx/Jötunn dependencies are introduced into `Magenheim.Core`;
- runtime inventory mutation and RPC authority remain outside this pure decision layer.

Compilation/runtime status:

A .NET SDK/compiler is unavailable in the current execution environment, so compilation and Valheim runtime validation were not executed. Static admission only; runtime admission is deferred. The next dependency-valid validation step is to establish a reproducible standalone build/test path and execute deterministic formula/boundary vectors before runtime adapters consume this API.
