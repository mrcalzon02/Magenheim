"""Checked body-to-chamber attachment comparison."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-seated-r9'
contact=json.loads((OUT/'cavity-attachment.json').read_text());assert contact['rim_vertices']==48 and contact['triangles_added']==0
audit=json.loads((OUT/'attachment-verification.json').read_text())
rim=next(c for c in audit['checks'] if c['end']=='complete_body_flange');assert rim['endpoint_vertices']==48 and rim['all_endpoint_vertices_inside_support']
source=(ROOT/'tools/build-first-bloom-radiating-review.py').read_text().replace('first-bloom-radiating-r7','first-bloom-seated-r9').replace('first-bloom-frills-r6','first-bloom-fan-r8')
source=source.replace("a['endpoints']==120","a['endpoints']==121")
source=source.replace('First Bloom radiating fleshy frills','First Bloom cavity attached to torso').replace('First Bloom: radiating fleshy frills','First Bloom: cavity attached to torso')
source=source.replace('The concept shows pale, thin fins radiating from a fleshy central join, curving outward and down around the chest. Amber tissue sits inside and beneath their deep faces. This revision replaces the previous separate wall shelves with that connected fan arrangement.', 'The torso opening now forms a fitted organic collar around the recessed chest cavity. All 48 front/back cavity rim vertices are embedded into the body shell and follow the fitted chest collar. The approved radial fan remains inside the cavity; nearby tissue contacts follow the revised supports. No triangles were added.')
source=source.replace('All 120 structural ends','All 121 structural contact records, including the complete 48-vertex cavity rim,')
source=source.replace('Previous shelves → radiating frills','Floating cavity → fitted body collar').replace('Revision 6 · separate wall shelves','Revision 8 · cavity separated from torso').replace('Revision 7 · central join and radiating fins','Revision 9 · cavity seated into torso')
source=source.replace('<figcaption>Revision 6</figcaption>','<figcaption>Revision 8</figcaption>').replace('<figcaption>Revision 7</figcaption>','<figcaption>Revision 9</figcaption>')
source=source.replace("page+='<h2>Concept reference</h2>","page+='<h2>Front attachment</h2><div class=\"pair\"><figure><img src=\"before--tissue-front--textured.png\"><figcaption>Revision 8 · exposed cavity edge</figcaption></figure><figure><img src=\"after--tissue-front--textured.png\"><figcaption>Revision 9 · fitted torso lip</figcaption></figure></div><h2>Concept reference</h2>")
source=source.replace("page+=f'<h2>Whole creature comparison</h2>","page+='<h2>Body connection from the side</h2><div class=\"pair\"><figure><img src=\"before--tissue-depth--clay.png\"><figcaption>Revision 8 · gap behind the cavity rim</figcaption></figure><figure><img src=\"after--tissue-depth--clay.png\"><figcaption>Revision 9 · torso collar surrounds the cavity</figcaption></figure></div>'\npage+=f'<h2>Whole creature comparison</h2>")
source=source.replace("scripts=['first_bloom_radiating_frills.py'","scripts=['first_bloom_cavity_attachment.py','refine-first-bloom-seated.py','review-first-bloom-seated.py','verify-first-bloom-seated-poses.py','render-first-bloom-seated-closeups.py','build-first-bloom-seated-review.py','first_bloom_fan_guide.py','render-first-bloom-fan-closeups.py','first_bloom_radiating_frills.py'")
source=source.replace('120 saved contacts','121 saved structural contacts including the entire cavity flange')
exec(compile(source,str(__file__),'exec'))
