"""Blender-independent Rift Titan head anatomy and cost regressions."""
import unittest
from collections import Counter
from rift_titan_head_layout import head_parts
from rift_titan_rock_facets import faceted_plate_geometry

class RiftTitanHeadTests(unittest.TestCase):
    def setUp(self):
        self.stones, self.eyes, self.motes = head_parts()

    def test_reuse_existing_budget_and_distinct_names(self):
        self.assertEqual((len(self.stones), len(self.eyes), len(self.motes)), (6, 2, 4))
        self.assertEqual(len({p[0] for group in (self.stones,self.eyes,self.motes) for p in group}), 12)
        self.assertEqual(sum(sum(len(f)-2 for f in faceted_plate_geometry(*p[1:7])[1]) for p in self.stones), 144)

    def test_head_mass_below_crown_and_above_core(self):
        for part in self.stones:
            verts,_ = faceted_plate_geometry(*part[1:7])
            self.assertGreater(min(v[2] for v in verts), 2.69, part[0])
            self.assertLess(max(v[2] for v in verts), 3.55, part[0])
        self.assertLess(min(v[2] for v in faceted_plate_geometry(*self.stones[1][1:7])[0]), 2.9)
        self.assertGreater(max(v[2] for v in faceted_plate_geometry(*self.stones[0][1:7])[0]), 3.3)

    def test_crystal_cage_window_remains_clear(self):
        for part in self.stones:
            verts,_ = faceted_plate_geometry(*part[1:7])
            self.assertGreater(min(v[2] for v in verts), 2.60)
        for eye in self.eyes:
            self.assertGreater(eye[1][2]-eye[3]/2, 2.9)

    def test_eyes_pair_and_protrude_from_dark_wells(self):
        wells = {p[0]:p for p in self.stones if p[0].startswith('EyeWell')}
        self.assertEqual(set(wells), {'EyeWellL','EyeWellR'})
        for eye in self.eyes:
            well = wells['EyeWell'+eye[0][-1]]
            self.assertEqual(well[-1], 'rift-bands')
            self.assertAlmostEqual(eye[1][0], well[1][0])
            self.assertAlmostEqual(eye[1][2], well[1][2])
            verts,_ = faceted_plate_geometry(*well[1:7])
            self.assertLess(eye[1][1]-eye[2]*.8, min(v[1] for v in verts))

    def test_brow_and_jaw_bridge_cheeks(self):
        for cheek in self.stones[2:4]:
            v,_ = faceted_plate_geometry(*cheek[1:7])
            self.assertLess(min(a[0] for a in v), .5)
            self.assertGreater(max(a[0] for a in v), -.5)
            self.assertLess(min(a[2] for a in v), 3.0)
            self.assertGreater(max(a[2] for a in v), 3.20)
        for p in self.stones[:2]:
            verts,_ = faceted_plate_geometry(*p[1:7])
            self.assertLess(min(v[0] for v in verts), -.30)
            self.assertGreater(max(v[0] for v in verts), .30)

    def test_closed_geometry(self):
        for part in self.stones:
            _,faces = faceted_plate_geometry(*part[1:7])
            edges = Counter()
            for f in faces:
                for a,b in zip(f,f[1:]+f[:1]):
                    edges[tuple(sorted((a,b)))] += 1
            self.assertEqual(set(edges.values()), {2}, part[0])

if __name__ == '__main__':
    unittest.main()
