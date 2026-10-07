# Boss donor-quality revision 2

The user rejected the first pass's model detail on 4 October and established the
actual donor models as the minimum acceptance barrier. A valid mesh, a 2K map or
a high triangle count does not satisfy that visual requirement.

[Open the four-way comparison](gallery.html): concept, native donor, rejected
first pass and revised candidate. Each revision has front, side, three-quarter,
detail, neutral clay and inherited attack-pose renders. The first-pass source
files and pictures remain preserved. Revision sources have `-detail-r2` names.

## Native evidence

The gameplay roster maps First Bloom through Crowncap Brute to **Troll**, Blackwater
Maw through Deep Hunter to **Serpent**, Furnace Heart / White Silence / Rift Titan
to **StoneGolem**, and Carrion Crown through Corpse Orchard to **Greydwarf_Shaman**.
The comparison imports the local extracted donor geometry, UVs, texture maps and
material properties; it does not use the previous generated species models as
the quality reference.

Donors and revisions use the same camera directions, normalized framing rule,
lighting, exposure and renderer. Native asset bind poses and approximate material
decoding differ from native animated gameplay. Geometry-only views make the
underlying anatomical construction visible without texture or emission.

The donor meshes demonstrate that coherent anatomy, purposeful edge structure,
material boundaries and textured surface variation matter more than polygon
count. Their extraction records are preserved in the provenance file.

## Revision

Repeated beveled boxes were replaced with asymmetric fractured profiles. The
organic bodies gained rebuilt trunks, facial/jaw anatomy, joined fibrous roots,
folded gills, overlapping scales, torn sepals, branching roots and ribbed pods.
The ice and fracture bodies retain separate material and structural languages.

Owned maps replace broad repeating sine bands with multiscale cavity stains,
grain, scale boundaries, erosion, fracture lines and edge variation. They retain
a restrained pixel palette and explicit normal/roughness/emission separation.
These maps are separate from both the concept paintings and native donor maps.

[The review record](donor-quality-review.json) records each donor, revision and
cost. [Source verification](source-verification.json) validates geometry, UVs,
binding, packed maps, scale and reference integrity. [Provenance](provenance.json)
records the exact sources, generators, extracted donor inputs and deliverables.

The geometry is a detailed source candidate. First Bloom's gill-rich source in
particular needs simplification/LOD work before runtime admission. Inherited
actions are inspection baselines; full retargeting and native rendering,
locomotion, attacks, hitboxes and multiplayer remain unverified. No model is
declared accepted from polygon counts or file-validity checks. No runtime
registration, release or installed profile was changed in this revision.

## Rebuild

1. Run `generate-boss-detail-textures.py` with the bundled Python/NumPy/Pillow.
2. `tools/blender.ps1 prepare-boss-native-donors`
3. `tools/blender.ps1 author-boss-concept-models --detail-r2`
4. `tools/blender.ps1 render-boss-quality-comparison donors`
5. `tools/blender.ps1 render-boss-quality-comparison models`
6. `tools/blender.ps1 verify-boss-concept-sources --detail-r2`
7. `tools/blender.ps1 export-boss-concept-studies --detail-r2`
8. `build-boss-quality-review.py`
