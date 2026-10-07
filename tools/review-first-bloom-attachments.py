"""Verify saved endpoint contact independently, then use matched art checks."""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from first_bloom_attachment_repair import inside
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-attachments-r5'
ID='underworld-boss-first-bloom-attachments-r5'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies'/(ID+'.blend')))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');records=json.loads(bpy.context.scene['tissue_attachment_contract'])
components=o.data.attributes['attachment_component'];markers=o.data.attributes['attachment_endpoint']
o.data.calc_loop_triangles()
points=[o.matrix_world@v.co for v in o.data.vertices];hosts={};checks=[]
for record in records:
 component=record['support_component']
 if component not in hosts:
  faces=[list(t.vertices) for t in o.data.loop_triangles if components.data[t.polygon_index].value==component]
  assert faces;hosts[component]=BVHTree.FromPolygons(points,faces,all_triangles=True)
 indices=[i for i,a in enumerate(markers.data) if a.value==record['id']];assert indices
 center=sum((points[i] for i in indices),Vector())/len(indices)
 assert all(inside(points[i],hosts[component]) for i in indices),('floating saved attachment rail',record)
 hit,normal,face,distance=hosts[component].find_nearest(center)
 checks.append(dict(tissue=record['tissue'],end=record['end'],support=record['support'],all_endpoint_vertices_inside_support=True,endpoint_vertices=len(indices),center_surface_distance_metres=distance))
(OUT/'attachment-verification.json').write_text(json.dumps(dict(result='PASS every saved vein/frame endpoint intersects its designated support',groups=len(records)//2,endpoints=len(records),checks=checks,scope='rest geometry; native animation and live contacts remain unverified'),indent=2)+'\n')
source=(ROOT/'tools/review-first-bloom-concept.py').read_text().replace('first-bloom-concept-r3','first-bloom-attachments-r5')
exec(compile(source,str(ROOT/'tools/review-first-bloom-concept.py'),'exec'),{'__file__':str(ROOT/'tools/review-first-bloom-concept.py'),'__name__':'__main__'})
print('VERIFIED saved tissue attachments '+str(len(records))+' endpoints',flush=True)
