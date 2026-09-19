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
arm=next((o for o in sc.objects if o.type=='ARMATURE'),None)
if not arm: raise RuntimeError('Capcrawler armature missing')

# Review is intentionally neutral: no dramatic fog/color grading that can hide UV,
# normal, ground-contact, interpenetration, or emission defects.
sc.render.engine='BLENDER_EEVEE_NEXT'
sc.render.resolution_x=768; sc.render.resolution_y=768; sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG'; sc.render.film_transparent=False
sc.world.color=(0.055,0.055,0.055)

bpy.ops.object.light_add(type='AREA',location=(2.2,-2.4,2.8)); key=bpy.context.object; key.data.energy=650; key.data.shape='DISK'; key.data.size=2.0
bpy.ops.object.light_add(type='AREA',location=(-1.8,1.0,1.6)); fill=bpy.context.object; fill.data.energy=260; fill.data.size=1.5

def point(obj,target=(0,0,.20)):
 obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

point(key); point(fill)
bpy.ops.object.camera_add(); cam=bpy.context.object; sc.camera=cam; cam.data.lens=58

def render(name,loc,target=(0,0,.20),action=None,frame=1):
 cam.location=loc; point(cam,target)
 if action:
  act=bpy.data.actions.get(action)
  if not act: raise RuntimeError(f'Missing review action {action}')
  arm.animation_data_create(); arm.animation_data.action=act; sc.frame_set(frame)
 else:
  arm.animation_data_create(); arm.animation_data.action=None; sc.frame_set(1)
 sc.render.filepath=str(OUT/f'{name}.png'); bpy.ops.render.render(write_still=True)
 p=OUT/f'{name}.png'
 if not p.exists() or p.stat().st_size<4096: raise RuntimeError(f'Review plate failed: {p}')

# Orthographic-like identity views expose silhouette, cap overhang, gill readability,
# mandible spacing and whether the low crawler still clears the ground.
render('01-front',(0,1.55,.42))
render('02-side',(1.55,0,.40))
render('03-rear',(0,-1.55,.40))
render('04-top',(0,-.02,1.70),(0,0,.18))
render('05-low-three-quarter',(1.15,1.20,.28),(0,.03,.17))
# Motion plates target the frames most likely to expose leg/body intersections.
render('06-scuttle-a',(1.25,1.25,.38),action='Capcrawler_Scuttle',frame=7)
render('07-scuttle-b',(1.25,1.25,.38),action='Capcrawler_Scuttle',frame=13)
render('08-turn',(1.25,1.25,.38),action='Capcrawler_Turn',frame=20)
render('09-front-attack',(0,1.55,.38),action='Capcrawler_AttackFront',frame=13)
render('10-left-attack',(1.10,1.25,.38),action='Capcrawler_AttackLeft',frame=14)
render('11-guard',(0,1.55,.34),action='Capcrawler_Guard',frame=20)
render('12-death',(1.25,1.25,.48),action='Capcrawler_Death',frame=30)
arm.animation_data.action=None
plates=sorted(OUT.glob('*.png'))
if len(plates)!=12: raise RuntimeError(f'Expected 12 review plates, found {len(plates)}')
print(f'RENDERED Capcrawler fidelity review: {len(plates)} plates -> {OUT}',flush=True)
