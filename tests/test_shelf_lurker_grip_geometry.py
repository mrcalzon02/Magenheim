"""Blender-free regression for six anatomically aligned Shelf Lurker suction pads."""
import math
import unittest
from collections import Counter
from shelf_lurker_grip_geometry import (CONTRACT, PROFILE, SIDES,
                                         grip_mesh_data, grip_uv_coord)


def signed_volume(vertices, faces):
    total = 0.
    for face in faces:
        p = vertices[face[0]]
        for k in range(1, len(face) - 1):
            q, r = vertices[face[k]], vertices[face[k + 1]]
            total += (p[0] * (q[1] * r[2] - q[2] * r[1])
                      + p[1] * (q[2] * r[0] - q[0] * r[2])
                      + p[2] * (q[0] * r[1] - q[1] * r[0])) / 6
    return total


class GripGeometryTests(unittest.TestCase):
    def test_all_six_pads_closed_outward_and_above_prior_primitive_density(self):
        for limb in ('L_1', 'L_2', 'L_3', 'R_1', 'R_2', 'R_3'):
            verts, faces = grip_mesh_data()
            self.assertEqual((len(verts), len(faces), sum(len(f) - 2 for f in faces)),
                             (770, 816, 1536), limb)
            edges, oriented = Counter(), Counter()
            for face in faces:
                for a, b in zip(face, face[1:] + face[:1]):
                    edges[tuple(sorted((a, b)))] += 1
                    oriented[(a, b)] += 1
            self.assertTrue(all(v == 2 for v in edges.values()), limb)
            self.assertTrue(all(oriented[(b, a)] == 1 for a, b in oriented), limb)
            self.assertGreater(signed_volume(verts, faces), 0, limb)

    def test_contact_ridges_and_recess_are_physical_and_preserve_envelope(self):
        verts, _ = grip_mesh_data()
        self.assertEqual(CONTRACT, 'sculpted-suction-pad-16x48')
        self.assertLess(verts[2 + 9 * SIDES][2], verts[2 + 10 * SIDES][2] - .008)
        self.assertLess(verts[2 + 11 * SIDES][2], verts[2 + 10 * SIDES][2] - .010)
        self.assertGreater(verts[1][2], verts[2 + 11 * SIDES][2] + .03)
        self.assertLessEqual(max(abs(v[0]) for v in verts), .185)
        self.assertLessEqual(max(abs(v[1]) for v in verts), .247)
        self.assertLessEqual(max(abs(v[2]) for v in verts), .056)
        self.assertEqual(len(PROFILE), 16)

    def test_radial_uv_aligns_concentric_material_and_remains_inside_atlas(self):
        verts, _ = grip_mesh_data()
        for x, y, z in verts:
            u, v = grip_uv_coord((x, y, z), .18, .24)
            self.assertTrue(0 < u < 1 and 0 < v < 1)
        self.assertEqual(grip_uv_coord((0, 0, -.02), .18, .24), (.5, .5))
        for ring in (9, 11):
            coords = [grip_uv_coord(verts[2 + ring * SIDES + j], .18, .24)
                      for j in range(SIDES)]
            distances = [math.hypot(u - .5, v - .5) for u, v in coords]
            self.assertLess(max(distances) - min(distances), 1e-10)
        lip = [grip_uv_coord(verts[2 + 6 * SIDES + j], .18, .24) for j in range(SIDES)]
        radii = [math.hypot(u - .5, v - .5) for u, v in lip]
        self.assertGreater(max(radii) - min(radii), .01)  # deliberate physical edge scallops

    def test_invalid_scale_fails_closed(self):
        for radii in ((0, .24, .055), (.18, -1, .055),
                      (.4, .24, .055), (.18, .24, float('nan'))):
            with self.assertRaises(ValueError):
                grip_mesh_data(*radii)
        with self.assertRaises(ValueError):
            grip_uv_coord((0, 0, 0), .18, 0)

if __name__ == '__main__':
    unittest.main()
