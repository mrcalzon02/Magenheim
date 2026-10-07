from pathlib import Path
import json,shutil,hashlib,re
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-teeth-eyes-r13'
row=json.loads((OUT/'refinement-metrics.json').read_text());ID=row['id'];check=json.loads((OUT/'verification.json').read_text());contacts=json.loads((OUT/'attachment-pose-verification.json').read_text())
assert check['source_sha256']==hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest()
assert row['triangles']==check['triangles']<=7308 and contacts['result']=='PASS'
shutil.copyfile(OUT.with_name(OUT.name+'-raw')/'teeth-eyes-authoring.json',OUT/'teeth-eyes-authoring.json')
shutil.copyfile('C:/Users/Admin/AppData/Local/Temp/codex-clipboard-98667d6d-dda6-4575-80e1-e51b68e27334.png',OUT/'teeth-eyes-reference.png')
shutil.copyfile(OUT.with_name('blackwater-maw-face-r12')/'brow-reference.png',OUT/'brow-reference.png')
html=f'''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Blackwater Maw — teeth and eyes</title><style>body{{background:#13191d;color:#e4dfd6;font:16px system-ui;margin:auto;padding:24px;max-width:1400px}}a{{color:#77c7d1}}img{{width:100%;object-fit:contain;background:#171e22}}.pair{{display:grid;grid-template-columns:1fr 1fr;gap:16px}}figure{{margin:0}}figcaption{{padding:8px}}p{{line-height:1.6}}.crop{{width:480px;max-width:100%}}@media(max-width:800px){{.pair{{grid-template-columns:1fr}}}}</style>
<h1>Blackwater Maw — teeth and eyes</h1><p>Uneven tapered fangs now have thicker shafts. Upper fangs sweep back into the mouth; lower fangs have a gentler forward rake. Their original gum attachments remain seated. Rounded cyan bulbs occupy an uneven rear-cheek cluster, with darker socket edges and shaded highlights.</p>
<p>The smaller head, low mouth brow, scaled rear head, small crown nub and separated feelers are retained. The final texture maps remain 512px; an offline 1024px bake is reduced for smoother small details.</p><p><strong>{row['triangles']:,} triangles · {row['ratio']:.3f}× Serpent · cap 7,308</strong>. One mesh/material and four 512px maps. {contacts['endpoints']} attachment records pass {contacts['sampled_poses']} sampled poses across {contacts['retained_actions']} actions. This remains an art candidate for concept review; native performance is unverified.</p>
<h2>Teeth close-up</h2><img src="{ID}--teeth.png" alt="Thicker uneven fangs and distinct upper/lower rake">
<h2>Eye and cheek close-up</h2><img src="{ID}--eyes.png" alt="Rounded cyan bulbs with dark roots and highlights">
<h2>Source details</h2><div class="pair"><figure><img class="crop" src="teeth-eyes-reference.png"><figcaption>User supplied teeth and eyes reference</figcaption></figure><figure><img class="crop" src="brow-reference.png"><figcaption>Low brow armor and scaled rear head</figcaption></figure></div>
<h2>Left-facing profile</h2><img src="{ID}--side.png" alt="Current model with mouth left and tail right">
<h2>Previous and current head</h2><div class="pair"><figure><img src="../blackwater-maw-face-r12/underworld-boss-blackwater-maw-face-r12--armor-and-feelers.png"><figcaption>Previous face revision</figcaption></figure><figure><img src="{ID}--armor-and-feelers.png"><figcaption>Current teeth and eyes</figcaption></figure></div>
<h2>Full concept</h2><img src="../../../creature-concepts/2026-10-03/the-blackwater-maw.png" alt="Original concept in the same direction">
<p><a href="../../../../../{row['source']}">Editable model</a> · <a href="{ID}.glb">Skinned export</a> · <a href="teeth-eyes-authoring.json">Recorded changes</a> · <a href="verification.json">Geometry checks</a> · <a href="attachment-pose-verification.json">Attachment checks</a> · <a href="../blackwater-maw-face-r12/gallery.html">Previous face revision</a></p></html>'''
(OUT/'gallery.html').write_text(html,encoding='utf-8')
for name in re.findall(r'(?:src|href)="([^"]+)"',html):
 if name.startswith('http'):continue
 assert (OUT/name).resolve().is_file(),name
p=ROOT/'docs/art/creature-models/README.md';s=p.read_text();link='[Blackwater Maw: teeth, rounded eyes and corrected face](2026-10-04/blackwater-maw-teeth-eyes-r13/gallery.html)\n\n'
if link not in s:p.write_text(link+s,encoding='utf-8')
p=ROOT/'docs/art/creature-models/REFINEMENT-STANDARDS.md';s=p.read_text();note='\nBlackwater Maw face: scale the outer rear head; confine flesh to a narrow seam between low armor sections. Keep the crown nub small and mouth brow low. Spread sensory feelers independently in projection; inspect blue bulb tips and eye surfaces for tiny unbaked atlas patches. Fangs need thickness and distinct upper/lower rake. Small-detail supersampling is an offline bake step; final atlas dimensions remain within the donor budget.\n'
if note not in s:p.write_text(s+note,encoding='utf-8')
print('Published teeth/eyes review:',row['triangles'],'triangles; all links and source hash verified')
