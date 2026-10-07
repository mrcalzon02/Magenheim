"""Publish checked foot comparisons, preserving the preceding art reviews."""
from pathlib import Path
from html.parser import HTMLParser
import json,hashlib,os,shutil
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-feet-r15'
RAW=OUT.with_name('first-bloom-feet-r15-raw')
row=json.loads((OUT/'refinement-metrics.json').read_text())
v=json.loads((OUT/'verification.json').read_text())
a=json.loads((OUT/'attachment-verification.json').read_text())
p=json.loads((OUT/'attachment-pose-verification.json').read_text())
boundary=json.loads((OUT/'trim-boundary-verification.json').read_text())
assert row['triangles']==v['triangles']==8830<=row['soft_cap']==9309
assert row['renderers']==row['materials']==1 and row['atlas_size']==512
assert a['endpoints']==132 and all(c['all_endpoint_vertices_inside_support'] for c in a['checks'])
assert p['result']=='PASS' and p['max_outside_distance_metres']<=.001
assert boundary['result'].startswith('PASS')
assert hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest()==v['source_sha256']
row.update(previous_id='underworld-boss-first-bloom-trim-r14',previous_triangles=8810,reduction_percent=(1-8830/8810)*100)
(OUT/'refinement-metrics.json').write_text(json.dumps(row,indent=2)+'\n')
for name in ('foot-refinement.json','attachment-authoring.json'):
 shutil.copyfile(RAW/name,OUT/name)
def rel(path):return os.path.relpath(path,OUT).replace('\\','/')
concept=rel(ROOT/'docs/art/creature-concepts/2026-10-03/the-first-bloom.png')
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>First Bloom: root feet — revision 15</title>
<style>body{background:#14191d;color:#e2ddd2;font:16px system-ui;margin:0 auto;padding:28px;max-width:1400px}a{color:#e3bb6c}h1,h2{font-weight:600}p{line-height:1.6;max-width:1000px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}img{width:100%;background:#1d2429;border-radius:8px}figcaption{padding:8px;color:#c9c6bd}.concept{max-height:720px;object-fit:contain}nav{display:flex;flex-wrap:wrap;gap:18px}@media(max-width:650px){.pair{grid-template-columns:1fr}}</style>
<h1>First Bloom: root feet — revision 15</h1>
<p>The existing foot loops now form broader root fans with unequal crooked toes, lower contact pads, and flattened, staggered fungal brackets around the ankles. All ten root bases are seated into the leg surface with matching skin weights. The approved chest shaping is preserved.</p>
<p><strong>8,830 triangles · 1.423× Troll · soft cap 9,309</strong><br>One mesh, one material, four 512px atlas maps. No new triangles were added during foot shaping. Reauditing buried geometry removes 422 triangles from the untrimmed model; retaining the new ankle contacts leaves 20 more triangles than revision 14.</p>
<p>Offline geometry, export, 132 attachment records and five attack-pose contact samples pass. Buried-face retention was checked at 50 samples across ten actions. This remains an uninstalled art study; full animation and live performance acceptance are pending.</p>'''
page+=f'<nav><a href="../first-bloom-trim-r14/gallery.html">Previous revision</a><a href="{rel(ROOT/row["source"])}">Editable Blender source</a><a href="{row["id"]}.glb">Skinned GLB</a><a href="verification.json">Geometry checks</a><a href="attachment-verification.json">Attachment checks</a><a href="buried-face-audit.json">Buried-face audit</a></nav>'
page+=f'<h2>Concept reference</h2><img class="concept" src="{concept}" alt="First Bloom source concept">'
for view,title in [('feet-front','Root spread from the front'),('feet-angle','Root fans and ankle transition'),('feet-side','Foot depth and bracket profile')]:
 for kind in ('textured','clay'):
  page+=f'<h2>{title} — {kind}</h2><div class="pair">'
  for label,caption in [('before','Before — revision 14'),('after','After — revision 15')]:
   page+=f'<figure><img loading="lazy" src="{label}--{view}--{kind}.png" alt="{caption}, {view}, {kind}"><figcaption>{caption}</figcaption></figure>'
  page+='</div>'
page+='<h2>Whole creature</h2><div class="pair">'
for view in ('front','three-quarter','side','attack'):
 page+=f'<figure><img loading="lazy" src="{row["id"]}--{view}.png" alt="Revised creature {view}"><figcaption>{view}</figcaption></figure>'
page+='</div></html>'
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,value in attrs:
   if key in ('src','href') and value:assert (OUT/value).is_file(),value
Links().feed(page)
(OUT/'gallery.html').write_text(page,encoding='utf-8')
files=[ROOT/row['source'],ROOT/'docs/art/creature-concepts/2026-10-03/the-first-bloom.png']+list((ROOT/'tools').glob('*first*bloom*.py'))+list((ROOT/'assets/textures/underworld/bosses/first-bloom-feet-r15').glob('*.png'))+[f for f in OUT.iterdir() if f.is_file() and f.name!='provenance.json']
(OUT/'provenance.json').write_text(json.dumps({f.relative_to(ROOT).as_posix():hashlib.sha256(f.read_bytes()).hexdigest() for f in files},indent=2)+'\n')
print('VERIFIED root-feet review: 8830 triangles, 132 contacts, sampled poses, hidden cuts, source hash and gallery links')
