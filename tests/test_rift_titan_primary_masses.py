import math
import unittest
from collections import Counter
from rift_titan_primary_masses import primary_mass_strata
from rift_titan_rock_facets import faceted_plate_geometry


def triangles(faces):
    return sum(len(face) - 2 for face in faces)


class RiftTitanPrimaryMassTests(unittest.TestCase):
    def test_four_unique_masses_with_bounded_source_budget(self):
        masses = primary_mass_strata()
        self.assertEqual(len(masses), 4)
        self.assertEqual(len({m[0] for m in masses}), 4)
        self.assertEqual(sum(triangles(faceted_plate_geometry(*m[1:])[1]) for m in masses), 96)
        self.assertLessEqual(1668 + 96, 1800)

    def test_paired_primary_load_bearing_shapes(self):
        masses = primary_mass_strata()
        for left, right in ((masses[0], masses[2]), (masses[1], masses[3])):
            self.assertEqual(left[0][:-1], right[0][:-1])
            self.assertAlmostEqual(left[1][0], -right[1][0])
            self.assertAlmostEqual(left[1][2], right[1][2])
            self.assertEqual(left[2:5], right[2:5])
            self.assertAlmostEqual(left[5], -right[5])

    def test_open_core_window_retained_in_front_projection(self):
        # The center of the exposed crystal cage at Z=2.25 must remain visible.
        # Reject any new body plate whose actual projected polygons cross the
        # central X strip in the core's 2.12..2.40 high readability window.
        for name, center, w, h, d, tilt, seed in primary_mass_strata():
            verts, _ = faceted_plate_geometry(center, w, h, d, tilt, seed)
            for v in verts:
                self.assertFalse(abs(v[0]) < .18 and 2.12 <= v[2] <= 2.40,
                                 (name, v))
            self.assertTrue(max(v[2] for v in verts) < 2.12 or
                            min(v[2] for v in verts) > 2.40, name)

    def test_mantle_contacts_existing_shoulder_envelope(self):
        for name, center, w, h, d, tilt, seed in primary_mass_strata():
            if not name.startswith('ThoraxMantle'):
                continue
            verts, _ = faceted_plate_geometry(center, w, h, d, tilt, seed)
            self.assertGreater(max(v[2] for v in verts), 2.85)
            self.assertLess(min(v[2] for v in verts), 2.70)
            self.assertGreater(max(abs(v[0]) for v in verts), .65)

    def test_lower_buttress_bridges_pelvis_and_cage_lower_edge(self):
        for name, center, w, h, d, tilt, seed in primary_mass_strata():
            if not name.startswith('LowerCoreButtress'):
                continue
            verts, _ = faceted_plate_geometry(center, w, h, d, tilt, seed)
            self.assertLess(min(v[2] for v in verts), 1.50)
            self.assertGreater(max(v[2] for v in verts), 1.92)

    def test_closed_outward_facing_geometry(self):
        for m in primary_mass_strata():
            verts, faces = faceted_plate_geometry(*m[1:])
            edges = Counter()
            for f in faces:
                for a,b in zip(f, f[1:]+f[:1]):
                    edges[tuple(sorted((a,b)))] += 1
            self.assertEqual(set(edges.values()), {2})
            signed_volume = 0.0
            for f in faces:
                for i in range(1, len(f)-1):
                    a,b,c = (verts[j] for j in (f[0],f[i],f[i+1]))
                    cross = ((b[1]-a[1])*(c[2]-a[2])-(b[2]-a[2])*(c[1]-a[1]),
                             (b[2]-a[2])*(c[0]-a[0])-(b[0]-a[0])*(c[2]-a[2]),
                             (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0]))
                    signed_volume += sum(a[j]*cross[j] for j in range(3))/6
            self.assertGreater(signed_volume, 0, m[0])

if __name__ == '__main__':
    unittest.main()
