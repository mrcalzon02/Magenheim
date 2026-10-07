"""Four-way concept/native/rejected-pass/revision review and artifact evidence."""
from pathlib import Path
import json,html,hashlib,struct
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/detail-r2'
rows=json.loads((OUT/'boss-construction-manifest.json').read_text())
donors=json.loads((ROOT/'dist/boss-donor-quality/native-source/metrics.json').read_text())
native={r['donor']:r for r in donors}
mapping={'first-bloom':'Troll','blackwater-maw':'Serpent','furnace-heart':'StoneGolem','white-silence':'StoneGolem','rift-titan':'StoneGolem','carrion-crown':'Greydwarf_Shaman'}
notes={
 'first-bloom':'Rebuilt the plain pelvis/torso into a continuous root trunk; added interlaced load-bearing roots, scarred joints, a recessed facial mask, rolled shelf rims and individual folded gill lamellae.',
 'blackwater-maw':'Replaced the pipe-shaped mouth with a closed, thickness-bearing wedge skull and hinged lower jaw. Added uneven fang rows, cheek armor, flank scales, strengthened fin roots and membrane folds.',
 'furnace-heart':'Replaced beveled blocks with unequal fractured rock profiles; added interlocking shoulder spalls, joint scales, chipped facial armor and narrow molten cracks around the exposed chamber.',
 'white-silence':'Replaced the square face with a cleft ice mask; layered the mantle with irregular ice blades, fine icicles, overlapping joint shards and a ribbed hollow spine.',
 'rift-titan':'Rebuilt repeated blocks as distinct fractured strata; added outer broken planes, asymmetric shoulder masses and split crown menhirs surrounding the exposed binding cage.',
 'carrion-crown':'Rebuilt the uniform flower rim into torn overlapping sepals; added a layered tooth crown, grafted branches, torn bark sheaths, ribbed seed capsules and divided root fingers.',
}
cards=[];report=[]
for row in rows:
 name=row['id'].removeprefix('underworld-boss-').removesuffix('-detail-r2');donor=mapping[name]
 title='The '+name.replace('-',' ').title()
 images=[('Adopted concept',f'../../../creature-concepts/2026-10-03/the-{name}--thumb.jpg','concept'),(f'Native {donor} · bind pose',f'donor-{donor}--three-quarter.png','donor'),('Rejected first pass',f'../{name}--three-quarter.png','before'),('Revised anatomy and materials',f'{row["id"]}--three-quarter.png','revision')]
 figures=''.join(f'<figure><img class="{cls}" src="{src}" alt="{html.escape(title+": "+label)}"><figcaption>{html.escape(label)}</figcaption></figure>' for label,src,cls in images)
 controls=''.join(f'<button data-view="{view}">{label}</button>' for view,label in [('three-quarter','Three-quarter'),('front','Front'),('side','Side'),('detail','Face / core detail'),('clay','Geometry only')])
 cards.append(f'<article data-id="{row["id"]}" data-donor="{donor}" id="{name}"><h2>{title}</h2><div class="four">{figures}</div><div class="controls">{controls}<a href="{row["id"]}--attack.png">Inherited attack pose</a><a href="../../../../../{row["source"]}">Blender source</a><a href="{row["id"]}.glb">Rest-pose GLB</a></div><p>{notes[name]}</p><p class="metrics">Source study: {row["triangles"]:,} triangles / {row["renderers"]} bone/material parts. Native extraction: {native[donor]["triangles"]:,} triangles / {native[donor]["renderers"]} renderers. Counts describe cost; donor visual quality is the acceptance criterion.</p></article>')
 # Verify portable interchange without claiming that file validity establishes art quality.
 data=(OUT/(row['id']+'.glb')).read_bytes();magic,version,length=struct.unpack_from('<4sII',data)
 assert magic==b'glTF' and version==2 and length==len(data),row['id']
 size,kind=struct.unpack_from('<II',data,12);assert kind==0x4e4f534a
 doc=json.loads(data[20:20+size]);assert doc['meshes'] and doc['materials']
 assert all('bufferView' in im and 'uri' not in im for im in doc['images'])
 report.append(dict(id=row['id'],donor=donor,donor_triangles=native[donor]['triangles'],source_triangles=row['triangles'],source_renderers=row['renderers'],changes=notes[name],art_integrity='validated separately in source-verification.json',portable_glb='PASS embedded geometry/materials/images',quality_bar='native donor model detail; file validity and triangle count do not grant acceptance',acceptance='revised candidate for matched visual review; runtime, deformation, cost and user acceptance pending'))
