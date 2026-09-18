#!/usr/bin/env python3
"""Render deterministic Sporeling fidelity plates from the authored source blend.

Run after author-underworld-sporeling.py. Produces neutral-light orthographic review
renders plus key animation-contact frames; this is an inspection aid, not acceptance.
"""
from pathlib import Path
from math import radians
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
MODEL=ROOT/'assets/models/source/underworld-creature-sporeling.blend'
OUT=ROOT/'artifacts/review/sporeling'; OUT.mkdir(parents=True,exist_ok=True)
if not MODEL.exists(): raise RuntimeError(f'Missing {MODEL}; author the Sporeling first')
bpy.ops.wm.open_mainfile(filepath=str(MODEL))
sc=bpy.context.scene
if sc.get('magenheim_model_id')!='underworld-creature-sporeling': raise RuntimeError('Wrong review model identity')

# Neutral studio world prevents emission from hiding weak albedo/normal work.
world=bpy.data.worlds.get('World') or bpy.data.worlds.new('World'); sc.world=world; world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(0.055,0.055,0.055,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.32
for o in list(sc.objects):
    if o.type in {'LIGHT','CAMERA'}: bpy.data.objects.remove(o,do_unlink=True)

# Ground reference catches floating feet and establishes the creature's actual scale.
bpy.ops.mesh.primitive_plane_add(size=2,location=(0,0,0)); ground=bpy.context.object; ground.name='REVIEW_Ground'
mat=bpy.data.materials.new('REVIEW_GroundMaterial'); mat.diffuse_color=(.16,.16,.16,1); mat.use_nodes=True
mat.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.88; ground.data.materials.append(mat)

def area(name,loc,energy,size):
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.shape='DISK'; data.size=size
    o=bpy.data.objects.new(name,data); sc.collection.objects.link(o); o.location=loc
    o.rotation_euler=(Vector((0,0,.22))-o.location).to_track_quat('-Z','Y').to_euler(); return o
area('REVIEW_Key',(-1.2,-1.0,1.45),520,1.0); area('REVIEW_Fill',(1.0,-.35,.85),240,.75); area('REVIEW_Rim',(0,1.15,1.0),360,.65)
cam_data=bpy.data.cameras.new('REVIEW_Camera'); cam=bpy.data.objects.new('REVIEW_Camera',cam_data); sc.collection.objects.link(cam); sc.camera=cam
cam_data.type='ORTHO'; cam_data.ortho_scale=.72
sc.render.engine='BLENDER_EEVEE_NEXT'; sc.render.resolution_x=768; sc.render.resolution_y=768; sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG'; sc.render.film_transparent=False
sc.view_settings.look='AgX - Medium High Contrast'

def point_camera(loc,target=(0,0,.22)):
    cam.location=loc; cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()

def render(name,loc,action=None,frame=1):
    arm=next((o for o in sc.objects if o.type=='ARMATURE'),None)
    if not arm: raise RuntimeError('Sporeling review requires one armature')
    arm.animation_data_create(); arm.animation_data.action=bpy.data.actions.get(action) if action else None
    if action and arm.animation_data.action is None: raise RuntimeError(f'Missing review action {action}')
    sc.frame_set(frame); point_camera(loc); sc.render.filepath=str(OUT/f'{name}.png'); bpy.ops.render.render(write_still=True)
    print(f'RENDERED {name}.png',flush=True)

# Orthographic identity plate: silhouette, leg clearance, cap/gill read and dorsal anatomy.
render('sporeling-front',(0,-1.1,.28)); render('sporeling-side',(1.1,0,.28)); render('sporeling-rear',(0,1.1,.28)); render('sporeling-top',(0,-.01,1.25))
# Motion plate: opposing tripod extremes, bite contact, alert silhouette and death-puff anticipation.
render('sporeling-scuttle-a',(1.0,-.7,.38),'Sporeling_Scuttle',1)
render('sporeling-scuttle-b',(1.0,-.7,.38),'Sporeling_Scuttle',7)
render('sporeling-bite-contact',(0,-1.05,.28),'Sporeling_Bite',12)
render('sporeling-alert',(0,-1.05,.30),'Sporeling_Alert',12)
render('sporeling-death-puff',(1.0,-.7,.38),'Sporeling_DeathSporePuff',27)

expected=9
outputs=list(OUT.glob('sporeling-*.png'))
if len(outputs)!=expected: raise RuntimeError(f'Expected {expected} review plates, found {len(outputs)}')
print(f'RENDERED Sporeling fidelity review: {len(outputs)} plates -> {OUT}',flush=True)
