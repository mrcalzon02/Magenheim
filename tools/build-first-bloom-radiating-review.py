"""Review the concept's radiating fleshy heat-sink lamellae."""
from pathlib import Path
from html.parser import HTMLParser
import json,hashlib,struct
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-radiating-r7'
r=json.loads((OUT/'refinement-metrics.json').read_text());v=json.loads((OUT/'verification.json').read_text());a=json.loads((OUT/'attachment-verification.json').read_text());p=json.loads((OUT/'attachment-pose-verification.json').read_text());n=json.loads((OUT/'frill-anatomy.json').read_text());ID=r['id']
assert r['triangles']==9252 and r['triangles']<=r['soft_cap'] and r['materials']==r['renderers']==1 and r['atlas_size']==512
assert n['frills']==n['central_join_roots']==n['wall_roots']==22
assert a['endpoints']==120 and all(c['all_endpoint_vertices_inside_support'] for c in a['checks'])
assert p['result']=='PASS' and p['max_outside_distance_metres']<=.001
assert hashlib.sha256((ROOT/r['source']).read_bytes()).hexdigest()==v['source_sha256']
data=(OUT/(ID+'.glb')).read_bytes();size,kind=struct.unpack_from('<II',data,12);doc=json.loads(data[20:20+size]);primitive=doc['meshes'][0]['primitives'][0];assert doc['accessors'][primitive['indices']]['count']//3==9252
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>First Bloom radiating fleshy frills</title><style>
body{margin:0;background:#14191c;color:#e5e6df;font:16px/1.5 system-ui}main{max-width:1500px;margin:auto;padding:26px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:14px}figure{margin:0}img{width:100%;border-radius:7px}a,button{color:#b9dfef}button{background:#2b3a43;border:1px solid #617481;padding:8px;margin:10px 4px 10px 0;cursor:pointer}p{max-width:1100px}.note{color:#e3be82}h2{margin-top:30px}@media(max-width:650px){.pair{grid-template-columns:1fr}main{padding:12px}}
</style><main><h1>First Bloom: radiating fleshy frills</h1><p>The concept shows pale, thin fins radiating from a fleshy central join, curving outward and down around the chest. Amber tissue sits inside and beneath their deep faces. This revision replaces the previous separate wall shelves with that connected fan arrangement.</p>'''
page+=f'<p><strong>22 curved pale lamellae · 9,252 triangles / {r["ratio"]:.2f}× Troll · 9,309 soft cap</strong><br>One mesh, one material, four 512px atlas maps. All 120 structural ends pass the saved rest contact check. Five inherited attack samples measure at most {p["max_outside_distance_metres"]*1000:.2f} mm outside separation, within the 1mm tolerance.</p>'
page+='<h2>Concept reference</h2><div class="pair"><figure><img src="concept-chest-reference.png"><figcaption>User-provided crop of the source concept · pale radial fins, recessed amber tissue</figcaption></figure><figure><img src="after--tissue--textured.png"><figcaption>Current modeled interpretation · matched chest close-up</figcaption></figure></div><h2>Previous shelves → radiating frills</h2>'
for mode in ('textured','clay'):
 page+=f'<div class="pair"><figure><img src="before--tissue--{mode}.png"><figcaption>Revision 6 · separate wall shelves · {mode}</figcaption></figure><figure><img src="after--tissue--{mode}.png"><figcaption>Revision 7 · central join and radiating fins · {mode}</figcaption></figure></div>'
page+=f'<h2>Whole creature comparison</h2><div class="pair"><figure><img id="before" src="../first-bloom-frills-r6/underworld-boss-first-bloom-frills-r6--detail.png"><figcaption>Revision 6</figcaption></figure><figure><img id="after" src="{ID}--detail.png"><figcaption>Revision 7</figcaption></figure></div>'
for view,label in [('detail','Chest / canopy'),('three-quarter','Three-quarter'),('front','Front'),('side','Side'),('clay','Geometry only')]:page+=f'<button data-view="{view}">{label}</button>'
page+=f'<p><a href="../../../creature-concepts/2026-10-03/the-first-bloom.png">Full concept</a> · <a href="../../../../../{r["source"]}">Editable Blender study</a> · <a href="{ID}.glb">Skinned GLB</a> · <a href="{ID}--attack.png">Inherited attack pose</a> · <a href="attachment-verification.json">Saved contact audit</a> · <a href="README.md">Revision record</a></p><p class="note">The model remains an uninstalled art study. Offline geometry and sampled contact checks do not establish visual acceptance, all-animation deformation quality or native runtime performance. Attachments intersect their supports and are not welded topology.</p>'
page+=f'''</main><script>document.querySelectorAll('button').forEach(b=>b.onclick=()=>{{let v=b.dataset.view;document.getElementById('before').src='../first-bloom-frills-r6/underworld-boss-first-bloom-frills-r6--'+v+'.png';document.getElementById('after').src='{ID}--'+v+'.png';}});</script></html>'''
(OUT/'gallery.html').write_text(page,encoding='utf-8')
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,value in attrs:
   if key in ('src','href') and value:assert (OUT/value).is_file(),value
Links().feed(page)
scripts=['first_bloom_radiating_frills.py','refine-first-bloom-radiating.py','review-first-bloom-radiating.py','verify-first-bloom-radiating-poses.py','render-first-bloom-radiating-closeups.py','build-first-bloom-radiating-review.py','first_bloom_frill_sculpt.py','first_bloom_attachment_repair.py','first_bloom_precision_sculpt.py','first_bloom_concept_sculpt.py','refine-first-bloom-attachments.py','review-first-bloom-attachments.py','verify-first-bloom-attachment-poses.py','render-first-bloom-frill-closeups.py','render-first-bloom-precision-closeups.py','refine-first-bloom-precision.py','refine-first-bloom-concept.py','refine-first-bloom.py','review-first-bloom-concept.py','review-first-bloom-refinement.py','author-boss-concept-models.py','creature_apex_authoring.py']
files=[ROOT/r['source'],ROOT/'assets/models/studies/underworld-boss-first-bloom-frills-r6.blend',ROOT/'docs/art/creature-concepts/2026-10-03/the-first-bloom.png']+[ROOT/'tools'/s for s in scripts]
files+=list((ROOT/'assets/textures/underworld/bosses/first-bloom-radiating-r7').glob('*.png'))+[q for q in OUT.iterdir() if q.is_file() and q.name!='provenance.json']
(OUT/'provenance.json').write_text(json.dumps({q.relative_to(ROOT).as_posix():hashlib.sha256(q.read_bytes()).hexdigest() for q in files},indent=2)+'\n')
print('VERIFIED radiating frill review: concept reference, 120 saved contacts, pose tolerance, export and links')
