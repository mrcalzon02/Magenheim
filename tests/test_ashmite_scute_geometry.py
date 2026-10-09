"""Blender-free regression for the five established Ashmite dorsal scutes."""
import math
import unittest
from collections import Counter
from ashmite_scute_geometry import CONTRACT, RINGS, SIDES, scute_mesh_data, scute_uv

PLATES=[(.092,.047,.018),(.092,.047,.018),(.092,.047,.018),(.075,.047,.018),(.075,.047,.018)]


def signed_volume(verts,faces):
    volume=0.
    for face in faces:
        a=verts[face[0]]
        for i in range(1,len(face)-1):
            b,c=verts[face[i]],verts[face[i+1]]
            volume+=(a[0]*(b[1]*c[2]-b[2]*c[1])
                     +a[1]*(b[2]*c[0]-b[0]*c[2])
                     +a[2]*(b[0]*c[1]-b[1]*c[0]))/6.
    return volume


class AshmiteScuteTests(unittest.TestCase):
    def test_five_distinct_closed_outward_plates(self):
        self.assertEqual(CONTRACT,'ashmite-five-heat-fractured-scutella-16x64')
        for i,(rx,ry,rz) in enumerate(PLATES,1):
            verts,faces=scute_mesh_data(rx,ry,rz,i)
            self.assertEqual((len(verts),len(faces),sum(len(f)-2 for f in faces)),(1026,1088,2048))
            edges=Counter(); oriented=Counter()
            for f in faces:
                for a,b in zip(f,f[1:]+f[:1]):
                    edges[tuple(sorted((a,b)))]+=1;oriented[(a,b)]+=1
            self.assertTrue(all(n==2 for n in edges.values()))
            self.assertTrue(all(oriented[(b,a)]==1 for a,b in oriented))
            self.assertGreater(signed_volume(verts,faces),0)

    def test_silhouette_stays_inside_five_scute_contract(self):
        for i,(rx,ry,rz) in enumerate(PLATES,1):
            verts,_=scute_mesh_data(rx,ry,rz,i)
            self.assertLess(max(abs(v[0]) for v in verts),rx*1.07)
            self.assertLess(max(abs(v[1]) for v in verts),ry*1.07)
            self.assertGreater(max(v[2] for v in verts),rz*.94)
            self.assertGreater(min(v[2] for v in verts),-rz*.55)
            self.assertLess(max(v[2] for v in verts),rz*1.08)

    def test_physical_crest_fissures_and_perimeter_scallops(self):
        for i,scale in enumerate(PLATES,1):
            verts,_=scute_mesh_data(*scale,i)
            ring=6; pts=verts[2+ring*SIDES:2+(ring+1)*SIDES]
            self.assertGreater(max(p[2] for p in pts)-min(p[2] for p in pts),scale[2]*.09)
            edge=verts[2+10*SIDES:2+11*SIDES]
            normalized=[math.hypot(p[0]/scale[0],p[1]/scale[1]) for p in edge]
            self.assertGreater(max(normalized)-min(normalized),.07)

    def test_uvs_are_bounded_and_not_collapsed(self):
        for i,(rx,ry,rz) in enumerate(PLATES,1):
            verts,faces=scute_mesh_data(rx,ry,rz,i)
            uvs=[scute_uv(v,rx,ry) for v in verts]
            self.assertTrue(all(0<=u<=1 and 0<=v<=1 for u,v in uvs))
            top=[uvs[k] for k in faces[0]]
            area=abs(sum(top[j][0]*top[(j+1)%3][1]-top[(j+1)%3][0]*top[j][1] for j in range(3)))/2
            self.assertGreater(area,0)

    def test_rejects_invalid_dimensions_or_unapproved_plate(self):
        for radii,plate in [((0,.047,.018),1),((.092,.047,.018),6),((float('nan'),.047,.018),2)]:
            with self.assertRaises(ValueError): scute_mesh_data(*radii,plate)


if __name__=='__main__': unittest.main()
