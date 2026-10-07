"""Evidence-backed concept, donor, pilot and revision comparison."""
from pathlib import Path
from html.parser import HTMLParser
import json,hashlib,struct
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-concept-r3'
r=json.loads((OUT/'refinement-metrics.json').read_text());v=json.loads((OUT/'verification.json').read_text());ID=r['id']
assert hashlib.sha256((ROOT/r['source']).read_bytes()).hexdigest()==v['source_sha256']
data=(OUT/(ID+'.glb')).read_bytes();size,kind=struct.unpack_from('<II',data,12)
doc=json.loads(data[20:20+size]);primitive=doc['meshes'][0]['primitives'][0]
assert doc['accessors'][primitive['indices']]['count']//3==r['triangles'],'GLB triangle drift'
concept=ROOT/'docs/art/creature-concepts/2026-10-03/the-first-bloom.png'
observations=[
 dict(region='Whole silhouette',pilot_gap='Broad hat stack and humanoid face dominate.',revision='Eight tapering asymmetric brackets around a branching crown; remove eyes and smiling jaw.',remaining='Bracket shapes still need more individual edge wear and variation.'),
 dict(region='Chest focus',pilot_gap='Two flat bright chest plates cover a solid trunk.',revision='Cut a real aperture through the trunk; recess an oval chamber behind 22 curved ribs and a central amber organ.',remaining='Core illumination and rib integration need native shader/pose review.'),
 dict(region='Draped tissue',pilot_gap='Concept hanging pale webbing is absent.',revision='Add thickness-bearing tapered curtains and sparse branching scaffold, seated beneath the canopy.',remaining='Tissue still reads as ribbons in close-up; finer branching and frayed edges need another texture/geometry pass.'),
 dict(region='Legs and ground contact',pilot_gap='Bare segmented legs and narrow toe fans.',revision='Narrow the bow-legged rest stance, layer fungal brackets down both legs, add hip forks and five spreading ground buttresses per foot.',remaining='Root-to-ground masses and hip transitions still need more organic integration.'),
 dict(region='Materials',pilot_gap='Smooth tan caps, uniform bark and bright chest stripes.',revision='Darker bark with directional grain, ochre/moss cap pigmentation, pale undersides and isolated amber emission; bake to one 512px atlas.',remaining='512px texel allocation, seams and close-range crispness need continued review.'),
]
(OUT/'concept-comparison.json').write_text(json.dumps({'concept':concept.relative_to(ROOT).as_posix(),'concept_sha256':hashlib.sha256(concept.read_bytes()).hexdigest(),'observations':observations,'visual_acceptance':'iteration candidate; not final concept fidelity or live acceptance'},indent=2)+'\n')
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>First Bloom — concept anatomy revision</title><style>
body{margin:0;background:#14191c;color:#e5e6df;font:16px/1.5 system-ui}main{max-width:1800px;margin:auto;padding:28px}.grid{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}figure{margin:0}img{width:100%;aspect-ratio:1;object-fit:contain;border-radius:7px;background:#20262a}figcaption{font-size:14px;color:#cad0ce}button,a{color:#b9dfef}button{background:#2b3a43;border:1px solid #617481;padding:9px;margin:12px 4px 6px 0;cursor:pointer}table{border-collapse:collapse;margin:18px 0}td,th{border:1px solid #4b5960;padding:9px;text-align:left}.note{color:#e3be82}p{max-width:1200px}h2{font-size:22px;margin-top:28px}@media(max-width:1000px){.grid{grid-template-columns:1fr 1fr}}@media(max-width:550px){.grid{grid-template-columns:1fr}main{padding:12px}}
</style><main><h1>First Bloom: another pass against the concept</h1>'''
page+=f'<p>Rebuilt the defining anatomy while preserving the donor budget: <strong>{r["triangles"]:,} triangles / {r["ratio"]:.2f}× Troll</strong>, below the 9,309 soft cap. One skinned mesh, one material, four 512px atlas maps. This pass spends triangles on the recessed chamber, draped tissue and rooted leg silhouette.</p>'
page+='<div class="grid"><figure><a href="../../../creature-concepts/2026-10-03/the-first-bloom.png"><img src="../../../creature-concepts/2026-10-03/the-first-bloom.png" alt="First Bloom adopted concept sheet"></a><figcaption>Adopted concept · click to inspect full sheet</figcaption></figure><figure><img id="donor" src="../detail-r2/donor-Troll--three-quarter.png" alt="Native Troll donor"><figcaption>Native Troll · 6,206 triangles</figcaption></figure><figure><img id="before" src="../first-bloom-refinement/underworld-boss-first-bloom-refinement--three-quarter.png" alt="Previous pilot"><figcaption>Previous pilot · 7,940 triangles</figcaption></figure>'
page+=f'<figure><img id="after" src="{ID}--three-quarter.png" alt="Revised First Bloom"><figcaption>Concept anatomy revision · {r["triangles"]:,} triangles</figcaption></figure></div>'
for view,label in [('three-quarter','Three-quarter'),('front','Front'),('side','Side'),('detail','Chest / canopy detail'),('clay','Geometry only')]:page+=f'<button data-view="{view}">{label}</button>'
page+=f'<p><a href="{ID}--attack.png">Inherited attack pose</a> · <a href="../../../../../{r["source"]}">Editable Blender study</a> · <a href="{ID}.glb">Skinned rest-pose GLB</a> · <a href="README.md">Detailed record</a></p>'
page+='<table><tr><th>Cost</th><th>Troll donor</th><th>Previous pilot</th><th>Concept revision</th></tr>'
page+=f'<tr><td>Triangles</td><td>6,206</td><td>7,940</td><td>{r["triangles"]:,} / 9,309 soft cap</td></tr><tr><td>Meshes / materials</td><td>2 / 1</td><td>1 / 1</td><td>1 / 1</td></tr><tr><td>Map dimensions</td><td>256px extraction</td><td>512px atlas</td><td>512px atlas</td></tr></table>'
for obs in observations:page+=f'<h2>{obs["region"]}</h2><p><strong>Previous gap:</strong> {obs["pilot_gap"]}</p><p>{obs["revision"]}</p><p class="note">Remaining: {obs["remaining"]}</p>'
page+='<p class="note">Offline geometry, weights, atlas, scale, sampled pose bounds and portable export checks pass. These do not establish final visual quality or live performance. This candidate remains outside production export and is uninstalled; the current runtime creature path expects rigid parts, while this study uses a consolidated skin.</p><p>Full-body views share lighting, exposure, camera directions and normalized framing with the previous comparisons. Close-ups focus on different anatomy. Native extraction uses bind poses and approximate materials. Geometry-only views remove texture and emission.</p>'
page+=f'''</main><script>document.querySelectorAll('button').forEach(b=>b.onclick=()=>{{let v=b.dataset.view;document.getElementById('donor').src='../detail-r2/donor-Troll--'+v+'.png';document.getElementById('before').src='../first-bloom-refinement/underworld-boss-first-bloom-refinement--'+v+'.png';document.getElementById('after').src='{ID}--'+v+'.png';}});</script></html>'''
(OUT/'gallery.html').write_text(page,encoding='utf-8')
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,value in attrs:
   if key in ('src','href') and value:assert (OUT/value).is_file(),value
Links().feed(page)
for view in ('three-quarter','front','side','detail','clay','attack'):assert (OUT/(ID+'--'+view+'.png')).is_file()
files=[ROOT/r['source'],ROOT/'assets/models/studies/underworld-boss-first-bloom-refinement.blend',ROOT/'assets/models/source/underworld-creature-crowncap-brute.blend',concept]
files += [ROOT/'tools'/s for s in ('refine-first-bloom.py','review-first-bloom-refinement.py','refine-first-bloom-concept.py','first_bloom_concept_sculpt.py','review-first-bloom-concept.py','build-first-bloom-concept-review.py','author-boss-concept-models.py','creature_apex_authoring.py')]
files += list((ROOT/'assets/textures/underworld/bosses/first-bloom-concept-r3').glob('*.png'))
files += [p for p in OUT.iterdir() if p.is_file() and p.name!='provenance.json']
(OUT/'provenance.json').write_text(json.dumps({p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in files},indent=2)+'\n')
print('VERIFIED concept gallery links, six matched views, source and provenance')
