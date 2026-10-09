"""Blender-free regression for the existing Ashmite central vent scraper."""
import math
import unittest
from collections import Counter
from ashmite_scraper_geometry import CONTRACT, RINGS, SIDES, ORIGIN, scraper_mesh_data


def volume(vertices, faces):
    total = 0.0
    for face in faces:
        a = vertices[face[0]]
        for j in range(1, len(face)-1):
            b, c = vertices[face[j]], vertices[face[j+1]]
            total += (a[0]*(b[1]*c[2]-b[2]*c[1]) +
                      a[1]*(b[2]*c[0]-b[0]*c[2]) +
                      a[2]*(b[0]*c[1]-b[1]*c[0]))/6
    return total


class AshmiteScraperTests(unittest.TestCase):
    def test_watertight_outward_geometry_and_uvs(self):
        vertices, faces, uvs = scraper_mesh_data()
        self.assertEqual(CONTRACT, 'ashmite-sulfurized-vent-scraper-17x32')
        self.assertEqual(len(vertices), RINGS * SIDES + 2)
        self.assertEqual(sum(len(f)-2 for f in faces), 2 * SIDES * RINGS)
        edges = Counter()
        for face in faces:
            for a, b in zip(face, face[1:]+face[:1]):
                edges[(a, b)] += 1
        self.assertTrue(all(n == 1 and edges[(b, a)] == 1 for (a, b), n in edges.items()))
        self.assertGreater(volume(vertices, faces), 0)
        self.assertEqual(len(faces), len(uvs))
        self.assertTrue(all(len(f) == len(uv) for f, uv in zip(faces, uvs)))
        self.assertTrue(all(0 <= u <= 1 and 0 <= v <= 1 for face in uvs for u, v in face))

    def test_preserved_anchor_and_original_envelope(self):
        vertices, _, _ = scraper_mesh_data()
        self.assertEqual(ORIGIN, (0.0, 0.185, 0.048))
        self.assertAlmostEqual(max(v[1] for v in vertices)-min(v[1] for v in vertices), 0.05)
        self.assertLessEqual(max(abs(v[0]) for v in vertices), 0.0185)
        self.assertLessEqual(max(abs(v[2]) for v in vertices), 0.0175)
        self.assertTrue(all(math.isfinite(c) for v in vertices for c in v))

    def test_three_physical_underside_ridges_and_taper(self):
        vertices, _, _ = scraper_mesh_data()
        def lower(r): return vertices[r*SIDES+3*SIDES//4][2]
        for ring in (4, 8, 12):
            self.assertLess(lower(ring), (lower(ring-1)+lower(ring+1))/2 - .0001)
        def diameter(r):
            a,b = vertices[r*SIDES],vertices[r*SIDES+SIDES//2]
            return math.dist(a,b)
        self.assertLess(diameter(RINGS-1), diameter(0)*.2)
        self.assertGreater(vertices[8*SIDES+SIDES//4][2], vertices[8*SIDES+SIDES//8][2])

    def test_clearance_between_established_paired_mandibles(self):
        from ashmite_mandible_geometry import anchors, mandible_mesh_data
        scraper, _, _ = scraper_mesh_data()
        scraper_half_width = max(abs(v[0]) for v in scraper)
        for side in (-1, 1):
            mandible, _, _ = mandible_mesh_data(side)
            anchor, _ = anchors(side)
            closest_inner_edge = min(abs(v[0] + anchor[0]) for v in mandible)
            self.assertGreater(closest_inner_edge - scraper_half_width, 0.004)


if __name__ == '__main__': unittest.main()
