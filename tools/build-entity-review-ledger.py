"""Inventory the 48 selected concepts and actual current runtime model routes."""
from pathlib import Path
import json,re,hashlib,html
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/entity-review/2026-10-25';OUT.mkdir(parents=True,exist_ok=True)
CONCEPT=ROOT/'docs/art/creature-concepts/2026-10-03'
manifest=json.loads((CONCEPT/'generation-manifest.json').read_text());assert len(manifest)==48
source=(ROOT/'src/Magenheim.Runtime/UnderworldCreaturePrototypes.cs').read_text();ordinary=dict(re.findall(r'"([^"\n]+)"\s*=>\s*"(underworld-creature-[^"]+)"',source))
donors={name:donor for name,biome,donor in re.findall(r'new\("([^"]+)",\s*"([^"]+)",\s*"([^"]+)"',source)}
native_file=ROOT/'dist/vanilla-creature-comparison/native-metrics.json';saved_native=OUT/'native-donor-triangles.json'
if native_file.exists():
    native={r['name']:r['highDetailTriangles'] for r in json.loads(native_file.read_text())}
    saved_native.write_text(json.dumps(dict(source='dist/vanilla-creature-comparison/native-metrics.json',source_sha256=hashlib.sha256(native_file.read_bytes()).hexdigest(),high_detail_triangles=native),indent=2)+'\n')
else:native=json.loads(saved_native.read_text())['high_detail_triangles']
boss_base={'The First Bloom':'Crowncap Brute','The Rift Titan':'Rift Colossus','The Carrion Crown':'Corpse Orchard','The White Silence':'Cryolith Guardian','The Furnace Heart':'Furnace Golem','The Blackwater Maw':'Deep Hunter'}
refined={'The First Bloom':'first-bloom','The Rift Titan':'rift-titan','The Carrion Crown':'carrion-crown'}
old={r['name']:r for r in json.loads((OUT/'entities.json').read_text())['entities']} if (OUT/'entities.json').exists() else {}
entries=[]
for row in manifest:
    name=row['name'];concept=CONCEPT/row['file'];assert concept.is_file()
    base=boss_base.get(name,name);donor=donors[base];model='underworld-boss-'+refined[name]+'-refined' if name in refined else ordinary[base]
    runtime=ROOT/'assets/models/runtime'/(model+'.model.json');doc=json.loads(runtime.read_text());points=[p for part in doc['parts'] for p in part['vertices']];bounds=[max(p[k] for p in points)-min(p[k] for p in points) for k in range(3)]
    default='accepted-preserve' if name=='The First Bloom' else 'paused-by-user' if name=='The Blackwater Maw' else 'active-anatomy-review' if name in ('The Rift Titan','The Carrion Crown') else 'queued-concept-audit'
    previous=old.get(name,{})
    entry=dict(name=name,kind=row['kind'],biome=row['biome'],concept=concept.relative_to(ROOT).as_posix(),concept_sha256=hashlib.sha256(concept.read_bytes()).hexdigest(),runtime_model=model,runtime=runtime.relative_to(ROOT).as_posix(),runtime_sha256=hashlib.sha256(runtime.read_bytes()).hexdigest(),runtime_triangles=sum(len(p['triangles'])//3 for p in doc['parts']),runtime_renderers=len(doc['parts']),runtime_materials=len({p['material']['name'] for p in doc['parts']}),runtime_rest_dimensions_metres=bounds,native_donor=donor,native_triangles=native.get(donor),soft_triangle_cap=int(native[donor]*1.5) if donor in native else None,status=previous.get('status',default),user_art_accepted=name=='The First Bloom',visual_audit=previous.get('visual_audit','pending actual concept/model inspection'),latest_study=previous.get('latest_study'),findings=previous.get('findings',[]))
    entries.append(entry)
priority={'The Rift Titan':0,'The Carrion Crown':1,'The White Silence':2,'The Furnace Heart':3,'The First Bloom':100,'The Blackwater Maw':101}
entries.sort(key=lambda r:(0 if r['kind']=='boss' else 1,priority.get(r['name'],0) if r['kind']=='boss' else -max(r['runtime_rest_dimensions_metres']),r['name']))
doc=dict(date='2026-10-25',total_entities=48,accepted_art=1,paused_entities=1,scope='Six bosses first, then 42 ordinary creatures from largest current rest envelope to smallest. Inventory/rendering is not visual acceptance. Blackwater Maw explicitly remains paused by the user on October 25.',entities=entries)
(OUT/'entities.json').write_text(json.dumps(doc,indent=2)+'\n')
style='<style>body{background:#181d20;color:#e4e1d8;font:16px/1.5 system-ui;max-width:1550px;margin:auto;padding:28px}a{color:#b6d4ed}table{width:100%;border-collapse:collapse}td,th{padding:10px;border-bottom:1px solid #465052;vertical-align:top;text-align:left}img{width:180px;max-height:160px;object-fit:contain}.note{padding:16px;background:#28302e}small{color:#b6bdbb}</style>'
page=['<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Entity review campaign</title>',style,'<h1>Entity review campaign — 25 October 2026</h1>','<p class="note">48 adopted concepts. One boss has explicit art acceptance. Blackwater Maw remains paused. This ledger distinguishes current game models, review candidates, technical checks and user acceptance. A rendered baseline is not a completed visual review.</p>','<table><tr><th>Entity / concept</th><th>Current game model</th><th>Cost / donor</th><th>Review state</th></tr>']
for r in entries:
    concept='../../../'+r['concept'].removeprefix('docs/');slug=r['name'].lower().replace(' ','-');render='baselines/'+slug+'--three-quarter.png';preview=fig=f'<a href="{render}"><img src="{render}" alt="Actual runtime geometry"></a>' if (OUT/render).exists() else '<small>Current baseline pending</small>'
    cost=f'{r["runtime_triangles"]:,} triangles / {r["runtime_renderers"]} renderer(s)<br><small>{r["native_donor"]}: {r["native_triangles"] if r["native_triangles"] else "measurement pending"}</small>'
    page.append(f'<tr><td><a href="{slug}.html">{html.escape(r["name"])}</a><br><a href="{concept}"><small>Selected concept</small></a><br><small>{r["biome"]}</small></td><td>{preview}<br><small>{r["runtime_model"]}</small></td><td>{cost}</td><td>{html.escape(r["status"])}<br><small>{html.escape(r["visual_audit"])}</small></td></tr>')
page.append('</table></html>');(OUT/'gallery.html').write_text('\n'.join(page),encoding='utf-8')
print('VERIFIED 48 selected concept files and current runtime routes; accepted/paused work preserved.')
