"""Remove audited wholly buried faces; retain UVs, weights and contact markers."""
import bpy,bmesh,json,shutil
from pathlib import Path
from mathutils import Matrix
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-trim-r14'
ID='underworld-boss-first-bloom-trim-r14';BASE='underworld-boss-first-bloom-membranes-r13'
audit=json.loads((OUT/'buried-face-audit.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies'/(BASE+'.blend')))
scene=bpy.context.scene;o=next(o for o in scene.objects if o.type=='MESH');arm=next(o for o in scene.objects if o.type=='ARMATURE')
arm.animation_data.action=None;arm.data.pose_position='REST'
for b in arm.pose.bones:b.matrix_basis=Matrix.Identity(4)
cuts=set(audit['accepted_faces']);incident={}
for p in o.data.polygons:
 for i in p.vertices:incident.setdefault(i,set()).add(p.index)
# Keep every structural marker attached to surviving surface geometry.
for i,marker in enumerate(o.data.attributes['attachment_endpoint'].data):
 if marker.value and incident[i]<=cuts:cuts.remove(min(incident[i]))
attr=o.data.attributes.new('buried_boundary_host','INT','POINT')
for face in cuts:
 for i in o.data.polygons[face].vertices:attr.data[i].value=audit['hosts'][str(face)]
bm=bmesh.new();bm.from_mesh(o.data);bm.faces.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.faces[i] for i in cuts],context='FACES');bm.to_mesh(o.data);bm.free();o.data.update();o.data.calc_loop_triangles()
triangles=len(o.data.loop_triangles);assert triangles<9252
TEX=ROOT/'assets/textures/underworld/bosses/first-bloom-trim-r14';TEX.mkdir(parents=True,exist_ok=True)
for node in o.data.materials[0].node_tree.nodes:
 if node.type!='TEX_IMAGE':continue
 old=Path(bpy.path.abspath(node.image.filepath));new=TEX/old.name.replace(BASE,ID);shutil.copyfile(old,new);node.image.filepath=str(new)
scene['magenheim_model_id']=ID;scene['refinement_status']='buried-face trim study; native acceptance pending'
bpy.context.preferences.filepaths.save_version=0
path=ROOT/'assets/models/studies'/(ID+'.blend');bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
bpy.ops.export_scene.gltf(filepath=str(OUT/(ID+'.glb')),export_format='GLB',export_animations=False,export_yup=True)
metrics=json.loads((ROOT/'docs/art/creature-models/2026-10-04/first-bloom-membranes-r13/refinement-metrics.json').read_text())
metrics.update(id=ID,source=path.relative_to(ROOT).as_posix(),triangles=triangles,ratio=triangles/6206,previous_triangles=9252,previous_id=BASE,reduction_percent=(1-triangles/9252)*100)
metrics.pop('category_triangles',None)
(OUT/'refinement-metrics.json').write_text(json.dumps(metrics,indent=2)+'\n')
(OUT/'trim-result.json').write_text(json.dumps(dict(before_triangles=9252,after_triangles=triangles,removed_triangles=9252-triangles,removed_faces=len(cuts),skipped_faces_to_preserve_contact_vertices=len(audit['accepted_faces'])-len(cuts),textures='unchanged atlas pixels and UVs',method='whole buried faces removed; partial intersections retained; open boundaries remain inside closed support surfaces'),indent=2)+'\n')
print('REVISED hidden face trim',9252,'to',triangles,'triangles; removed',9252-triangles,flush=True)
