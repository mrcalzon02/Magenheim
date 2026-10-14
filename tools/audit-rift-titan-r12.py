"""Read-only geometry/atlas inventory for the visual concept discrepancy audit."""
import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-systematic-audit-r12';OUT.mkdir(exist_ok=True)
source=ROOT/'assets/models/studies/underworld-boss-rift-titan-cracked-head-r12.blend';before=hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene;arm=next(o for o in scene.objects if o.type=='ARMATURE');arm.animation_data.action=None;arm.data.pose_position='REST'
o=next(o for o in scene.objects if o.type=='MESH');me=o.data;parts=json.loads(scene['silhouette_components']);labels=me.attributes['attachment_component'];rows={}
for name,component in parts.items():
 faces=[p for p in me.polygons if labels.data[p.index].value==component];ids={i for p in faces for i in p.vertices};points=[o.matrix_world@me.vertices[i].co for i in ids]
 lo=[min(p[k] for p in points) for k in range(3)];hi=[max(p[k] for p in points) for k in range(3)]
 rows[name]=dict(triangles=len(faces),vertices=len(ids),minimum=lo,maximum=hi,dimensions=[b-a for a,b in zip(lo,hi)])
foot_min=min(rows[n]['minimum'][2] for n in ('FootL','FootR'));below={n:dict(lowest_z=r['minimum'][2],distance_below_feet=foot_min-r['minimum'][2]) for n,r in rows.items() if r['minimum'][2]<foot_min-.001}
digits={n:r['minimum'][2]-foot_min for n,r in rows.items() if n.startswith('FingerStratum')}
uv=me.uv_layers.active.data;uv_pixels={}
me.calc_loop_triangles()
for role in (1,2,3):
 area=0
 for t in me.loop_triangles:
  if labels.data[t.polygon_index].value!=parts['UnifiedCrystalHead'] or me.attributes['head_surface_role'].data[t.polygon_index].value!=role:continue
  a,b,c=[uv[i].uv for i in t.loops];area+=abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))/2
 uv_pixels[str(role)]=area*512*512
after=hashlib.sha256(source.read_bytes()).hexdigest();assert before==after
report=dict(source=source.relative_to(ROOT).as_posix(),source_sha256=before,model_unchanged=True,actor_triangles=len(me.loop_triangles),foot_minimum_z=foot_min,geometry_below_foot_level=below,fingertip_clearance_above_foot_level=digits,head_nominal_uv_texel_area_by_role=uv_pixels,components=rows,scope='saved rest-space geometry inventory, not native grounding or an objective concept similarity score')
(OUT/'geometry-inventory.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('VERIFIED audit is read-only;',len(rows),'components;',len(below),'components below foot level;',len(me.loop_triangles),'triangles')
