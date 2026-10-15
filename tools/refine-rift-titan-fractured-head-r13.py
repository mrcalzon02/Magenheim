"""Standard atlas and export recipe for the audited head repair candidate."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-rift-titan-notched-r11.py').read_text(encoding='utf-8').replace('rift-titan-notched-r11','rift-titan-fractured-head-r13').replace('rift_titan_notched_stone','rift_titan_fractured_head')
exec(compile(source,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-fractured-head-r13'
p=out/'refinement-metrics.json';row=json.loads(p.read_text());row.update(previous_id='underworld-boss-rift-titan-cracked-head-r12',previous_triangles=1792,reduction_percent=(1-row['triangles']/1792)*100,changes='head only: tapered shell discarded; twenty-corner extruded slab perimeter with long straight shaft edges, abrupt shoulders and triangular lower breaks; four cortex fracture points, recessed variable fault and six reseated neck ends',status='unaccepted replacement after user rejected the still-oval first r13 attempt');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
p=out/'concept-pass.json';row=json.loads(p.read_text());row.update(scope='H01-H04 head repair candidate; preserve remaining body and rig',remaining='secondary fracture hierarchy, surface detail and all other open audit regions; native validation',status='unaccepted repair candidate');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
