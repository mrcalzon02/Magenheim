"""Fail-closed checks for Ashmite's thorax/abdomen primary silhouette."""
import importlib.util
from collections import Counter
from math import isfinite
from pathlib import Path
import unittest

MODULE=Path(__file__).resolve().parents[1]/'tools/ashmite_body_geometry.py'
spec=importlib.util.spec_from_file_location('ashmite_body_geometry',MODULE)
g=importlib.util.module_from_spec(spec);spec.loader.exec_module(g)

class AshmiteBodyGeometry(unittest.TestCase):
    def test_approved_body_identity(self):
        self.assertEqual(set(g.BODIES),{'thorax','abdomen'})
        self.assertEqual(g.specification('thorax')[0],(0.,0.,.085))
        self.assertEqual(g.specification('abdomen')[0],(0.,-.105,.080))
        with self.assertRaises(ValueError):g.body_mesh_data('horned-worm')

    def test_topology_donor_floor_and_manifold(self):
        for kind in g.BODIES:
            v,f,uv=g.body_mesh_data(kind)
            self.assertEqual(len(v),g.RINGS*g.SIDES+2)
            self.assertEqual(len(f),g.RINGS*g.SIDES+g.SIDES)
            self.assertEqual(sum(len(p)-2 for p in f),2*g.RINGS*g.SIDES)
            # Original two level-4 ico-spheres each had 5120 triangles.
            self.assertGreaterEqual(sum(len(p)-2 for p in f),5120)
            edges=Counter(tuple(sorted((p[i],p[(i+1)%len(p)]))) for p in f for i in range(len(p)))
            self.assertTrue(all(n==2 for n in edges.values()))
            self.assertEqual(len(uv),len(f))
            self.assertTrue(all(len(a)==len(b) for a,b in zip(f,uv)))

    def test_positive_volume_and_non_degenerate_faces(self):
        for kind in g.BODIES:
            v,f,_=g.body_mesh_data(kind)
            volume=0.
            for face in f:
                for a,b,c in [(face[0],face[k],face[k+1]) for k in range(1,len(face)-1)]:
                    p,q,r=v[a],v[b],v[c]
                    cross=(q[1]*r[2]-q[2]*r[1],q[2]*r[0]-q[0]*r[2],q[0]*r[1]-q[1]*r[0])
                    volume+=sum(p[i]*cross[i] for i in range(3))/6
            self.assertGreater(volume,.0002)

    def test_silhouette_and_hip_clearance(self):
        for kind in g.BODIES:
            v,_,_=g.body_mesh_data(kind)
            origin,scale,_=g.specification(kind)
            self.assertAlmostEqual(min(p[1] for p in v),-scale[1])
            self.assertAlmostEqual(max(p[1] for p in v),scale[1])
            self.assertGreater(max(abs(p[0]) for p in v),scale[0]*.85)
            self.assertLess(max(abs(p[0]) for p in v),scale[0]*1.12)
            self.assertLess(max(p[2] for p in v)+origin[2],.145)
            self.assertGreater(min(p[2] for p in v)+origin[2],.012)

    def test_bilateral_symmetry_and_seam_uv(self):
        for kind in g.BODIES:
            v,f,uv=g.body_mesh_data(kind)
            for i in range(g.RINGS):
                for j in range(g.SIDES):
                    a=v[1+i*g.SIDES+j];b=v[1+i*g.SIDES+(g.SIDES//2-j)%g.SIDES]
                    self.assertAlmostEqual(a[0],-b[0],places=9)
                    self.assertAlmostEqual(a[1],b[1],places=9)
                    self.assertAlmostEqual(a[2],b[2],places=9)
            self.assertTrue(all(isfinite(x) and 0<=x<=1 for poly in uv for pair in poly for x in pair))
            seam=uv[g.SIDES+g.SIDES-1]
            self.assertAlmostEqual(seam[2][0],g.SIDE_U[1])
            self.assertAlmostEqual(seam[3][0],g.SIDE_U[1])

    def test_physical_fissures_and_sternites(self):
        for kind in g.BODIES:
            v,_,_=g.body_mesh_data(kind)
            # Along the same lateral meridian, there must be tangible local
            # recesses at two heat fractures rather than texture-only lines.
            radial=[v[1+i*g.SIDES][0] for i in range(g.RINGS)]
            self.assertTrue(any(radial[i]<(radial[i-2]+radial[i+2])/2-.001 for i in range(3,g.RINGS-3)))
            # Ventral plate breaks are not the same silhouette as dorsal scutes.
            under=[v[1+i*g.SIDES+3*g.SIDES//4][2] for i in range(g.RINGS)]
            self.assertGreater(max(under)-min(under),.025)

if __name__=='__main__':unittest.main()
