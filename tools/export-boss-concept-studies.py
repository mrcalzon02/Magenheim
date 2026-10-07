"""Export review-only rest-pose GLBs; production runtime remains unmodified."""
import bpy, json, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04'
if '--detail-r2' in sys.argv:OUT=OUT/'detail-r2'
for row in json.loads((OUT/'boss-construction-manifest.json').read_text()):
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/row['source']))
 for arm in [o for o in bpy.context.scene.objects if o.type=='ARMATURE']:
  arm.data.pose_position='REST';arm.animation_data.action=None
 bpy.ops.export_scene.gltf(filepath=str(OUT/(row['id']+'.glb')),export_format='GLB',export_animations=False,export_yup=True)
 print('EXPORTED study '+row['id'],flush=True)
