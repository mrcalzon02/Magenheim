"""Offline regression checks for the six sculpted Ashmite hip joints."""
import math
import unittest
from ashmite_joint_geometry import RINGS,SIDES,CONTRACT,SCALE,anchor,joint_mesh_data

class AshmiteJointGeometry(unittest.TestCase):
    def setUp(self):
        self.vertices,self.faces,self.uvs=joint_mesh_data()
    def test_original_hip_anchors(self):
        self.assertEqual(CONTRACT,'ashmite-six-folded-coxal-membranes-v1')
        for pair,y in enumerate((.065,0.,-.070),1):
            for side in (-1,1):
                self.assertEqual(anchor(side,pair),(side*.068,y,.070))
        for side,pair in ((0,1),(1,0),(-1,4),(2,2)):
            with self.assertRaises(ValueError): anchor(side,pair)
    def test_closed_winding_and_triangle_count(self):
        self.assertEqual(len(self.vertices),2+(RINGS-1)*SIDES)
        self.assertEqual(len(self.faces),RINGS*SIDES)
        self.assertEqual(sum(len(f)-2 for f in self.faces),2*(RINGS-1)*SIDES)
        edges={}
        for f in self.faces:
            for a,b in zip(f,f[1:]+f[:1]):
                edges.setdefault(tuple(sorted((a,b))),[]).append((a,b))
        self.assertTrue(all(len(v)==2 and v[0]==v[1][::-1] for v in edges.values()))
    def test_positive_volume_and_envelope(self):
        volume=0.
        for f in self.faces:
            for i in range(1,len(f)-1):
                a,b,c=(self.vertices[f[k]] for k in (0,i,i+1))
                volume+=(a[0]*(b[1]*c[2]-b[2]*c[1])
                         +a[1]*(b[2]*c[0]-b[0]*c[2])
                         +a[2]*(b[0]*c[1]-b[1]*c[0]))/6
        self.assertGreater(volume,0)
        for axis in range(3):
            self.assertLessEqual(max(abs(v[axis]) for v in self.vertices),
                                 SCALE[axis]*1.04)
    def test_physical_compression_folds(self):
        def radial(i):
            p=self.vertices[2+(i-1)*SIDES]
            return abs(p[1])/(SCALE[1]*math.sin(math.pi*i/RINGS))
        self.assertLess(radial(6),radial(10)-.055)
        self.assertLess(radial(14),radial(10)-.055)
    def test_uv_area_and_seam(self):
        self.assertEqual(len(self.uvs),len(self.faces))
        for f,uvs in zip(self.faces,self.uvs):
            self.assertEqual(len(f),len(uvs))
            self.assertTrue(all(0<u<1 and 0<v<1 for u,v in uvs))
            area=sum(uvs[i][0]*uvs[(i+1)%len(uvs)][1]-
                     uvs[(i+1)%len(uvs)][0]*uvs[i][1]
                     for i in range(len(uvs)))/2
            self.assertGreater(abs(area),1e-9)
        strip=self.uvs[SIDES:-SIDES]
        self.assertAlmostEqual(strip[SIDES-1][2][1],.95)
        self.assertAlmostEqual(strip[SIDES][0][1],.05)
if __name__=='__main__': unittest.main()
