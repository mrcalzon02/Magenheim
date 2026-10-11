"""Offline regression suite for the eight existing Capcrawler leg pairs."""
import collections
import math
import sys
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]/'tools'))
import capcrawler_leg_geometry as geo


def triangles(face):
    for i in range(1,len(face)-1):
        yield face[0],face[i],face[i+1]


def volume(vertices,faces):
    v=0.
    for face in faces:
        for a,b,c in triangles(face):
            p,q,r=vertices[a],vertices[b],vertices[c]
            v+=(p[0]*(q[1]*r[2]-q[2]*r[1])+
                p[1]*(q[2]*r[0]-q[0]*r[2])+
                p[2]*(q[0]*r[1]-q[1]*r[0]))/6
    return v


class LegGeometryTests(unittest.TestCase):
    def test_all_sixteen_segments_have_consistent_topology(self):
        for side in (-1,1):
            for index in range(1,5):
                for section in ('A','B'):
                    vertices,faces,uvs,_=geo.leg_mesh_data(side,index,section)
                    self.assertEqual(len(vertices),142)
                    self.assertEqual(len(faces),160)
                    self.assertEqual(sum(len(f)-2 for f in faces),280)
                    self.assertEqual(len(uvs),len(faces))

    def test_anchors_preserved_at_endcap_centers(self):
        for side in (-1,1):
            for index in range(1,5):
                hip,knee,foot=geo.anchors(side,index)
                for section,expected in (('A',(hip,knee)),('B',(knee,foot))):
                    vertices,_,_,origin=geo.leg_mesh_data(side,index,section)
                    for point,target in zip(vertices[-2:],expected):
                        self.assertTrue(all(abs(point[k]+origin[k]-target[k])<1e-10
                                            for k in range(3)))

    def test_closed_outward_winding_and_positive_volume(self):
        for side in (-1,1):
            for index in range(1,5):
                for section in ('A','B'):
                    vertices,faces,_,_=geo.leg_mesh_data(side,index,section)
                    edges=collections.defaultdict(list)
                    for face in faces:
                        for a,b in zip(face,face[1:]+face[:1]):
                            edges[tuple(sorted((a,b)))].append((a,b))
                    self.assertTrue(all(len(e)==2 and e[0]==e[1][::-1]
                                        for e in edges.values()))
                    self.assertGreater(volume(vertices,faces),0.)

    def test_non_degenerate_and_bounded_geometry(self):
        for side in (-1,1):
            for index in range(1,5):
                for section in ('A','B'):
                    vertices,faces,_,origin=geo.leg_mesh_data(side,index,section)
                    for face in faces:
                        self.assertEqual(len(face),len(set(face)))
                        for a,b,c in triangles(face):
                            p,q,r=vertices[a],vertices[b],vertices[c]
                            ab=[q[k]-p[k] for k in range(3)]
                            ac=[r[k]-p[k] for k in range(3)]
                            cr=(ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0])
                            self.assertGreater(sum(k*k for k in cr),1e-13)
                    world=[tuple(p[k]+origin[k] for k in range(3)) for p in vertices]
                    self.assertLess(max(abs(p[0]) for p in world),.48)
                    self.assertGreater(min(p[2] for p in world),.005)

    def test_uv_islands_do_not_overlap_and_have_valid_loops(self):
        for side in (-1,1):
            for index in range(1,5):
                for section in ('A','B'):
                    _,faces,uvs,_=geo.leg_mesh_data(side,index,section)
                    wall=uvs[:geo.SECTORS*(geo.RINGS-1)]
                    caps=uvs[geo.SECTORS*(geo.RINGS-1):]
                    self.assertGreater(min(v for face in wall for u,v in face),.35)
                    self.assertLess(max(v for face in caps for u,v in face),.34)
                    for face,face_uv in zip(faces,uvs):
                        self.assertEqual(len(face),len(face_uv))
                        for u,v in face_uv:
                            self.assertTrue(math.isfinite(u) and math.isfinite(v))
                            self.assertGreaterEqual(u,0.)
                            self.assertLessEqual(u,1.)
                            self.assertGreaterEqual(v,0.)
                            self.assertLessEqual(v,1.)

    def test_mirrored_leg_identity_and_flaring(self):
        for index in range(1,5):
            for section in ('A','B'):
                left,_,_,lo=geo.leg_mesh_data(-1,index,section)
                right,_,_,ro=geo.leg_mesh_data(1,index,section)
                for a,b in zip(left,right):
                    self.assertAlmostEqual(a[0]+lo[0],-(b[0]+ro[0]),places=9)
                    self.assertAlmostEqual(a[1]+lo[1],b[1]+ro[1],places=9)
                    self.assertAlmostEqual(a[2]+lo[2],b[2]+ro[2],places=9)
                self.assertGreater(len(left),100)

    def test_invalid_identity_fails_closed(self):
        for side,index in ((0,1),(1,0),(-1,5),(2,2)):
            with self.assertRaises(ValueError): geo.anchors(side,index)
        with self.assertRaises(ValueError): geo.leg_mesh_data(1,1,'C')


if __name__=='__main__': unittest.main()
