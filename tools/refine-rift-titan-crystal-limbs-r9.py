from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-rift-titan-joints-r8.py').read_text(encoding='utf-8').replace('rift-titan-joints-r8','rift-titan-crystal-limbs-r9').replace('rift_titan_joint_silhouette','rift_titan_crystal_limbs')
exec(compile(source,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-crystal-limbs-r9'
p=out/'refinement-metrics.json';row=json.loads(p.read_text());row.update(previous_id='underworld-boss-rift-titan-joints-r8',changes='deeper individualized arm/leg facets and sixteen attached crystal growths; twelve new, four repurposed; joint bindings condensed',status='unaccepted requested limb depth/detail and crystal candidate')
p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
p=out/'concept-pass.json';row=json.loads(p.read_text());row.update(scope='requested limb depth/detail and additional crystals',remaining='user limb/crystal review, further silhouette and stone texture refinement, native animation/performance acceptance',status='unaccepted candidate');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
