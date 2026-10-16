from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/refine-rift-titan-lined-volume-r14.py').read_text(encoding='utf-8').replace('rift-titan-lined-volume-r14','rift-titan-paired-recesses-r15').replace('rift_titan_head_volume','rift_titan_paired_recesses')
exec(compile(s,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-paired-recesses-r15'
p=out/'refinement-metrics.json';row=json.loads(p.read_text());row.update(previous_id='underworld-boss-rift-titan-lined-volume-r14',changes='continuous frontal plate restored; two deep crystal-lined flank pockets behind it; approved front outline and r14 rake retained',status='unaccepted paired-pocket candidate');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
p=out/'concept-pass.json';row=json.loads(p.read_text());row.update(scope='user supplied side crop: front plate with deep crystal recess on either side, resembling eyes',remaining='finer fracture layers, crisp stone material and all outstanding other audit regions',status='unaccepted paired-pocket candidate');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
