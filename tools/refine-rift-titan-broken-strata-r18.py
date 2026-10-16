from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/refine-rift-titan-concept-body-r17.py').read_text().replace('rift-titan-concept-body-r17','rift-titan-broken-strata-r18').replace('rift_titan_concept_body','rift_titan_broken_strata')
exec(compile(s,str(__file__),'exec'))
import json
out=ROOT/'docs/art/creature-models/2026-10-16/rift-titan-broken-strata-r18'
for name in ('refinement-metrics','concept-pass'):
    p=out/(name+'.json');row=json.loads(p.read_text());row.update(previous_id='underworld-boss-rift-titan-concept-body-r17',previous_triangles=1798,reduction_percent=(1-row.get('triangles',1800)/1798)*100,rig='eight original bones and seven actions; exact r17 rest rig preserved',texture_bake='locally directed gray stone fractures and sparse warm mineral; four packed 512 maps',changes='thick bent ridged-knuckle fingers, broad toe stones and rear heel per foot, bent/chipped rear crest, longer mantle/limb strata; head/neck and torso ring geometry preserved',status='continued concept candidate; art and native acceptance pending');p.write_text(json.dumps(row,indent=2)+'\n')
