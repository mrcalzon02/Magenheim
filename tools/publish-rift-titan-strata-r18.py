"""Publish the verified R18 study without rewriting earlier review evidence."""
from pathlib import Path
import hashlib,json,html,re
ROOT=Path(__file__).resolve().parents[1];slug='rift-titan-broken-strata-r18';ID='underworld-boss-'+slug
campaign=ROOT/'docs/art/creature-models/2026-10-16';out=campaign/slug
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
metrics=json.loads((out/'refinement-metrics.json').read_text());proof=json.loads((out/'body-verification.json').read_text());general=json.loads((out/'verification.json').read_text());portable=json.loads((out/'portable-export-verification.json').read_text())
source=ROOT/metrics['source'];source_hash=sha(source)
assert proof['result']=='PASS' and general['result'].startswith('PASS') and portable['result']=='PASS'
assert general['source_sha256']==portable['source_sha256']==source_hash
assert portable['glb_sha256']==sha(out/(ID+'.glb'))
assert proof['actor_triangles']==metrics['triangles']==1800 and metrics['soft_cap']==1800
assert max(proof['head_neck_max_difference_metres'].values())==max(proof['r17_rest_bone_matrix_max_difference'].values())==0
layout=json.loads((out/'camera-layout.json').read_text())
for path in [*(out/'before').glob('*-provenance.json'),out/(ID+'-provenance.json')]:
    row=json.loads(path.read_text());assert row['source_unchanged'] and row['source_sha256']==sha(ROOT/row['source']) and row['current_source_sha256']==source_hash
    assert all(layout[name]==cfg for name,cfg in row['camera_configuration'].items())
style='''<style>body{background:#181d20;color:#e5e2da;font:16px/1.5 system-ui;max-width:1550px;margin:auto;padding:28px}a{color:#bbcef4}h1{font-size:30px}h2{margin-top:38px}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:14px}figure{margin:0;background:#242b2e}figcaption{padding:10px}img,svg{display:block;width:100%;height:470px;object-fit:contain}.note{padding:16px;border-left:3px solid #bc9e67;background:#292c2b}@media(max-width:850px){.grid{grid-template-columns:1fr}}</style>'''
def fig(label,path):return f'<figure><figcaption>{html.escape(label)}</figcaption><a href="{path}"><img src="{path}" alt="{html.escape(label)}"></a></figure>'
sheet='../../../creature-concepts/2026-10-03/the-rift-titan.png'
def concept(label,box):return f'<figure><figcaption>{label} — committed concept</figcaption><svg viewBox="{box}"><image href="{sheet}" width="1536" height="1024"/></svg></figure>'
changes='''Six fingers now have embedded four-point roots, a five-corner knuckle with a forward ridge, and a thick unequal lower section. Each foot comprises two broad flat-topped toe stones and a rear heel fragment, with all sole vertices sharing the existing standing plane. Six mantle slabs retain their chipped fronts while simplified rear sections fund the two inner front ridges and thicker digits. Sixteen unmarked limb plates replace radial face hubs with paired long fracture edges, constrained into their existing outlines, and follow each bone's actual three-dimensional axis. Two high crest stones gain chipped five-corner profiles; the lower rear crest bends around the back through three sections. Secondary core splinters and small crest motes fund the changes at 1,800 triangles. Stone marks now follow each component's local direction, using gray strata, sparse warm/olive mineral, recessed seams and baked edge shading within the existing four 512px maps.'''
remaining='''The large shoulders, upper cage and core still need more reference-specific construction. The crest needs smaller supporting architecture and more broken layering. Hands and feet are thicker and separated, but their forms remain coarse; stone detail still needs sharper authored chips and more deliberate lichen. This is another revision candidate. The user's “getting closer” does not establish full head or boss acceptance.'''
evidence=f'''The saved head, six neck bars, stone collarbone and narrow wrapped torso band have zero coordinate difference from r17. All eight saved rest-bone matrices and parent identities also match exactly; seven retained source actions remain. All 76 attachment contracts pass. {proof['closing_face_samples']:,} actual marked closing-face samples pass over {proof['poses']} rest/action poses. Both feet retain the prior sole plane, both shins clear it, and the unequal fingertips clear it by {min(proof['fingertip_clearance_metres'].values()):.3f}–{max(proof['fingertip_clearance_metres'].values()):.3f} m. The final export has one mesh, one material, four embedded 512px maps and compact normalized skin weights; unused construction paint channels are absent.'''
page=['<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Rift Titan r18 — broken stone forms</title>',style,'<h1>Rift Titan r18 — broken stone forms</h1>',
      '<p class="note"><b>Editable art study, in review.</b> Adequate boss tally remains 1/6. Native model, animation and performance acceptance are separate.</p>',
      f'<p><a href="{ID}.glb">Portable skinned model</a> · <a href="../../../../../{metrics["source"]}">Editable Blender source</a> · <a href="REPAIR.md">Changes and remaining work</a> · <a href="body-verification.json">Saved-source checks</a> · <a href="../gallery.html">Review index</a></p>',
      '<p>1,800 triangles / native Stone Golem 1,200 / soft cap 1,800. Original concepts are unaltered. The previous source is rendered with the exact current camera transforms and lights; native donor framing is normalized.</p>']
