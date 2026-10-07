"""Build the checked chest-frill comparison from saved outputs."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-frills-r6'
anatomy=json.loads((OUT/'frill-anatomy.json').read_text())
assert anatomy['frills']==anatomy['wall_roots']==anatomy['declared_free_leading_edges']==22
code=(ROOT/'tools/build-first-bloom-attachment-review.py').read_text()
code=code.replace('first-bloom-attachments-r5','first-bloom-frills-r6').replace('first-bloom-precision-r4','first-bloom-attachments-r5')
code=code.replace('9188','9032').replace("a['endpoints']==120","a['endpoints']==98")
code=code.replace('First Bloom attachment repair','First Bloom chest frills')
code=code.replace('First Bloom: repair floating tissue attachments','First Bloom: wall-rooted chest frills')
code=code.replace('The previous review missed tissue groups that began or ended in empty space. The repair gives each vein/frame end an explicit cap, body or rooted tissue support. <strong>All 120 ends across 60 groups are seated into their supports in saved rest geometry.</strong>', 'The chest now contains 22 broad, thin fungal shelves projecting from the cavity walls. Their inner leading edges remain free, with a clear central gap. The long central rod is replaced by a small recessed amber growth, and six straight glowing struts are removed. <strong>All 98 structural attachment ends across 60 groups are seated into their supports.</strong> The 22 free frill edges are explicitly marked and have an anchored wall root.')
code=code.replace('Still 9,188 triangles / 1.48× Troll','9,032 triangles / 1.46× Troll, below the 9,309 soft cap')
code=code.replace('Before · floating frame ends','Before · rib-like chest').replace('After · explicit support contacts','After · cavity shelves')
code=code.replace("for area in ('caps','tissue'):","for area in ('tissue',):")
code=code.replace('Body frame attachments','Chest shelf close-ups')
code=code.replace('Repair record','Frill revision record')
code=code.replace("scripts=['first_bloom_attachment_repair.py'", "scripts=['first_bloom_frill_sculpt.py','refine-first-bloom-frills.py','review-first-bloom-frills.py','verify-first-bloom-frill-poses.py','render-first-bloom-frill-closeups.py','build-first-bloom-frill-review.py','first_bloom_attachment_repair.py'")
code=code.replace('120 saved contacts','98 saved contacts and 22 declared free frill edges')
exec(compile(code,str(__file__),'exec'))
