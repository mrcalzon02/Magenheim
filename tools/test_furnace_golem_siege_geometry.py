"""Blender-free Furnace Golem siege gauntlet geometry regression tests."""
import unittest
from collections import Counter
from furnace_golem_siege_geometry import make_siege_fist,make_siege_tooth,FIST_SECTORS,TOOTH_SECTORS

def volume(vertices,faces):
    v=0.
    for f in faces:
        for i in range(1,len(f)-1):
            a,b,c=(vertices[j] for j in (f[0],f[i],f[i+1]))
            v+=(a[0]*(b[1]*c[2]-b[2]*c[1])+a[1]*(b[2]*c[0]-b[0]*c[2])+a[2]*(b[0]*c[1]-b[1]*c[0]))/6
    return v

class SiegeTests(unittest.TestCase):
    def check(self,fn):
        verts,faces,uv,regions=fn()
        self.assertEqual(len(faces),len(uv));self.assertEqual(len(faces),len(regions))
        self.assertTrue(all(len(f)==len(t) for f,t in zip(faces,uv)))
        self.assertTrue(all(0<=u<=1 and 0<=v<=1 for face in uv for u,v in face))
        edges=Counter(tuple(sorted((a,b))) for f in faces for a,b in zip(f,f[1:]+f[:1]))
        self.assertEqual(set(edges.values()),{2})
        self.assertGreater(volume(verts,faces),.00001)
        self.assertTrue(all(len(set(f))==len(f) for f in faces))
        self.assertIn('strike',regions);self.assertIn('wrist',regions)
        return verts,faces,regions
    def test_fist_closed_manifold(self):self.check(make_siege_fist)
    def test_tooth_closed_manifold(self):self.check(make_siege_tooth)
    def test_fist_brow_and_forward_hammer_mass(self):
        verts,_,_=self.check(make_siege_fist)
        back=max(abs(x) for x,y,z in verts if abs(y+.15)<1e-8)
        front=max(abs(x) for x,y,z in verts if abs(y-.135)<1e-8)
        self.assertGreater(front,back*1.5)
        self.assertGreater(max(z for x,y,z in verts if y>.12),.15)
    def test_tooth_tip_curves_down_and_tapers(self):
        verts,_,_=self.check(make_siege_tooth)
        root=[(x,z) for x,y,z in verts if abs(y)<1e-8]
        tip=[(x,z) for x,y,z in verts if abs(y-.21)<1e-8]
        self.assertLess(max(x for x,z in tip),max(x for x,z in root)*.1)
        self.assertLess(sum(z for x,z in tip)/len(tip),sum(z for x,z in root)/len(root)-.03)
    def test_component_triangle_counts(self):
        for fn,n in ((make_siege_fist,FIST_SECTORS),(make_siege_tooth,TOOTH_SECTORS)):
            _,faces,_=self.check(fn)
            self.assertEqual(sum(len(f)-2 for f in faces),14*n if fn is make_siege_fist else 12*n)
    def test_invalid_dimensions(self):
        with self.assertRaises(ValueError):make_siege_fist(0,.2)
        with self.assertRaises(ValueError):make_siege_tooth(-.01,.2)

if __name__=='__main__':unittest.main()
