"""Publish evidence for the first audited head repair, without closing findings."""
from pathlib import Path
import hashlib,html,json,re,struct
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-fractured-head-r13'
ID='underworld-boss-rift-titan-fractured-head-r13'
BEFORE='underworld-boss-rift-titan-cracked-head-r12'
concept=ROOT/'docs/art/creature-concepts/2026-10-03/the-rift-titan.png'
audit=OUT.parent/'rift-titan-systematic-audit-r12/findings.json'
backlog=json.loads(audit.read_text(encoding='utf-8'))
metrics=json.loads((OUT/'refinement-metrics.json').read_text())
assert metrics['triangles']==1800 and metrics['category_triangles']['head-crystal']==116
metrics['changes']='head only: tapered shell discarded; twenty-corner extruded slab perimeter with long straight shaft edges, abrupt shoulders and triangular lower breaks; four cortex fracture points, recessed variable fault and six reseated neck ends'
metrics['status']='unaccepted replacement after user rejected the still-oval first r13 attempt'
(OUT/'refinement-metrics.json').write_text(json.dumps(metrics,indent=2)+'\n',encoding='utf-8')
author=json.loads((OUT/'head-authoring.json').read_text())
author['construction_chain_removed_head_triangles']=author.pop('old_head_triangles',116)
author['previous_review_head_triangles']=108
author['preserved']='body/limb/core geometry verified against the actual saved r12; only head and six neck sections excluded'
(OUT/'head-authoring.json').write_text(json.dumps(author,indent=2)+'\n',encoding='utf-8')
for name in ('verification','head-verification','body-preservation','neck-head-face-verification'):
 report=json.loads((OUT/(name+'.json')).read_text());assert report['result'].startswith('PASS')
assert json.loads((OUT/'body-preservation.json').read_text())['max_coordinate_difference_metres']==0
before=json.loads((OUT/'before/provenance.json').read_text())
assert before['source_sha256']==backlog['source_sha256']==hashlib.sha256((ROOT/before['source']).read_bytes()).hexdigest()
changes={
 'H01':dict(change='The tapered shell was discarded after the user rejected its still-oval shape. An explicit twenty-corner extruded slab perimeter now has long straight sides, abrupt ledges, an uneven summit and triangular lower wings.',remaining='The slab is still a coarse interpretation; finer local breaks and reference-led facet proportions remain unresolved.'),
 'H02':dict(change='Four integrated cortex fracture points interrupt the broad slab faces; front/back share the polygon perimeter and the fault remains recessed into one connected shell.',remaining='The source has a much denser hierarchy of chips, staggered layers and secondary fracture planes. This is only a first structural repair.'),
 'H03':dict(change='The connected fault changes direction, width and depth; asymmetric bevels expose a recessed violet floor backed by a solid shell.',remaining='The opening still reads too cleanly in places, and the violet surface lacks the reference crystalline facet hierarchy.'),
 'H04':dict(change='The lower reveal widens between broken, unequal cortex edges before closing into the connected base.',remaining='The lower tip remains too simple; fractured ledges and the violet-to-stone transition need further fitting.'),
}
status=[]
for f in backlog['findings']:
 row=dict(id=f['id'],priority=f['priority'],status='open')
 if f['id'] in changes:row.update(repair_state='first repair candidate; user acceptance pending',**changes[f['id']])
 else:row['repair_state']='not changed in this head-only pass'
 status.append(row)
progress=dict(date='2026-10-14',revision='r13',baseline='r12',findings=status,open_findings=44,closed_findings=0,adequate_bosses=1,acceptance='unaccepted',percentage_forecast=None)
(OUT/'repair-status.json').write_text(json.dumps(progress,indent=2)+'\n',encoding='utf-8')
md=['# Rift Titan r13 — first audited head repair','',
 'This is an unaccepted head repair candidate for **H01–H04**. All 44 baseline findings remain open. The original r12 audit and source are preserved. No concept-match percentage or completion forecast is claimed.','',
 '[Comparison gallery](gallery.html) · [Structured repair status](repair-status.json) · [Original audit](../rift-titan-systematic-audit-r12/AUDIT.md)','',
 '| Finding | Authored change | Still unresolved |','| --- | --- | --- |']
