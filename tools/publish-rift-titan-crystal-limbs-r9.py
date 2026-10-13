"""Publish matched concept/limb comparisons and measured budget/support checks."""
from pathlib import Path
import json,hashlib,shutil,re,subprocess
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-crystal-limbs-r9'
OLD=OUT.parent/'rift-titan-joints-r8'
row=json.loads((OUT/'refinement-metrics.json').read_text());ID=row['id'];previous='underworld-boss-rift-titan-joints-r8'
checks={n:json.loads((OUT/(n+'.json')).read_text()) for n in ('verification','attachment-verification','closing-face-verification','crystal-face-verification','limb-verification','side-orientation')}
assert row['triangles']==checks['verification']['triangles']==1800
assert checks['attachment-verification']['result']=='PASS' and checks['attachment-verification']['endpoints']==74
assert checks['closing-face-verification']['sampled_points']==8064 and checks['closing-face-verification']['result']=='PASS'
assert checks['crystal-face-verification']['sampled_points']==14112 and checks['crystal-face-verification']['result']=='PASS'
sha=hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest()
assert checks['verification']['source_sha256']==checks['side-orientation']['source_sha256']==sha
assert checks['side-orientation']['camera_direction'][0]<0
(OUT/'before').mkdir(exist_ok=True)
views=('three-quarter','front-clay','side-clay','side-reference')
for view in views:shutil.copy2(OLD/(previous+'--'+view+'.png'),OUT/'before'/(previous+'--'+view+'.png'))
for p in ('concept-front.png','concept-side.png'):shutil.copy2(OLD/p,OUT/p)
html=f'''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rift Titan — crystal and limb depth r9</title>
<style>body{{background:#14191c;color:#e4e5df;font:16px system-ui;max-width:1500px;margin:auto;padding:24px}}p{{line-height:1.6;max-width:1150px}}a{{color:#9ccdda}}img{{width:100%;object-fit:contain;background:#171e22}}.pair{{display:grid;grid-template-columns:1fr 1fr;gap:14px}}.reference img{{height:650px}}figure{{margin:0}}figcaption{{padding:8px;color:#bbc5c6}}button{{background:#283840;color:#e4e5df;border:1px solid #60747a;padding:8px 14px;margin:5px;cursor:pointer}}@media(max-width:800px){{.pair{{grid-template-columns:1fr}}.reference img{{height:500px}}}}</style>
<h1>Rift Titan — crystal and limb depth r9</h1>
<p>The arms and legs have deeper front/rear stone planes, staggered oblique face ridges and narrower splinter ends. Twelve new violet shards grow from their stone plates; four formerly floating shards now grow from the wrists and lower legs. Their complete basal sections are embedded into the stone and follow the same bone weights. Crystal bases were widened after the initial render read too much like thin spikes. Side inspection prompted turning their axes along the limb fractures to reduce forward projection.</p>
<p><strong>1,800 triangles · Stone Golem donor 1,200 · 1.500× · cap 1,800.</strong> The six thin internal joint bindings were condensed from 120 to 48 triangles, funding 72 triangles of new crystals. This is a topology simplification, not a boolean union. Existing limb plate allocations remain unchanged. One mesh/material, four packed 512px maps, eight original bone identities and seven actions remain.</p>
<h2>Previous and current — matched cameras</h2><div><button data-view="three-quarter">Surface</button><button data-view="front-clay">Front clay</button><button data-view="side-clay">True side clay</button><button data-view="side-reference">Angled side</button></div><div class="pair"><figure><img id="before" src="before/{previous}--three-quarter.png" alt="Preserved r8"><figcaption>Previous r8</figcaption></figure><figure><img id="after" src="{ID}--three-quarter.png" alt="Current r9"><figcaption>Current r9 — more crystals and deeper limbs, unchanged triangle total</figcaption></figure></div>
<h2>Front reference</h2><div class="pair reference"><figure><img src="concept-front.png" alt="Concept front"><figcaption>Adopted concept front</figcaption></figure><figure><img src="{ID}--front-reference.png" alt="Current front clay"><figcaption>Current front — limb depth and preserved upper body</figcaption></figure></div>
<h2>Left-facing side reference</h2><div class="pair reference"><figure><img src="concept-side.png" alt="Concept side"><figcaption>Concept side — front left</figcaption></figure><figure><img src="{ID}--side-reference-clay.png" alt="Current left-facing side"><figcaption>Current angled side — front left; true profile available above</figcaption></figure></div>
<h2>Adopted concept</h2><img src="../../../creature-concepts/2026-10-03/the-rift-titan.png" alt="Full adopted concept">
<p>All 74 support groups pass rest plus 35 sampled action poses. The eight arm/digit closing sections pass 8,064 interior/edge checks; the sixteen crystal basal sections pass 14,112 checks across the same 36 poses, with zero measured outside distance. Both torso rings remain closed and connected. Side cameras retain the left-facing projection guard.</p>
<p>This remains an unaccepted refinement study. More irregular rock fragmentation and richer weathering remain to be developed against the concept. Native loading, full animation deformation and live performance remain pending. Adequately reworked boss tally remains 1/6.</p>
<p><a href="../../../../../{row['source']}">Editable Blender model</a> · <a href="{ID}.glb">Skinned export</a> · <a href="{ID}--attack.png">Attack pose</a> · <a href="limb-authoring.json">Limb changes and budget</a> · <a href="verification.json">Mesh/export checks</a> · <a href="attachment-verification.json">Support checks</a> · <a href="closing-face-verification.json">Arm/digit basal faces</a> · <a href="crystal-face-verification.json">Crystal basal faces</a> · <a href="limb-verification.json">Crystal ownership and budget</a> · <a href="side-orientation.json">Camera direction</a> · <a href="provenance.json">Source provenance</a></p>
<script>document.querySelectorAll('button[data-view]').forEach(b=>b.onclick=()=>{{document.getElementById('before').src='before/{previous}--'+b.dataset.view+'.png';document.getElementById('after').src='{ID}--'+b.dataset.view+'.png';}});</script></html>'''
sources=['tools/rift_titan_r4_base.py','tools/rift_titan_primary_layout.py','tools/rift_titan_rock_facets.py','tools/rift_titan_wrapped_torso.py','tools/rift_titan_silhouette_fit.py','tools/rift_titan_joint_silhouette.py','tools/rift_titan_crystal_limbs.py','tools/refine-rift-titan-crystal-limbs-r9.py','tools/review-rift-titan-crystal-limbs-r9.py','tools/review-rift-titan-silhouette-r7.py','tools/verify-rift-titan-crystal-limbs-r9.py','tools/publish-rift-titan-crystal-limbs-r9.py',row['source'],'assets/models/studies/'+previous+'.blend','docs/art/creature-concepts/2026-10-03/the-rift-titan.png']
(OUT/'provenance.json').write_text(json.dumps(dict(base_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),hashes={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in sources},scope='r8 limb study continued; concurrent crown profile authoring preserved in repository, not incorporated into this limb comparison',acceptance='unaccepted; tally 1/6'),indent=2)+'\n',encoding='utf-8')
(OUT/'gallery.html').write_text(html,encoding='utf-8')
for target in re.findall(r'(?:href|src)="([^"]+)"',html):assert (OUT/target).resolve().is_file(),target
for view in views:assert (OUT/(ID+'--'+view+'.png')).is_file()
print('VERIFIED r9 gallery links, source hashes, left-facing comparisons, 74 supports and 1800 triangle cap')
