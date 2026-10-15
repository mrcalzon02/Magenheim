"""Owned atlas/export recipe, new head volume and neck, unchanged cost."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/refine-rift-titan-notched-r11.py').read_text(encoding='utf-8').replace('rift-titan-notched-r11','rift-titan-lined-volume-r14').replace('rift_titan_notched_stone','rift_titan_head_volume')
exec(compile(s,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-lined-volume-r14'
p=out/'refinement-metrics.json';row=json.loads(p.read_text());row.update(previous_id='underworld-boss-rift-titan-fractured-head-r13',previous_triangles=1800,reduction_percent=0,changes='approved head front outline retained; back-raked volumetric prism, left-receding crystal-lined fracture and six neck bars arranged across front/back arcs',status='unaccepted head/neck candidate; approved r13 front outline preserved');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
p=out/'concept-pass.json';row=json.loads(p.read_text());row.update(scope='head depth/rake, specifically asymmetric lined fracture and three-dimensional neck',remaining='fine fractures, crisp head/body surfaces and other audit regions; native validation',status='unaccepted candidate');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
