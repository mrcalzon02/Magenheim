"""Rebuild the online plate-layout/facet changes without replacing the r3 review."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-rift-titan-primary.py').read_text(encoding='utf-8').replace('rift-titan-primary-r3','rift-titan-strata-r4').replace('creature-models/2026-10-12','creature-models/2026-10-13')
hook="s=s.replace('from rift_titan_primary_refinement import build', 'import rift_titan_cage_refinement as sculpt\\nsculpt.OUT=OUT\\nbuild=sculpt.build')\n"
source=source.replace("exec(compile(s,str(__file__),'exec'))",hook+"exec(compile(s,str(__file__),'exec'))")
exec(compile(source,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-strata-r4';p=out/'refinement-metrics.json';row=json.loads(p.read_text(encoding='utf-8'))
row.update(previous_id='underworld-boss-rift-titan-primary-r3',previous_triangles=1480,reduction_percent=(1-row['triangles']/1480)*100,study_height_metres=7.0,rig='retained eight-bone Rift Colossus hierarchy and seven actions; one influence per rigid fragment',changes='online bone-aligned long limb strata, thinner shoulders, shallow fracture facets; closed hoop seams and supported brace roots',status='unaccepted art candidate; native admission and performance pending')
p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
