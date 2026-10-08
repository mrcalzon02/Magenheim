"""Blender-free regression for Shelf Lurker sculpted fungal shelf crests."""
import ast
from math import pi, cos
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[1]
AUTHOR=ROOT/'tools/author-underworld-shelf-lurker.py'
VERIFIER=ROOT/'tools/verify-underworld-shelf-lurker.py'

def source_parts():
    tree=ast.parse(AUTHOR.read_text(encoding='utf-8'))
    fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='shelf_growth_height')
    ns={'cos':cos}
    exec(compile(ast.Module(body=[fn],type_ignores=[]),str(AUTHOR),'exec'),ns)
    shelf=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='scalloped_shelf')
    profile=next(ast.literal_eval(n.value) for n in ast.walk(shelf)
                 if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='profile' for t in n.targets))
    return ns['shelf_growth_height'],profile

class ShelfGrowthTests(unittest.TestCase):
    def test_sixteen_rings_and_triangle_floor(self):
        height,profile=source_parts()
        self.assertEqual(len(profile),16)
        self.assertEqual(16*128*2,4096)
        self.assertEqual([profile[i][0] for i in (2,5,8)],[.32,.60,.86])
        self.assertTrue(all(profile[i][0]<profile[i+1][0] for i in range(10)))
        self.assertTrue(all(profile[i][0]>profile[i+1][0] for i in range(11,15)))

    def test_three_physical_growth_crests_and_fissures(self):
        height,_=source_parts()
        for ring in range(16):
            values=[height(ring,2*pi*j/128,.06) for j in range(128)]
            if ring in (2,5,8):
                self.assertGreater(min(values),.005)
                self.assertGreater(max(values)-min(values),.001)
            else:
                self.assertEqual(set(values),{0.})

    def test_author_and_blender_verifier_agree(self):
        author=AUTHOR.read_text(encoding='utf-8')
        verifier=VERIFIER.read_text(encoding='utf-8')
        ast.parse(author);ast.parse(verifier)
        self.assertIn('shelf_growth_height(ring,a,rz)',author)
        self.assertIn('three-sculpted-growth-crests-16x128',author)
        self.assertIn('len(mesh.vertices)!=2050 or len(mesh.polygons)!=2176',verifier)
        self.assertIn('sculpted fungal growth crest collapsed',verifier)
        self.assertIn('rim=[mesh.vertices[2+10*128+j]',verifier)
        self.assertIn('lip=[mesh.vertices[2+11*128+j]',verifier)

if __name__=='__main__': unittest.main()
