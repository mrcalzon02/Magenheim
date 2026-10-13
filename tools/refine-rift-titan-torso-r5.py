import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-rift-titan-strata-r4.py').read_text(encoding='utf-8').replace('rift-titan-strata-r4','rift-titan-torso-r5').replace('rift_titan_cage_refinement','rift_titan_ribbed_torso')
exec(compile(source,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-torso-r5';p=out/'refinement-metrics.json';row=json.loads(p.read_text(encoding='utf-8'))
row.update(previous_id='underworld-boss-rift-titan-strata-r4',previous_triangles=1660,reduction_percent=(1-row['triangles']/1660)*100,changes='fourteen swept metal torso ribs including a lower cage, central V stone plate, collarbone stone plate and six supported neck ribs',status='unaccepted art candidate; user torso interpretation implemented for review')
p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
concept=json.loads((out/'concept-pass.json').read_text(encoding='utf-8'))
concept.update(scope='torso and neck interpretation correction; not final surface detail',anatomy='metal ribs wrap around the crystal core above and below a solid V-shaped stone cross-chest band; upper stone collarbone supports separate metal neck ribs into the retained head',remaining='user torso review; crown and shoulder silhouette; individualized weathering, stone fractures and corroded metal textures; native animation/performance acceptance')
(out/'concept-pass.json').write_text(json.dumps(concept,indent=2)+'\n',encoding='utf-8')
