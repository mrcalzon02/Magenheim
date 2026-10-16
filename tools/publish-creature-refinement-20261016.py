"""Publish measured review artifacts without inferring art acceptance."""
from pathlib import Path
import hashlib,json,html,re,shutil,os
ROOT=Path(__file__).resolve().parents[1]
CAMPAIGN=ROOT/'docs/art/creature-models/2026-10-16'
STUDIES=[
 ('rift-titan-paired-recesses-r15','2026-10-14','Rift Titan r15 — restored front plate','paired-recess-verification'),
 ('rift-titan-fracture-planes-r16','2026-10-16','Rift Titan r16 — stepped head planes','paired-recess-verification'),
 ('rift-titan-concept-body-r17','2026-10-16','Rift Titan r17 — whole-creature correction','body-verification'),
 ('carrion-crown-anatomy-r3','2026-10-16','Carrion Crown r3 — anatomy rebuild','anatomy-verification')]
STYLE='''<style>body{background:#181d20;color:#e5e2da;font:16px/1.5 system-ui;max-width:1550px;margin:auto;padding:28px}a{color:#bbcef4}h1{font-size:30px}h2{margin-top:38px}p{max-width:1100px}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:14px}figure{margin:0;background:#242b2e}figcaption{padding:10px}img,svg{display:block;width:100%;height:470px;object-fit:contain}.note{padding:16px;border-left:3px solid #bc9e67;background:#292c2b}table{border-collapse:collapse}td,th{padding:9px 18px;border-bottom:1px solid #596166;text-align:left}@media(max-width:850px){.grid{grid-template-columns:1fr}}</style>'''
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def figure(label,path):return f'<figure><figcaption>{html.escape(label)}</figcaption><a href="{path}"><img src="{path}" alt="{html.escape(label)}"></a></figure>'
def concept(label,sheet,box):return f'<figure><figcaption>{label} — original concept crop</figcaption><svg viewBox="{box}" role="img" aria-label="{label}"><image href="{sheet}" width="1536" height="1024"/></svg></figure>'
summaries=[]
for slug,date,title,proof_name in STUDIES:
    out=ROOT/f'docs/art/creature-models/{date}/{slug}';ID='underworld-boss-'+slug
    metrics=json.loads((out/'refinement-metrics.json').read_text());proof=json.loads((out/(proof_name+'.json')).read_text());general=json.loads((out/'verification.json').read_text())
    assert proof['result'].startswith('PASS') and general['result'].startswith('PASS')
    assert general['source_sha256']==sha(ROOT/metrics['source']),('stale saved-source proof',slug)
    assert metrics['triangles']<=metrics['soft_cap']
    portable=out/'portable-export-verification.json'
    if portable.exists():
        row=json.loads(portable.read_text());assert row['result']=='PASS' and row['source_sha256']==sha(ROOT/metrics['source']) and row['glb_sha256']==sha(out/(ID+'.glb'))
    for p in (out/'before').glob('*-provenance.json'):
        row=json.loads(p.read_text());assert row['source_unchanged'] and row['source_sha256']==sha(ROOT/row['source'])
        if 'current_source_sha256' in row:
            assert row['current_source_sha256']==sha(ROOT/metrics['source'])
            layout=json.loads((out/'camera-layout.json').read_text())
            assert all(cfg==layout[view] for view,cfg in row['camera_configuration'].items()),'stale matched-camera render'
    sheet='../../../creature-concepts/2026-10-03/'+('the-carrion-crown.png' if slug.startswith('carrion') else 'the-rift-titan.png')
    source_rel='../../../../../'+metrics['source']
    page=['<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">',f'<title>{title}</title>',STYLE,f'<h1>{title}</h1>',
          '<p class="note"><b>Editable art study; not accepted or installed.</b> The adequate-boss tally remains 1/6. Offline geometry checks and these renders do not establish native animation, collision or frame cost.</p>',
          f'<p><a href="{ID}.glb">Portable skinned model</a> · <a href="{source_rel}">Editable Blender source</a> · <a href="REPAIR.md">Changes, measurements and remaining gaps</a> · <a href="{proof_name}.json">Saved-source verification</a></p>',
          f'<p>{metrics["triangles"]:,} triangles / native donor {metrics["donor_triangles"]:,} / soft cap {metrics["soft_cap"]:,}; one mesh/material and four packed 512px maps. Historical models are re-rendered from saved sources with exact current cameras; native donors use the same directions/light recipe with normalized framing. Painted views have their own perspective.</p>']
    if 'paired-recesses' in slug or 'fracture-planes' in slug:
        crop=out/'reference-side-crop.png'
        if not crop.exists():
            reference=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-paired-recesses-r15/reference-side-crop.png'
            if not reference.exists():reference=Path('C:/Users/Admin/AppData/Local/Temp/codex-clipboard-9c9f838a-fa66-4f68-bde4-42a20924a01a.png')
            shutil.copyfile(reference,crop)
        before='r14' if 'paired-recesses' in slug else 'r15'
        for label,suffix,box in [('Front','crown-front','973 17 78 137'),('Left-facing concept angle','crown-side','1225 18 115 139')]:
            page += [f'<h2>{label}</h2><div class="grid">',concept(label,sheet,box),figure(before+' — saved previous source',f'before/{before}--{suffix}.png'),figure('Current saved candidate',ID+'--'+suffix+'.png'),'</div>']
        page += ['<h2>Supplied side crop and true profile</h2><div class="grid">',figure('Unaltered supplied side crop','reference-side-crop.png'),figure('True left profile',ID+'--crown-profile.png'),figure('True left profile — clay',ID+'--crown-profile-clay.png'),'</div>',
                 '<h2>Opposite recess and depth</h2><div class="grid">',figure('Right side — independently modeled pocket',ID+'--crown-right.png'),figure('Head from above',ID+'--crown-top.png'),figure('Three-quarter',ID+'--crown-detail.png'),'</div>']
        if before=='r14':
            text='''The final r15 height mask has now been built and reviewed. It restores a solid frontal stone plate and two separate seven-corner side pockets, replacing r14's median violet fracture. All twenty front landmarks are retained. The result is superseded by r16: its broad smooth sockets and cloudy stone were visually rejected during this continuation. This preserved stage is evidence of what was actually built, not the final head candidate.'''
        else:
            text='''The head keeps all twenty accepted frontal X/Z landmarks, while replacing the side depth profile with a long backward rake. Twelve interior vertices form three chains of stepped stone planes; no violet strip cuts the front plate. Each flank has an independent five-corner recessed pocket, narrower than r15, with localized lower violet. A dedicated upper atlas strip increases head texel allocation within the four existing 512px maps. Complete neck head-end caps were reseated; the other body geometry and non-end neck geometry remain unchanged.'''
        remaining='The head still needs more individually chipped layering and more reference-specific stone treatment. The whole boss is not accepted. See r17 for the subsequent body work; this head stage keeps the earlier body intact.'
    elif slug.startswith('rift'):
        for label,suffix,box in [('Front','front','830 0 370 455'),('True left side','side','1225 12 290 443')]:
            page += [f'<h2>{label}</h2><div class="grid">',concept(label,sheet,box),figure('r16 — saved previous body',f'before/r16--{suffix}.png'),figure('r17 — current candidate',ID+'--'+suffix+'.png'),'</div>']
        page += ['<h2>Clay and standing plane</h2><div class="grid">',figure('Front — clay',ID+'--front-clay.png'),figure('Left side — clay',ID+'--side-clay.png'),figure('Three-quarter',ID+'--three-quarter.png'),'</div>',
                 '<h2>Native donor quality reference</h2><p>Actual extracted Stone Golem, normalized framing; 1,200 triangles. This is source evidence, not a live game render.</p><div class="grid">',figure('Native Stone Golem — front','before/native-StoneGolem--front.png'),figure('Native Stone Golem — side','before/native-StoneGolem--side.png'),figure('Native Stone Golem — clay','before/native-StoneGolem--front-clay.png'),'</div>',
                 '<h2>Preserved head and retained action sample</h2><div class="grid">',figure('Solid front plate',ID+'--crown-front.png'),figure('Left recess',ID+'--crown-side.png'),figure('Retained attack sample',ID+'--attack.png'),'</div>']
        text=f'''The actual r16 head and all six neck bars have zero coordinate difference in the saved r17 source. The body adds three raised rear crest beams, varied suspended fragments and violet motes; rounds/cants the twelve continuous ribs around six interlocking core crystals; lowers wrists with their bone tails; extends unequal ridged digits; narrows hanging pelvic fragments; and gives the feet modeled clefts and a shared standing plane. Rear face hubs and compact lower-priority forms fund the crest/core/feet at 1,798 triangles. Coherent fracture-plane stone pigment replaces the prior cloud field, with local occlusion and small edge bevel normals baked into the same atlas. Fingertips now clear the sole plane by {min(proof['fingertip_clearance_metres'].values()):.3f}–{max(proof['fingertip_clearance_metres'].values()):.3f} m, versus approximately 1.20–1.24 m in the r12 audit. Both lower shins lie above the feet. {proof['closing_face_samples']:,} actual closing-face samples pass across {proof['poses']} retained rest/action poses.'''
        remaining='This is a broader correction candidate, not a finished concept match. The crest remains too simple; the shoulders and limbs need more specific long overlapping fractures; the upper cage/core still needs finer organic construction; hands, knuckles and feet remain coarse; and the stone needs authored chips, lichen and fracture detail. The original 44 audit entries remain tracked for art review; these measurements do not close them by visual acceptance.'
    else:
        for label,suffix,box in [('Front','front','780 0 385 475'),('Left-facing side','side','1190 0 345 480')]:
            page += [f'<h2>{label}</h2><div class="grid">',concept(label,sheet,box),figure('r2 — saved previous source',f'before/r2--{suffix}.png'),figure('r3 — current anatomy candidate',ID+'--'+suffix+'.png'),'</div>']
        page += ['<h2>Construction and retained pose</h2><div class="grid">',figure('Front — clay',ID+'--front-clay.png'),figure('Three-quarter',ID+'--three-quarter.png'),figure('Retained cast sample',ID+'--attack.png'),'</div>',
                 '<h2>Native donor quality reference</h2><p>Actual extracted Greydwarf Shaman; 1,422 triangles. Original seven-bone Corpse Orchard study hierarchy and five clips are retained separately from native acceptance.</p><div class="grid">',figure('Native donor — front','before/native-Greydwarf_Shaman--front.png'),figure('Native donor — side','before/native-Greydwarf_Shaman--side.png'),figure('Native donor — clay','before/native-Greydwarf_Shaman--front-clay.png'),'</div>']
        text=f'''New construction replaces r2's solid barrel and flat flower mask with a closed thin-walled timber shell containing a real irregular front opening, eight buttress roots, a deep central crown cup, four grafted secondary cups, uneven rim teeth, rear crown timber, four grafted branch arms, six individually hung amber pods, diagonal abdominal ribs and hanging lichen. Broken broad lips are part of the cup wall; the rejected paddle-like added petals are absent. Owned bark/ivory/cavity/amber surfaces and pore normals are packed into four 512px maps. The saved source has {proof['contacts']} named supports, {proof['closing_face_samples']:,} closing-face samples and {proof['poses']} rest/action poses checked. Trunk and attachment ownership were corrected after the initial cast-pose separations; no tolerance was relaxed.'''
        remaining='This starts Carrion Crown refinement; it is not adequate art acceptance. The cups still need more reference-specific overlapping torn sepal shapes and the body needs denser crooked timber, attached tissue, less regular rib/branch rhythms, clearer porous seed forms and more deliberate bark/lichen. The source remains a compact anatomy study, with native deformation and performance unverified.'
    page+=['<h2>Remaining visible work</h2><p class="note">'+remaining+'</p></html>']
    content='\n'.join(page);(out/'gallery.html').write_text(content,encoding='utf-8')
    md=f'# {title}\n\n{text}\n\n[Actual comparison gallery](gallery.html) · [Saved-source verification]({proof_name}.json)\n\n## Remaining work\n\n{remaining}\n\nAdequately reworked bosses remain **1/6**. No native loader, installed model, collision, every-frame deformation or live performance acceptance is claimed. The First Bloom and paused Blackwater Maw were not edited.\n'
    (out/'REPAIR.md').write_text(md,encoding='utf-8')
    for link in re.findall(r'(?:src|href)="([^"]+)"',content):assert (out/link).resolve().is_file(),(slug,link)
    # Earlier authoring-stage snapshots are build provenance, never final proof.
    for p in out.glob('*authoring.json'):
        if p.name in ('fracture-plane-authoring.json','body-authoring.json','anatomy-authoring.json') or ('paired-recesses' in slug and p.name=='paired-recess-authoring.json'):continue
        row=json.loads(p.read_text());row['intermediate_build_stage_only']=True;row['final_saved_geometry_authority']=proof_name+'.json';p.write_text(json.dumps(row,indent=2)+'\n')
    files=[ROOT/metrics['source'],out/(ID+'.glb'),(out/sheet).resolve()]+list(out.glob('*.png'))+list((out/'before').glob('*.png'))+list((ROOT/f'assets/textures/underworld/bosses/{slug}').glob('*.png'))
    hashes={p.relative_to(ROOT).as_posix():sha(p) for p in files}
    report=dict(result='PASS',date='2026-10-16',source_sha256=sha(ROOT/metrics['source']),saved_source_checks_current=True,all_gallery_links_exist=True,historical_sources_hash_verified=True,visual_inspection='actual source concept/crop and rendered front, left side, opposite head recess, clay and action images directly inspected',art_acceptance=False,native_acceptance=False,adequate_bosses=1,total_bosses=6,hashes=hashes)
    (out/'artifact-validation.json').write_text(json.dumps(report,indent=2)+'\n')
    summaries.append((title,os.path.relpath(out,CAMPAIGN).replace('\\','/')+'/gallery.html',metrics['triangles']))
