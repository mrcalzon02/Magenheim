"""Validate concept files and build a portable review gallery and honest coverage ledger."""
import json,html,hashlib,re,math
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/asset-concepts/2026-10-03'
entries=json.loads((OUT/'prompts.json').read_text(encoding='utf-8'))
catalog=ROOT/'assets/models/catalog.json'
models=json.loads(catalog.read_text(encoding='utf-8'))
weapon_source=ROOT/'src/Magenheim.Core/Underworld/UnderworldWeaponUpgradeCatalog.cs'
ancestry=[]
model_index={m['id']:m for m in models}
for prefab,name,model_id,parent_id in re.findall(r'Upgrade\("([^"]+)", "([^"]+)", "([^"]+)", "([^"]+)"',weapon_source.read_text(encoding='utf-8')):
 parent=model_index[parent_id]
 ancestry.append(dict(name=name,prefab=prefab,model_id=model_id,parent_model_id=parent_id,
  authoritative_parent_source='../../../../assets/models/'+parent['source'],
  parent_source_sha256=hashlib.sha256((ROOT/'assets/models'/parent['source']).read_bytes()).hexdigest(),
  rule='Keep parent geometry, orientation, grip and sockets. New art supplies material/fitting proposals only; illustrated dimensions cannot replace the parent.'))
(OUT/'weapon-ancestry.json').write_text(json.dumps(ancestry,indent=2),encoding='utf-8')
font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',20)
complete=[]
for e in entries:
 p=OUT/e['file']
 if not p.exists():continue
 with Image.open(p) as im: im.verify()
 with Image.open(p) as im:
  e['dimensions']=list(im.size)
  im=im.convert('RGB');im.thumbnail((480,320))
  im.save(OUT/(e['id']+'--thumb.jpg'),quality=88)
 e['sha256']=hashlib.sha256(p.read_bytes()).hexdigest();e['status']='generated';complete.append(e)
groups=list(dict.fromkeys(e['category'] for e in entries))
overview_groups=[(group,[e for e in complete if e['category']==group]) for group in groups]
overview_groups.append(('Elemental Surtlings',[e for e in complete if e['id'].startswith('surtlings-')]))
for group,subset in overview_groups:
 if not subset:continue
 canvas=Image.new('RGB',(1600,58+math.ceil(len(subset)/4)*300),'#171c22');draw=ImageDraw.Draw(canvas)
 draw.text((18,15),group,fill='#eee5d0',font=font)
 for i,e in enumerate(subset):
  x=i%4*400;y=58+i//4*300
  with Image.open(OUT/e['file']) as im:
   im=im.convert('RGB');im.thumbnail((384,248));canvas.paste(im,(x+8,y+8))
  label=e['name'].split(' — ')[0]
  if len(label)>33:label=label[:30]+'...'
  draw.text((x+8,y+263),label,fill='#ede7dc',font=font)
 canvas.save(OUT/(re.sub('[^a-z0-9]+','-',group.lower())+'--overview.jpg'),quality=94)
