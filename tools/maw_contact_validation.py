"""Independent solid-angle check for ambiguous ray hits on thin oral surfaces."""
import numpy as np,math
from first_bloom_attachment_repair import inside

def contains(point,bvh,triangles):
 if inside(point,bvh):return True
 xyz=triangles-np.array(point);a,b,c=xyz[:,0],xyz[:,1],xyz[:,2]
 la=np.linalg.norm(a,axis=1);lb=np.linalg.norm(b,axis=1);lc=np.linalg.norm(c,axis=1)
 denominator=la*lb*lc+np.sum(a*b,axis=1)*lc+np.sum(b*c,axis=1)*la+np.sum(c*a,axis=1)*lb
 winding=float(np.sum(2*np.arctan2(np.sum(np.cross(a,b)*c,axis=1),denominator))/(4*math.pi))
 # A closed oriented support encloses a point once. Do not accept overlapping
 # winding-two regions or relax the original one-millimetre contact tolerance.
 return abs(abs(winding)-1)<1e-5

def validation_source(source):
 source=source.replace('from first_bloom_attachment_repair import inside','from first_bloom_attachment_repair import inside\nfrom maw_contact_validation import contains\nimport numpy as np')
 source=source.replace('hosts={};gaps=[]','hosts={};host_triangles={};gaps=[]')
 source=source.replace("ids=[i for i,a in enumerate(markers.data) if a.value==item['id']];assert ids,item", "if component not in host_triangles:host_triangles[component]=np.array([[list(points[i]) for i in t.vertices] for t in me.loop_triangles if components.data[t.polygon_index].value==component])\n  ids=[i for i,a in enumerate(markers.data) if a.value==item['id']];assert ids,item")
 source=source.replace('if not inside(points[i],hosts[component])','if not contains(points[i],hosts[component],host_triangles[component])')
 source=source.replace("scope='saved rest mesh and five Blender samples per retained action; native and every-frame acceptance pending'", "scope='saved rest and five Blender samples per retained action; ray containment with independent closed-support winding check for ambiguous hits; native and every-frame acceptance pending'")
 return source
