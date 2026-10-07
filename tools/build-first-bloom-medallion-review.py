from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-medallion-r12'
change=json.loads((OUT/'medallion-arch.json').read_text())
assert change['medallion_width_scale']==change['medallion_height_scale']==1.25 and change['medallion_depth_scale']==1
assert change['arch_intersecting_triangle_pairs']>0 and change['triangles_added']==0
source=(ROOT/'tools/build-first-bloom-looming-review.py').read_text().replace('first-bloom-looming-r10','first-bloom-medallion-r12').replace('first-bloom-seated-r9','first-bloom-inset-r11').replace('first-bloom-fan-r8','first-bloom-looming-r10')
source=source.replace("a['endpoints']==121","a['endpoints']==122")
source=source.replace('Revision 10','Revision 12').replace('Revision 9','Revision 11').replace('Revision 8','Revision 10')
source=source.replace('The attached chest assembly is recessed farther into the torso and pitched forward by 12 degrees around its lower hinge. The top moves forward and down; the lower interior approaches the hip joint. The surrounding front torso follows the lean with a smooth transition toward the waist. The radial shelf fan and fitted body flange remain connected.', 'The central medallion is 25% wider and taller, with its depth retained. The two existing lowest frills are brought together to close the bottommost arch. The recessed posture, skin-colored rim, white exposed frill lips and warm yellow deep glow are preserved. No triangles were added.')
source=source.replace("(OUT/'gallery.html').write_text(page,encoding='utf-8')", "page=page.replace('First Bloom deeper chest and central frill glow','First Bloom enlarged medallion and closed lower arch').replace('First Bloom: deeper chest and central frill glow','First Bloom: enlarged medallion and closed lower arch').replace('Deeper inset and central frill glow','Medallion and lower arch comparison').replace('All 121 structural contact records','All 122 structural contact records')\n(OUT/'gallery.html').write_text(page,encoding='utf-8')")
exec(compile(source,str(__file__),'exec'))
