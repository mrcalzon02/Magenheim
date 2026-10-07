"""Publish the pilot's measured comparisons, provenance and process record."""
from pathlib import Path
from html.parser import HTMLParser
import json,hashlib
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-refinement'
r=json.loads((OUT/'refinement-metrics.json').read_text())
v=json.loads((OUT/'verification.json').read_text())
assert v['source_sha256']==hashlib.sha256((ROOT/r['source']).read_bytes()).hexdigest()
ID=r['id'];old='underworld-boss-first-bloom-detail-r2'
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>First Bloom surface refinement</title><style>
body{margin:0;background:#14191c;color:#e5e6df;font:16px/1.5 system-ui}main{max-width:1700px;margin:auto;padding:28px}.grid{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}figure{margin:0}img{width:100%;border-radius:7px}button,a{color:#b9dfef}button{background:#2b3a43;border:1px solid #617481;padding:9px;margin:6px 4px 6px 0;cursor:pointer}table{border-collapse:collapse;margin:15px 0}td,th{border:1px solid #4b5960;padding:8px;text-align:left}.note{color:#e3be82}p{max-width:1150px}@media(max-width:950px){.grid{grid-template-columns:1fr 1fr}}@media(max-width:550px){.grid{grid-template-columns:1fr}}
</style><main><h1>First Bloom: coherent surfaces within the donor budget</h1>'''
page+=f'<p>Soft triangle ceiling: 1.5 × Troll’s 6,206 triangles = 9,309. This individually rebuilt pilot uses <strong>{r["triangles"]:,} triangles ({r["ratio"]:.2f}× donor)</strong>. It removes {r["reduction_percent"]:.1f}% of the previous revision’s triangles.</p>'
page+='<div class="grid"><figure><img src="../../../creature-concepts/2026-10-03/the-first-bloom--thumb.jpg"><figcaption>Adopted concept</figcaption></figure><figure><img id="donor" src="../detail-r2/donor-Troll--three-quarter.png"><figcaption>Actual Troll donor · bind pose</figcaption></figure>'
page+=f'<figure><img id="before" src="../detail-r2/{old}--three-quarter.png"><figcaption>Previous detail revision · 91,516 triangles</figcaption></figure><figure><img id="after" src="{ID}--three-quarter.png"><figcaption>Individualized surface rebuild · {r["triangles"]:,} triangles</figcaption></figure></div>'
for view,label in [('three-quarter','Three-quarter'),('front','Front'),('side','Side'),('detail','Face detail'),('clay','Geometry only')]:page+=f'<button data-view="{view}">{label}</button>'
page+=f'<p><a href="{ID}--attack.png">Inherited attack pose</a> · <a href="../../../../../{r["source"]}">Editable Blender study</a> · <a href="{ID}.glb">Skinned rest-pose GLB</a> · <a href="README.md">Procedure and limitations</a></p>'
page+='<table><tr><th>Metric</th><th>Native Troll</th><th>Previous detail revision</th><th>Pilot</th></tr>'
page+=f'<tr><td>Triangles</td><td>6,206</td><td>91,516</td><td>{r["triangles"]:,}</td></tr><tr><td>Meshes / materials</td><td>2 / 1</td><td>31 / 5</td><td>1 / 1</td></tr><tr><td>Map dimensions</td><td>256px extraction</td><td>2048px per material map</td><td>512px atlas · four maps</td></tr></table>'
page+='<p>Caps have a shared rolled rim and folded underside instead of hundreds of detached gill tubes. Bark ribs are part of the trunk and limb surfaces. Elbows and knees share continuous weighted surfaces; palm masses seat the root fingers. Chest gills are two closed folded panels. Owned materials and fine gill normals are baked into one atlas.</p><p class="note">This is a cost and coherence pilot, not final art or live performance acceptance. The texture atlas still needs authored local detail and seam review. The GLB is one skinned primitive; the current production creature path uses rigid parts and cannot load this study directly. No runtime admission or installation was made.</p><p>Views use the preceding gallery’s lighting, exposure, camera directions and framing rule. Native bind-pose materials are approximate. Detail views have individual focus points. Five sampled attack frames passed finite-coordinate/bounds checks; that does not prove good animation deformation.</p>'
page+=f'''</main><script>document.querySelectorAll('button').forEach(b=>b.onclick=()=>{{let v=b.dataset.view;document.getElementById('donor').src='../detail-r2/donor-Troll--'+v+'.png';document.getElementById('before').src='../detail-r2/{old}--'+v+'.png';document.getElementById('after').src='{ID}--'+v+'.png';}});</script></html>'''
(OUT/'gallery.html').write_text(page,encoding='utf-8')
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,value in attrs:
   if key in ('src','href') and value:assert (OUT/value).is_file(),value
Links().feed(page)
for view in ('three-quarter','front','side','detail','clay','attack'):assert (OUT/(ID+'--'+view+'.png')).is_file()
files=[ROOT/r['source'],ROOT/'assets/models/source/underworld-boss-first-bloom-detail-r2.blend',ROOT/'assets/models/source/underworld-creature-crowncap-brute.blend',ROOT/'docs/art/creature-concepts/2026-10-03/the-first-bloom.png']
files += [ROOT/'tools'/s for s in ('refine-first-bloom.py','review-first-bloom-refinement.py','build-first-bloom-refinement-review.py','author-boss-concept-models.py','creature_apex_authoring.py')]
files += list((ROOT/'assets/textures/underworld/bosses/first-bloom-refinement').glob('*.png'))
files += [p for p in OUT.iterdir() if p.is_file() and p.name!='provenance.json']
(OUT/'provenance.json').write_text(json.dumps({p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in files},indent=2)+'\n')
print('VERIFIED pilot gallery links, six views, current source and provenance')
