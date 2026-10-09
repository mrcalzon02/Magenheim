"""Blender-free QA for the existing Ashmite's two compound eyes."""
import importlib.util
import math
from collections import Counter
from pathlib import Path
import unittest

MODULE=Path(__file__).resolve().parents[1]/'tools/ashmite_eye_geometry.py'
spec=importlib.util.spec_from_file_location('ashmite_eye_geometry',MODULE)
g=importlib.util.module_from_spec(spec);spec.loader.exec_module(g)


class EyeGeometryTests(unittest.TestCase):
    def test_eye_identity_and_anchors(self):
        self.assertEqual(g.anchor(-1),(-.047,.151,.085))
        self.assertEqual(g.anchor(1),(.047,.151,.085))
        with self.assertRaises(ValueError): g.anchor(0)
        with self.assertRaises(ValueError): g.eye_mesh_data('L')

    def test_closed_manifold_and_outward_orientation(self):
        for side in (-1,1):
            v,f,_=g.eye_mesh_data(side)
            edges=Counter(tuple(sorted((a,b))) for face in f for a,b in zip(face,face[1:]+face[:1]))
            self.assertTrue(edges)
            self.assertTrue(all(n==2 for n in edges.values()))
            signed=0.
            for face in f:
                a=v[face[0]]
                for i in range(1,len(face)-1):
                    b,c=v[face[i]],v[face[i+1]]
                    signed += (a[0]*(b[1]*c[2]-b[2]*c[1])+
                               a[1]*(b[2]*c[0]-b[0]*c[2])+
                               a[2]*(b[0]*c[1]-b[1]*c[0]))/6
            self.assertGreater(signed,0.)

    def test_topology_and_uv(self):
        v,f,uv=g.eye_mesh_data(1)
        self.assertEqual(len(v),g.RINGS*g.SIDES+2)
        self.assertEqual(sum(len(p)-2 for p in f),2*g.RINGS*g.SIDES)
        self.assertEqual(len(f),len(uv))
        self.assertTrue(all(len(face)==len(mapping) for face,mapping in zip(f,uv)))
        self.assertTrue(all(math.isfinite(a) and 0<=a<=1 for poly in uv for pair in poly for a in pair))
        # No UV seam stretching across almost the entire atlas.
        self.assertLess(max(max(pair[0] for pair in poly)-min(pair[0] for pair in poly)
                            for poly in uv if len(poly)==4),.05)

    def test_bilateral_eye_geometry(self):
        lv,lf,_=g.eye_mesh_data(-1);rv,rf,_=g.eye_mesh_data(1)
        self.assertEqual(lf,rf)
        self.assertEqual(lv,rv)
        self.assertEqual(g.anchor(-1)[0],-g.anchor(1)[0])

    def test_unchanged_envelope_and_placement(self):
        v,_,_=g.eye_mesh_data(1)
        self.assertLessEqual(max(abs(x) for x,y,z in v),.014)
        self.assertLessEqual(max(abs(z) for x,y,z in v),.013)
        self.assertLessEqual(max(abs(y) for x,y,z in v),.0091)
        self.assertGreater(max(x for x,y,z in v),.011)
        self.assertGreater(max(z for x,y,z in v),.010)

    def test_socket_rim_groove_and_facets_are_physical(self):
        v,_,_=g.eye_mesh_data(1)
        def rad(i,j=0):
            x,y,z=v[1+i*g.SIDES+j];return math.hypot(x,z/.92)
        # Relative to a smooth ellipsoid: physical rim and groove change profile.
        self.assertGreater(rad(4),rad(6)*.93)
        # Twelve-sector radial relief, not a texture-only optical effect.
        i=10
        self.assertGreater(rad(i,0)-rad(i,2),.00015)

if __name__=='__main__':unittest.main()