md += [f"| {k} | {v['change']} | {v['remaining']} |" for k,v in changes.items()]
md += ['', '## Visual review','',
 'The user rejected the first r13 attempt during work: the head was still oval. That attempt had only modified the same tapered ring shell. It was discarded before commit. The replacement is an explicit extruded polygonal slab with twenty perimeter corners, long straight shaft edges, abrupt shoulders and triangular lower breaks. Its solid front/side views were inspected before the full material review. The model still lacks the reference’s smaller layered fracture construction and crisp stone surface treatment. This pass does not solve H02, H05, H06 or M07.','',
 'The previous head is rendered from the actual saved r12 source using the exact camera transforms, scales, resolution and lighting of the current head views. Concept panels are labelled original source views; their perspective differs from the orthographic model. Side views project anatomical +Y toward screen-left. No source image is mirrored or retouched.','',
 '## Cost and verification','',
 '- Actor: **1,800 triangles**, versus native Stone Golem **1,200** and soft cap **1,800**. Head: **116 triangles / 60 vertices**, up from 108 / 56. The eight triangles reserved in r12 were spent on integrated fracture planes; no body triangles were removed.',
 '- One mesh/material, four packed 512px maps, original eight-bone hierarchy and seven actions. The owned r10 material recipe was re-baked for the new topology; the cloudy stone appearance remains an open defect.',
 '- One connected closed head, Euler characteristic 2; shared floor/bevel edges, purple floor recessed behind the lips and a continuous rear wall. Six complete neck end sections are seated into the head with Body weights.',
 '- Neck closing faces: 1,512 interior/edge samples across rest plus 35 retained-action samples. Inherited arm/digit, limb-crystal, thorax and support checks also pass.',
 '- Every retained body/limb/core component compared with the actual saved r12 has identical face/vertex counts and **zero coordinate difference**. Only the head and its six reseated neck sections are excluded.',
 '- Saved model geometry, normalized compact weights, atlas, finite sampled pose bounds and the skinned GLB export pass the offline review. These checks do not establish native runtime deformation, collision, performance or art acceptance.','',
 '## Next repair priorities','',
 'Continue the head’s fractured layer construction and surface hierarchy before treating it as matched. The missing raised crest, torso cage/core, hand reach, shin/foot ordering and all other audit regions remain unchanged. Boss tally remains **1/6 adequate**.','']
(OUT/'REPAIR.md').write_text('\n'.join(md),encoding='utf-8')
sheet='../../../creature-concepts/2026-10-03/the-rift-titan.png'
def reference(title,box):
 x,y,w,h=box
 return f'<figure><figcaption>{title} — original concept perspective</figcaption><svg viewBox="{x} {y} {w} {h}" role="img" aria-label="{title}"><image href="{sheet}" width="1536" height="1024"/></svg></figure>'
def render(label,path):return f'<figure><figcaption>{label}</figcaption><a href="{path}"><img src="{path}" alt="{label}"></a></figure>'
page=['<!doctype html><html lang="en"><meta charset="utf-8"><title>Rift Titan r13 head repair</title>',
 '<style>body{margin:28px auto;max-width:1500px;padding:0 20px;background:#151b1d;color:#e8e5dd;font:17px/1.5 system-ui}a{color:#b7cbef}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:16px}figure{margin:0;background:#20272a}figcaption{padding:12px}img,svg{display:block;width:100%;height:480px;object-fit:contain}section{margin:32px 0}strong{color:#e4bd96}table{border-collapse:collapse}td,th{padding:12px;text-align:left;border:1px solid #465052}@media(max-width:800px){.grid{grid-template-columns:1fr}}</style>',
 '<h1>Rift Titan r13 — head repair candidate</h1><p><strong>Unaccepted. All 44 audit findings remain open.</strong> H01–H04 received a first structural repair. 1,800 triangles / 1,800 soft cap. Body geometry preserved.</p><p><a href="REPAIR.md">Repair notes and limits</a> · <a href="repair-status.json">Rolling findings</a> · <a href="../rift-titan-systematic-audit-r12/AUDIT.md">Original audit</a> · <a href="'+ID+'.glb">Portable skinned model</a></p>',
 '<p>Reference perspective differs from the orthographic model. Before/after renders use exactly the same cameras and lighting. Side views face left.</p>']
