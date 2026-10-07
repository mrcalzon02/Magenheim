"""Verified support contacts and focused attachment repair comparison."""
from pathlib import Path
from html.parser import HTMLParser
import json,hashlib,struct
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-attachments-r5'
r=json.loads((OUT/'refinement-metrics.json').read_text());v=json.loads((OUT/'verification.json').read_text());a=json.loads((OUT/'attachment-verification.json').read_text());p=json.loads((OUT/'attachment-pose-verification.json').read_text());ID=r['id']
assert r['triangles']==9188 and r['materials']==r['renderers']==1 and r['atlas_size']==512
assert a['groups']==60 and a['endpoints']==120 and all(c['all_endpoint_vertices_inside_support'] for c in a['checks'])
assert p['result']=='PASS' and p['max_outside_distance_metres']<=.001
assert hashlib.sha256((ROOT/r['source']).read_bytes()).hexdigest()==v['source_sha256']
data=(OUT/(ID+'.glb')).read_bytes();size,kind=struct.unpack_from('<II',data,12);doc=json.loads(data[20:20+size]);primitive=doc['meshes'][0]['primitives'][0];assert doc['accessors'][primitive['indices']]['count']//3==9188
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>First Bloom attachment repair</title><style>
body{margin:0;background:#14191c;color:#e5e6df;font:16px/1.5 system-ui}main{max-width:1500px;margin:auto;padding:26px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:14px}figure{margin:0}img{width:100%;border-radius:7px}a,button{color:#b9dfef}button{background:#2b3a43;border:1px solid #617481;padding:8px;margin:10px 4px 10px 0;cursor:pointer}p{max-width:1100px}.note{color:#e3be82}h2{margin-top:30px}@media(max-width:650px){.pair{grid-template-columns:1fr}main{padding:12px}}
</style><main><h1>First Bloom: repair floating tissue attachments</h1><p>The previous review missed tissue groups that began or ended in empty space. The repair gives each vein/frame end an explicit cap, body or rooted tissue support. <strong>All 120 ends across 60 groups are seated into their supports in saved rest geometry.</strong></p>'''
page+=f'<p>Every vertex across each endpoint—not only its center—is inside its designated support. Bone weights follow the actual support triangle. Five inherited attack samples have a maximum measured separation of <strong>{p["max_outside_distance_metres"]*1000:.2f} mm</strong>, below the 1 mm review tolerance.</p><p>Still 9,188 triangles / 1.48× Troll, one mesh, one material and four 512px atlas maps.</p>'
page+=f'<div class="pair"><figure><img id="before" src="../first-bloom-precision-r4/underworld-boss-first-bloom-precision-r4--detail.png"><figcaption>Before · floating frame ends</figcaption></figure><figure><img id="after" src="{ID}--detail.png"><figcaption>After · explicit support contacts</figcaption></figure></div>'
for view,label in [('detail','Chest / canopy'),('three-quarter','Three-quarter'),('front','Front'),('side','Side'),('clay','Geometry only')]:page+=f'<button data-view="{view}">{label}</button>'
for area in ('caps','tissue'):
 page+=f'<h2>{"Cap attachments" if area=="caps" else "Body frame attachments"}</h2>'
 for mode in ('textured','clay'):
  page+=f'<div class="pair"><figure><img src="before--{area}--{mode}.png"><figcaption>Before · {mode}</figcaption></figure><figure><img src="after--{area}--{mode}.png"><figcaption>After · {mode}</figcaption></figure></div>'
page+=f'<p><a href="../../../creature-concepts/2026-10-03/the-first-bloom.png">Full concept</a> · <a href="../../../../../{r["source"]}">Editable Blender study</a> · <a href="{ID}.glb">Skinned GLB</a> · <a href="{ID}--attack.png">Inherited attack pose</a> · <a href="attachment-verification.json">Saved contact audit</a> · <a href="README.md">Repair record</a></p><p class="note">These are intersecting attachment surfaces, not topologically welded meshes. The rest contact check and five inherited pose samples do not establish all-animation or native runtime acceptance. This candidate remains uninstalled.</p>'
page+=f'''</main><script>document.querySelectorAll('button').forEach(b=>b.onclick=()=>{{let v=b.dataset.view;document.getElementById('before').src='../first-bloom-precision-r4/underworld-boss-first-bloom-precision-r4--'+v+'.png';document.getElementById('after').src='{ID}--'+v+'.png';}});</script></html>'''
(OUT/'gallery.html').write_text(page,encoding='utf-8')
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,value in attrs:
   if key in ('src','href') and value:assert (OUT/value).is_file(),value
Links().feed(page)
scripts=['first_bloom_attachment_repair.py','refine-first-bloom-attachments.py','review-first-bloom-attachments.py','verify-first-bloom-attachment-poses.py','render-first-bloom-attachment-closeups.py','build-first-bloom-attachment-review.py','first_bloom_precision_sculpt.py','refine-first-bloom-precision.py','first_bloom_concept_sculpt.py','refine-first-bloom-concept.py','review-first-bloom-concept.py','refine-first-bloom.py','review-first-bloom-refinement.py','render-first-bloom-precision-closeups.py','author-boss-concept-models.py','creature_apex_authoring.py']
files=[ROOT/r['source'],ROOT/'assets/models/studies/underworld-boss-first-bloom-precision-r4.blend',ROOT/'docs/art/creature-concepts/2026-10-03/the-first-bloom.png']+[ROOT/'tools'/s for s in scripts]
files+=list((ROOT/'assets/textures/underworld/bosses/first-bloom-attachments-r5').glob('*.png'))+[q for q in OUT.iterdir() if q.is_file() and q.name!='provenance.json']
(OUT/'provenance.json').write_text(json.dumps({q.relative_to(ROOT).as_posix():hashlib.sha256(q.read_bytes()).hexdigest() for q in files},indent=2)+'\n')
print('VERIFIED attachment review: 120 saved contacts, pose tolerance, export and links')