cards=''.join('<article data-category="'+html.escape(e['category'])+'" data-biome="'+html.escape(e['biome'])+'" data-search="'+html.escape((' '.join(e['members'])+' '+e['name']).lower())+'"><a href="'+e['file']+'"><img loading="lazy" src="'+e['id']+'--thumb.jpg" alt="'+html.escape(e['name'])+'"></a><h2>'+html.escape(e['name'])+'</h2><p>'+html.escape(e['category']+' · '+e['biome'])+'</p><details><summary>Named members and brief</summary><p>'+html.escape('; '.join(e['members']))+'</p><p>'+html.escape(e['prompt'])+'</p></details></article>' for e in complete)
options=lambda vals:''.join('<option>'+html.escape(v)+'</option>' for v in vals)
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Magenheim asset design library</title><style>body{margin:0;background:#171c22;color:#ece6dc;font:16px system-ui}header,nav{padding:24px 5vw}header p{max-width:960px;color:#bfc6c9}a{color:#acd1dc}nav{display:flex;gap:12px;flex-wrap:wrap}input,select{font:inherit;padding:10px;background:#252e36;color:#eee;border:1px solid #657078;border-radius:5px}main{padding:0 5vw 50px;display:grid;grid-template-columns:repeat(auto-fit,minmax(310px,1fr));gap:20px}article{background:#252b32;border:1px solid #424c55;border-radius:7px;overflow:hidden}img{width:100%;display:block}h2{font-size:20px;margin:16px}article p,details{margin:16px;font-size:14px}details p{margin:14px 0}summary{cursor:pointer}[hidden]{display:none!important}</style><header><h1>Magenheim · Asset design library</h1><p>COUNT new reference sheets · 3 October 2026. Proposed family directions for review, alongside the already adopted creature targets. A family sheet establishes shared construction language; it is not a measured drawing of every variant.</p><p><a href="../README.md">Design and production guide</a> · <a href="../../creature-concepts/2026-10-03/gallery.html">48 adopted creature targets</a> · <a href="coverage.md">Model-catalog coverage</a></p></header><nav><input id="search" placeholder="Find an asset" aria-label="Find an asset"><select id="category" aria-label="Category"><option value="">All categories</option>CATEGORIES</select><select id="biome" aria-label="Biome"><option value="">All biomes</option>BIOMES</select></nav><main>CARDS</main><script>const f=['search','category','biome'].map(x=>document.getElementById(x));function update(){let [s,c,b]=f.map(x=>x.value.toLowerCase());document.querySelectorAll('article').forEach(a=>a.hidden=!(a.dataset.search.includes(s)&&(!c||a.dataset.category.toLowerCase()===c)&&(!b||a.dataset.biome.toLowerCase()===b)))}f.forEach(x=>x.addEventListener('input',update))</script></html>'''
page=page.replace('COUNT',str(len(complete))).replace('CATEGORIES',options(groups)).replace('BIOMES',options(list(dict.fromkeys(e['biome'] for e in entries)))).replace('CARDS',cards)
(OUT/'gallery.html').write_text(page,encoding='utf-8')
existing=json.loads((ROOT/'docs/art/creature-concepts/2026-10-03/generation-manifest.json').read_text(encoding='utf-8'))
def slug(s):return re.sub('[^a-z0-9]+','-',s.lower()).strip('-')
biome_names={'fungal':'Fungal Forest','blackwater':'Blackwater Deep','sulfur':'Sulfurous Wastes','frozen':'Frozen Caverns','fracture':'Fracture Zones','decay':'Great Decay'}
def sheet(key):return next(e['file'] for e in entries if e['id']==key)
def classify(mid):
 if mid.startswith(('staff-','crystal-weapon-','Magenheim_Staff_')):return 'preserve-approved-base',None
 if mid.startswith('underworld-creature-'):
  tail=mid.removeprefix('underworld-creature-')
  match=next((e for e in existing if slug(e['name'])==tail),None)
  if match:return 'adopted-individual','../../creature-concepts/2026-10-03/'+match['file']
 if mid.startswith('underworld-boss-') and not mid.startswith('underworld-boss-arena-'):
  match=next((e for e in existing if e['kind']=='boss' and slug(e['name']).removeprefix('the-')==mid.removeprefix('underworld-boss-')),None)
  if match:return 'adopted-individual','../../creature-concepts/2026-10-03/'+match['file']
 if mid.startswith('deep-fracture-creature-'):
  token=mid.split('-')[-2];name=re.sub(r'(?<!^)(?=[A-Z])',' ',token)
  return 'proposed-chassis',sheet('deep-'+slug(name))
 for old,new in [('crystal-golem','crystal-golem'),('deep-colossus','deep-colossus'),('obelisk-warden','obelisk-warden')]:
  if mid.startswith('deep-fracture-'+old+'-visual'):return 'proposed-chassis',sheet('deep-'+new)
 if mid.startswith('underworld-surtling-'):return 'proposed-elemental-anatomy',sheet('surtlings-'+mid.split('-')[-2])
 if mid.startswith('underworld-armor-'):
  sets={'sporeweave':'fungal','palewater':'blackwater','emberiron':'sulfur','rimeward':'frozen','stoneanchor':'fracture','defiant':'decay'}
  for s,b in sets.items():
   if s in mid:return 'native-donor-texture-direction',sheet(b+'-armour')
 for e in entries:
  if e['category'] in ('Tools','Stations','Weapon variants'):
   if any(mid.endswith(slug(member)) for member in e['members']):return 'proposed-family',e['file']
 if mid.startswith(('underworld-resource-','underworld-refined-')):
  for e in entries:
   if e['category']=='Materials' and any(mid.endswith(slug(m)) for m in e['members']):return 'proposed-family',e['file']
 if mid.startswith(('underworld-flora-','underworld-ground-','underworld-geothermal-')):
  for b in biome_names:
   if b in mid:return 'proposed-family',sheet(b+'-ecology')
  if 'cinderstalk' in mid or 'geothermal' in mid:return 'proposed-family',sheet('sulfur-ecology')
  return 'proposed-family',sheet('fungal-ecology')
 if mid.startswith('rootforged-'):return 'proposed-family',sheet('rootforged-modular-kit')
 if mid.startswith(('architecture-crystal-','crystal-banner-')) or mid in ('crystal-brazier','crystal-lantern','crystal-sentinel','crystal-wardstone'):return 'proposed-family',sheet('crystal-building-kit')
 if mid.startswith('blackstone-') or mid=='dark-throne':return 'proposed-family',sheet('blackstone-architecture')
 if mid.startswith(('furniture-','decor-','crystal-bed-')) or mid=='crystalline-ice-box':return 'proposed-family',sheet('mineral-furniture')
 if mid.startswith(('DF-','deep-fracture-')):return 'proposed-family',sheet('fracture-dungeon-kit')
 if mid in ('broken-crown','null-mantle') or mid.startswith('nowhere-king-sword-'):return 'preserve-shape-material-direction',sheet('null-mantle-regalia')
 if mid.startswith('underworld-boss-arena-'):return 'biome-environment-direction',sheet({'first-bloom':'fungal','blackwater-maw':'blackwater','furnace-heart':'sulfur','white-silence':'frozen','rift-titan':'fracture','carrion-crown':'decay'}.get(mid.removeprefix('underworld-boss-arena-'),'fungal')+'-ecology')
 if mid.startswith(('effect-','sentinel-ammo-')):return 'shared-effect-direction',None
 if mid.startswith('underworld-deepstone-') or mid in ('underworld-dais','underworld-standing-stone','underworld-descent-monolith'):return 'ritual-family-direction',sheet('fracture-dungeon-kit')
 if mid.startswith(('earth-','boss-','spirit-fetish-')) or mid in ('elemental-focus','fate-crystal','fate-shard','geode-sample','norn-spindle','passage-stone','rune-engraver','runed-totem','runic-keelstone','seidr-ritual-focus','crystal-enchanting-dais'):return 'proposed-family',sheet('arcane-materials-workshop')
 return 'needs-specific-reference',None
coverage=[]
for m in models:
 status,ref=classify(m['id'])
 coverage.append(dict(model_id=m['id'],status=status,reference=ref))
planned_files={e['file'] for e in entries}
for c in coverage:
 ref=c['reference']
 if ref and not ref.startswith('../../') and ref not in planned_files:
  raise SystemExit('Coverage points to unknown sheet: '+ref)
 if ref and ref.startswith('../../') and not (OUT/ref).is_file():
  raise SystemExit('Missing adopted reference: '+ref)
ledger=dict(catalog_sha256=hashlib.sha256(catalog.read_bytes()).hexdigest(),catalog_records=len(models),meaning='Catalog snapshot, not proof that every source is active at runtime. Family coverage is a direction, not individual approved construction drawings.',models=coverage,non_model_foods=['Glowcap Stew','Mycelial Broth','Heartcap Ration'])
(OUT/'coverage.json').write_text(json.dumps(ledger,indent=2),encoding='utf-8')
lines=['# Catalog reference coverage','','This ledger covers the '+str(len(models))+' model records in the source catalog snapshot. Some are legacy or retired (including generated armor). Inclusion does not establish runtime admission. Family references establish shared direction; individual proportions still require construction drawings. Foods registered from native donors are indexed separately in the food sheet.','','| Model | Coverage | Reference |','|---|---|---|']
for c in coverage:lines.append('| '+c['model_id']+' | '+c['status']+' | '+('[sheet]('+c['reference']+')' if c['reference'] else 'Existing base / guide only')+' |')
(OUT/'coverage.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
(OUT/'generation-manifest.json').write_text(json.dumps(complete,indent=2),encoding='utf-8')
(OUT/'prompts.json').write_text(json.dumps(entries,indent=2),encoding='utf-8')
lines=['# Asset reference run — 3 October 2026','',str(len(complete))+' / '+str(len(entries))+' sheets generated with built-in imagegen. [Review gallery](gallery.html) · [Design guide](../README.md) · [Coverage ledger](coverage.md) · [Exact prompts](prompts.json)','','New references are proposals pending user review. Previously adopted creature sheets and approved weapon bases retain their status.','']
for group in groups:
 lines+=['## '+group,'','| Sheet | Named members |','|---|---|']
 lines += ['| ['+e['name']+']('+e['file']+') | '+'; '.join(e['members'])+' |' for e in complete if e['category']==group]
 lines+=['']
(OUT/'README.md').write_text('\n'.join(lines),encoding='utf-8')
missing=[c['model_id'] for c in coverage if c['status']=='needs-specific-reference']
ids=[e['id'] for e in entries];files=[e['file'] for e in entries]
if len(ids)!=len(set(ids)) or len(files)!=len(set(files)):
 raise SystemExit('Duplicate selected target ID or file')
hashes=[e['sha256'] for e in complete]
if len(hashes)!=len(set(hashes)):
 raise SystemExit('Duplicate image contents across selected targets')
old_names={e['name'].lower().removeprefix('the ') for e in existing}
creature_overlap=[member for e in entries if e['category']=='Creatures' for member in e['members'] if member.lower().removeprefix('the ') in old_names]
if creature_overlap:raise SystemExit('New creature duplicates adopted target: '+str(creature_overlap))
audit=dict(selected_targets=len(entries),unique_target_ids=len(set(ids)),unique_selected_files=len(set(files)),
 unique_selected_image_hashes=len(set(hashes)),
 adopted_creature_targets_reused=len(existing),exact_creature_name_overlaps=creature_overlap,
 deep_fracture_rule='One target per 13 chassis; eight elemental model versions reuse the chassis target.',
 surtling_rule='Six anatomically distinct elemental pair sheets; each covers its masculine/feminine bodies. Shared bone-role contract, not shared visible physique. Original recolor board rejected.',
 context_rule='Materials shown on benches, weapons and armour are use-context studies. The six material sheets govern shared material identity.',
 drafts_rule='Retained drafts are unselected; only generation-manifest.json and gallery selections govern the proposed target.')
(OUT/'deduplication-audit.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
print(json.dumps(dict(verified=len(complete),expected=len(entries),catalog_records=len(models),unassigned=missing),indent=2))
if len(complete)!=len(entries): raise SystemExit('Incomplete art run')
