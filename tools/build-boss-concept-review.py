"""Build the local comparison gallery and reproducible art provenance."""
from pathlib import Path
import json, html, hashlib, re
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04'
rows=json.loads((OUT/'boss-construction-manifest.json').read_text())
titles={'first-bloom':'The First Bloom','blackwater-maw':'The Blackwater Maw','furnace-heart':'The Furnace Heart','white-silence':'The White Silence','rift-titan':'The Rift Titan','carrion-crown':'The Carrion Crown'}
notes={
 'first-bloom':'Asymmetric shelf crown, exposed ribbed gills, living arm cords, root feet and separate talons. Next: reduce cylindrical joins and add finer underside folds.',
 'blackwater-maw':'Thick open jaw vault, modeled trap teeth, inner throat, complete fin webs, sensory organs and a tapered body. Next: wedge-shaped skull and smoother jaw-to-throat joins.',
 'furnace-heart':'Wide broken masonry boiler cage, exposed molten chamber, heavy impact fists and unequal chimney stacks. Next: fracture larger slabs and vary erosion.',
 'white-silence':'Hollow narrow spine, hanging shard mantle, long ice claws and a cleft vertical crown. Next: replace the provisional face veil and inspect shoulder deformation.',
 'rift-titan':'Suspended segmented slabs, sparse binding cage and split crown menhirs. Next: introduce larger irregular masses, more pronounced voids and asymmetric shoulder strata.',
 'carrion-crown':'One massive rooted trunk, radial bone crown, exposed amber organ, flared roots and hanging seed pods. Next: make the crown less radial and articulate the branching tendrils.',
}
cards=[]
for row in rows:
 name=row['id'].removeprefix('underworld-boss-')
 buttons=''.join(f'<button data-view="{v}">{label}</button>' for v,label in [('three-quarter','Three-quarter'),('front','Front'),('side','Side'),('attack','Inherited pose')])
 cards.append(f'''<article id="{name}" data-name="{name}"><h2>{titles[name]}</h2><div class="pair"><figure><a href="../../creature-concepts/2026-10-03/the-{name}.png"><img src="../../creature-concepts/2026-10-03/the-{name}--thumb.jpg" alt="Adopted {titles[name]} concept"></a><figcaption>Adopted concept · 3 October</figcaption></figure><figure><img class="model" src="{name}--three-quarter.png" alt="{titles[name]} construction study"><figcaption>Construction pass 1 · {row['target_metres']:g} m · {row['triangles']:,} triangles</figcaption></figure></div><div class="controls">{buttons}<a href="../../../../{row['source']}">Blender source</a><a href="{row['id']}.glb">Rest-pose GLB</a></div><p>{html.escape(notes[name])}</p></article>''')
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Boss construction review · 4 October 2026</title><style>*{box-sizing:border-box}body{margin:0;background:#14191c;color:#e5e6df;font:16px/1.5 system-ui}main{max-width:1300px;margin:auto;padding:30px}h1{font-size:30px;font-weight:600;margin:0}h2{font-size:23px;font-weight:600}header{margin-bottom:35px}.status{color:#e2bb7c}article{padding:22px;background:#1b2328;border:1px solid #394349;border-radius:12px;margin:25px 0}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px;align-items:center}figure{margin:0}img{width:100%;display:block;border-radius:6px}figcaption{color:#bdc7ca;margin-top:8px;font-size:14px}.controls{display:flex;flex-wrap:wrap;gap:9px;margin:16px 0}button,.controls a{font:inherit;color:#e1e8e9;background:#2a363d;border:1px solid #52646e;border-radius:6px;padding:7px 13px;text-decoration:none;cursor:pointer}button:hover,.controls a:hover{background:#435764}a{color:#b9dbea}p{max-width:1000px}nav{display:flex;gap:15px;flex-wrap:wrap;margin-top:18px}@media(max-width:700px){.pair{grid-template-columns:1fr}main{padding:12px}}</style><main><header><h1>Six bosses — first construction pass</h1><p class="status">Authored shape and material studies. Not installed or accepted as finished game creatures.</p><p>Compare each boss with its committed concept. The sources keep the existing species rig and action names as a retargeting baseline. The GLBs show the rest pose; inherited pose images are offline inspection aids.</p><nav>'''
page+=''.join(f'<a href="#{name}">{title}</a>' for name,title in titles.items())
page+='</nav></header>'+''.join(cards)
page+='''<footer><p>Production continues through boss anatomy and deformation refinement, then large apex/elite creatures, then medium and small creatures. Collider, hitbox, LOD, native-material, locomotion, attack and multiplayer acceptance remain pending.</p></footer></main><script>document.querySelectorAll('button[data-view]').forEach(button=>button.addEventListener('click',()=>{const card=button.closest('article');card.querySelector('.model').src=card.dataset.name+'--'+button.dataset.view+'.png';}));</script></html>'''
(OUT/'gallery.html').write_text(page,encoding='utf-8')
scripts=['generate-boss-concept-textures.py','author-boss-concept-models.py','render-boss-concept-review.py','export-boss-concept-studies.py','verify-boss-concept-sources.py','verify-boss-concept-review.py','build-boss-concept-review.py','creature_apex_authoring.py']
def hashfile(p):return hashlib.sha256(p.read_bytes()).hexdigest()
sources={str(Path('tools')/s).replace('\\','/'):hashfile(ROOT/'tools'/s) for s in scripts}
inputs={row['source']:hashfile(ROOT/row['source']) for row in rows}
inputs.update({'assets/models/source/underworld-creature-'+row['rig_source']+'.blend':hashfile(ROOT/'assets/models/source'/('underworld-creature-'+row['rig_source']+'.blend')) for row in rows})
tex=ROOT/'assets/textures/underworld/bosses/concept-pass-1'
outputs={p.relative_to(ROOT).as_posix():hashfile(p) for p in sorted(tex.glob('*.png'))}
outputs.update({p.relative_to(ROOT).as_posix():hashfile(p) for p in OUT.glob('*') if p.suffix in ('.png','.glb')})
(OUT/'provenance.json').write_text(json.dumps(dict(generators=sources,source_models=inputs,outputs=outputs),indent=2)+'\n')
concepts=json.loads((ROOT/'docs/art/creature-concepts/2026-10-03/generation-manifest.json').read_text())
ordinary=[r for r in concepts if r['kind']=='creature']
ordinary.sort(key=lambda r:max(map(float,re.findall(r'\d+(?:\.\d+)?',r.get('size','0')))),reverse=True)
queue=[dict(name=titles[row['id'].removeprefix('underworld-boss-')],kind='boss',status='construction study authored; refinement and acceptance pending',source=row['source'],concept=row['concept']) for row in rows]
queue += [dict(name=r['name'],kind='creature',size_band=r['size'],biome=r['biome'],status='concept adaptation queued',concept='docs/art/creature-concepts/2026-10-03/'+r['file']) for r in ordinary]
(OUT/'production-order.json').write_text(json.dumps(queue,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('AUTHORED six-boss gallery and provenance')
