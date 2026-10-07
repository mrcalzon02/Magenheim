from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-membranes-r13'
change=json.loads((OUT/'membrane-detail.json').read_text())
assert abs(change['join_width_scale']-1.5)<1e-5 and abs(change['lower_panel_width_scale']-3)<1e-5
assert change['arch_intersecting_triangle_pairs']>0 and change['triangles_added']==0
source=(ROOT/'tools/build-first-bloom-looming-review.py').read_text().replace('first-bloom-looming-r10','first-bloom-membranes-r13').replace('first-bloom-seated-r9','first-bloom-medallion-r12').replace('first-bloom-fan-r8','first-bloom-inset-r11')
source=source.replace("a['endpoints']==121","a['endpoints']==122")
source=source.replace('Revision 10','Revision 13').replace('Revision 9','Revision 12').replace('Revision 8','Revision 11')
source=source.replace('The attached chest assembly is recessed farther into the torso and pitched forward by 12 degrees around its lower hinge. The top moves forward and down; the lower interior approaches the hip joint. The surrounding front torso follows the lean with a smooth transition toward the waist. The radial shelf fan and fitted body flange remain connected.', 'The central lower membrane panel is three times its previous width, a 200% increase. The central join is 50% wider. Shallow wrinkles, pores and fibrous pigmentation add tissue texture while retaining ivory exposed edges and warm yellow glow on deep central faces. The cavity remains attached and the bottom arch stays closed. Geometry and atlas sizes are unchanged.')
source=source.replace("(OUT/'gallery.html').write_text(page,encoding='utf-8')", "page=page.replace('First Bloom enlarged medallion and closed lower arch','First Bloom wider textured membranes').replace('First Bloom: enlarged medallion and closed lower arch','First Bloom: wider textured membranes').replace('Medallion and lower arch comparison','Width and membrane texture comparison')\n(OUT/'gallery.html').write_text(page,encoding='utf-8')")
exec(compile(source,str(__file__),'exec'))