style='''*{box-sizing:border-box}body{margin:0;background:#14191c;color:#e5e6df;font:16px/1.5 system-ui}main{max-width:1800px;margin:auto;padding:26px}header{max-width:1200px}h1{font-size:30px;margin:0}h2{font-size:24px}article{padding:20px;margin:24px 0;border:1px solid #3c4850;background:#1b2328;border-radius:10px}.four{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;align-items:start}figure{margin:0}img{width:100%;display:block;border-radius:6px}figcaption{font-size:14px;color:#c2c9c8;padding-top:6px}.controls{display:flex;gap:8px;flex-wrap:wrap;margin:16px 0}button,.controls a{background:#2b3a43;color:#e3e8e9;border:1px solid #617481;padding:7px 12px;border-radius:6px;font:inherit;cursor:pointer;text-decoration:none}button:hover{background:#4b606c}.metrics{font-size:14px;color:#b3c1c7}.bar{color:#e3be82}a{color:#aed5e4}@media(max-width:1000px){.four{grid-template-columns:1fr 1fr}}@media(max-width:600px){main{padding:8px}.four{grid-template-columns:1fr}}'''
page=f'<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Boss donor-quality revision 2</title><style>{style}</style><main><header><h1>Boss donor-quality revision 2</h1><p class="bar">The actual donor models are the minimum visual quality bar.</p><p>Compare the adopted concepts, extracted native donors, rejected first pass and revised anatomy. Donor and revision renders use the same lighting, exposure, viewing direction and framing rule. Select Geometry only to remove texture and emission from the comparison.</p><p>The native references are local asset bind poses with approximate Principled materials. They show authored geometry and texture detail; native animation, shader effects and live combat poses are not reproduced. Revisions retain species rigs as a baseline and remain uninstalled art candidates. More polygons are not an acceptance result.</p></header>'+''.join(cards)
page+='''</main><script>document.querySelectorAll('button[data-view]').forEach(b=>b.addEventListener('click',()=>{const c=b.closest('article');c.querySelector('.donor').src='donor-'+c.dataset.donor+'--'+b.dataset.view+'.png';c.querySelector('.revision').src=c.dataset.id+'--'+b.dataset.view+'.png';}));</script></html>'''
(OUT/'gallery.html').write_text(page,encoding='utf-8')
(OUT/'donor-quality-review.json').write_text(json.dumps(report,indent=2)+'\n')
scripts=['author-boss-concept-models.py','boss_detail_authoring.py','generate-boss-detail-textures.py','prepare-boss-native-donors.py','render-boss-quality-comparison.py','verify-boss-concept-sources.py','export-boss-concept-studies.py','build-boss-quality-review.py','verify-boss-quality-review.py']
files=[ROOT/'tools'/s for s in scripts]+[ROOT/r['source'] for r in rows]+[ROOT/r['concept'] for r in rows]
files +=list((ROOT/'assets/textures/underworld/bosses/concept-detail-r2').glob('*.png'))
files +=[p for p in OUT.glob('*') if p.suffix in ('.png','.glb')]
for donor in native:
 directory=ROOT/'dist/vanilla-creature-comparison/native'/donor
 files +=list(directory.glob('*'))
(OUT/'provenance.json').write_text(json.dumps({p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in files if p.is_file()},indent=2)+'\n')
print('VERIFIED six revision GLBs; AUTHORED matched donor-quality comparison gallery')
