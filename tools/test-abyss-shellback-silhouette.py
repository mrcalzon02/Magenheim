"""Blender-independent projected-support regressions for elite Shellback LOD0."""
import importlib.util
from pathlib import Path
import unittest

spec=importlib.util.spec_from_file_location("shellback_lod",Path(__file__).with_name("abyss_shellback_runtime_lod.py"))
lod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(lod)

class SilhouetteGuardTests(unittest.TestCase):
    def setUp(self):
        self.cube=[(x,y,z) for x in (-1.,1.) for y in (-1.,1.) for z in (-1.,1.)]
    def test_same(self):
        self.assertTrue(lod.projected_support_guard(self.cube,self.cube))
    def test_interior_vertex_irrelevant(self):
        self.assertTrue(lod.projected_support_guard(self.cube,self.cube+[(0.,0.,0.)]))
    def test_lost_distinct_outer_tip_rejected(self):
        with self.assertRaisesRegex(RuntimeError,"projected silhouette support drift"):
            lod.projected_support_guard(self.cube+[(2.,2.,2.)],self.cube)
    def test_diagonal_shrink_rejected(self):
        with self.assertRaisesRegex(RuntimeError,"projected silhouette support drift"):
            lod.projected_support_guard(self.cube,[(x*.95,y*.95,z*.95) for x,y,z in self.cube])
    def test_submillimeter_change_accepted(self):
        self.assertTrue(lod.projected_support_guard(self.cube,[(x*.9999,y*.9999,z*.9999) for x,y,z in self.cube]))
    def test_empty_geometry_rejected(self):
        with self.assertRaisesRegex(RuntimeError,"missing vertices"):
            lod.projected_support_guard([],self.cube)
    def test_too_few_directions_rejected(self):
        with self.assertRaises(ValueError):
            lod.projected_support_guard(self.cube,self.cube,directions=2)

if __name__=="__main__":unittest.main()
