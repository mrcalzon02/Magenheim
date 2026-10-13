from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-rift-titan-wrap-r6.py').read_text(encoding='utf-8').replace('rift-titan-wrap-r6','rift-titan-silhouette-r7').replace('rift_titan_wrapped_torso','rift_titan_silhouette_fit')
exec(compile(source,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-silhouette-r7';p=out/'refinement-metrics.json';row=json.loads(p.read_text())
row.update(previous_id='underworld-boss-rift-titan-wrap-r6',previous_triangles=1800,reduction_percent=(1-row['triangles']/1800)*100,changes='front-first primary silhouette: compact cleft head, fuller shoulders and limbs, narrower arm spread, longer lower-body proportion; fitted left-facing depth',rig='original eight-bone hierarchy and seven retained actions; fitted rest positions',status='unaccepted front/side silhouette candidate; texture detail deliberately deferred')
p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
concept=json.loads((out/'concept-pass.json').read_text())
concept.update(scope='whole-creature front-first silhouette followed by left-facing side profile; surface detail deferred',anatomy='one long cleft stone head; layered broad mantle and fuller limb/hand/foot masses; exposed continuous rib cage with wrapped middle and upper rings; tapered depth and forward profile',preserved='1800 triangles, one material, four 512px maps, original eight-bone hierarchy and seven actions; rest positions fitted',remaining='user front/side silhouette review; surface fracture and weathering only after silhouette acceptance; native loader and live deformation/performance acceptance',status='silhouette candidate; not adequately reworked')
(out/'concept-pass.json').write_text(json.dumps(concept,indent=2)+'\n',encoding='utf-8')
torso=json.loads((out/'torso-authoring.json').read_text());torso.update(scope='r6 ring topology retained within the r7 whole-creature spatial fit; all collar, rib, band and neck contacts reseated',parameter_units='r6 authored ring dimensions precede r7 width/height fitting and final uniform study scaling')
(out/'torso-authoring.json').write_text(json.dumps(torso,indent=2)+'\n',encoding='utf-8')
