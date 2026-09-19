#!/usr/bin/env python3
"""Render deterministic Capcrawler review plates for silhouette/material/animation inspection."""
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-capcrawler.blend'
OUT=ROOT/'build/review/capcrawler'
OUT.mkdir(parents=True,exist_ok=True)
if not MODEL.exists(): raise RuntimeError(f'Missing authored Capcrawler: {MODEL}')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
sc=bpy.context.scene
if sc.get('magenheim_model_id')!='underworld-creature-capcrawler': raise RuntimeError('Wrong review model')
if sc.get('magenheim_fidelity')!='production-creature-r5': raise RuntimeError('Review requires production-creature-r5')
arm=next((o for o in sc.objects if o.type=='ARMATURE'),None)
if not arm: raise RuntimeError('Capcrawler armature missing')

# Never let a failed current render pass by counting plates left by an older review.
for p in OUT.glob('*.png'): p.unlink()

_engines={e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items}
if 'BLENDER_EEVEE_NEXT' in _engines: sc.render.engine='BLENDER_EEVEE_NEXT'
elif 'BLENDER_EEVEE' in _engines: sc.render.engine='BLENDER_EEVEE'
else: raise RuntimeError(f'EEVEE render engine unavailable; installed engines: {sorted(_engines)}')
sc.render.resolution_x=768; sc.render.resolution_y=768; sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG'; sc.render.film_transparent=False
sc.world.color=(0.055,0.055,0.055)

# Real receiving surface + 10 cm checker expose hovering, penetration and physical scale.
bpy.ops.mesh.primitive_plane_add(size=4.0,location=(0,0,0)); floor=bpy.context.object; floor.name='REVIEW_Ground_10cm'
fm=bpy.data.materials.new('REVIEW_GroundGrid'); fm.use_nodes=True
nodes=fm.node_tree.nodes; links=fm.node_tree.links; nodes.clear()
out=nodes.new('ShaderNodeOutputMaterial'); bs=nodes.new('ShaderNodeBsdfPrincipled'); tex=nodes.new('ShaderNodeTexCoord'); chk=nodes.new('ShaderNodeTexChecker')
chk.inputs['Color1'].default_value=(0.12,0.12,0.12,1); chk.inputs['Color2'].default_value=(0.20,0.20,0.20,1); chk.inputs['Scale'].default_value=40.0
bs.inputs['Roughness'].default_value=.82
links.new(tex.outputs['Generated'],chk.inputs['Vector']); links.new(chk.outputs['Color'],bs.inputs['Base Color']); links.new(bs.outputs['BSDF'],out.inputs['Surface']); floor.data.materials.append(fm)

bpy.ops.object.light_add(type='AREA',location=(2.2,-2.4,2.8)); key=bpy.context.object; key.data.energy=650; key.data.shape='DISK'; key.data.size=2.0
bpy.ops.object.light_add(type='AREA',location=(-1.8,1.0,1.6)); fill=bpy.context.object; fill.data.energy=260; fill.data.size=1.5

def point(obj,target=(0,0,.20)): obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
point(key); point(fill)
bpy.ops.object.camera_add(); cam=bpy.context.object; sc.camera=cam; cam.data.lens=58

def render(name,loc,target=(0,0,.20),action=None,frame=1,lens=58):
 cam.data.lens=lens; cam.location=loc; point(cam,target)
 if action:
  act=bpy.data.actions.get(action)
  if not act: raise RuntimeError(f'Missing review action {action}')
  arm.animation_data_create(); arm.animation_data.action=act; sc.frame_set(frame)
 else:
  arm.animation_data_create(); arm.animation_data.action=None; sc.frame_set(1)
 sc.render.filepath=str(OUT/f'{name}.png'); bpy.ops.render.render(write_still=True)
 p=OUT/f'{name}.png'
 if not p.exists() or p.stat().st_size<4096: raise RuntimeError(f'Review plate failed: {p}')
 print(f'RENDERED {name}.png',flush=True)

# Identity/material views.
render('01-front',(0,1.55,.42)); render('02-side',(1.55,0,.40)); render('03-rear',(0,-1.55,.40)); render('04-top',(0,-.02,1.70),(0,0,.18)); render('05-low-three-quarter',(1.15,1.20,.28),(0,.03,.17))
# Motion views.
render('06-scuttle-a',(1.25,1.25,.38),action='Capcrawler_Scuttle',frame=7); render('07-scuttle-b',(1.25,1.25,.38),action='Capcrawler_Scuttle',frame=13); render('08-turn',(1.25,1.25,.38),action='Capcrawler_Turn',frame=20)
render('09-front-attack',(0,1.55,.38),action='Capcrawler_AttackFront',frame=13); render('10-left-attack',(1.10,1.25,.38),action='Capcrawler_AttackLeft',frame=14); render('11-guard',(0,1.55,.34),action='Capcrawler_Guard',frame=20); render('12-death',(1.25,1.25,.48),action='Capcrawler_Death',frame=30)
# Ground-contact diagnostics: low, long-lens orthographic-like views centered on the tarsal plane.
# These make the new pads/claws large enough to judge instead of hiding them beneath the carapace.
render('13-feet-rest',(1.85,1.85,.115),(0,0,.055),lens=85)
render('14-feet-scuttle-a',(1.85,1.85,.115),(0,0,.055),action='Capcrawler_Scuttle',frame=7,lens=85)
render('15-feet-scuttle-b',(-1.85,1.85,.115),(0,0,.055),action='Capcrawler_Scuttle',frame=13,lens=85)
render('16-feet-turn',(1.85,-1.85,.115),(0,0,.055),action='Capcrawler_Turn',frame=20,lens=85)
arm.animation_data.action=None
plates=sorted(OUT.glob('*.png'))
if len(plates)!=16: raise RuntimeError(f'Expected 16 fresh review plates, found {len(plates)}')
required={f'{i:02d}' for i in range(1,17)}
found={p.stem.split('-',1)[0] for p in plates}
if found!=required: raise RuntimeError(f'Review plate numbering incomplete: {sorted(found)}')
print(f'RENDERED Capcrawler fidelity review: {len(plates)} fresh plates including four foot-contact diagnostics -> {OUT}',flush=True)
