from pathlib import Path
import json,shutil
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-left-facing-r10'
row=json.loads((OUT/'refinement-metrics.json').read_text());ID=row['id']
checks=json.loads((OUT/'verification.json').read_text())
assert checks['triangles']==row['triangles']<=7308
shutil.copyfile(Path('C:/Users/Admin/AppData/Local/Temp/codex-clipboard-1e44a411-f191-4131-92ed-7c475b0263fc.png'),OUT/'fin-shape-reference.png')
shutil.copyfile(OUT.with_name(OUT.name+'-raw')/'blade-fin-anatomy.json',OUT/'blade-fin-anatomy.json')
html=f'''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Blackwater Maw — left-facing fin review</title>
<style>body{{background:#13191d;color:#e4dfd6;font:16px system-ui;margin:0 auto;padding:24px;max-width:1400px}}a{{color:#77c7d1}}p{{line-height:1.6}}img{{width:100%;object-fit:contain;background:#171e22}}.pair{{display:grid;grid-template-columns:1fr 1fr;gap:16px}}figure{{margin:0}}figcaption{{padding:8px}}.crop{{width:486px;max-width:100%;image-rendering:auto}}@media(max-width:800px){{.pair{{grid-template-columns:1fr}}}}</style>
<h1>Blackwater Maw — left-facing fin review</h1>
<p>Both comparisons put the mouth on the left and tail on the right. Dorsal peaks sit lower and sweep farther toward the tail. Lower fins have separate body attachments: a broad lower curved blade and a narrower upper blade extending farther back. The lower blade surface is rotated into view. This is a revised art candidate, still requiring visual review against the concept.</p>
<p><strong>{row['triangles']:,} triangles · {row['ratio']:.3f}× Serpent · soft cap 7,308</strong>. One mesh/material, four 512px atlas maps. All 116 attachment records pass 65 sampled poses across 13 retained actions. Offline checks passed; native runtime performance is unverified.</p>
<h2>Left-facing side profile</h2><img src="{ID}--side.png" alt="Model with mouth left and tail right">
<h2>Source artwork — same direction</h2><img src="../../../creature-concepts/2026-10-03/the-blackwater-maw.png" alt="Source concept with mouth left and tail right">
<h2>Lower fin shape reference</h2><img class="crop" src="fin-shape-reference.png" alt="User supplied fin detail showing different blade shapes and attachments">
<h2>Current surface and clay</h2><div class="pair"><figure><img src="{ID}--three-quarter.png"><figcaption>Left-facing three-quarter view</figcaption></figure><figure><img src="{ID}--clay.png"><figcaption>Geometry without texture</figcaption></figure></div>
<p><a href="../../../../../assets/models/studies/{ID}.blend">Editable model</a> · <a href="{ID}.glb">Skinned export</a> · <a href="verification.json">Geometry checks</a> · <a href="attachment-pose-verification.json">Attachment checks</a> · <a href="../blackwater-maw-jaws-r5/gallery.html">Earlier jaw review</a></p></html>'''
(OUT/'gallery.html').write_text(html,encoding='utf-8')
p=ROOT/'docs/art/creature-models/README.md';s=p.read_text();link='[Blackwater Maw: left-facing fin review](2026-10-04/blackwater-maw-left-facing-r10/gallery.html)\n\n'
if link not in s:s=link+s
p.write_text(s,encoding='utf-8')
p=ROOT/'docs/art/creature-models/REFINEMENT-STANDARDS.md';s=p.read_text();note='\nDirection in review renders must match the concept: Blackwater Maw mouth left, tail right. Compare dorsal sweep and lower fin roots in that same view before changing anatomy. Projected fin silhouettes must not be treated as interchangeable duplicate fans.\n'
if note not in s:p.write_text(s+note,encoding='utf-8')
print('Published left-facing review',row['triangles'],'triangles')