for label,view,box in [('Front','front','830 0 370 455'),('True left side','side','1225 12 290 443')]:
    page.extend([f'<h2>{label}</h2><div class="grid">',concept(label,box),fig('r17 — previous saved model',f'before/r17--{view}.png'),fig('r18 — current saved model',f'{ID}--{view}.png'),'</div>'])
for label,view,box in [('Hand and fingers','hand-detail','835 235 115 220'),('Planted feet','foot-detail','930 380 235 75'),('Raised rear crest','crest-detail','1290 12 225 170')]:
    page.extend([f'<h2>{label}</h2><div class="grid">',concept(label,box),fig('r17 — same close-up camera',f'before/r17-details--{view}.png'),fig('r18 — saved close-up',f'{ID}--{view}.png'),'</div>'])
page.extend(['<h2>Geometry and retained pose</h2><div class="grid">',fig('Front — clay',ID+'--front-clay.png'),fig('Left side — clay',ID+'--side-clay.png'),fig('Retained attack sample',ID+'--attack.png'),'</div>',
             '<h2>Preserved head and neck geometry</h2><div class="grid">',fig('Solid front stone plate',ID+'--crown-front.png'),fig('Left recessed pocket',ID+'--crown-side.png'),fig('Opposite recessed pocket',ID+'--crown-right.png'),'</div>',
             '<h2>Native donor quality reference</h2><p>Actual extracted Stone Golem source, 1,200 triangles. Same review lighting and directions with normalized framing.</p><div class="grid">',fig('Stone Golem — front','before/native-StoneGolem--front.png'),fig('Stone Golem — side','before/native-StoneGolem--side.png'),fig('Stone Golem — clay','before/native-StoneGolem--front-clay.png'),'</div>',
             '<h2>Changed</h2><p>'+changes+'</p><h2>Verified saved artifacts</h2><p>'+evidence+'</p><h2>Remaining visible work</h2><p class="note">'+remaining+'</p></html>'])
(out/'REPAIR.md').write_text('# Rift Titan r18 — broken stone forms\n\n'+changes+'\n\n'+evidence+'\n\n## Remaining work\n\n'+remaining+'\n\n[Concept / r17 / r18 comparison](gallery.html). Boss tally remains **1/6**. No runtime files were changed.\n',encoding='utf-8')
content='\n'.join(page)
for link in re.findall(r'(?:src|href)="([^"]+)"',content):assert (out/link).resolve().is_file(),link
(out/'gallery.html').write_text(content,encoding='utf-8')
for path in out.glob('*authoring.json'):
    if path.name=='strata-authoring.json':continue
    row=json.loads(path.read_text());row.update(intermediate_build_stage_only=True,final_saved_geometry_authority='body-verification.json');path.write_text(json.dumps(row,indent=2)+'\n')
