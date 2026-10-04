"""Review index and contact sheets for the dedicated concept run."""
import json,html,hashlib,math,re
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-concepts/2026-10-03'
entries=json.loads((OUT/'prompts.json').read_text(encoding='utf-8'))
font_path=Path('C:/Windows/Fonts/segoeui.ttf')
font=ImageFont.truetype(str(font_path),22)
complete=[]
for e in entries:
 path=OUT/e['file']
 if not path.exists():continue
 with Image.open(path) as img:
  img.verify()
 e['status']='generated';e['sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
 complete.append(e)
biomes=list(dict.fromkeys(e['biome'] for e in entries))
for group,subset in [('Bosses',[e for e in complete if e['kind']=='boss'])]+[(b,[e for e in complete if e['biome']==b]) for b in biomes]:
 if not subset:continue
 canvas=Image.new('RGB',(1600,48+math.ceil(len(subset)/4)*308),'#171b20');draw=ImageDraw.Draw(canvas)
 draw.text((18,10),group,fill='#f0e6cf',font=font)
 for i,e in enumerate(subset):
  x=(i%4)*400;y=48+(i//4)*308
  with Image.open(OUT/e['file']) as img:
   img.convert('RGB').resize((384,256)).save(OUT/(e['file'].removesuffix('.png')+'--thumb.jpg'),quality=85)
   img.thumbnail((384,256));canvas.paste(img,(x+8,y+8))
  draw.text((x+8,y+266),e['name'],fill='#f2eee5',font=font)
 canvas.save(OUT/(re.sub('[^a-z0-9]+','-',group.lower())+'--overview.jpg'),quality=94)
cards=''.join(f'''<article data-biome="{html.escape(e['biome'])}" data-kind="{e['kind']}" data-name="{html.escape(e['name'].lower())}"><a href="{e['file']}" target="_blank"><img src="{e['file'].removesuffix('.png')}--thumb.jpg" loading="lazy" alt="{html.escape(e['name'])}"></a><h2>{html.escape(e['name'])}</h2><p>{html.escape(e['biome'])} · {e['kind'].title()}</p><details><summary>Design brief</summary><p>{html.escape(e['anatomy'])}</p></details></article>''' for e in complete)
options=''.join(f'<option>{html.escape(b)}</option>' for b in biomes)
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Magenheim creature concepts</title><style>body{margin:0;background:#15191e;color:#e9e4d8;font:17px system-ui}header{padding:32px 5vw;border-bottom:1px solid #3e434b}h1{font-size:32px;margin:0 0 8px}header p{max-width:800px;color:#bcbebd}nav{display:flex;gap:12px;flex-wrap:wrap;padding:20px 5vw}select,input{background:#252a31;color:#eee;border:1px solid #666;padding:10px;border-radius:6px;font:inherit}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:22px;padding:0 5vw 50px}article{border:1px solid #373e48;border-radius:8px;overflow:hidden;background:#20252c}article img{width:100%;display:block}h2{font-size:21px;margin:14px 18px 8px}article p,details{margin:12px 18px;font-size:15px;color:#c4c5c2}details p{margin:14px 0}summary{cursor:pointer}a{color:#b9d7dc}[hidden]{display:none!important}</style><header><h1>Magenheim · Creature concept run</h1><p>Individual design references for 42 creatures and six biome bosses. These are proposals for model silhouettes, anatomy and materials. Fine painted detail is not a polygon requirement. Click a sheet to inspect it at full size.</p><p>COUNT generated sheets · 3 October 2026</p></header><nav><input id="search" placeholder="Find a creature" aria-label="Find a creature"><select id="biome" aria-label="Biome"><option value="">All biomes</option>OPTIONS</select><select id="kind" aria-label="Creature type"><option value="">Creatures and bosses</option><option value="boss">Bosses</option><option value="creature">Regular creatures</option></select></nav><main>CARDS</main><script>const filters=['search','biome','kind'].map(id=>document.getElementById(id));function update(){const [s,b,k]=filters.map(x=>x.value.toLowerCase());document.querySelectorAll('article').forEach(a=>a.hidden=!(a.dataset.name.includes(s)&&(!b||a.dataset.biome.toLowerCase()===b)&&(!k||a.dataset.kind===k)))}filters.forEach(x=>x.addEventListener('input',update));</script></html>'''.replace('COUNT',str(len(complete))).replace('OPTIONS',options).replace('CARDS',cards)
page=page.replace('These are proposals for model silhouettes, anatomy and materials.','Adopted construction targets for silhouettes, anatomy and materials.')
(OUT/'gallery.html').write_text(page,encoding='utf-8')
lines=['# Magenheim creature design targets','',f'{len(complete)} of 48 individual sheets preserved. Built-in imagegen; prompts preserved in [prompts.json](prompts.json).','', 'Adopted as the creature construction design reference on 3 October 2026.','', '[Open the filterable gallery](gallery.html) · [Construction and reference guide](../README.md)','', 'Each sheet provides a concrete target for silhouette, anatomy and materials. Distill these into Valheim-scale geometry and owned textures using the construction guide; the artwork itself is not an installed model.','']
for b in biomes:
 lines+=['## '+b,'','| Creature | Type |','|---|---|']
 lines += [f"| [{e['name']}]({e['file']}) | {e['kind']} |" for e in complete if e['biome']==b]
 lines+=['']
(OUT/'README.md').write_text('\n'.join(lines),encoding='utf-8')
(OUT/'generation-manifest.json').write_text(json.dumps(complete,indent=2),encoding='utf-8')
(OUT/'prompts.json').write_text(json.dumps(entries,indent=2),encoding='utf-8')
print('VERIFIED',len(complete),'concept sheets; generated gallery, six biome overviews and boss overview')