for title,suffix,box in [('Front','crown-front',(973,17,78,137)),('Three-quarter','crown-detail',(580,8,180,340)),('Left-facing side','crown-side',(1225,18,115,139))]:
 page += ['<section><h2>'+title+'</h2><div class="grid">',reference(title,box),render('r12 — rejected baseline','before/'+BEFORE+'--'+suffix+'.png'),render('r13 — repair candidate',ID+'--'+suffix+'.png'),'</div></section>']
page+=['<section><h2>Current clay and whole-creature silhouettes</h2><div class="grid">']
for suffix,label in [('crown-front-clay','Front head clay'),('crown-side-clay','Left-facing head clay'),('front','Whole creature — front'),('side','Whole creature — left-facing side')]:page.append(render(label,ID+'--'+suffix+'.png'))
page+=['</div></section><h2>Open head findings</h2><table><tr><th>Finding</th><th>Repair</th><th>Still unresolved</th></tr>']
for key,value in changes.items():page.append('<tr><td>'+key+'</td><td>'+html.escape(value['change'])+'</td><td>'+html.escape(value['remaining'])+'</td></tr>')
page+=['</table><p>The surface still needs substantial work. Missing crest, cage/core, hands, legs and feet were not repaired here. Native runtime acceptance is pending. Adequate bosses: 1/6.</p></html>']
page='\n'.join(page);(OUT/'gallery.html').write_text(page,encoding='utf-8')
for path in re.findall(r'(?:href|src)="([^"]+)"',page):assert (OUT/path).resolve().is_file(),path
for suffix in ('crown-front','crown-detail','crown-side','crown-front-clay','crown-side-clay'):
 for prefix in (OUT/ID,OUT/'before'/BEFORE):
  p=Path(str(prefix)+'--'+suffix+'.png');assert struct.unpack('>II',p.read_bytes()[16:24])==(900,900)
evidence=[ROOT/metrics['source'],OUT/(ID+'.glb'),concept,audit,ROOT/before['source']]
evidence += list(OUT.glob('*.png'))+list((OUT/'before').glob('*.png'))
evidence += [ROOT/'tools'/p for p in ('rift_titan_fractured_head.py','refine-rift-titan-fractured-head-r13.py','preview-rift-titan-fractured-head-r13.py','review-rift-titan-fractured-head-r13.py','review-rift-titan-fractured-before-r13.py','verify-rift-titan-fractured-head-r13.py','publish-rift-titan-fractured-head-r13.py')]
evidence += list((ROOT/'assets/textures/underworld/bosses/rift-titan-fractured-head-r13').glob('*.png'))
(OUT/'provenance.json').write_text(json.dumps(dict(hashes={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in evidence},previous_source_unchanged=True,art_acceptance=False),indent=2)+'\n',encoding='utf-8')
(OUT/'artifact-validation.json').write_text(json.dumps(dict(result='PASS',links_exist=True,before_after_resolution=[900,900],before_source_hash_verified=True,model_source_sha256=hashlib.sha256((ROOT/metrics['source']).read_bytes()).hexdigest(),browser_validation='not performed; local-file browser policy respected',open_findings=44,closed_findings=0),indent=2)+'\n',encoding='utf-8')
print('VERIFIED r13 comparison artifacts; 44 findings remain open; no art acceptance claimed')
