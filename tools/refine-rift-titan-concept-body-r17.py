from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/refine-rift-titan-fracture-planes-r16.py').read_text().replace('rift-titan-fracture-planes-r16','rift-titan-concept-body-r17').replace('rift_titan_fracture_planes','rift_titan_concept_body').replace("sculpt.build(B,arm,record,profile,tube,cap)","sculpt.build(B,arm,record,profile,tube,cap)")
exec(compile(s,str(__file__),'exec'))
import json
out=ROOT/'docs/art/creature-models/2026-10-16/rift-titan-concept-body-r17'
for name in ('refinement-metrics','concept-pass'):
    p=out/(name+'.json');row=json.loads(p.read_text());row.update(previous_id='underworld-boss-rift-titan-fracture-planes-r16',previous_triangles=1800,reduction_percent=(1-row.get('triangles',1798)/1800)*100,rig='eight original bones/hierarchy and seven actions; forearm tails fitted with geometry; support-matched roots',texture_bake='fracture-plane stone pigment, baked local occlusion and small edge bevel normals; four packed 512px maps; head uses a dedicated atlas strip',changes='raised rear crest; fuller canted cage/core; lower wrists and long ridged digits; hanging pelvic rocks; cleft planted foot masses; r16 head and neck preserved',status='whole-creature correction candidate; art and native acceptance pending');p.write_text(json.dumps(row,indent=2)+'\n')
