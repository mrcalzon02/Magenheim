from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-rift-titan-joints-r8.py').read_text(encoding='utf-8').replace('rift-titan-joints-r8','rift-titan-layered-r10').replace('rift_titan_joint_silhouette','rift_titan_layered_weathering')
exec(compile(source,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-layered-r10'
p=out/'refinement-metrics.json';row=json.loads(p.read_text());row.update(previous_id='underworld-boss-rift-titan-crystal-limbs-r9',changes='forward crest, supported fuller upper thorax, overlapping longer limb strata and authored weathering/crystal surfaces',texture_bake='owned spatial stone fracture/lichen/ochre, corroded metal and varied violet crystal shaders baked at 1024 and condensed to four 512px maps',status='unaccepted concept refinement; 85% not achieved or measured');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
p=out/'concept-pass.json';row=json.loads(p.read_text());row.update(scope='continued primary silhouette and requested depth/detail, including owned surface authoring',remaining='head/shoulder fracture contours, irregular core and floating crest debris, further limb edge chips, texture polish and native validation',status='unaccepted concept refinement; current user rating not great');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
