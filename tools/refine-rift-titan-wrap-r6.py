import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-rift-titan-strata-r4.py').read_text(encoding='utf-8').replace('rift-titan-strata-r4','rift-titan-wrap-r6').replace('rift_titan_cage_refinement','rift_titan_wrapped_torso')
exec(compile(source,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-wrap-r6';p=out/'refinement-metrics.json';row=json.loads(p.read_text(encoding='utf-8'))
row.update(previous_id='underworld-boss-rift-titan-torso-r5',previous_triangles=1780,reduction_percent=(1-row['triangles']/1780)*100,changes='thin stone band wraps twelve continuous ribs; upper collar is a closed ring seated into both upper torso masses; smaller V',status='unaccepted art candidate; torso wrap and collar ring correction implemented for review')
p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
concept=json.loads((out/'concept-pass.json').read_text(encoding='utf-8'))
concept.update(scope='torso wrap and collar ring correction; not final surface detail',anatomy='thin stone band encircles continuous metal rib cage with ribs above and below a small V; upper stone collar is a closed ring seated into the shoulder masses, supporting torso and neck ribs',remaining='user torso review; crown and shoulder silhouette; individualized weathering, stone fractures and corroded metal textures; native animation/performance acceptance')
(out/'concept-pass.json').write_text(json.dumps(concept,indent=2)+'\n',encoding='utf-8')