index=['<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Boss model review — October 16</title>',STYLE,'<h1>Boss model review — October 16</h1><p>Actual editable models, saved-source comparisons and original adopted concepts. Adequate tally: <b>1/6</b>. Rift Titan remains in review; Carrion Crown has started. First Bloom is preserved and Blackwater Maw remains paused.</p><table><tr><th>Review</th><th>Triangles</th></tr>']
for title,link,count in summaries:index.append(f'<tr><td><a href="{link}">{title}</a></td><td>{count:,}</td></tr>')
index.append('</table><p>Each gallery records visible remaining gaps and separates offline checks from art/native acceptance.</p></html>')
(CAMPAIGN/'gallery.html').write_text('\n'.join(index),encoding='utf-8')
# Update the rolling authority only after every current-source proof and link
# above has passed. Starting Carrion Crown never counts Titan as accepted.
instructions=ROOT/'INSTRUCTIONS.md';lines=instructions.read_text(encoding='utf-8').splitlines()
for i,line in enumerate(lines):
    if line.startswith('Updated 2026-10-14 from the user'):lines[i]="Updated 2026-10-16 from the user's concept-model review and continued boss work."
    if line.startswith('| The Rift Titan |'):
        lines[i]="| The Rift Titan | **No — r17 whole-creature candidate, unaccepted** | The latest interpretation supersedes the median fracture: a solid frontal stone plate with a deep crystal-lined pocket on each flank. r15's final material mask is now actually built; r16 narrows the sockets, adds twelve inner stone-plane vertices and preserves all twenty approved frontal X/Z landmarks. r17 (`docs/art/creature-models/2026-10-16/rift-titan-concept-body-r17/gallery.html`) preserves the saved r16 head/neck exactly and revises rear crest, canted cage/core, forearm reach, ridged digits, pelvic fragments and planted cleft feet. 1,798 triangles / Stone Golem 1,200 / cap 1,800; one mesh/material, four 512px maps, original eight bone identities/hierarchy and seven actions. Forearm tails were fitted with geometry. Saved roots and 35,280 cap samples across 36 poses pass. | Inspect the actual concept/before/current views. Crest architecture, fractured layering, upper cage/core, hands/feet and authored stone detail remain visibly incomplete. Preserve r13 frontal-outline acceptance only; no full-head or boss acceptance inferred. Original 44 audit entries remain tracked for art review. Tally stays 1/6; native acceptance is separate. |"
    if line.startswith('| The Carrion Crown |'):
        lines[i]="| The Carrion Crown | **No — r3 anatomy work started** | User explicitly requested continuing to the next boss after the Rift Titan corrections. r3 (`docs/art/creature-models/2026-10-16/carrion-crown-anatomy-r3/gallery.html`) replaces the solid barrel/flat mask with a hollow timber abdomen, five deep flower cups, buttress roots, grafted branches, amber pods, ribs and rear crown timber. 2,046 triangles / Greydwarf_Shaman 1,422 / cap 2,133; one mesh/material and four 512px maps; original seven-bone source hierarchy and five actions. Saved supports and cap interiors pass sampled actions. | Continue the reference-specific torn sepals, crooked timber/tissue, porous amber, branch/rib asymmetry and bark/lichen. This is a new anatomy candidate, not adequate art or native acceptance. Starting it does not change Rift Titan's acceptance. |"
