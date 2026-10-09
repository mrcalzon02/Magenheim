"""Blender-free anatomy, topology, silhouette and UV tests for Ashmite scute ridges."""
import importlib.util
import math
import unittest
from collections import Counter
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / 'tools/ashmite_sulfur_ridge_geometry.py'
spec = importlib.util.spec_from_file_location('ashmite_sulfur_ridge_geometry', SCRIPT)
geo = importlib.util.module_from_spec(spec)
spec.loader.exec_module(geo)


def volume(vertices, faces):
    total = 0.
    for face in faces:
        a = vertices[face[0]]
        for k in range(1, len(face)-1):
            b, c = vertices[face[k]], vertices[face[k+1]]
            cross = (b[1]*c[2]-b[2]*c[1], b[2]*c[0]-b[0]*c[2], b[0]*c[1]-b[1]*c[0])
            total += sum(a[i]*cross[i] for i in range(3))/6
    return total


class AshmiteSulfurRidgeTests(unittest.TestCase):
    def test_all_ten_ridges_are_closed_and_outward(self):
        for index in range(1,6):
            for side in (-1,1):
                with self.subTest(index=index,side=side):
                    vertices, faces, uvs = geo.ridge_mesh_data(side,index)
                    self.assertEqual(len(vertices),geo.RINGS*geo.SIDES+2)
                    self.assertEqual(sum(len(f)-2 for f in faces),2*geo.RINGS*geo.SIDES)
                    edges=Counter(tuple(sorted((f[i],f[(i+1)%len(f)]))) for f in faces for i in range(len(f)))
                    self.assertTrue(all(count==2 for count in edges.values()))
                    self.assertGreater(volume(vertices,faces),0)
                    self.assertEqual(len(uvs),len(faces))
                    self.assertTrue(all(len(f)==len(u) for f,u in zip(faces,uvs)))
                    self.assertTrue(all(math.isfinite(x) and 0<=x<=1 for face in uvs for uv in face for x in uv))

    def test_original_scute_anchors_and_bilateral_reflection(self):
        for index in range(1,6):
            left,right=geo.anchors(-1,index),geo.anchors(1,index)
            for a,b in zip(left,right):
                self.assertAlmostEqual(a[0],-b[0]); self.assertAlmostEqual(a[1],b[1]); self.assertAlmostEqual(a[2],b[2])
            for side in (-1,1):
                v,_,_=geo.ridge_mesh_data(side,index)
                start,end=geo.anchors(side,index)
                self.assertEqual(v[-2],(0.,0.,0.))
                self.assertTrue(all(abs(v[-1][k]-(end[k]-start[k]))<1e-9 for k in range(3)))

    def test_physical_dorsal_crest_and_two_seams(self):
        for side in (-1,1):
            v,_,_=geo.ridge_mesh_data(side,3)
            # Dorsal (+Z) ring peak stands above ventral surface.
            for i in (1,3,6,9,11):
                ring=v[i*geo.SIDES:(i+1)*geo.SIDES]
                self.assertGreater(max(p[2] for p in ring)-min(p[2] for p in ring),.005)
            # Both narrow seam valleys physically reduce the radial envelope.
            def radial_extent(i):
                ring=v[i*geo.SIDES:(i+1)*geo.SIDES]
                return max(p[2] for p in ring)-min(p[2] for p in ring)
            self.assertLess(radial_extent(4),radial_extent(3))
            self.assertLess(radial_extent(8),radial_extent(7))

    def test_length_and_dorsal_silhouette_preserved(self):
        for index in range(1,6):
            for side in (-1,1):
                v,_,_=geo.ridge_mesh_data(side,index)
                start,end=geo.anchors(side,index)
                self.assertTrue(all(math.dist(p,(0,0,0))<.055 for p in v))
                self.assertLess(max(p[2] for p in v)+start[2],.18)
                self.assertAlmostEqual(math.dist(start,end),math.sqrt(.04**2+.01**2+.007**2))

    def test_invalid_ridge_rejected(self):
        for args in ((0,1),(-1,0),(1,6)):
            with self.assertRaises(ValueError):geo.anchors(*args)


if __name__=='__main__':
    unittest.main()
