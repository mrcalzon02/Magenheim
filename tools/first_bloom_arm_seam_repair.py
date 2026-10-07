"""Replace warped arm-root caps with planar, consistently ordered buried roots."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector,geometry
import first_bloom_head_bundles as previous
from first_bloom_attachment_repair import tree,inside
from first_bloom_frill_sculpt import assign
from first_bloom_deeper_chest import embed
materials=previous.materials
unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-arm-seams-r22-raw'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT;trunk=previous.build(B,arm,record,profile,tube,cap)
 records=json.loads(bpy.context.scene['tissue_attachment_contract']);byname={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'}
 bvh=tree(trunk);report=[];changed=set()
 for side in ('L','R'):
  o=byname['RootedUpperArm'+side]
  old=[o.matrix_world@o.data.vertices[i].co for i in range(30)]
  c0=sum(old[:10],Vector())/10;c1=sum(old[10:20],Vector())/10;c2=sum(old[20:30],Vector())/10
  ranked=[]
  for tri in trunk.data.loop_triangles:
   weights=[{trunk.vertex_groups[g.group].name:round(g.weight,6) for g in trunk.data.vertices[i].groups if g.weight>1e-7} for i in tri.vertices]
   if not all(w=={'Chest':1.0} for w in weights):continue
   coords=[trunk.matrix_world@trunk.data.vertices[i].co for i in tri.vertices];center=sum(coords,Vector())/3
   normal=(coords[1]-coords[0]).cross(coords[2]-coords[0])
   if normal.length<1e-10:continue
   ranked.append(((center-c0).length,coords,center,normal.normalized()))
  for distance,coords,center,normal in sorted(ranked,key=lambda r:r[0]):
   # Fit a complete circular root into one planar support triangle.
   edge_distances=[]
   for a,b in zip(coords,coords[1:]+coords[:1]):edge_distances.append((center-geometry.intersect_point_line(center,a,b)[0]).length)
   radius=min(.055,min(edge_distances)*.68)
   if radius<.012:continue
   tangent=(coords[1]-coords[0]).normalized();bitangent=normal.cross(tangent).normalized()
   options=[]
   for sign in (-1,1):
    seated=center+normal*.004*sign
    ring=[seated+(tangent*math.cos(math.tau*k/10)+bitangent*math.sin(math.tau*k/10))*radius for k in range(10)]
    if all(inside(p,bvh) for p in ring) and inside(seated,bvh):options.append((seated,ring))
   if options:break
  else:raise ValueError('no planar buried shoulder patch '+side)
  center,ring=options[0]
  # Preserve the original ring correspondence; prevent a rotated root from
  # winding the connecting strip around itself.
  layouts=[list(reversed(ring)),ring]
  candidates=[r[k:]+r[:k] for r in layouts for k in range(10)]
  ring=min(candidates,key=lambda r:sum((p-q).length_squared for p,q in zip(r,old[:10])))
  for index,p in enumerate(ring):assign(o,index,p,{'Chest':1.0})
  axis=(c2-center).normalized();q=axis.to_track_quat('Z','Y')
  # A regular transition ring removes the collapsed upper-arm saddle while
  # the lower rings retain the previous arm silhouette and bark ridges.
  middle=[c1+q@Vector((.165*math.cos(math.tau*k/10),.137*math.sin(math.tau*k/10),0)) for k in range(10)]
  layouts=[middle,list(reversed(middle))]
  middle=min([r[k:]+r[:k] for r in layouts for k in range(10)],key=lambda r:sum((p-q).length_squared for p,q in zip(r,old[10:20])))
  for k,p in enumerate(middle):assign(o,k+10,p,{'Chest':.25,'UpperArm_'+side:.75})
  o.data.update();changed.add(o.name)
  # Sample the entire closing face, not only its seated boundary vertices.
  for k in range(10):
   for a,b in ((.2,.2),(.2,.6),(.6,.2),(.33,.33)):
    point=center*(1-a-b)+ring[k]*a+ring[(k+1)%10]*b
    assert inside(point,bvh),'root cap exposed outside torso'
  report.append(dict(arm=o.name,root_radius=radius,closing_face='planar inside one rigid Chest support face',cap_interior_samples=40,root_shift=(center-c0).length))
 # Refit only tissue contacts supported by the two edited arm surfaces.
 for item in records:
  if item['support'] not in changed:continue
  o=byname[item['tissue']];host=byname[item['support']]
  for index,a in enumerate(o.data.attributes['attachment_endpoint'].data):
   if a.value!=item['id']:continue
   point,w=embed(o.matrix_world@o.data.vertices[index].co,host,.003);assign(o,index,point,w)
 for item in records:
  o=byname[item['tissue']];host=byname[item['support']];host_tree=tree(host)
  ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']]
  assert ids and all(inside(o.matrix_world@o.data.vertices[i].co,host_tree) for i in ids),item
 (OUT/'arm-seam-repair.json').write_text(json.dumps(dict(cause='nonplanar closing face at the projected arm root; visible in trimmed and untrimmed model',repairs=report,triangles_added=0,preserved='body, crown bundles, beard, caps and lower arm geometry'),indent=2)+'\n')
 return trunk