instructions.write_text('\n'.join(lines)+'\n',encoding='utf-8')
standards=ROOT/'docs/art/creature-models/REFINEMENT-STANDARDS.md'
text=standards.read_text(encoding='utf-8');marker='## October 16 interpretation and review continuation'
if marker in text:text=text[:text.index(marker)].rstrip()+'\n'
text+='''
## October 16 interpretation and review continuation

The supplied side crop supersedes the earlier Rift Titan median-fracture
instructions above. Preserve a continuous solid stone front plate, with a
separate deep crystal-lined recess on each flank behind it, resembling recessed
eyes. No violet slit passes through the median front plate. Preserve the twenty
accepted frontal X/Z landmarks while matching the long backward side rake and
overlapping stone planes. The upper recess is dark; violet is localized lower
down. Inspect front, true anatomical left, oblique left and opposite-side images.

r15's formerly unbuilt height mask was built during this continuation. Its broad
smooth sockets were rejected in direct comparison; r16 is the narrower stepped
head candidate. r17 preserves that saved head/neck and continues the body. It
corrects the rest-geometry sole/shin ordering and long hand reach, but all broader
art acceptance remains pending. Use the actual current source hash and render
proof; inherited authoring-stage JSON is not proof of the final saved mesh.

Carrion Crown refinement was explicitly requested next. Its native donor is
Greydwarf_Shaman: 1,422 triangles, soft cap 2,133. Read the actual front/side
concept before moving geometry: hollow timber abdomen, broad buttress roots,
one central open crown cup plus four grafted cups, thick uneven rim teeth,
crooked rear crown timber, hanging stem-supported amber pods and diagonal ribs.
Do not substitute a solid barrel, a flat flower mask, or disconnected paddle
petals. Preserve the source's seven bone identities/hierarchy and five retained
actions; inspect complete supports across retained poses. r3 is a starting
anatomy candidate, not adequate art acceptance. Boss tally remains 1/6.
'''
standards.write_text(text,encoding='utf-8')
status=json.loads((ROOT/'docs/art/creature-models/2026-10-14/rift-titan-systematic-audit-r12/findings.json').read_text())
for f in status['findings']:
    f['status']='open for art review'
    if f['id']=='H01':f['accepted_scope']='r13 frontal outline only; twenty landmarks preserved by r16/r17'
    if f['id'] in ('H02','H03','H04','H05','H06','M07'):f['current_candidate']='r16 solid plate, stepped planes, paired narrow flank recesses and head atlas allocation'
    if f['id'] in ('C01','C02','C03','T01','T02','T03','T05','T06','P01','A01','D01','D02','D03','L01','F01','F02','M01'):f['current_candidate']='r17 body correction; saved measurements are separate from visual acceptance'
