from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-inset-r11'
posture=json.loads((OUT/'chest-posture.json').read_text());assert posture['recess_authored_metres']==.125
source=(ROOT/'tools/build-first-bloom-looming-review.py').read_text().replace('first-bloom-looming-r10','first-bloom-inset-r11').replace('first-bloom-seated-r9','first-bloom-looming-r10').replace('first-bloom-fan-r8','first-bloom-seated-r9')
source=source.replace('Revision 10','Revision 11').replace('Revision 9','Revision 10').replace('Revision 8','Revision 9')
source=source.replace('First Bloom recessed looming chest','First Bloom deeper chest and skin-colored rim').replace('First Bloom: recessed looming chest','First Bloom: deeper chest and skin-colored rim')
source=source.replace('The attached chest assembly is recessed farther into the torso and pitched forward by 12 degrees around its lower hinge. The top moves forward and down; the lower interior approaches the hip joint. The surrounding front torso follows the lean with a smooth transition toward the waist. The radial shelf fan and fitted body flange remain connected.', 'The chest assembly sits farther inside the torso while retaining the 12-degree forward pitch and lower alignment. Its outer textured band shares the body skin material. Warm yellow glow covers the deeper central frill faces, fading before their exposed ivory lips; amber remains in the central cavity underneath. The fitted body flange stays attached, with no added triangles.')
source=source.replace('Attached cavity → deeper, forward-pitched chest','Previous chest → deeper inset and skin rim')
source=source.replace("(OUT/'gallery.html').write_text(page,encoding='utf-8')", "page=page.replace('First Bloom recessed looming chest','First Bloom deeper chest and central frill glow').replace('First Bloom: recessed looming chest','First Bloom: deeper chest and central frill glow').replace('Attached cavity → deeper, forward-pitched chest','Deeper inset and central frill glow')\n(OUT/'gallery.html').write_text(page,encoding='utf-8')")
exec(compile(source,str(__file__),'exec'))
