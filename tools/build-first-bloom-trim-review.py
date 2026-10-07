"""Validate counts, unchanged atlases, image comparisons and buried-cut review."""
from pathlib import Path
import json,hashlib
from PIL import Image,ImageChops,ImageStat
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-trim-r14';BEFORE=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-membranes-r13'
r=json.loads((OUT/'refinement-metrics.json').read_text());trim=json.loads((OUT/'trim-result.json').read_text());audit=json.loads((OUT/'buried-face-audit.json').read_text());bound=json.loads((OUT/'trim-boundary-verification.json').read_text())
assert r['triangles']==8810 and trim['removed_triangles']==442 and bound['result'].startswith('PASS')
for kind in ('albedo','normal','roughness','emission'):
 old=ROOT/f'assets/textures/underworld/bosses/first-bloom-membranes-r13/underworld-boss-first-bloom-membranes-r13-{kind}.png'
 new=ROOT/f'assets/textures/underworld/bosses/first-bloom-trim-r14/underworld-boss-first-bloom-trim-r14-{kind}.png'
 assert hashlib.sha256(old.read_bytes()).digest()==hashlib.sha256(new.read_bytes()).digest()
comparison=[]
for view in ('three-quarter','front','side','detail','clay','attack'):
 a=Image.open(BEFORE/f'underworld-boss-first-bloom-membranes-r13--{view}.png').convert('RGB');b=Image.open(OUT/f'{r["id"]}--{view}.png').convert('RGB');assert a.size==b.size
 diff=ImageChops.difference(a,b);mean=sum(ImageStat.Stat(diff).mean)/3/255;hist=diff.convert('L').histogram();changed=sum(hist[6:])/(a.width*a.height)
 comparison.append(dict(view=view,mean_absolute_rgb_difference=mean,pixels_changed_by_more_than_five_levels_fraction=changed))
 assert mean<.01 and changed<.02,('visible trim difference requires review',view,mean,changed)
(OUT/'visual-comparison.json').write_text(json.dumps(dict(result='PASS small matched-render differences',comparisons=comparison,scope='six matched views; this is not a proof of all-view visual identity'),indent=2)+'\n')
source=(ROOT/'tools/build-first-bloom-looming-review.py').read_text().replace('first-bloom-looming-r10','first-bloom-trim-r14').replace('first-bloom-seated-r9','first-bloom-membranes-r13').replace('first-bloom-fan-r8','first-bloom-medallion-r12')
source=source.replace("assert r['triangles']==r['previous_triangles']==9252", "assert r['triangles']==8810 and r['previous_triangles']==9252").replace("count']//3==9252", "count']//3==8810").replace("a['endpoints']==121","a['endpoints']==122")
source=source.replace('Revision 10','Revision 14').replace('Revision 9','Revision 13').replace('Revision 8','Revision 12')
source=source.replace('The attached chest assembly is recessed farther into the torso and pitched forward by 12 degrees around its lower hinge. The top moves forward and down; the lower interior approaches the hip joint. The surrounding front torso follows the lean with a smooth transition toward the waist. The radial shelf fan and fitted body flange remain connected.', 'Fully buried surface faces have been removed from the assembled mesh. The cuts remain inside retained closed supports through five samples of every retained action. Partial intersections are preserved. Contact-marker surfaces are retained where needed. UVs and all four atlas images are unchanged. Internal cut boundaries remain covered; this is not a watertight Boolean union.')
extra=f'<p><strong>9,252 → 8,810 triangles · 442 removed ({442/9252*100:.2f}%) · {8810/6206:.2f}× donor</strong><br>{len(audit["sampled_actions"])} retained actions / {len(audit["samples"])} pose samples audited. <a href="buried-face-audit.json">Buried-face audit</a> · <a href="trim-boundary-verification.json">Saved internal-boundary audit</a> · <a href="visual-comparison.json">Matched render differences</a></p>'
source=source.replace("(OUT/'gallery.html').write_text(page,encoding='utf-8')", "page=page.replace('First Bloom wider textured membranes','First Bloom buried geometry trim').replace('First Bloom: wider textured membranes','First Bloom: buried geometry trim').replace('Width and membrane texture comparison','Before and after buried-face trimming').replace('9,252 triangles / 1.49× Troll','8,810 triangles / 1.42× Troll')\npage=page.replace('<h2>Front attachment</h2>',"+repr(extra)+"+'<h2>Front attachment</h2>')\n(OUT/'gallery.html').write_text(page,encoding='utf-8')")
exec(compile(source,str(__file__),'exec'))
