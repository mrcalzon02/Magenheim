"""Blender-independent regressions for the approved Capcrawler shield replacement."""
import importlib.util
import math
from collections import Counter
from pathlib import Path
import unittest

SPEC=importlib.util.spec_from_file_location('capcrawler_shield',Path(__file__).resolve().parents[1]/'tools/capcrawler_carapace_geometry.py')
g=importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(g)

class ShieldTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.vertices,cls.faces,cls.uvs=g.shield_mesh_data()

    def test_contract_and_original_anchor(self):
        self.assertEqual(g.ORIGIN,(0.,-.015,.31))
        self.assertEqual(g.SECTORS,64)
        self.assertTrue(g.CONTRACT.startswith('capcrawler-'))

    def test_closed_manifold_and_outward_orientation(self):
        edges=Counter()
        for face in self.faces:
            for a,b in zip(face,face[1:]+face[:1]):
                edges[tuple(sorted((a,b)))]+=1
        self.assertTrue(edges)
        self.assertEqual(set(edges.values()),{2})
        volume=sum(sum(self.vertices[a][0]*(self.vertices[b][1]*self.vertices[c][2]-self.vertices[c][1]*self.vertices[b][2])
                       +self.vertices[a][1]*(self.vertices[b][2]*self.vertices[c][0]-self.vertices[c][2]*self.vertices[b][0])
                       +self.vertices[a][2]*(self.vertices[b][0]*self.vertices[c][1]-self.vertices[c][0]*self.vertices[b][1])
                       for a,b,c in [(face[0],face[i],face[i+1]) for i in range(1,len(face)-1)]) for face in self.faces)/6
        self.assertGreater(volume,.01)

    def test_quality_and_approved_low_envelope(self):
        triangles=sum(len(f)-2 for f in self.faces)
        self.assertGreaterEqual(triangles,2500)
        self.assertGreater(triangles,1280) # former subdivision-3 ico sphere
        x=[v[0] for v in self.vertices];y=[v[1] for v in self.vertices];z=[v[2] for v in self.vertices]
        self.assertLessEqual(max(map(abs,x)),.36)
        self.assertLessEqual(max(map(abs,y)),.43)
        self.assertLessEqual(max(z)-min(z),.21)
        self.assertGreater(max(x)-min(x),.66)
        self.assertGreater(max(y)-min(y),.80)

    def test_fungal_scallops_are_physical(self):
        rim=1+(g.RADIAL_RINGS-1)*g.SECTORS
        radii=[math.hypot(self.vertices[rim+j][0]/.356,self.vertices[rim+j][1]/.425) for j in range(g.SECTORS)]
        self.assertGreater(max(radii)-min(radii),.07)
        self.assertLess(min(radii),.93)
        self.assertGreater(len(set(round(r,4) for r in radii)),12)

    def test_uv_islands_separate_and_have_nonzero_area(self):
        self.assertEqual(len(self.faces),len(self.uvs))
        top_count=g.SECTORS*g.RADIAL_RINGS
        rim_count=g.SECTORS
        for idx,(face,uv) in enumerate(zip(self.faces,self.uvs)):
            self.assertEqual(len(face),len(uv))
            for u,v in uv:
                self.assertTrue(math.isfinite(u) and math.isfinite(v))
                self.assertGreaterEqual(u,.04)
                self.assertLessEqual(u,.96)
                self.assertGreaterEqual(v,.04)
                self.assertLessEqual(v,.89)
            if idx<top_count:
                self.assertLess(max(u for u,v in uv),.48)
                self.assertLess(max(v for u,v in uv),.48)
            elif idx<top_count+rim_count:
                self.assertGreater(min(v for u,v in uv),.59)
            else:
                self.assertGreater(min(u for u,v in uv),.52)
                self.assertLess(max(v for u,v in uv),.48)
            area=sum(uv[k][0]*uv[(k+1)%len(uv)][1]-uv[(k+1)%len(uv)][0]*uv[k][1] for k in range(len(uv)))/2
            self.assertGreater(abs(area),1e-8)

    def test_finite_vertices_and_index_integrity(self):
        self.assertTrue(all(math.isfinite(c) for p in self.vertices for c in p))
        self.assertTrue(all(0<=idx<len(self.vertices) for f in self.faces for idx in f))
        self.assertTrue(all(len(set(f))==len(f) for f in self.faces))

if __name__=='__main__': unittest.main()
