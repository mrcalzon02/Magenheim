"""Check physical rear plate overlap and lateral buttress relief, without Blender."""
import importlib.util
from pathlib import Path
import unittest
from math import isfinite

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('ashmite_scute_geometry',ROOT/'tools/ashmite_scute_geometry.py')
g=importlib.util.module_from_spec(spec);spec.loader.exec_module(g)

class AshmiteScuteOverlap(unittest.TestCase):
    def test_trailing_lips_have_physical_relief(self):
        for plate in range(1,6):
            rx=.075 if plate>3 else .092; ry=.047; rz=.018
            vertices,_=g.scute_mesh_data(rx,ry,rz,plate)
            ring=9
            rear=vertices[2+ring*g.SIDES+3*g.SIDES//4][2]
            front=vertices[2+ring*g.SIDES+g.SIDES//4][2]
            self.assertGreater(rear-front,rz*.075)
    def test_lateral_buttresses_reinforce_shell_flanks(self):
        for plate in range(1,6):
            vertices,_=g.scute_mesh_data(.092,.047,.018,plate)
            ring=6
            left=vertices[2+ring*g.SIDES+g.SIDES//2][2]
            right=vertices[2+ring*g.SIDES][2]
            diagonal=max(vertices[2+ring*g.SIDES+j][2] for j in (8,24,40,56))
            self.assertTrue(isfinite(left) and isfinite(right))
            self.assertGreater(min(left,right)-diagonal,.018*.04)
    def test_original_mesh_identity_and_uv_bounds_unchanged(self):
        self.assertEqual(g.CONTRACT,'ashmite-five-heat-fractured-scutella-16x64')
        for plate in range(1,6):
            v,f=g.scute_mesh_data(.092,.047,.018,plate)
            self.assertEqual((len(v),len(f),sum(len(p)-2 for p in f)),(1026,1088,2048))
            self.assertTrue(all(0<=u<=1 and 0<=w<=1 for u,w in (g.scute_uv(p,.092,.047) for p in v)))
if __name__=='__main__':unittest.main()
