"""Blender-free regression of Shelf Lurker fungal gill geometry."""
import ast
import sys
from math import pi, sin, cos, hypot
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT/'tools'))
from shelf_lurker_gill_geometry import shelf_gill_path

class ShelfGillTests(unittest.TestCase):
    def test_endpoints_and_rings(self):
        a=(.07,0,1.678); b=(.429,0,1.674)
        points=shelf_gill_path(a,b)
        self.assertEqual(len(points),9)
        for x,y in zip(points[0],a): self.assertAlmostEqual(x,y)
        for x,y in zip(points[-1],b): self.assertAlmostEqual(x,y)

    def test_sag_bow_and_reach(self):
        p=shelf_gill_path((.07,0,1.678),(.429,0,1.674))
        self.assertAlmostEqual(p[4][1],.018)
        self.assertLess(p[4][2],min(p[0][2],p[-1][2])-.025)
        self.assertTrue(all(p[i+1][0]>p[i][0] for i in range(8)))

    def test_six_gills_remain_separated(self):
        paths=[]
        for i in range(6):
            a=2*pi*i/6
            d=(cos(a),sin(a))
            paths.append(shelf_gill_path((d[0]*.07,d[1]*.05,1.678),
                                         (d[0]*.55*.78,d[1]*.34*.78,1.674)))
        for i in range(6):
            a,b=paths[i][7],paths[(i+1)%6][7]
            self.assertGreater(hypot(a[0]-b[0],a[1]-b[1]),.18)

    def test_invalid_geometry_rejected(self):
        with self.assertRaises(ValueError): shelf_gill_path((0,0,0),(.01,0,0))
        with self.assertRaises(ValueError): shelf_gill_path((0,0,0),(1,0,0),5)

    def test_author_and_verifier_contract(self):
        author=(ROOT/'tools/author-underworld-shelf-lurker.py').read_text()
        verifier=(ROOT/'tools/verify-underworld-shelf-lurker.py').read_text()
        ast.parse(author); ast.parse(verifier)
        self.assertIn("keep(shelf_gill(f'ShelfLurker_ShelfGill_",author)
        self.assertNotIn("keep(seg(f'ShelfLurker_ShelfGill_",author)
        self.assertIn("len(mesh.vertices)!=204 or len(mesh.polygons)!=194",verifier)
        self.assertIn("non-manifold gill",verifier)
        self.assertIn("swept-scalloped-lamella-17x12",verifier)
        self.assertIn("scalloped gill margins collapsed",verifier)
        self.assertIn("gill dorsal midrib collapsed",verifier)

    def test_scallop_and_vein_are_geometry(self):
        source=(ROOT/'tools/shelf_lurker_gill_geometry.py').read_text()
        self.assertIn("sides, steps = 12, 16",source)
        self.assertIn("sin(6*pi*t)",source)
        self.assertIn("max(0.,sin(a))**8",source)

if __name__=='__main__': unittest.main()
