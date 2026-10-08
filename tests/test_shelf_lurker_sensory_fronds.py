"""Blender-free anatomical regression for Shelf Lurker sensory crown curvature."""
import ast
from math import sin, cos, pi, sqrt
from pathlib import Path
import unittest

AUTHOR=Path(__file__).resolve().parents[1]/'tools/author-underworld-shelf-lurker.py'

def frond_path():
    module=ast.parse(AUTHOR.read_text(encoding='utf-8'))
    fn=next(n for n in module.body if isinstance(n,ast.FunctionDef) and n.name=='sensory_frond_path')
    namespace={'sin':sin,'pi':pi}
    exec(compile(ast.Module(body=[fn],type_ignores=[]),str(AUTHOR),'exec'),namespace)
    return namespace['sensory_frond_path']


def frond_frames():
    module=ast.parse(AUTHOR.read_text(encoding='utf-8'))
    fn=next(n for n in module.body if isinstance(n,ast.FunctionDef) and n.name=='sensory_frond_frames')
    namespace={'sqrt':sqrt}
    exec(compile(ast.Module(body=[fn],type_ignores=[]),str(AUTHOR),'exec'),namespace)
    return namespace['sensory_frond_frames']

def frond_uv():
    module=ast.parse(AUTHOR.read_text(encoding='utf-8'))
    fn=next(n for n in module.body if isinstance(n,ast.FunctionDef) and n.name=='sensory_frond_uv')
    namespace={'sin':sin,'cos':cos,'pi':pi}
    exec(compile(ast.Module(body=[fn],type_ignores=[]),str(AUTHOR),'exec'),namespace)
    return namespace['sensory_frond_uv']

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

    def test_all_five_fronds_keep_continuous_cross_section_frames(self):
        path=frond_path(); frames=frond_frames()
        for i,(x,y,z) in enumerate(((-.22,.58,1.52),(.22,.58,1.52),
                                    (-.30,.42,1.48),(.30,.42,1.48),(0,.70,1.50)),1):
            bow=(.065 if x>0 else -.065 if x<0 else 0,.105,.035)
            centers=path((x*.55,y-.18,z),(x,y,z-.34),bow)
            rings=frames(centers)
            self.assertEqual(len(rings),9)
            for j in range(8):
                dot=sum(a*b for a,b in zip(rings[j][0],rings[j+1][0]))
                self.assertGreater(dot,.866,f'Frond {i} ring {j}: abrupt twist')
            for axis,other in rings:
                self.assertAlmostEqual(sum(v*v for v in axis),1,places=7)
                self.assertAlmostEqual(sum(v*v for v in other),1,places=7)
                self.assertAlmostEqual(sum(a*b for a,b in zip(axis,other)),0,places=7)

    def test_degenerate_reference_rejected_without_world_axis_switch(self):
        with self.assertRaises(ValueError):
            frond_frames()([(i*.1,0,0) for i in range(9)])

    def test_blender_adapter_uses_stable_frames(self):
        source=AUTHOR.read_text(encoding='utf-8')
        self.assertIn('frames=sensory_frond_frames([tuple(p) for p in centers])',source)
        self.assertIn("obj['magenheim_sensory_frame']='species-x-continuous'",source)
        self.assertNotIn('if abs(tangent.y)<.9',source)

    def test_end_cap_uvs_are_planar_and_have_nonzero_texture_area(self):
        mapper=frond_uv()
        for ring in (0,8):
            points=[mapper(ring*12+j,True,False) for j in range(12)]
            twice_area=sum(points[j][0]*points[(j+1)%12][1]
                           -points[(j+1)%12][0]*points[j][1] for j in range(12))
            self.assertGreater(abs(twice_area)/2,.59)
            self.assertTrue(all(0<=v<=1 for uv in points for v in uv))

    def test_all_96_side_quads_have_continuous_uv_seams(self):
        mapper=frond_uv()
        for ring in range(8):
            for angular in range(12):
                following=(angular+1)%12
                seam=angular==11
                indices=(ring*12+angular,ring*12+following,
                         (ring+1)*12+following,(ring+1)*12+angular)
                coords=[mapper(i,False,seam) for i in indices]
                self.assertLess(max(u for u,v in coords)-min(u for u,v in coords),.09)
                self.assertAlmostEqual(max(v for u,v in coords)-min(v for u,v in coords),.125)
                self.assertTrue(all(0<=value<=1 for uv in coords for value in uv))

    def test_blender_adapter_uses_noncollapsed_cap_uvs(self):
        source=AUTHOR.read_text(encoding='utf-8')
        self.assertIn('sensory_frond_uv(idx,len(poly.vertices)>4,seam,sides,steps)',source)
        verifier=(AUTHOR.parents[0]/'verify-underworld-shelf-lurker.py').read_text(encoding='utf-8')
        self.assertIn('sensory crown end-cap UV island collapsed',verifier)

if __name__=='__main__': unittest.main()
