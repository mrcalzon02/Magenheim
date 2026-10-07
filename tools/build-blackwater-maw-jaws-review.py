"""Deliver the user's oval/pointed jaw correction with preserved comparisons."""
from pathlib import Path
from html.parser import HTMLParser
import json,hashlib,os,shutil,struct
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-jaws-r5';RAW=OUT.with_name('blackwater-maw-jaws-r5-raw')
r=json.loads((OUT/'refinement-metrics.json').read_text());v=json.loads((OUT/'verification.json').read_text());a=json.loads((OUT/'attachment-verification.json').read_text());p=json.loads((OUT/'attachment-pose-verification.json').read_text());trim=json.loads((OUT/'trim-result.json').read_text());bound=json.loads((OUT/'trim-boundary-verification.json').read_text())
assert r['triangles']==v['triangles']==6412<=r['soft_cap']==7308 and r['donor_triangles']==4872
assert r['materials']==r['renderers']==1 and r['atlas_size']==512
assert a['endpoints']==p['endpoints']==155 and all(c['all_endpoint_vertices_inside_support'] for c in a['checks'])
assert p['result']=='PASS' and p['sampled_poses']==65 and p['retained_actions']==13
assert trim['removed_triangles']==728 and bound['result'].startswith('PASS')
assert hashlib.sha256((ROOT/r['source']).read_bytes()).hexdigest()==v['source_sha256']
data=(OUT/(r['id']+'.glb')).read_bytes();size,kind=struct.unpack_from('<II',data,12);doc=json.loads(data[20:20+size]);primitive=doc['meshes'][0]['primitives'][0]
assert len(doc['meshes'])==len(doc['materials'])==1 and doc['accessors'][primitive['indices']]['count']//3==6412
for name in ('anatomy.json','attachment-authoring.json'):shutil.copyfile(RAW/name,OUT/name)
for kind in ('albedo','normal','roughness','emission'):
 old=ROOT/f'assets/textures/underworld/bosses/blackwater-maw-jaws-r5-raw/underworld-boss-blackwater-maw-jaws-r5-raw-{kind}.png';new=ROOT/f'assets/textures/underworld/bosses/blackwater-maw-jaws-r5/underworld-boss-blackwater-maw-jaws-r5-{kind}.png'
 assert hashlib.sha256(old.read_bytes()).digest()==hashlib.sha256(new.read_bytes()).digest()
def rel(path):return os.path.relpath(path,OUT).replace('\\','/')
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Blackwater Maw: oval gape and pointed jaws</title>
<style>body{background:#13191d;color:#e4dfd6;font:16px system-ui;margin:0 auto;padding:28px;max-width:1500px}a{color:#77c7d1}h1,h2{font-weight:600}p{line-height:1.6;max-width:1100px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}img{width:100%;border-radius:8px;background:#1a2329}figcaption{padding:8px;color:#c7c8c3}.concept{max-height:850px;object-fit:contain}nav{display:flex;gap:18px;flex-wrap:wrap}@media(max-width:750px){.pair{grid-template-columns:1fr}}</style>
<h1>Blackwater Maw: oval gape and pointed jaws</h1>
<p>The frontal mouth is a tall tapered oval, with a peaked upper brow and a pointed lower-jaw chin. The upper and lower tooth beds curve back toward the cheek hinges; the lower jaw projects as a separate U-shaped structure in profile. The palate is broad rather than a long flared funnel. A raised mouth floor, fleshy posterior wall and throat folds form the interior between the jaws.</p>
<p><strong>6,412 triangles · 1.316× native Serpent · soft cap 7,308</strong><br>7,140 before trimming; 728 wholly buried triangles removed. One mesh, one material, four 512px atlas maps and at most two normalized bone influences per vertex. 10 m art-study length.</p>
<p>155 saved attachment records pass, including 65 sampled poses across 13 retained actions. Contact checks retain a one-millimetre tolerance and confirm ambiguous ray hits with an independent closed-support winding query. This remains an uninstalled art candidate; every-frame animation, native runtime/performance and final concept-detail acceptance remain pending.</p>'''
page+=f'<nav><a href="{rel(ROOT/r["source"])}">Editable Blender source</a><a href="{r["id"]}.glb">Skinned GLB</a><a href="verification.json">Geometry and export</a><a href="attachment-pose-verification.json">All-action sampled contacts</a><a href="buried-face-audit.json">Buried-face audit</a><a href="trim-boundary-verification.json">Saved cut boundaries</a><a href="../blackwater-maw-refinement-r4/gallery.html">Earlier refinement and native donor comparisons</a><a href="../../REFINEMENT-STANDARDS.md">Refinement standards</a></nav>'
page+='<h2>Your front and side shape guide</h2><img class="concept" src="mouth-shape-reference.png" alt="User reference showing tall oval mouth, pointed chin and separate jaws in profile">'
for view,title in [('front','Frontal circle → tall pointed oval'),('side','Hinged jaw in profile'),('detail','Tooth beds, palate and mouth floor'),('clay','Actual geometry without textures')]:
 page+=f'<h2>{title}</h2><div class="pair">'
 for filename,caption in [(f'../blackwater-maw-refinement-r4/underworld-boss-blackwater-maw-refinement-r4--{view}.png','Before — round flared mouth'),(f'{r["id"]}--{view}.png','After — oval gape and defined jaws')]:page+=f'<figure><img loading="lazy" src="{filename}" alt="{caption}"><figcaption>{caption}</figcaption></figure>'
 page+='</div>'
page+='<h2>Whole creature and inherited bite pose</h2><div class="pair">'
for view in ('three-quarter','attack'):page+=f'<figure><img src="{r["id"]}--{view}.png" alt="Revised creature {view}"><figcaption>{view}</figcaption></figure>'
page+=f'</div><h2>Complete concept</h2><img class="concept" src="{rel(ROOT/"docs/art/creature-concepts/2026-10-03/the-blackwater-maw.png")}" alt="Blackwater Maw complete concept"></html>'
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,value in attrs:
   if key in ('src','href') and value:assert (OUT/value).is_file(),value
Links().feed(page);(OUT/'gallery.html').write_text(page,encoding='utf-8')
files=[ROOT/r['source'],ROOT/'docs/art/creature-concepts/2026-10-03/the-blackwater-maw.png',ROOT/'docs/art/creature-models/REFINEMENT-STANDARDS.md',ROOT/'tools/maw_contact_validation.py']+list((ROOT/'tools').glob('*blackwater*maw*.py'))+[q for q in OUT.iterdir() if q.is_file() and q.name!='provenance.json']+list((ROOT/'assets/textures/underworld/bosses/blackwater-maw-jaws-r5').glob('*.png'))
(OUT/'provenance.json').write_text(json.dumps({q.relative_to(ROOT).as_posix():hashlib.sha256(q.read_bytes()).hexdigest() for q in files},indent=2)+'\n')
print('VERIFIED Blackwater Maw pointed-jaw gallery: 6412 triangles, 155 contacts, 65 poses, atlas hashes and review links')
