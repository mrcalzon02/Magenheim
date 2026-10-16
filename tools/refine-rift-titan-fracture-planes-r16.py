from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/refine-first-bloom.py').read_text().replace('first-bloom-refinement','rift-titan-fracture-planes-r16').replace('creature-models/2026-10-04','creature-models/2026-10-16').replace('crowncap-brute','rift-colossus').replace('6206','1200')
start=s.index('# One uninterrupted hip-to-shoulder trunk');end=s.index('meshes=[o for o in bpy.context.scene.objects',start)
s=s[:start]+"import rift_titan_fracture_planes as sculpt\ntrunk=sculpt.build(B,arm,record,profile,tube,cap)\n\n"+s[end:]
start=s.index('# Add fine radial gill normals');end=s.index("bpy.ops.object.select_all(action='DESELECT')",start);s=s[:start]+s[end:]
s=s.replace('FirstBloom','RiftTitan').replace('First Bloom','Rift Titan').replace('factor=4.65/height','factor=7.0/height').replace("scene['boss_target_metres']=4.65","scene['boss_target_metres']=7.0")
s=s.replace('width=512,height=512','width=1024,height=1024').replace('path=TEX/(ID+','im.scale(512,512)\n path=TEX/(ID+')
s=s.replace('export_animations=False,','export_animations=False,export_vertex_color=\'NONE\',')
atlas_priority='''# Allocate the head a dedicated upper atlas strip after normal global packing.
component=json.loads(bpy.context.scene['silhouette_components'])['UnifiedCrystalHead']
bpy.context.tool_settings.mesh_select_mode=(False,False,True)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='DESELECT');bpy.ops.object.mode_set(mode='OBJECT')
labels=o.data.attributes['attachment_component']; atlas_uv=o.data.uv_layers.active.data
for f in o.data.polygons:
 f.select=labels.data[f.index].value==component
 if not f.select:
  for li in f.loop_indices:atlas_uv[li].uv.y*=.64
bpy.context.tool_settings.mesh_select_mode=(False,False,True)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.uv.smart_project(angle_limit=math.radians(50),island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
atlas_uv=o.data.uv_layers.active.data;labels=o.data.attributes['attachment_component']
for f in o.data.polygons:
 if labels.data[f.index].value==component:
  for li in f.loop_indices:atlas_uv[li].uv.y=.66+atlas_uv[li].uv.y*.34
'''
s=s.replace("scene=bpy.context.scene;scene.render.engine='CYCLES'",atlas_priority+"\nscene=bpy.context.scene;scene.render.engine='CYCLES'")
exec(compile(s,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-16/rift-titan-fracture-planes-r16'
for name in ('refinement-metrics','concept-pass'):
    p=out/(name+'.json')
    if name=='concept-pass':row={}
    else:row=json.loads(p.read_text())
    row.update(previous_id='underworld-boss-rift-titan-paired-recesses-r15',previous_triangles=1800,reduction_percent=0,study_height_metres=7.0,rig='original eight-bone Rift Colossus hierarchy and seven actions; six neck bars retain their three-dimensional spread',texture_bake='owned spatial head stone and localized violet; dedicated upper atlas strip; four packed 512px maps',changes='continuous ridged stone face, narrower broken side sockets, localized lower violet and dedicated upper atlas strip for crisp head stone patches',status='head correction candidate; art acceptance pending');p.write_text(json.dumps(row,indent=2)+'\n')
