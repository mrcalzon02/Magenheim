from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-rift-titan-notched-r11.py').read_text(encoding='utf-8').replace('rift-titan-notched-r11','rift-titan-cracked-head-r12').replace('rift_titan_notched_stone','rift_titan_cracked_head')
exec(compile(source,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-cracked-head-r12'
p=out/'refinement-metrics.json';row=json.loads(p.read_text());row.update(previous_id='underworld-boss-rift-titan-notched-r11',previous_triangles=1800,reduction_percent=(1-row['triangles']/1800)*100,changes='one connected crystal head with beveled recessed purple fissure; six neck roots reseated; former split shells and floating head slivers replaced',status='unaccepted corrected head interpretation; 85% not claimed');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
p=out/'concept-pass.json';row=json.loads(p.read_text());row.update(scope='user correction: head is one crystal form with a recessed beveled crack and purple crystal inside it',remaining='user head review, core density, crest debris and finer texture/fragmentation; native validation',status='unaccepted interpretation correction');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