files=[source,out/(ID+'.glb'),(out/sheet).resolve(),*out.glob('*.png'),*(out/'before').glob('*.png'),*(ROOT/f'assets/textures/underworld/bosses/{slug}').glob('*.png')]
report=dict(result='PASS',source_sha256=source_hash,all_gallery_links_exist=True,historical_sources_and_cameras_verified=True,art_acceptance=False,native_acceptance=False,adequate_bosses=1,total_bosses=6,visual_inspection='actual original front/side and saved front/side/clay/head/action/hand/foot/crest images inspected',hashes={p.relative_to(ROOT).as_posix():sha(p) for p in files})
(out/'artifact-validation.json').write_text(json.dumps(report,indent=2)+'\n')
index=campaign/'gallery.html';text=index.read_text(encoding='utf-8');text=re.sub(r'<tr><td><a href="rift-titan-broken-strata-r18/gallery.html".*?</tr>','',text)
text=text.replace('<table><tr><th>Review</th><th>Triangles</th></tr>','<table><tr><th>Review</th><th>Triangles</th></tr>\n<tr><td><a href="rift-titan-broken-strata-r18/gallery.html">Rift Titan r18 — broken stone forms</a></td><td>1,800</td></tr>');index.write_text(re.sub(r'\n{2,}','\n',text),encoding='utf-8')
instructions=ROOT/'INSTRUCTIONS.md';lines=instructions.read_text(encoding='utf-8').splitlines()
for i,line in enumerate(lines):
    if line.startswith('| The Rift Titan |'):
        lines[i]=f'| The Rift Titan | **No — r18 continued refinement, unaccepted** | [r18 comparison](docs/art/creature-models/2026-10-16/{slug}/gallery.html): thick bent stone digits, three low fractured masses per foot, longer mantle/limb fracture planes, bent chipped rear crest and locally directed gray stone treatment. Exact saved r17 head, six neck bars, collarbone, wrapped torso ring and eight rest-bone matrices preserved; seven source actions retained. 1,800 triangles / Stone Golem 1,200 / cap 1,800; one mesh/material and four packed 512px maps. All 76 supports and {proof["closing_face_samples"]:,} closing-face samples across {proof["poses"]} poses pass. | Continue reference-specific shoulder layering, upper cage/core, smaller crest supports, coarse hands/feet and authored chips/lichen. User said “getting closer” and requested further revision; this is not boss acceptance. Preserve the twenty approved front head landmarks and solid plate with paired flank pockets. Original 44 audit entries remain open for art review; tally stays 1/6. |'
instructions.write_text('\n'.join(lines)+'\n',encoding='utf-8')
status=json.loads((campaign/'rift-titan-concept-body-r17/repair-status.json').read_text());status.update(revision='r18',art_acceptance=False,adequate_bosses=1)
for f in status['findings']:
    f['status']='open for art review'
    if f['id'] in ('C01','C02','C03','A01','D01','D02','D03','L01','F01','F02','M01'):f['current_candidate']='r18 broken crest, long constrained limb fracture planes, thick bent digits, three-mass feet and directed stone atlas; saved proof separate from art acceptance'
(out/'repair-status.json').write_text(json.dumps(status,indent=2)+'\n')
for path in [ROOT/'docs/art/creature-models/REFINEMENT-STANDARDS.md',ROOT/'docs/validation/2026-10-16-boss-model-refinement.md']:
    text=path.read_text(encoding='utf-8');marker='## Rift Titan r18 continuation'
    if marker in text:text=text[:text.index(marker)].rstrip()+'\n'
    text+='\n'+marker+'\n\n'+changes+'\n\n'+evidence+'\n\n'+remaining+' No native or new adequate-boss acceptance is inferred; tally stays 1/6.\n';path.write_text(text,encoding='utf-8')
print('VERIFIED r18 gallery, exact source/export provenance, comparisons and unchanged art tally')
