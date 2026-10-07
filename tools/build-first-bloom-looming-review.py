"""Build a checked posture comparison from the preceding attached-cavity review."""
from pathlib import Path
from html.parser import HTMLParser
import json,hashlib,struct,re
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-looming-r10'
r=json.loads((OUT/'refinement-metrics.json').read_text());v=json.loads((OUT/'verification.json').read_text());a=json.loads((OUT/'attachment-verification.json').read_text());p=json.loads((OUT/'attachment-pose-verification.json').read_text());posture=json.loads((OUT/'chest-posture.json').read_text())
assert r['triangles']==r['previous_triangles']==9252 and r['triangles']<=r['soft_cap']
assert r['materials']==r['renderers']==1 and r['atlas_size']==512
assert a['endpoints']==121 and all(c['all_endpoint_vertices_inside_support'] for c in a['checks'])
rim=next(c for c in a['checks'] if c['end']=='complete_body_flange');assert rim['endpoint_vertices']==48
assert p['result']=='PASS' and p['max_outside_distance_metres']<=.001
assert posture['forward_pitch_degrees']==12 and posture['triangles_added']==0
assert hashlib.sha256((ROOT/r['source']).read_bytes()).hexdigest()==v['source_sha256']
data=(OUT/(r['id']+'.glb')).read_bytes();size,kind=struct.unpack_from('<II',data,12);doc=json.loads(data[20:20+size]);primitive=doc['meshes'][0]['primitives'][0];assert doc['accessors'][primitive['indices']]['count']//3==9252
page=(ROOT/'docs/art/creature-models/2026-10-04/first-bloom-seated-r9/gallery.html').read_text(encoding='utf-8')
page=page.replace('first-bloom-seated-r9','first-bloom-looming-r10').replace('first-bloom-fan-r8','first-bloom-seated-r9')
page=page.replace('Revision 9','Revision 10').replace('Revision 8','Revision 9')
page=page.replace('First Bloom cavity attached to torso','First Bloom recessed looming chest').replace('First Bloom: cavity attached to torso','First Bloom: recessed looming chest')
page=re.sub(r'(<h1>.*?</h1>)<p>.*?</p>',r'\1<p>The attached chest assembly is recessed farther into the torso and pitched forward by 12 degrees around its lower hinge. The top moves forward and down; the lower interior approaches the hip joint. The surrounding front torso follows the lean with a smooth transition toward the waist. The radial shelf fan and fitted body flange remain connected.</p>',page,count=1)
page=page.replace('Floating cavity → fitted body collar','Attached cavity → deeper, forward-pitched chest')
page=page.replace('cavity separated from torso','upright attached cavity').replace('cavity seated into torso','recessed forward-pitched cavity')
page=page.replace('exposed cavity edge','previous chest position').replace('fitted torso lip','recessed chest and attached lip')
page=page.replace('gap behind the cavity rim','previous chest depth').replace('torso collar surrounds the cavity','pitched chest, retained body collar')
page=page.replace('--detail.png','--side.png').replace('Whole creature comparison','Whole creature profile comparison')
page=page.replace('Concept reference','Concept reference and revised chest')
(OUT/'gallery.html').write_text(page,encoding='utf-8')
class Links(HTMLParser):
 def handle_starttag(self,tag,attrs):
  for key,value in attrs:
   if key in ('src','href') and value:assert (OUT/value).is_file(),value
Links().feed(page)
files=[ROOT/r['source'],ROOT/'assets/models/studies/underworld-boss-first-bloom-seated-r9.blend',ROOT/'docs/art/creature-concepts/2026-10-03/the-first-bloom.png']
files+=list((ROOT/'tools').glob('*first*bloom*.py'))+[ROOT/'tools/author-boss-concept-models.py',ROOT/'tools/creature_apex_authoring.py']
files+=list((ROOT/'assets/textures/underworld/bosses/first-bloom-looming-r10').glob('*.png'))+[q for q in OUT.iterdir() if q.is_file() and q.name!='provenance.json']
(OUT/'provenance.json').write_text(json.dumps({q.relative_to(ROOT).as_posix():hashlib.sha256(q.read_bytes()).hexdigest() for q in files},indent=2)+'\n')
print('VERIFIED looming chest: posture, 48-vertex body flange, 121 contacts, sampled poses, export and review links')
