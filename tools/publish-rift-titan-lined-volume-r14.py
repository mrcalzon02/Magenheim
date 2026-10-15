"""Evidence and rolling repair record for the specifically directed head/neck pass."""
from pathlib import Path
import hashlib,html,json,re
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-lined-volume-r14'
ID='underworld-boss-rift-titan-lined-volume-r14';BEFORE='underworld-boss-rift-titan-fractured-head-r13'
metrics=json.loads((OUT/'refinement-metrics.json').read_text());proof=json.loads((OUT/'volume-fracture-neck-verification.json').read_text())
assert metrics['triangles']==1800 and proof['result']=='PASS'
for name in ('verification','head-verification','body-preservation','neck-head-face-verification'):
 assert json.loads((OUT/(name+'.json')).read_text())['result'].startswith('PASS')
before=json.loads((OUT/'before/provenance.json').read_text());assert before['source_sha256']==hashlib.sha256((ROOT/before['source']).read_bytes()).hexdigest()
backlog=json.loads((OUT.parent/'rift-titan-systematic-audit-r12/findings.json').read_text());rows=[]
for f in backlog['findings']:
 row=dict(id=f['id'],priority=f['priority'],status='open')
 if f['id']=='H01':row.update(accepted_scope='User accepted r13 front outline; all twenty X/Z landmarks preserved exactly.',user_evidence="Good good you've got the outer shape looking right now")
 if f['id'] in ('H03','H04','H05','H06','M07'):row['repair_state']='r14 specific head depth/rake and asymmetric crystal lining candidate; further visual review pending'
 else:row['repair_state']='preserved; no new acceptance inferred'
 rows.append(row)
progress=dict(revision='r14',date='2026-10-14',user_direction='Fracture mainly frontal, asymmetric and receding back on the left, lined with purple crystal; head must have real side volume and neck must be three-dimensional.',findings=rows,open_findings=44,closed_findings=0,adequate_bosses=1,accepted_scope='r13 frontal head outline only',art_acceptance='r14 pending; boss remains unaccepted',numerical_forecast=None)
progress['user_followups']=[dict(id='N01',finding='Neck was a near-planar array of iron bars',repair='Six bars distributed across front/back arcs with complete collar/head sections',status='r14 candidate; visual acceptance pending')]
(OUT/'repair-status.json').write_text(json.dumps(progress,indent=2)+'\n',encoding='utf-8')
md=f'''# Rift Titan r14 — head volume, left-receding lining and neck depth

The user accepted r13's frontal head outline, then identified the wrong crack,
flat neck and forward-pointing slab profile. This pass preserves that accepted
front outline while rebuilding the head in depth. The r14 result is a candidate;
the whole boss remains unaccepted and the numerical forecast stays withdrawn.

[Comparison gallery](gallery.html) · [Saved measurements](volume-fracture-neck-verification.json)
· [Rolling findings](repair-status.json) · [Original audit](../rift-titan-systematic-audit-r12/AUDIT.md)

- **Front:** all twenty approved outline X/Z landmarks have zero difference
  against the actual saved r13 mesh.
- **Side:** the head has changing prismatic depth and broken side planes.
  Its summit now recedes {proof['tip_recession_from_base_metres']:.3f} m behind
  the bottom point, correcting the former forward rake. Closed head volume
  changed from {proof['previous_head_volume_cubic_metres']:.3f} to
  {proof['head_volume_cubic_metres']:.3f} cubic metres. Increased volume is a
  geometric measurement, not an art-match score.
- **Fracture:** seven irregular stations form unequal banks. The face recedes
  on the head's anatomical left (-X); that bank/floor goes farther back than
  the right. Purple crystal lines the sloped inner wall and floor, with
  facet-based color and restrained emission. The crack stays predominantly
  frontal, recessed inside one closed head with a continuous back.
- **Neck:** the same six 14-triangle bars occupy front and back arcs, with
  independently spaced middle sections and complete collar/head attachments.
  Their middle centers span {proof['neck_middle_extent_metres'][0]:.3f} m across
  and {proof['neck_middle_extent_metres'][1]:.3f} m front-to-back. Isolated saved
  head/neck/collar clay views expose the arrangement without shoulder occlusion.

## Cost and verification

Actor remains **1,800 triangles / native Stone Golem 1,200 / soft cap 1,800**.
Head remains 116 triangles and 60 vertices; neck remains 84 triangles. Eight
redundant rear perimeter vertices were condensed to twelve rear landmarks,
reinvesting sixteen triangles in two additional visible fracture stations.
One mesh/material, four packed 512px maps, eight original bones and seven
retained actions remain. Other body/limb/core geometry has zero coordinate
difference against saved r13, excluding the remodeled head and six neck bars.

The head is one connected closed surface with Euler characteristic 2. Purple
lining is verified on twelve left-wall triangles and twelve floor triangles;
its total nominal atlas UV area is {proof['lining_nominal_uv_texel_area']:.1f}
texels at 512px, not that count per triangle. Both ends of all six neck bars
pass 3,024 closing-face interior/edge samples across 36 poses. The inherited
support, arm/digit, limb-crystal and thorax checks pass, along with normalized
weights, finite sampled poses and the skinned GLB export.

Actual saved r13 is re-rendered using exact r14 camera transforms, scales,
resolution and lighting. True left profile is shown separately from the oblique
concept angle. Anatomical +Y projects screen-left. Original concepts and all
prior saved studies remain unchanged. Local-file browser automation is unused;
source images and actual generated renders were inspected directly.

## Remaining limits

The head still needs finer chipped layers and crisp stone treatment; the main
body remains an early concept interpretation. Crest architecture, shoulders,
cage/core density, hands, shin/foot ordering and other audit findings are not
repaired here. All 44 baseline entries remain tracked as open, with the user's
specific frontal-outline acceptance recorded separately. Adequate bosses stay
1/6. Offline geometry does not establish art acceptance or native deformation,
collision, loader admission or frame cost. No runtime package is changed.
'''
(OUT/'REPAIR.md').write_text(md,encoding='utf-8')
sheet='../../../creature-concepts/2026-10-03/the-rift-titan.png'
def image(label,path):return '<figure><figcaption>'+label+'</figcaption><a href="'+path+'"><img src="'+path+'" alt="'+label+'"></a></figure>'
def concept(label,box):return f'<figure><figcaption>{label} — original concept</figcaption><svg viewBox="{box}"><image href="{sheet}" width="1536" height="1024"/></svg></figure>'
page=['<!doctype html><html lang="en"><meta charset="utf-8"><title>Rift Titan r14 volume and fracture</title><style>body{background:#171d20;color:#eee8dc;font:17px/1.5 system-ui;max-width:1500px;margin:25px auto;padding:20px}a{color:#b8ccf2}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px}figure{margin:0;background:#242b2e}figcaption{padding:12px}img,svg{width:100%;height:480px;object-fit:contain}section{margin:35px 0}@media(max-width:800px){.grid{grid-template-columns:1fr}}</style>',
 '<h1>Rift Titan r14 — head volume, lined fracture and neck depth</h1><p>Approved r13 front outline preserved. New volume, backward rake, specifically left-receding purple lining and six bars arranged across the neck depth. <b>1,800 triangles. Candidate awaiting art review.</b></p><p><a href="REPAIR.md">Changes and limits</a> · <a href="volume-fracture-neck-verification.json">Measured preservation and depth</a> · <a href="repair-status.json">Rolling findings</a> · <a href="'+ID+'.glb">Portable skinned model</a></p><p>Before/after use exact matching cameras and lighting. The painted concept has different perspective; the true profile is shown separately.</p>']
