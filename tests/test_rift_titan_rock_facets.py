import unittest
from collections import Counter
from rift_titan_rock_facets import faceted_plate_geometry


def triangulate(face):
    for i in range(1, len(face)-1):
        yield (face[0], face[i], face[i+1])


def signed_volume(vertices, faces):
    total = 0
    for f in faces:
        for i, j, k in triangulate(f):
            a, b, c = (vertices[n] for n in (i, j, k))
            cross = (b[1]*c[2]-b[2]*c[1], b[2]*c[0]-b[0]*c[2], b[0]*c[1]-b[1]*c[0])
            total += sum(a[t]*cross[t] for t in range(3))/6
    return total


class RiftTitanRockFacetTests(unittest.TestCase):
    def test_exact_geometry_budget(self):
        verts, faces = faceted_plate_geometry((0, 0, 0), .4, .3, .18, .13, 5)
        self.assertEqual(len(verts), 14)
        self.assertEqual(sum(len(f)-2 for f in faces), 24)

    def test_closed_edge_incidence_and_winding(self):
        verts, faces = faceted_plate_geometry((0, 0, 0), .4, .3, .18, .13, 5)
        edges = Counter()
        directed = Counter()
        for f in faces:
            for a, b in zip(f, f[1:]+f[:1]):
                edges[tuple(sorted((a,b)))] += 1
                directed[(a,b)] += 1
        self.assertEqual(set(edges.values()), {2})
        for a,b in directed:
            self.assertEqual(directed[(a,b)], directed[(b,a)])
        self.assertGreater(signed_volume(verts, faces), 0)

    def test_preserves_silhouette_perimeter(self):
        a, _ = faceted_plate_geometry((.5, 0, 2), .47, .34, .18, -.1, 5)
        b, _ = faceted_plate_geometry((.5, 0, 2), .47, .34, .18, -.1, 5)
        self.assertEqual(a[:12], b[:12])
        self.assertEqual(a[0][0], a[6][0])
        self.assertEqual(a[0][2], a[6][2])

    def test_shoulders_stay_thinner_than_previous_r3(self):
        for depth, old in ((.18,.29),(.15,.25),(.12,.21)):
            verts, _ = faceted_plate_geometry((0,0,0),.4,.3,depth)
            self.assertLess(max(abs(v[1]) for v in verts), old*.8)

    def test_ridge_gives_nonplanar_faces(self):
        verts, _ = faceted_plate_geometry((0,0,0),.4,.3,.18)
        self.assertLess(verts[12][1], min(v[1] for v in verts[:6]))
        self.assertGreater(verts[13][1], max(v[1] for v in verts[6:12]))

    def test_invalid_input_rejected(self):
        for args in (((0,0,0),0,.3,.1), ((0,0,0),.3,-1,.1),
                     ((0,0,0),.3,.4,0), ((0,float('nan'),0),.3,.4,.1)):
            with self.assertRaises(ValueError):
                faceted_plate_geometry(*args)


if __name__=='__main__': unittest.main()
