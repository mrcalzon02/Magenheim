from pathlib import Path
import builtins,json
ROOT=Path(__file__).resolve().parents[1]
ATLAS='''# Allocate compact explicit atlas regions to bark, flower mouths and amber.
parts=json.loads(bpy.context.scene['silhouette_components'])
flower={v for k,v in parts.items() if k.startswith(('CentralCrownCup','GraftedFlower','TimberTooth'))}
amber={v for k,v in parts.items() if k.startswith(('CentralAmberSeed','HangingAmberPod'))}
bpy.context.tool_settings.mesh_select_mode=(False,False,True)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='DESELECT');bpy.ops.object.mode_set(mode='OBJECT')
labels=o.data.attributes['attachment_component'];uv=o.data.uv_layers.active.data
for f in o.data.polygons:
 if labels.data[f.index].value not in flower|amber:
  for li in f.loop_indices:uv[li].uv.y*=.50
for selected,offset,height in ((flower,.52,.30),(amber,.84,.16)):
 labels=o.data.attributes['attachment_component']
 for f in o.data.polygons:f.select=labels.data[f.index].value in selected
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.uv.smart_project(angle_limit=math.radians(55),island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT')
 labels=o.data.attributes['attachment_component'];uv=o.data.uv_layers.active.data
 for f in o.data.polygons:
  if labels.data[f.index].value in selected:
   for li in f.loop_indices:uv[li].uv.y=offset+height*uv[li].uv.y
'''
def compile(source,filename,mode):
    source=source.replace("scene=bpy.context.scene;scene.render.engine='CYCLES'",ATLAS+"\nscene=bpy.context.scene;scene.render.engine='CYCLES'")
    return builtins.compile(source,filename,mode)
s=(ROOT/'tools/refine-carrion-crown-anatomy-r3.py').read_text().replace('carrion-crown-anatomy-r3','carrion-crown-gnarled-r5').replace('carrion_crown_anatomy','carrion_crown_gnarled').replace('2026-10-16','2026-10-25')
s=s.replace('export_animations=False,',"export_animations=False,export_vertex_color='NONE',")
exec(compile(s,str(__file__),'exec'))
p=OUT/'refinement-metrics.json';row=json.loads(p.read_text());row.pop('reduction_percent',None);row.update(previous_id='underworld-boss-carrion-crown-sepals-r4',previous_triangles=2126,triangle_delta=row['triangles']-2126,changes='narrow twisted hollow trunk, crooked root fan, outward flower mouths with alternating layered sepals, compact inward teeth, crooked branch forks, splintered bark and rooted knots, larger porous seed; directed gray timber and partitioned 512 atlas',status='full silhouette/anatomy revision candidate; art and native acceptance separate');p.write_text(json.dumps(row,indent=2)+'\n')
