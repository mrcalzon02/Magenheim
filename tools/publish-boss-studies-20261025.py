"""Publish intermediate saved-source studies with explicit unresolved art work."""
from pathlib import Path
import json,hashlib,html,re
ROOT=Path(__file__).resolve().parents[1];CAMPAIGN=ROOT/'docs/art/creature-models/2026-10-25'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
rows=[
 ('carrion-crown-sepals-r4','The Carrion Crown','the-carrion-crown.png',None,None,'This builds the previously committed fifth sepal ring. It remains close to r3: regular cups, broad smooth timber and insufficient seed/bark anatomy. Preserved as an intermediate, superseded for review by r5.'),
 ('carrion-crown-gnarled-r5','The Carrion Crown','the-carrion-crown.png','r3','Greydwarf_Shaman','The five mouths still read as regular polygonal cups. The seed needs convincing porous amber volume and drips, the pods need fibrous husks, and the trunk needs better bark, crooked support and lichen. Smooth broad root and arm sections remain too simple. This is not at the accepted First Bloom standard.'),
 ('rift-titan-layered-mantle-r19','The Rift Titan','the-rift-titan.png','r18','StoneGolem','The unchanged inner mantle remains too bulky and faceted. The upper cage, core and rear architecture still lack the concept-specific layered structure. Body stone remains coarse at full view and needs authored chips and deliberate lichen. The head/neck geometry is preserved; no new whole-boss acceptance is inferred.')]
style='<style>body{background:#181d20;color:#e4e1d8;font:16px/1.5 system-ui;max-width:1550px;margin:auto;padding:28px}a{color:#bdd6ef}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}img,svg{width:100%;height:480px;object-fit:contain}figure{margin:0;background:#242b2e;padding:10px}.note{background:#29302e;padding:16px;border-left:3px solid #c2a273}</style>'
def fig(label,p):return f'<figure><figcaption>{html.escape(label)}</figcaption><a href="{p}"><img src="{p}"></a></figure>'
for slug,name,sheet,previous,donor,remaining in rows:
    out=CAMPAIGN/slug;ID='underworld-boss-'+slug;m=json.loads((out/'refinement-metrics.json').read_text());source=ROOT/m['source'];source_hash=sha(source);v=json.loads((out/'verification.json').read_text());p=json.loads((out/'portable-export-verification.json').read_text())
    assert v['result'].startswith('PASS') and p['result']=='PASS' and v['source_sha256']==p['source_sha256']==source_hash and p['glb_sha256']==sha(out/(ID+'.glb'))
    assert m['triangles']<=m['soft_cap'] and m['renderers']==m['materials']==1
    proof_name='body-verification.json' if 'rift' in slug else 'anatomy-verification.json';proof=json.loads((out/proof_name).read_text());assert proof['result']=='PASS'
    sheetpath='../../../creature-concepts/2026-10-03/'+sheet;layout=json.loads((out/'camera-layout.json').read_text())
    for path in (out/'before').glob('*-provenance.json'):
        record=json.loads(path.read_text());assert record['source_unchanged'] and record['source_sha256']==sha(ROOT/record['source']) and record['current_source_sha256']==source_hash
        assert all(layout[key]==cfg for key,cfg in record['camera_configuration'].items())
    page=['<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">',f'<title>{name} — {slug}</title>',style,f'<h1>{name} — {slug}</h1>','<p class="note"><b>Intermediate editable study. Not installed, not accepted.</b> The new study does not change the 1/6 adequate-boss tally.</p>',f'<p><a href="{ID}.glb">Portable skinned model</a> · <a href="../../../../../{m["source"]}">Blender source</a> · <a href="REPAIR.md">Review findings</a> · <a href="{proof_name}">Geometry/pose support proof</a> · <a href="../../../entity-review/2026-10-25/gallery.html">48-entity ledger</a></p>',f'<p>{m["triangles"]:,} triangles / measured native donor {m["donor_triangles"]:,} / soft cap {m["soft_cap"]:,}; one mesh/material, four packed 512px maps.</p>',f'<p><a href="{sheetpath}">Original committed concept, unaltered</a>. Previous model comparisons use the exact current cameras and lighting.</p>']
    for view,box in [('front','780 0 415 475' if 'carrion' in slug else '875 15 330 445'),('side','1190 0 340 475' if 'carrion' in slug else '1220 10 300 450')]:
        page.extend([f'<h2>{view}</h2><div class="grid">',f'<figure><figcaption>Selected concept</figcaption><svg viewBox="{box}"><image href="{sheetpath}" width="1536" height="1024"/></svg></figure>'])
        if previous:page.append(fig(previous+' — matched saved model',f'before/{previous}--{view}.png'))
        page.extend([fig(slug+' — saved model',f'{ID}--{view}.png'),'</div>'])
    page.extend(['<h2>Geometry and retained action sample</h2><div class="grid">',fig('Front clay',ID+'--front-clay.png'),fig('Side clay',ID+'--side-clay.png'),fig('Retained attack sample',ID+'--attack.png'),'</div>'])
    if donor:page.extend(['<h2>Actual native donor — normalized framing</h2><div class="grid">',fig(donor+' front',f'before/native-{donor}--front.png'),fig(donor+' side',f'before/native-{donor}--side.png'),fig(donor+' clay',f'before/native-{donor}--front-clay.png'),'</div>'])
    changes=m.get('changes','Fifth overlapping sepal ring with broken radial spacing and lip depth.');page.extend(['<h2>Changed</h2><p>'+changes+'</p>','<h2>Unresolved visual work</h2><p class="note">'+remaining+'</p>','<p>Offline manifold topology, skin weights, atlas, export and sampled supports pass. These checks do not establish appearance, animation quality, runtime admission or live performance.</p></html>'])
    content='\n'.join(page)
    for link in re.findall(r'(?:src|href)="([^"]+)"',content):
        if link!='REPAIR.md':assert (out/link).resolve().is_file(),link
    (out/'gallery.html').write_text(content,encoding='utf-8');(out/'REPAIR.md').write_text('# '+name+' — '+slug+'\n\nIntermediate, unaccepted and not installed.\n\n'+changes+'\n\n## Still required\n\n'+remaining+'\n\n[Matched comparison](gallery.html). Offline checks pass; art and live runtime acceptance remain separate. Boss tally stays **1/6**.\n',encoding='utf-8')
    files=[source,out/(ID+'.glb'),*(out.glob('*.png')),*(out/'before').glob('*.png')]
    (out/'artifact-validation.json').write_text(json.dumps(dict(result='PASS',source_sha256=source_hash,all_gallery_links_exist=True,matching_historical_cameras_verified=True,art_accepted=False,installed=False,remaining=remaining,hashes={f.relative_to(ROOT).as_posix():sha(f) for f in files}),indent=2)+'\n')
print('VERIFIED three intermediate boss-study galleries; source hashes, compact exports, support proof, historical cameras and links')