status.update(date='2026-10-16',revision='r17',art_acceptance=False,adequate_bosses=1,numerical_forecast=None)
(CAMPAIGN/'rift-titan-concept-body-r17/repair-status.json').write_text(json.dumps(status,indent=2)+'\n')
(ROOT/'docs/validation/2026-10-16-boss-model-refinement.md').write_text('''# October 16 boss model refinement

The unfinished local Rift Titan r15 work was preserved, its final height mask
actually built, and its saved source/export independently verified. Its broad
smooth purple sockets were rejected during direct visual review. r16 corrects
the head to the supplied side crop: solid frontal plate, long stepped stone
planes, backward rake, five-corner deep flank pockets and localized lower violet.
All twenty user-accepted frontal X/Z landmarks remain exact. r14/r15/r16 source
comparisons and the original supplied crop are preserved in the galleries.

r17 continues the whole body at 1,798 / 1,800 triangles: raised rear crest,
canted rounded cage, six-crystal core, lowered wrists with matching forearm bone
tails, long unequal ridged digits, hanging pelvic stones and cleft foot masses.
Actual saved r16 head and six neck bars have zero coordinate difference.
The sole plane is -0.188849583 m in the authored rest frame; both shins now lie
above it. Fingertips clear it by 0.0185–0.2163 m. These are saved-mesh measurements,
not native root placement or grounding acceptance. Seventy-six support contracts
and 35,280 closing-face samples pass across 36 rest/action poses.

Carrion Crown was started as the next explicitly requested boss. The adopted
concept and prior r2/native donor were inspected before the new construction.
r3 is a hollow timber anatomy candidate with five deep flower cups, eight
buttress roots, four grafted branches, uneven teeth, hanging amber pods, diagonal
ribs and rear crown timber. The initial trunk intrusion into the flower floor,
paddle-like petals and cast-pose support separations were caught and repaired.
It uses 2,046 / 2,133 triangles against Greydwarf_Shaman's 1,422. Original seven
source bone identities/hierarchy and five actions remain. Ninety-eight named
supports and 25,480 closing-face samples pass across 26 rest/action poses.

Both current models have one mesh/material, four packed 512px maps, normalized
finite skin weights with at most two influences, nondegenerate closed component
surfaces and portable skinned GLB exports. Retained actions are in the editable
Blender sources; portable exports carry the skin but do not claim exported clips.
Actual front/left/opposite-head/clay/retained-pose renders were inspected. Earlier
sources and actual extracted native donors were re-rendered using matching
cameras or explicitly labeled normalized donor framing. Source hashes and local
gallery links are checked by the publisher. Browser visual QA is not claimed.

The visible concept gaps remain in each gallery's REPAIR.md. Rift Titan still
needs finer fractured crest/shoulder/limb construction, upper cage/core detail,
hands/feet and authored surface treatment. Carrion Crown still needs torn sepal
layering, denser crooked timber/tissue, branch/rib asymmetry and surface detail.
No concept-match percentage, completion forecast or new user acceptance is
invented. The adequate tally remains 1/6. First Bloom and paused Blackwater Maw
were untouched. No runtime model, release package or installed profile changed.

[Open the comparison index](../art/creature-models/2026-10-16/gallery.html).
''',encoding='utf-8')
print('VERIFIED four comparison galleries, current source hashes and unchanged adequate tally')
