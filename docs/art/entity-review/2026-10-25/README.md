# Entity review campaign — October 25

The campaign covers 48 selected concepts: six bosses and 42 ordinary creatures.
It is **not complete**. First Bloom r22 is preserved as the sole accepted boss;
Blackwater Maw stays paused by the user's explicit instruction on October 25.

[Open the ledger and individual comparisons](gallery.html). All 47 unpaused
entities have actual runtime front, side, three-quarter and rear renders. Each
page links its selected committed concept. JSON records retain runtime hashes,
measured costs, known native donor counts and explicit review states. Missing
native measurements remain unknown rather than being invented.

The first renderer mislabeled legacy rear views as fronts because two exporters
use opposite forward signs. Those renders and screening sheets were replaced.
The corrected renderer inverts each versioned rig's actual exporter basis and
records it. [Orientation diagnosis and installed correction](../../../validation/2026-10-25-creature-orientation.md).
Initial screening does not establish visual acceptance; individual detailed
reviews remain open. Earlier rear-facing previews must not be used as evidence
that a front feature is absent.

Three separate studies were completed and technically checked:

- [Carrion Crown r4](../../creature-models/2026-10-25/carrion-crown-sepals-r4/gallery.html)
  builds the previously committed fifth sepal ring, 2,126 triangles.
- [Carrion Crown r5](../../creature-models/2026-10-25/carrion-crown-gnarled-r5/gallery.html)
  changes the trunk, roots, branches, cups, teeth, organs and atlas allocation,
  still 2,126 triangles against a 2,133 soft cap. All 120 named supports pass
  the rest/action samples, including cap-interior checks.
- [Rift Titan r19](../../creature-models/2026-10-25/rift-titan-layered-mantle-r19/gallery.html)
  slims four outer shoulder plates, adds long front ridges and repacks the body
  atlas at 1,800 triangles. All 76 supports pass; the head, neck, collar, wrapped
  band, inner shoulder geometry and all eight rest-bone matrices match r18.

These are **intermediate studies, unaccepted and not installed**. Their galleries
describe the visible shortcomings. Technical checks are not art completion.
The installed 0.0.178 release fixes legacy visual orientation; it retains the
previous First Bloom r22/Rift Titan r18/Carrion Crown r3 runtime model assets.

Continue the largest unaccepted, unpaused boss first (Rift Titan), then Carrion
Crown, White Silence and Furnace Heart. Prioritize primary anatomy and clay
silhouettes before surface polish. Do not replace these distinct concepts with
one shared stone body. After bosses, proceed through the ledger's ordinary
creatures from largest to smallest, measuring missing donor budgets as needed.

Reproduce inventory/pages with `build-entity-review-ledger.py`,
`blender.ps1 render-entity-review-baselines` and `publish-entity-review-pages.py`.
The page publisher requires Pillow. The renderer reuses cached output only
when both the exact runtime file and recipe hashes match.
