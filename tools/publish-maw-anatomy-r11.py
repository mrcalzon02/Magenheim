from pathlib import Path
import json,shutil,hashlib
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-anatomy-r11'
row=json.loads((OUT/'refinement-metrics.json').read_text());ID=row['id'];check=json.loads((OUT/'verification.json').read_text());contacts=json.loads((OUT/'attachment-pose-verification.json').read_text())
assert check['triangles']==row['triangles']<=7308 and contacts['result']=='PASS'
assert check['source_sha256']==hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest()
shutil.copyfile(OUT.with_name(OUT.name+'-raw')/'concept-corrections.json',OUT/'concept-corrections.json')
shutil.copyfile(OUT.with_name('blackwater-maw-left-facing-r10')/'fin-shape-reference.png',OUT/'fin-shape-reference.png')
style='body{background:#13191d;color:#e4dfd6;font:16px system-ui;margin:auto;padding:24px;max-width:1400px}a{color:#77c7d1}p{line-height:1.6}img{width:100%;object-fit:contain;background:#171e22}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}figcaption{padding:8px}.crop{width:486px;max-width:100%}@media(max-width:800px){.pair{grid-template-columns:1fr}}'
html=f'''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Blackwater Maw — fin attachments and head proportions</title><style>{style}</style>
<h1>Blackwater Maw — fin attachments and head proportions</h1>
<p>The forward fin attaches directly to the torso: no shoulder and no short arm. Only the middle fin retains a thick tapered muscular member. The head is narrower and shorter, with the upper jaw compressed toward the body skyline and the open lower jaw beneath it. Fang shafts are straighter in front view; the cheek organs form an uneven cluster. Low armor ridges travel from snout toward the cheek plates, with a separate body crown near the first dorsal fin. Three curved feelers per side sweep back from the crown and have luminous ends.</p>
<p><strong>{row['triangles']:,} triangles · {row['ratio']:.3f}× Serpent · soft cap 7,308</strong>. One mesh/material, four 512px atlas maps. {contacts['endpoints']} attachment records pass {contacts['sampled_poses']} sampled poses across {contacts['retained_actions']} retained actions. This is an art study requiring visual review; offline checks do not establish native performance or donor-level visual acceptance.</p>
<h2>Current left-facing profile</h2><img src="{ID}--side.png" alt="Mouth left, tail right; smaller head and distinct fin roots">
<h2>Concept — matching direction</h2><img src="../../../creature-concepts/2026-10-03/the-blackwater-maw.png" alt="Original concept, mouth left and tail right">
<h2>Fin roots and reference detail</h2><div class="pair"><figure><img src="{ID}--fin-roots.png" alt="Direct forward fin and muscular middle fin"><figcaption>Current fin attachments</figcaption></figure><figure><img class="crop" src="fin-shape-reference.png" alt="User supplied fin anatomy reference"><figcaption>Source anatomy detail</figcaption></figure></div>
<h2>Head armor, cheek cluster and feelers</h2><img src="{ID}--armor-and-feelers.png" alt="Close-up of revised crown and face">
<h2>Previous and revised profiles</h2><div class="pair"><figure><img src="../blackwater-maw-left-facing-r10/underworld-boss-blackwater-maw-left-facing-r10--side.png"><figcaption>Previous r10</figcaption></figure><figure><img src="{ID}--side.png"><figcaption>Revised r11</figcaption></figure></div>
<h2>Front view and surface view</h2><div class="pair"><figure><img src="{ID}--front.png"><figcaption>Fang placement and open jaw</figcaption></figure><figure><img src="{ID}--three-quarter.png"><figcaption>Current surface view</figcaption></figure></div>
<p><a href="../../../../../{row['source']}">Editable model</a> · <a href="{ID}.glb">Skinned export</a> · <a href="concept-corrections.json">Recorded changes</a> · <a href="verification.json">Geometry checks</a> · <a href="attachment-pose-verification.json">Attachment checks</a></p></html>'''
(OUT/'gallery.html').write_text(html,encoding='utf-8')
for p in OUT.glob('*.png'):assert p.stat().st_size>1000
p=ROOT/'docs/art/creature-models/README.md';s=p.read_text();link='[Blackwater Maw: direct forward fin and revised head](2026-10-04/blackwater-maw-anatomy-r11/gallery.html)\n\n'
if link not in s:p.write_text(link+s,encoding='utf-8')
p=ROOT/'docs/art/creature-models/REFINEMENT-STANDARDS.md';s=p.read_text();note='\nBlackwater Maw lower-fin correction: the forward blade attaches directly to the body with no shoulder or stubby arm; only the middle blade has a powerful tapered muscular member. The upper jaw follows the body skyline. Distinguish the snout-to-cheek armor ridge from the body crown that joins the first dorsal fin; crown feelers sweep over the body.\n'
if note not in s:p.write_text(s+note,encoding='utf-8')
print('Published anatomy r11:',row['triangles'],'triangles; source hash and attachment checks agree')
