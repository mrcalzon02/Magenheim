"""Undo the legacy authoring pipeline's whole-model height normalization."""
import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-head-r20-raw'
row=json.loads((OUT/'refinement-metrics.json').read_text());report=json.loads((OUT/'head-refinement.json').read_text())
path=ROOT/row['source'];bpy.ops.wm.open_mainfile(filepath=str(path))
scene=bpy.context.scene;o=next(o for o in scene.objects if o.type=='MESH');arm=next(o for o in scene.objects if o.type=='ARMATURE')
factor=report['pipeline_scale_correction']
for v in o.data.vertices:v.co*=factor
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
for b in arm.data.edit_bones:b.head*=factor;b.tail*=factor
bpy.ops.object.mode_set(mode='OBJECT')
for action in bpy.data.actions:
 for layer in action.layers:
  for strip in layer.strips:
   for slot in action.slots:
    bag=strip.channelbag(slot)
    if bag:
     for curve in bag.fcurves:
      if curve.data_path.endswith('.location'):
       for key in curve.keyframe_points:key.co.y*=factor;key.handle_left.y*=factor;key.handle_right.y*=factor
scene['refinement_head_focus']=[v*factor for v in scene['refinement_head_focus']]
o.data.update();bpy.context.view_layer.update()
points=[o.matrix_world@v.co for v in o.data.vertices];height=max(p.z for p in points)-min(p.z for p in points)
scene['boss_target_metres']=height;row['study_height_metres']=height
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
(OUT/'refinement-metrics.json').write_text(json.dumps(row,indent=2)+'\n')
report.update(revised_height_metres=height,height_reduction_metres=4.65-height)
(OUT/'head-refinement.json').write_text(json.dumps(report,indent=2)+'\n')
print('REVISED crown compression: retained body scale; height',height,flush=True)
