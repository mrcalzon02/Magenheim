"""Checked concept/contour review for corrected fin direction and placement."""
from pathlib import Path
from html.parser import HTMLParser
import json,hashlib,os,shutil
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-spined-fins-r8';RAW=OUT.with_name('blackwater-maw-spined-fins-r8-raw')
r=json.loads((OUT/'refinement-metrics.json').read_text());v=json.loads((OUT/'verification.json').read_text());p=json.loads((OUT/'attachment-pose-verification.json').read_text());a=json.loads((OUT/'attachment-verification.json').read_text());inventory=json.loads((OUT/'fin-inventory-verification.json').read_text());direction=json.loads((OUT/'fin-direction-verification.json').read_text());trim=json.loads((OUT/'trim-result.json').read_text())
assert r['triangles']==v['triangles']==7224<=r['soft_cap']==7308
assert r['materials']==r['renderers']==1 and r['atlas_size']==512
assert p['result']=='PASS' and p['endpoints']==a['endpoints']==112 and p['sampled_poses']==65
assert inventory['dorsals']==4 and inventory['lateral_pairs']==2 and inventory['tail_fan_area_ratio_to_revision5']>3
assert direction['result'].startswith('PASS') and direction['paired_root_separation_metres']>2
assert trim['removed_triangles']==808
assert hashlib.sha256((ROOT/r['source']).read_bytes()).hexdigest()==v['source_sha256']
for kind in ('albedo','normal','roughness','emission'):
 old=ROOT/f'assets/textures/underworld/bosses/blackwater-maw-spined-fins-r8-raw/underworld-boss-blackwater-maw-spined-fins-r8-raw-{kind}.png';new=ROOT/f'assets/textures/underworld/bosses/blackwater-maw-spined-fins-r8/underworld-boss-blackwater-maw-spined-fins-r8-{kind}.png'
 assert hashlib.sha256(old.read_bytes()).digest()==hashlib.sha256(new.read_bytes()).digest()
contours=json.loads((RAW/'fin-contours.json').read_text())
for part in contours['parts']:
 if 'Dorsal' in part['part']:part['profile']='tailward-swept dorsal spines'
 elif part['part'].startswith('PleatedPectoral'):part['profile']='smaller rear pelvic fins at a separate body station'
(OUT/'fin-contours.json').write_text(json.dumps(contours,indent=2)+'\n')
def rel(path):return os.path.relpath(path,OUT).replace('\\','/')
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Blackwater Maw: corrected spined fins</title>
<style>body{background:#13191d;color:#e4dfd6;font:16px system-ui;margin:0 auto;padding:28px;max-width:1500px}a{color:#77c7d1}h1,h2{font-weight:600}p{line-height:1.6;max-width:1100px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}img{width:100%;border-radius:8px;background:#1a2329}figcaption{padding:8px;color:#c7c8c3}.concept{max-height:850px;object-fit:contain}nav{display:flex;gap:18px;flex-wrap:wrap}@media(max-width:750px){.pair{grid-template-columns:1fr}}</style>
<h1>Blackwater Maw: corrected spined fins</h1>
<p>Four dorsal fins now sweep toward the tail, with raised supporting rays, recessed membranes, real perforations and irregular trailing edges. The large forward pectoral pair and smaller pelvic pair occupy different body stations and use different shapes and bindings. The broad two-lobed tail fan retains its expanded membrane area. Raised flank blocks remain removed; overlapping scale detail is carried by the owned skin texture.</p>
<p><strong>7,224 triangles · 1.483× Serpent · soft cap 7,308</strong><br>One mesh, one material and four 512px atlas maps. 808 verified buried triangles removed from the working mesh. At most two normalized bone influences per vertex. The body and jaw keep their previous scale; the broad tail extends overall art-study length to approximately 10.52 m.</p>
<p>112 saved attachment records pass across 65 sampled poses from 13 retained actions. Saved measurements confirm tailward dorsal tips, approximately 3.07 m between the lower fin root stations, and 4.66× the old tail's projected membrane area. This remains an uninstalled art candidate; full animation and native performance acceptance are pending.</p>'''
page+=f'<nav><a href="{rel(ROOT/r["source"])}">Editable Blender source</a><a href="{r["id"]}.glb">Skinned GLB</a><a href="fin-direction-verification.json">Direction and root spacing</a><a href="fin-inventory-verification.json">Fin inventory and tail area</a><a href="fin-contours.json">Surface construction</a><a href="attachment-pose-verification.json">Sampled contacts</a><a href="verification.json">Geometry and export</a><a href="../blackwater-maw-jaws-r5/gallery.html">Previous jaw study</a></nav>'
page+=f'<h2>Concept reference</h2><img class="concept" src="{rel(ROOT/"docs/art/creature-concepts/2026-10-03/the-blackwater-maw.png")}" alt="Blackwater Maw concept: swept supporting spines and ragged membranes">'
page+=f'<h2>Current profile and anatomy</h2><div class="pair"><figure><img src="{r["id"]}--side.png" alt="Tailward dorsal tips and separate pectoral and pelvic fins"><figcaption>Corrected profile: four tailward dorsals, large forward pair, separate smaller pelvic pair</figcaption></figure><figure><img src="{r["id"]}--three-quarter.png" alt="Swept fin surfaces and preserved pointed jaws"><figcaption>Current three-quarter view</figcaption></figure></div>'
for name,title in [('fin-inventory','Fin surface comparison'),('tail-fan','Broad tail fan comparison')]:
 page+=f'<h2>{title}</h2><div class="pair">'
 for label,caption in [('before','Previous jaw study'),('after','Current spined fins')]:page+=f'<figure><img loading="lazy" src="{label}--{name}--textured.png" alt="{caption}"><figcaption>{caption}</figcaption></figure>'
 page+='</div><div class="pair">'
 for label,caption in [('before','Previous geometry'),('after','Current geometry')]:page+=f'<figure><img loading="lazy" src="{label}--{name}--clay.png" alt="{caption}"><figcaption>{caption}</figcaption></figure>'
 page+='</div>'
page+=f'<h2>Owned scale texture</h2><p>Generated flat albedo tile, wrapped along the body and baked with derived relief/roughness into the compact atlas. The source tile is not an additional runtime texture.</p><img class="concept" src="{rel(ROOT/"assets/textures/underworld/bosses/blackwater-maw-skin-study/blackwater-maw-scales-albedo.png")}" alt="Owned overlapping fish-scale texture"><h2>Inherited bite pose</h2><img src="{r["id"]}--attack.png" alt="Inherited bite pose"></html>'
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,value in attrs:
   if key in ('src','href') and value:assert (OUT/value).is_file(),value
Links().feed(page);(OUT/'gallery.html').write_text(page,encoding='utf-8')
files=[ROOT/r['source'],ROOT/'docs/art/creature-concepts/2026-10-03/the-blackwater-maw.png',ROOT/'assets/textures/underworld/bosses/blackwater-maw-skin-study/blackwater-maw-scales-albedo.png']+list((ROOT/'tools').glob('*blackwater*maw*.py'))+[q for q in OUT.iterdir() if q.is_file() and q.name!='provenance.json']+list((ROOT/'assets/textures/underworld/bosses/blackwater-maw-spined-fins-r8').glob('*.png'))
(OUT/'provenance.json').write_text(json.dumps({q.relative_to(ROOT).as_posix():hashlib.sha256(q.read_bytes()).hexdigest() for q in files},indent=2)+'\n')
print('VERIFIED spined-fin review: 7224 triangles, tailward tips, separate pelvic roots, four dorsals, broad tail and linked comparisons')
