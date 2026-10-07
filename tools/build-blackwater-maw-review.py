"""Publish measured concept/native/previous-study comparisons for the Maw."""
from pathlib import Path
from html.parser import HTMLParser
import json,hashlib,os,struct,shutil
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-refinement-r4';RAW=OUT.with_name('blackwater-maw-refinement-r3')
r=json.loads((OUT/'refinement-metrics.json').read_text());v=json.loads((OUT/'verification.json').read_text());a=json.loads((OUT/'attachment-verification.json').read_text());p=json.loads((OUT/'attachment-pose-verification.json').read_text());trim=json.loads((OUT/'trim-result.json').read_text());bound=json.loads((OUT/'trim-boundary-verification.json').read_text())
assert r['triangles']==v['triangles']==6190<=r['soft_cap']==7308
assert r['donor_triangles']==4872 and r['materials']==r['renderers']==1 and r['atlas_size']==512
assert a['endpoints']==p['endpoints']==153 and all(i['all_endpoint_vertices_inside_support'] for i in a['checks'])
assert p['result']=='PASS' and p['sampled_poses']==65 and p['retained_actions']==13
assert trim['removed_triangles']==578 and bound['result'].startswith('PASS')
assert hashlib.sha256((ROOT/r['source']).read_bytes()).hexdigest()==v['source_sha256']
for kind in ('albedo','normal','roughness','emission'):
 before=ROOT/f'assets/textures/underworld/bosses/blackwater-maw-refinement-r3/underworld-boss-blackwater-maw-refinement-r3-{kind}.png';after=ROOT/f'assets/textures/underworld/bosses/blackwater-maw-refinement-r4/underworld-boss-blackwater-maw-refinement-r4-{kind}.png'
 assert hashlib.sha256(before.read_bytes()).digest()==hashlib.sha256(after.read_bytes()).digest()
for name in ('anatomy.json','attachment-authoring.json'):shutil.copyfile(RAW/name,OUT/name)
docdata=(OUT/(r['id']+'.glb')).read_bytes();length,kind=struct.unpack_from('<II',docdata,12);doc=json.loads(docdata[20:20+length]);primitive=doc['meshes'][0]['primitives'][0]
assert len(doc['meshes'])==len(doc['materials'])==1 and doc['accessors'][primitive['indices']]['count']//3==6190
def rel(path):return os.path.relpath(path,OUT).replace('\\','/')
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Blackwater Maw — concept refinement</title>
<style>body{background:#13191d;color:#e4dfd6;font:16px system-ui;margin:0 auto;padding:28px;max-width:1500px}a{color:#77c7d1}h1,h2{font-weight:600}p{line-height:1.6;max-width:1100px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}.three{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}figure{margin:0}img{width:100%;border-radius:8px;background:#1a2329}figcaption{padding:8px;color:#c7c8c3}.concept{max-height:850px;object-fit:contain}nav{display:flex;gap:18px;flex-wrap:wrap}@media(max-width:750px){.pair,.three{grid-template-columns:1fr}}</style>
<h1>Blackwater Maw: first concept refinement</h1>
<p>A continuous tapered body replaces separate body cylinders. The upper skull carries fractured brow and cheek armour; the articulated lower gum bowl and connected cheek tissue form a deep gape. Forty-four uneven fangs are rooted in the gums. Physical flank scales, throat folds and cyan sensory sockets add detail without exceeding the donor budget. Closed pleated pectorals, a ragged dorsal sail and a forked caudal fan replace disconnected fin rods.</p>
<p><strong>6,190 triangles · 1.271× Serpent · soft cap 7,308</strong><br>One mesh, one material, four 512px atlas maps, at most two normalized bone influences per vertex. 6,768 triangles before conservative hidden-face trimming; 578 removed. The previous detail study used 31,412 triangles. Art-study length remains 10 m.</p>
<p>153 saved attachment records pass, including 65 pose samples across all 13 retained actions. Saved cut boundaries remain inside retained closed supports. UVs and atlas pixels are unchanged by trimming. These are offline checks; this uninstalled study still needs concept/detail acceptance, every-frame deformation review and native runtime/performance acceptance. Comparable native donor quality remains the visual acceptance bar.</p>'''
page+=f'<nav><a href="{rel(ROOT/r["source"])}">Editable Blender source</a><a href="{r["id"]}.glb">Skinned GLB</a><a href="verification.json">Geometry and export checks</a><a href="attachment-pose-verification.json">All-action sampled contacts</a><a href="buried-face-audit.json">Buried-face audit</a><a href="trim-boundary-verification.json">Saved cut boundaries</a><a href="../../REFINEMENT-STANDARDS.md">Refinement standards</a><a href="../first-bloom-arm-seams-r22/gallery.html">First Bloom reference procedure</a></nav>'
page+=f'<h2>Concept anatomy and surfaces</h2><img class="concept" src="{rel(ROOT/"docs/art/creature-concepts/2026-10-03/the-blackwater-maw.png")}" alt="Blackwater Maw adopted concept">'
for view,title in [('three-quarter','Whole creature'),('front','Gape and lateral symmetry'),('side','Skull, jaw, body and fin profile'),('clay','Geometry without textures')]:
 page+=f'<h2>{title}</h2><div class="three">'
 for filename,caption in [(f'before-blackwater-maw--{view}.png','Previous detail study — 31,412 triangles'),(f'{r["id"]}--{view}.png','Current study — 6,190 triangles'),(f'donor-Serpent--{view}.png','Native Serpent quality reference — 4,872 triangles')]:
  page+=f'<figure><img loading="lazy" src="{filename}" alt="{caption}, {view}"><figcaption>{caption}</figcaption></figure>'
 page+='</div>'
page+='<p>Whole-creature views use the established bounds-fitted orthographic framing and proportional lighting. The native donor is a detail/cost reference, rather than the concept anatomy or encounter scale.</p><h2>Current mouth detail and inherited attack pose</h2><div class="pair">'
for view,caption in [('detail','Armoured skull, fang roots, inner gum bowl and throat'),('attack','Inherited DeepHunter_Bite pose — sampled support checks pass')]:page+=f'<figure><img src="{r["id"]}--{view}.png" alt="{caption}"><figcaption>{caption}</figcaption></figure>'
page+='</div></html>'
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,value in attrs:
   if key in ('src','href') and value:assert (OUT/value).is_file(),value
Links().feed(page);(OUT/'gallery.html').write_text(page,encoding='utf-8')
files=[ROOT/r['source'],ROOT/'assets/models/source/underworld-boss-blackwater-maw-detail-r2.blend',ROOT/'docs/art/creature-concepts/2026-10-03/the-blackwater-maw.png',ROOT/'dist/boss-donor-quality/native-source/Serpent.blend',ROOT/'docs/art/creature-models/REFINEMENT-STANDARDS.md']+list((ROOT/'tools').glob('*blackwater*maw*.py'))+[q for q in OUT.iterdir() if q.is_file() and q.name!='provenance.json']+list((ROOT/'assets/textures/underworld/bosses/blackwater-maw-refinement-r4').glob('*.png'))
(OUT/'provenance.json').write_text(json.dumps({q.relative_to(ROOT).as_posix():hashlib.sha256(q.read_bytes()).hexdigest() for q in files},indent=2)+'\n')
print('VERIFIED Blackwater Maw gallery: native/reference comparisons, 6190 triangles, 153 contacts, 65 poses, unchanged trim atlas and artifact links')
