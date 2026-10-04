"""Build review-only prompts from the authored roster; never alter game assets."""
import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-concepts/2026-10-03';OUT.mkdir(parents=True,exist_ok=True)
if (OUT/'prompts.json').exists():
    raise SystemExit('The curated reference set already exists. Preserve it and create a new dated set for another concept run.')
brief=(ROOT/'docs/UNDERWORLD_CREATURE_ART_RIG_STANDARD.md').read_text(encoding='utf-8')
style="""Use case: stylized-concept. Original production creature concept sheet for the Valheim Underworld mod Magenheim. Full-body three-quarter view dominates; smaller front and side orthographic views and one anatomical/material close-up. All views must depict the SAME creature. Neutral dark grey background, restrained studio lighting, full silhouette visible. Nordic dark-fantasy survival game visual language: deliberate low-poly facets, earthy pixel-painted textures, believable wear, organic asymmetry, visual complexity comparable to actual Valheim donor creatures. Model-building reference, not a cinematic illustration. Coherent anatomy with articulated joints, purposeful structures and clear attack organs. Avoid spherical/cube primitive assemblies, pipe limbs, plastic smoothness, perfect symmetry, random ornaments, excessive tiny geometry, blinding emission, cartoon cuteness and photorealism. Include a small simple human height silhouette for scale. No written paragraphs or invented gameplay claims. The title must be exact."""
palettes={
 'Fungal Forest':'Brown root hide, ochre and muted moss fungal caps, pale mycelium; restrained warm gill luminescence.',
 'Blackwater Deep':'Blue-black wet hide, tarnished pale shell, subdued turquoise light organs, pale mouth tissue. Deep aquatic adaptation.',
 'Sulfurous Wastes':'Charcoal basalt, oxidized iron, sulfur crust and localized ember-orange heat. Functional vents and thermal adaptation.',
 'Frozen Caverns':'Weathered dark mineral, desaturated pale ice and frost, small dim blue internal cores. Preserve contrast; do not make every surface glow.',
 'Fracture Zones':'Layered fractured grey-violet stone, sparse mineral crystal accents, dark binding joints. Geological masses rather than generic ice robot.',
 'Great Decay':'Rotwood, fibrous biomass, old bone, desaturated olive and carrion amber. Integrated growth and unsettling asymmetry without gratuitous gore.'}
biome=None;entries=[]
for line in brief.splitlines():
 if line.startswith('## '):
  biome=next((b for b in palettes if b+' roster' in line),None)
 if biome and line.startswith('| ') and not line.startswith('| Creature '):
  fields=[p.strip() for p in line.strip('|').split('|')]
  if len(fields)!=5:continue
  name,tier,size,host,anatomy=fields
  prompt=f"{style}\nExact title: '{name.upper()}'. Biome: {biome}. Role: {tier}. Size: {size}. Anatomy and motion brief: {anatomy}. {palettes[biome]} This is the regular creature, not a boss variant. Preserve this creature's distinct locomotion and silhouette."
  entries.append({'name':name,'biome':biome,'kind':'creature','size':size,'anatomy':anatomy,'prompt':prompt})
bosses=[
 ('The First Bloom','Crowncap Brute','A singular ancient fungal sovereign: towering irregular overlapping crown shelves, exposed living gill cathedral beneath, mature buttress-root legs and broad sweeping arms. More hierarchical asymmetric crown than the regular brute; no crown jewelry.'),
 ('The Blackwater Maw','Deep Hunter','An ancient abyssal predator with a cavernous trap jaw, powerful armored head and muscular tapering aquatic body, scarred fin membranes, subdued sensory light organs. The giant mouth is the central identity, not an oversized fish sphere.'),
 ('The Furnace Heart','Furnace Golem','A monumental geological furnace organism: massive irregular basalt armor shell around a recessed exposed molten heart, broken sacrificial outer plates, heat-scarred hammer forearms and functional fissure vents. No cute metal robot or twin toy chimneys.'),
 ('The White Silence','Cryolith Guardian','An ancient glacier guardian: tall solemn mineral body with layered compressed ice shelves, long deliberate arms, frozen hollow core and frost-eroded monolithic head. Asymmetric glacial geology; distinct proportions from Furnace Heart.'),
 ('The Rift Titan','Rift Colossus','An enormous segmented fault-line guardian: offset interlocking geological masses suspended around an exposed binding core, bridge-like load-bearing limb segments, fractured monolithic head. Deliberate negative spaces, suspended pieces have coherent relationships; no random orbiting rubble.'),
 ('The Carrion Crown','Corpse Orchard','A gigantic rooted colony, broad hollow trunk and crown of predatory flower-mouths, hanging seed pods, branching tendrils and root buttresses. A commanding single silhouette with subordinate mouths; visibly an old organism, not several identical flowers placed in a row.')]
for name,base,anatomy in bosses:
 parent=next(e for e in entries if e['name']==base)
 prompt=f"{style}\nExact title: '{name.upper()}'. Biome boss: {parent['biome']}. Related species: {base}. Design direction: {anatomy}. {palettes[parent['biome']]} Make a distinct encounter silhouette rather than a uniformly enlarged regular creature. Show structural front/side views and vulnerable-organ close-up; keep emission controlled. Concept proposal, no written statistics."
 entries.append({'name':name,'biome':parent['biome'],'kind':'boss','base':base,'anatomy':anatomy,'prompt':prompt})
assert len(entries)==48,len(entries)
priority=['Crowncap Brute','The First Bloom','Deep Hunter','The Blackwater Maw','Furnace Golem','The Furnace Heart','Cryolith Guardian','The White Silence','Rift Colossus','The Rift Titan','Corpse Orchard','The Carrion Crown','Graft Warden','Abyss Shellback','Puffback','Shelf Lurker']
entries.sort(key=lambda e:priority.index(e['name']) if e['name'] in priority else len(priority))
for e in entries:e['file']=re.sub('[^a-z0-9]+','-',e['name'].lower()).strip('-')+'.png';e['status']='pending'
(OUT/'prompts.json').write_text(json.dumps(entries,indent=2),encoding='utf-8')
print('PREPARED',len(entries),'individual creature/boss prompts',OUT)
