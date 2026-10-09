"""Blender-free geometric and anatomical regression for the Ashmite cephalic shield."""
import math
import unittest
from collections import Counter
from tools.ashmite_head_geometry import (CONTRACT, ORIGIN, RINGS, SCALE, SIDES,
                                         head_mesh_data, head_uv)


def signed_volume(verts, faces):
    total = 0.
    for face in faces:
        a = verts[face[0]]
        for j in range(1, len(face)-1):
            b, c = verts[face[j]], verts[face[j+1]]
            total += (a[0]*(b[1]*c[2]-b[2]*c[1]) +
                      a[1]*(b[2]*c[0]-b[0]*c[2]) +
                      a[2]*(b[0]*c[1]-b[1]*c[0]))/6
    return total


class AshmiteHeadGeometryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.v, cls.f = head_mesh_data()

    def test_closed_manifold_and_outward_winding(self):
        edges = Counter()
        for f in self.f:
            self.assertGreaterEqual(len(set(f)), 3)
            for a, b in zip(f, f[1:]+f[:1]):
                edges[(a,b)] += 1
        self.assertEqual(len(self.v), RINGS*SIDES+2)
        self.assertEqual(len(self.f), (RINGS+1)*SIDES)
        self.assertEqual(sum(len(f)-2 for f in self.f), 2*RINGS*SIDES)
        self.assertTrue(all(edges[(b,a)]==n for (a,b),n in edges.items()))
        self.assertGreater(signed_volume(self.v,self.f), 0)

    def test_head_anatomical_envelope_and_eye_sockets(self):
        x=[v[0] for v in self.v]; y=[v[1] for v in self.v]; z=[v[2] for v in self.v]
        self.assertLessEqual(max(abs(n) for n in x), SCALE[0]*1.06)
        self.assertAlmostEqual(min(y),-SCALE[1]); self.assertAlmostEqual(max(y),SCALE[1])
        self.assertLessEqual(max(z),SCALE[2]*1.16)
        self.assertGreater(min(z),-SCALE[2]*.80)
        # Existing eyes at (+/- .047, .151, .085) remain lateral/front.
        for side in (-1,1):
            candidates=[v for v in self.v if abs(v[1]-(.151-ORIGIN[1]))<.005 and v[0]*side>.035]
            self.assertTrue(candidates)
            self.assertLess(min(abs(v[0]-side*.047)+abs(v[2]-.015) for v in candidates),.030)

    def test_bilateral_symmetry_and_readable_wedge(self):
        for i in range(RINGS):
            ring=self.v[1+i*SIDES:1+(i+1)*SIDES]
            for j in range(SIDES):
                other=ring[(SIDES//2-j)%SIDES]
                self.assertAlmostEqual(ring[j][0],-other[0],places=9)
                self.assertAlmostEqual(ring[j][1],other[1],places=9)
                self.assertAlmostEqual(ring[j][2],other[2],places=9)
        # Front width must narrow relative to the rear armor shoulder.
        def width(i):
            ring=self.v[1+i*SIDES:1+(i+1)*SIDES]
            return max(v[0] for v in ring)-min(v[0] for v in ring)
        self.assertGreater(width(7),width(17)*1.55)
        self.assertGreater(width(9),width(19)*2.0)

    def test_physical_brow_and_suture_relief(self):
        i=16
        ring=self.v[1+i*SIDES:1+(i+1)*SIDES]
        # Brow peaks at angles 47 and 133 degrees, shallow seam at 90.
        t=(i+.55)/(RINGS+.10)
        smooth=SCALE[2]*math.sin(math.pi*t)**.65*(1.07-.20*t)*math.sin(math.pi/4)
        self.assertGreater(ring[8][2],smooth+.002)
        self.assertGreater(ring[24][2],smooth+.002)
        self.assertLess(ring[16][2],SCALE[2])
        self.assertTrue(CONTRACT.startswith('ashmite-cephalic'))

    def test_uv_finite_bounded_and_distinct(self):
        uv=[head_uv(v) for v in self.v]
        self.assertTrue(all(math.isfinite(q) and 0<=q<=1 for p in uv for q in p))
        self.assertGreater(len(set((round(a,4),round(b,4)) for a,b in uv)),500)

    def test_invalid_dimensions_fail_closed(self):
        for scale in ((0,.07,.04),(-.08,.07,.04),(.08,math.nan,.04),(.08,.07,math.inf),(.08,.07),()):
            with self.assertRaises(ValueError): head_mesh_data(scale)


if __name__=='__main__': unittest.main()
