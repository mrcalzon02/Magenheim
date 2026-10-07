"""Blender-free anatomical regression for Shelf Lurker sensory crown curvature."""
import ast
from math import sin, pi
from pathlib import Path
import unittest

AUTHOR=Path(__file__).resolve().parents[1]/'tools/author-underworld-shelf-lurker.py'

def frond_path():
    module=ast.parse(AUTHOR.read_text(encoding='utf-8'))
    fn=next(n for n in module.body if isinstance(n,ast.FunctionDef) and n.name=='sensory_frond_path')
    namespace={'sin':sin,'pi':pi}
    exec(compile(ast.Module(body=[fn],type_ignores=[]),str(AUTHOR),'exec'),namespace)
    return namespace['sensory_frond_path']

class SensoryCrownTests(unittest.TestCase):
    def test_endpoints_preserved(self):
        a=(-.121,.40,1.52); b=(-.22,.58,1.18)
        points=frond_path()(a,b,(-.065,.105,.035))
        self.assertEqual(len(points),9)
        for x,y in zip(points[0],a): self.assertAlmostEqual(x,y)
        for x,y in zip(points[-1],b): self.assertAlmostEqual(x,y)

    def test_curvature_and_downward_reach(self):
        a=(-.121,.40,1.52); b=(-.22,.58,1.18)
        points=frond_path()(a,b,(-.065,.105,.035))
        middle=points[4]
        chord=tuple((a[k]+b[k])/2 for k in range(3))
        self.assertGreater(sum((middle[k]-chord[k])**2 for k in range(3))**.5,.07)
        self.assertTrue(all(points[i+1][2]<points[i][2] for i in range(8)))

    def test_mirrored_antennae_and_center_frond(self):
        f=frond_path()
        left=f((-.121,.40,1.52),(-.22,.58,1.18),(-.065,.105,.035))
        right=f((.121,.40,1.52),(.22,.58,1.18),(.065,.105,.035))
        for a,b in zip(left,right):
            self.assertAlmostEqual(a[0],-b[0])
            self.assertAlmostEqual(a[1],b[1])
            self.assertAlmostEqual(a[2],b[2])
        center=f((0,.52,1.50),(0,.70,1.16),(0,.105,.035))
        self.assertTrue(all(abs(p[0])<1e-9 for p in center))

    def test_author_uses_curved_mesh_not_straight_cylinders(self):
        tree=ast.parse(AUTHOR.read_text(encoding='utf-8'))
        self.assertTrue(any(isinstance(n,ast.FunctionDef) and n.name=='sensory_frond' for n in tree.body))
        self.assertIn("keep(sensory_frond(f'ShelfLurker_SensoryFrond_",AUTHOR.read_text(encoding='utf-8'))
        self.assertNotIn("keep(seg(f'ShelfLurker_SensoryFrond_",AUTHOR.read_text(encoding='utf-8'))

if __name__=='__main__': unittest.main()
