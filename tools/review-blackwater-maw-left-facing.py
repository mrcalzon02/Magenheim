"""Validate internal cut boundaries instead of requiring buried skins to close."""
import bpy,bmesh,json,sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from first_bloom_attachment_repair import inside
def check_internal_boundaries(o):
 mesh=o.data;mesh.calc_loop_triangles();points=[o.matrix_world@v.co for v in mesh.vertices];components=mesh.attributes['attachment_component'];markers=mesh.attributes['buried_boundary_host'];hosts={}
 bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table();bm.verts.index_update()
 assert not any(e.is_wire or len(e.link_faces)>2 for e in bm.edges)
 boundary=0
 for edge in bm.edges:
  if not edge.is_boundary:continue
  boundary+=1
  ids=[v.index for v in edge.verts];candidate={markers.data[i].value for i in ids};assert 0 not in candidate
  for host in candidate:
   if host not in hosts:
    faces=[list(t.vertices) for t in mesh.loop_triangles if components.data[t.polygon_index].value==host]
    counts={}
    for f in faces:
     for a,b in zip(f,f[1:]+f[:1]):key=tuple(sorted((a,b)));counts[key]=counts.get(key,0)+1
    assert all(n==2 for n in counts.values()),'trimmed support is no longer closed'
    hosts[host]=BVHTree.FromPolygons(points,faces,all_triangles=True)
  assert any(all(inside(p,hosts[h]) for p in [points[ids[0]],points[ids[1]],(points[ids[0]]+points[ids[1]])/2]) for h in candidate),'exposed trim boundary'
 bm.free();return boundary
source=(ROOT/'tools/review-first-bloom-attachments.py').read_text().replace('first-bloom-attachments-r5','blackwater-maw-left-facing-r10').replace('groups=len(records)//2',"groups=len(set(r['tissue'] for r in records))")
source=source[:source.index("source=(ROOT/'tools/review-first-bloom-concept.py')")]
exec(compile(source,str(__file__),'exec'))
boundary=check_internal_boundaries(o)
source=(ROOT/'tools/review-first-bloom-refinement.py').read_text().replace('first-bloom-refinement','blackwater-maw-left-facing-r10').replace('span=size*.43','span=size*.52')
source=source.replace("assert abs(hi.z-lo.z-row.get('study_height_metres',4.65))<.005","assert abs(hi.y-lo.y-row.get('study_length_metres',10))<.005")
source=source.replace('(1,2,.55)','(-1,2,.55)').replace('(2,0,.06)','(-2,0,.06)').replace('(1,2,.35)','(-1,2,.35)')
source=source.replace("assert all(e.is_manifold for e in bm.edges),'open/nonmanifold edge'","assert not any(e.is_wire or len(e.link_faces)>2 for e in bm.edges),'wire or multiply connected edge';check_internal_boundaries(o)")
exec(compile(source,str(__file__),'exec'))
(OUT/'trim-boundary-verification.json').write_text(json.dumps(dict(result='PASS all saved cut edges remain inside retained closed supports',internal_boundary_edges=boundary),indent=2)+'\n')
print('VERIFIED trim boundaries and saved contacts',boundary,flush=True)