for title,suffix,box in [('Front','crown-front','973 17 78 137'),('Left-facing oblique','crown-side','1225 18 115 139'),('Three-quarter','crown-detail','580 8 180 340')]:
 page+=['<section><h2>'+title+'</h2><div class="grid">',concept(title,box),image('r13 — previous saved model','before/'+BEFORE+'--'+suffix+'.png'),image('r14 — current candidate',ID+'--'+suffix+'.png'),'</div></section>']
page+=['<section><h2>True left profile — shape and depth</h2><div class="grid">',image('r13 profile clay','before/'+BEFORE+'--crown-profile-clay.png'),image('r14 profile clay',ID+'--crown-profile-clay.png'),image('r14 true profile material',ID+'--crown-profile.png'),'</div></section>', '<section><h2>Isolated saved neck geometry</h2><p>Only the actual head, six neck bars and collar are displayed; no review geometry added.</p><div class="grid">',image('Neck — front',ID+'--neck-front.png'),image('Neck — left side',ID+'--neck-side.png'),image('Head and neck — above',ID+'--crown-top.png'),'</div></section>', '<section><h2>Whole creature</h2><div class="grid">',image('Front',ID+'--front.png'),image('Left-facing side',ID+'--side.png'),image('Sampled attack',ID+'--attack.png'),'</div></section><p>Head surface detail and the other audit regions remain unfinished. Native runtime acceptance is pending; adequate boss tally remains 1/6.</p></html>']
page='\n'.join(page);(OUT/'gallery.html').write_text(page,encoding='utf-8')
for path in re.findall(r'(?:href|src)="([^"]+)"',page):assert (OUT/path).resolve().is_file(),path
evidence=[ROOT/metrics['source'],ROOT/before['source'],OUT/(ID+'.glb'),(OUT/sheet).resolve()]
evidence+=list(OUT.glob('*.png'))+list((OUT/'before').glob('*.png'))
evidence+=list((ROOT/'assets/textures/underworld/bosses/rift-titan-lined-volume-r14').glob('*.png'))
evidence+=[ROOT/'tools'/name for name in ('rift_titan_head_volume.py','refine-rift-titan-lined-volume-r14.py','review-rift-titan-lined-volume-r14.py','review-rift-titan-lined-before-r14.py','review-rift-titan-neck-volume-r14.py','verify-rift-titan-lined-volume-r14.py','publish-rift-titan-lined-volume-r14.py')]
(OUT/'provenance.json').write_text(json.dumps(dict(hashes={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in evidence},previous_source_unchanged=True,approved_front_outline_preserved=True,art_acceptance=False),indent=2)+'\n',encoding='utf-8')
(OUT/'artifact-validation.json').write_text(json.dumps(dict(result='PASS',links_exist=True,previous_source_hash_verified=True,source_sha256=hashlib.sha256((ROOT/metrics['source']).read_bytes()).hexdigest(),open_findings=44,accepted_scope='r13 front outline only',browser_visual_validation='not performed; local-file browser policy respected'),indent=2)+'\n',encoding='utf-8')
print('VERIFIED r14 comparison artifacts, specific front-outline acceptance and preserved prior source')
