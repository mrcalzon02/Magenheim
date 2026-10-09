"""Blender-free regression tests for Shelf Lurker's radial fungal growth UVs."""
import ast
from math import hypot
from pathlib import Path
from types import SimpleNamespace
import unittest

AUTHOR=Path(__file__).resolve().parents[1]/'tools/author-underworld-shelf-lurker.py'

def projector():
    source=ast.parse(AUTHOR.read_text(encoding='utf-8'))
    fn=next(node for node in source.body if isinstance(node,ast.FunctionDef) and node.name=='shelf_uv')
    namespace={}
    exec(compile(ast.Module(body=[fn],type_ignores=[]),str(AUTHOR),'exec'),namespace)
    return namespace['shelf_uv']

class Layers(dict):
    def __init__(self,count):
        super().__init__()
        self.count=count
    def new(self,name):
        layer=SimpleNamespace(data=[SimpleNamespace(uv=None) for _ in range(self.count)])
        self[name]=layer
        return layer

def mesh_object(points):
    loops=[SimpleNamespace(vertex_index=i) for i in range(len(points))]
    mesh=SimpleNamespace(
        vertices=[SimpleNamespace(co=SimpleNamespace(x=x,y=y,z=z)) for x,y,z in points],
        loops=loops,
        polygons=[SimpleNamespace(loop_indices=range(len(points)))],
        uv_layers=Layers(len(points)),
    )
    return SimpleNamespace(data=mesh)

class ShelfUvTests(unittest.TestCase):
    def test_growth_center_and_outer_lip(self):
        obj=mesh_object([(0,0,0),(.55,0,.08),(0,.34,-.20),(-.55,0,.08)])
        projector()(obj,.55,.34)
        uv=[d.uv for d in obj.data.uv_layers['ShelfLurkerUV'].data]
        self.assertEqual(uv[0],(.5,.5))
        self.assertAlmostEqual(uv[1][0],.5+1/2.24)
        self.assertAlmostEqual(uv[2][1],.5+1/2.24)
        self.assertAlmostEqual(uv[3][0],.5-1/2.24)

    def test_uv_is_independent_of_height_and_shelf_dimensions(self):
        uv=[]
        for rx,ry in ((.55,.34),(.42,.28)):
            obj=mesh_object([(rx*.6,ry*.25,.04),(rx*.6,ry*.25,-.04)])
            projector()(obj,rx,ry)
            uv.append([d.uv for d in obj.data.uv_layers['ShelfLurkerUV'].data])
        self.assertEqual(uv[0][0],uv[0][1])
        for first,second in zip(uv[0],uv[1]):
            for a,b in zip(first,second): self.assertAlmostEqual(a,b)

    def test_growth_rings_increase_radially_and_scallops_stay_inside_atlas(self):
        obj=mesh_object([(.55*r,0,0) for r in (0,.3,.6,1,1.10)])
        projector()(obj,.55,.34)
        uv=[d.uv for d in obj.data.uv_layers['ShelfLurkerUV'].data]
        radii=[hypot(u-.5,v-.5) for u,v in uv]
        self.assertEqual(radii,sorted(radii))
        self.assertTrue(all(0<=u<=1 and 0<=v<=1 for u,v in uv))
        self.assertGreater(radii[-1]-radii[1],.30)

if __name__=='__main__': unittest.main()
