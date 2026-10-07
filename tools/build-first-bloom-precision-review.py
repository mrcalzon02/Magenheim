"""Deliver focused before/after detail review with topology evidence."""
from pathlib import Path
from html.parser import HTMLParser
import json,hashlib,struct
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-precision-r4'
r=json.loads((OUT/'refinement-metrics.json').read_text());v=json.loads((OUT/'verification.json').read_text());ID=r['id']
assert r['triangles']==9188 and r['renderers']==r['materials']==1 and r['atlas_size']==512
assert hashlib.sha256((ROOT/r['source']).read_bytes()).hexdigest()==v['source_sha256']
parity=json.loads((OUT/'precision-parity.json').read_text());assert parity['result'].startswith('PASS')
data=(OUT/(ID+'.glb')).read_bytes();size,kind=struct.unpack_from('<II',data,12);doc=json.loads(data[20:20+size]);primitive=doc['meshes'][0]['primitives'][0]
assert doc['accessors'][primitive['indices']]['count']//3==r['triangles']
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>First Bloom precision shaping</title><style>
body{margin:0;background:#14191c;color:#e5e6df;font:16px/1.5 system-ui}main{max-width:1600px;margin:auto;padding:26px}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:14px}figure{margin:0}img{width:100%;border-radius:7px}figcaption{font-size:14px;color:#cad0ce}a,button{color:#b9dfef}button{background:#2b3a43;border:1px solid #617481;padding:8px;margin:10px 4px 10px 0;cursor:pointer}p{max-width:1100px}.note{color:#e3be82}h2{margin-top:30px}@media(max-width:700px){.grid,.pair{grid-template-columns:1fr}main{padding:12px}}
</style><main><h1>First Bloom: cap and tissue precision pass</h1><p>Shape changes to the existing geometry. <strong>9,188 triangles, unchanged topology and vertex weights.</strong> Still 1.48× Troll, below the 9,309 soft cap; one mesh, one material and four 512px atlas maps.</p>'''
page+='<div class="grid"><figure><a href="../../../creature-concepts/2026-10-03/the-first-bloom.png"><img src="../../../creature-concepts/2026-10-03/the-first-bloom--thumb.jpg" alt="Concept sheet"><figcaption>Concept · open full sheet</figcaption></a></figure><figure><img id="before" src="../first-bloom-concept-r3/underworld-boss-first-bloom-concept-r3--three-quarter.png" alt="Before precision pass"><figcaption>Before · concept anatomy r3</figcaption></figure>'
page+=f'<figure><img id="after" src="{ID}--three-quarter.png" alt="After precision pass"><figcaption>After · precision shaping r4</figcaption></figure></div>'
for view,label in [('three-quarter','Three-quarter'),('front','Front'),('side','Side'),('detail','Chest / canopy'),('clay','Geometry only')]:page+=f'<button data-view="{view}">{label}</button>'
page+='<h2>Drooping caps</h2><p>Free rims sag most at exposed front lobes. Unequal scallops and radial folds interrupt the flat shelf edges while their centers stay seated. Tissue attachment points follow the altered rim.</p>'
for area in ('caps','tissue'):
 if area=='tissue':page+='<h2>Tissue frames</h2><p>Existing rails flare around the chamber, pinch between anchors, twist out of the flat plane and taper toward their lower tips. Crosslinks and ribs have unequal arches. No new branches or polygons were added.</p>'
 for mode in ('textured','clay'):
  page+=f'<div class="pair"><figure><img src="before--{area}--{mode}.png" alt="Before {area} {mode}"><figcaption>Before · {mode}</figcaption></figure><figure><img src="after--{area}--{mode}.png" alt="After {area} {mode}"><figcaption>After · {mode}</figcaption></figure></div>'
page+=f'<p><a href="../../../../../{r["source"]}">Editable Blender study</a> · <a href="{ID}.glb">Skinned GLB</a> · <a href="{ID}--attack.png">Inherited attack pose</a> · <a href="README.md">Record and limitations</a></p><p class="note">Offline integrity, topology/weight parity and sampled pose bounds pass. Tissue branching and fraying remain coarser than the concept. Native deformation, shaders and live performance remain unverified; this study is uninstalled.</p>'
page+=f'''</main><script>document.querySelectorAll('button').forEach(b=>b.onclick=()=>{{let v=b.dataset.view;document.getElementById('before').src='../first-bloom-concept-r3/underworld-boss-first-bloom-concept-r3--'+v+'.png';document.getElementById('after').src='{ID}--'+v+'.png';}});</script></html>'''
(OUT/'gallery.html').write_text(page,encoding='utf-8')
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,value in attrs:
   if key in ('src','href') and value:assert (OUT/value).is_file(),value
Links().feed(page)
files=[ROOT/r['source'],ROOT/'assets/models/studies/underworld-boss-first-bloom-concept-r3.blend',ROOT/'docs/art/creature-concepts/2026-10-03/the-first-bloom.png']
files += [ROOT/'tools'/s for s in ('first_bloom_precision_sculpt.py','refine-first-bloom-precision.py','review-first-bloom-precision.py','render-first-bloom-precision-closeups.py','build-first-bloom-precision-review.py','first_bloom_concept_sculpt.py','refine-first-bloom-concept.py','review-first-bloom-concept.py','refine-first-bloom.py','review-first-bloom-refinement.py','author-boss-concept-models.py','creature_apex_authoring.py')]
files += list((ROOT/'assets/textures/underworld/bosses/first-bloom-precision-r4').glob('*.png'))
files += [p for p in OUT.iterdir() if p.is_file() and p.name!='provenance.json']
(OUT/'provenance.json').write_text(json.dumps({p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in files},indent=2)+'\n')
print('VERIFIED precision gallery, topology parity, export triangles and provenance')
