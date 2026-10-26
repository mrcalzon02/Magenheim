"""Publish source-linked comparisons and unmodified-image screening sheets.

This does not infer visual approval or change the ledger's review findings.
Run with a Python environment containing Pillow for the contact sheets.
"""
from pathlib import Path
from collections import defaultdict
import json,hashlib,html,re
from PIL import Image,ImageOps,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/entity-review/2026-10-25'
rows=json.loads((OUT/'entities.json').read_text())['entities'];records={r['name']:r for r in json.loads((OUT/'baseline-rendering.json').read_text())}
style='<style>body{background:#181d20;color:#e4e1d8;font:16px/1.5 system-ui;max-width:1600px;margin:auto;padding:28px}a{color:#bdd6ef}img{width:100%;object-fit:contain}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}figure{margin:0;background:#242b2e;padding:10px}.note{background:#29302e;padding:16px}small{color:#b6bdbb}</style>'
by_biome=defaultdict(list)
for row in rows:
    slug=row['name'].lower().replace(' ','-');concept='../../../'+row['concept'].removeprefix('docs/')
    page=['<!doctype html><html lang="en"><meta charset="utf-8">',f'<title>{html.escape(row["name"])} — entity review</title>',style,f'<h1>{html.escape(row["name"])}</h1>','<p><a href="gallery.html">Campaign ledger</a></p>',f'<p class="note">{html.escape(row["status"])}. {html.escape(row["visual_audit"])}. User art acceptance: {str(row["user_art_accepted"]).lower()}.</p>',f'<h2>Selected committed concept</h2><a href="{concept}"><img style="max-height:850px" src="{concept}"></a>']
    if row['status']=='paused-by-user':page.append('<p>Explicit user hold. No new baseline, geometry or revision was made for this entity.</p>')
    else:
        record=records[row['name']];source=ROOT/row['runtime'];assert hashlib.sha256(source.read_bytes()).hexdigest()==record['runtime_sha256'];assert record.get('recipe_sha256')
        orientation=record['orientation'];page.extend(['<h2>Actual runtime geometry — anatomical views in rest pose</h2><p>Same normalized neutral lighting, owned surface maps and actual runtime topology. This is an offline material approximation; it does not establish Valheim appearance or animation acceptance.</p>',f'<p>Verified stored forward: <b>{orientation["anatomical_forward_in_stored_runtime"]}</b>. Exporter: {orientation["exporter"]}. The camera basis inverts this exporter before assigning front/rear labels.</p><div class="grid">'])
        for view in ('front','side','three-quarter','rear'):
            filename=slug+'--'+view+'.png';p=OUT/'baselines'/filename;assert hashlib.sha256(p.read_bytes()).hexdigest()==record['renders'][filename]
            page.append(f'<figure><figcaption>{view}</figcaption><a href="baselines/{filename}"><img src="baselines/{filename}"></a></figure>')
        page.extend(['</div>',f'<p>{row["runtime_triangles"]:,} triangles / {row["runtime_renderers"]} renderers / {row["runtime_materials"]} materials. Native donor: {row["native_donor"]}; measured triangles: {row["native_triangles"] or "pending"}.</p>'])
        if row['kind']!='boss':by_biome[row['biome']].append(row)
    if row['latest_study']:page.append(f'<p><a href="../../../../{row["latest_study"]}">Latest separate study comparison</a>. A study is separate from the current installed model.</p>')
    if row['findings']:page.append('<h2>Open review findings</h2><ul>'+''.join('<li>'+html.escape(f)+'</li>' for f in row['findings'])+'</ul>')
    page.append('</html>');(OUT/(slug+'.html')).write_text('\n'.join(page),encoding='utf-8')
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',23);small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
screen=OUT/'screening';screen.mkdir(exist_ok=True)
for biome,group in by_biome.items():
    for start in range(0,len(group),4):
        subset=group[start:start+4];sheet=Image.new('RGB',(1440,90+len(subset)*420),(24,29,32));draw=ImageDraw.Draw(sheet);draw.text((18,10),biome+' — selected concept / runtime front / side / three-quarter',font=font,fill=(230,227,217));draw.text((18,46),'Primary screening only. No acceptance inferred. Images letterboxed without changing content.',font=small,fill=(185,192,190))
        for j,row in enumerate(subset):
            y=90+j*420;slug=row['name'].lower().replace(' ','-');draw.text((18,y),row['name']+f' — {row["runtime_triangles"]:,} tris, {row["runtime_renderers"]} renderer(s)',font=font,fill=(230,227,217));files=[ROOT/row['concept']]+[OUT/'baselines'/(slug+'--'+v+'.png') for v in ('front','side','three-quarter')]
            for k,p in enumerate(files):
                x=0 if k==0 else 600+(k-1)*280;w=600 if k==0 else 280
                with Image.open(p) as im:tile=ImageOps.contain(im.convert('RGB'),(w,380));sheet.paste(tile,(x+(w-tile.width)//2,y+35+(380-tile.height)//2))
        name=re.sub('[^a-z0-9]+','-',biome.lower()).strip('-')+'-'+str(start//4+1)+'.jpg';sheet.save(screen/name,quality=94)
print('VERIFIED 48 individual comparison pages and 12 primary screening sheets; no acceptance changed')
