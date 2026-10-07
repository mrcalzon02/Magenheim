"""Checked local repair gallery with paired shoulder views and backface inspection."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/build-first-bloom-feet-review.py').read_text()
for old,new in [('first-bloom-feet-r15','first-bloom-arm-seams-r22'),('first-bloom-trim-r14','first-bloom-bundles-r21'),('8830','8846'),('8810','8862'),('8,830','8,846'),('1.423','1.425'),('132','157'),('revision 15','revision 22'),('Revision 15','Revision 22'),('revision 14','revision 21'),('Revision 14','Revision 21'),('root feet','repaired arm-to-shoulder seams'),('root-feet review','arm-seam review'),('foot-refinement.json','arm-seam-repair.json')]:source=source.replace(old,new)
source=source.replace('The existing foot loops now form broader root fans with unequal crooked toes, lower contact pads, and flattened, staggered fungal brackets around the ankles. All ten root bases are seated into the leg surface with matching skin weights. The approved chest shaping is preserved.', 'The pinched shoulder polygon was visible in both the trimmed and untrimmed revision 21 mesh. Its arm-root closing face spanned a nonplanar loop. Both arm roots now use a planar closing patch seated inside one rigid torso face, with consistently ordered transition rings. The nearby tissue contacts are reseated. Forty interior samples per closing patch are checked, in addition to its boundary vertices.')
source=source.replace('No new triangles were added during foot shaping. Reauditing buried geometry removes 422 triangles from the untrimmed model; retaining the new ankle contacts leaves 20 more triangles than revision 21.', 'No new triangles were added. Reauditing buried faces leaves 8,846 triangles, 16 fewer than revision 21. The head and shoulder mushroom bundles, beard, chest, hips and lower limbs retain their preceding shapes.')
start=source.index("for view,title in [('feet-front'")
end=source.index("page+='<h2>Whole creature",start)
source=source[:start]+'''for view,title in [('front','Front-left shoulder closing face'),('angle','Front-left shoulder from outside')]:
 page+=f'<h2>{title}</h2><div class="pair">'
 for label,caption in [('before','Before — revision 21'),('after','After — revision 22')]:
  page+=f'<figure><img src="{label}--left-join--{view}.png" alt="{caption} shoulder join"><figcaption>{caption}</figcaption></figure>'
 page+='</div>'
page+='<h2>Cause comparison: before trimming</h2><div class="pair"><figure><img src="untrimmed--left-join--front.png" alt="Untrimmed previous shoulder"><figcaption>The same pinched patch in the untrimmed revision 21 source</figcaption></figure><figure><img src="backfaces--left-join--front.png" alt="Repaired shoulder backface inspection"><figcaption>Backfaces are red. The repaired arm patch shows none in this view; red on the surrounding chest tissue is outside this local arm repair.</figcaption></figure></div>'
page+='<p><a href="arm-root-diagnostic.json">Original root-loop diagnostic</a> · <a href="arm-seam-repair.json">Closing patch repair</a></p>'
''' +source[end:]
exec(compile(source,str(__file__),'exec'))
