"""Blender-independent quality gates for the Shelf Lurker's 18 limb segments."""
import math
import sys
import unittest
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]/'tools'))
from shelf_lurker_limb_geometry import limb_mesh_data, RINGS, SIDES


class LimbGeometryTests(unittest.TestCase):
    def setUp(self):
        self.a = (.52, .38, 1.56)
        self.b = (1.02, .58, 1.22)
        self.radius = .115
        self.vertices, self.faces = limb_mesh_data(self.a, self.b, self.radius)

    def center(self, ring):
        return tuple(sum(self.vertices[ring*SIDES+j][k] for j in range(SIDES))/SIDES
                     for k in range(3))

    def test_closed_density(self):
        self.assertEqual((len(self.vertices), len(self.faces)), (208, 194))
        self.assertEqual(sum(len(f)-2 for f in self.faces), 412)
        edges = Counter()
        for face in self.faces:
            self.assertEqual(len(face), len(set(face)))
            for a,b in zip(face, face[1:]+face[:1]):
                edges[tuple(sorted((a,b)))] += 1
        self.assertTrue(all(count == 2 for count in edges.values()))
        self.assertEqual(len(edges), 400)

    def test_consistent_winding_and_positive_volume(self):
        directed = Counter()
        for face in self.faces:
            for a,b in zip(face,face[1:]+face[:1]):
                directed[(a,b)] += 1
        self.assertTrue(all(directed[(b,a)] == 1 for a,b in directed))
        volume=0.0
        for face in self.faces:
            p0=self.vertices[face[0]]
            for j in range(1,len(face)-1):
                p1=self.vertices[face[j]]
                p2=self.vertices[face[j+1]]
                cross=(p1[1]*p2[2]-p1[2]*p2[1],
                       p1[2]*p2[0]-p1[0]*p2[2],
                       p1[0]*p2[1]-p1[1]*p2[0])
                volume+=sum(x*y for x,y in zip(p0,cross))/6
        self.assertGreater(volume,.001)

    def test_joint_alignment_and_bowed_silhouette(self):
        for got, expected in ((self.center(0), self.a), (self.center(RINGS-1), self.b)):
            self.assertLess(math.dist(got, expected), 1e-10)
        midpoint = tuple((a+b)/2 for a,b in zip(self.a, self.b))
        self.assertGreater(math.dist(self.center(6), midpoint), .055)

    def test_distal_taper_and_ridges(self):
        def avg_radius(i):
            c=self.center(i)
            return sum(math.dist(self.vertices[i*SIDES+j], c) for j in range(SIDES))/SIDES
        self.assertGreater(avg_radius(0), avg_radius(12)*1.4)
        self.assertGreater(avg_radius(5), avg_radius(12)*1.4)
        self.assertGreater(max(math.dist(self.vertices[6*SIDES+j],self.center(6)) for j in range(SIDES))-
                           min(math.dist(self.vertices[6*SIDES+j],self.center(6)) for j in range(SIDES)), .012)

    def test_sclerites_face_world_up_on_both_sides(self):
        # The previous frame used axis x lateral, which pointed the raised
        # dorsal armor down on the ceiling-clinging animal's six legs.
        for side in (-1,1):
            for y0,y1 in ((.38,.58),(-.05,-.02),(-.48,-.62)):
                hip=(side*.52,y0,1.56)
                elbow=(side*1.02,y1,1.22)
                wrist=(side*1.34,y1+.08,.78)
                tip=(side*1.48,y1+.12,.54)
                for a,b,r in ((hip,elbow,.115),(elbow,wrist,.085),(wrist,tip,.06)):
                    vertices,_=limb_mesh_data(a,b,r)
                    for ring in (4,8):
                        center=tuple(sum(vertices[ring*SIDES+j][k] for j in range(SIDES))/SIDES for k in range(3))
                        self.assertGreater(vertices[ring*SIDES+4][2]-center[2],.02)
                        self.assertLess(vertices[ring*SIDES+12][2]-center[2],-.02)

    def test_sclerite_relief_and_flexible_ventrum(self):
        # Angular vertex 4 is dorsal; 12 is ventral in the section frame.
        # Both sclerites have physical relief and the mid-seam stays narrower.
        def radius(i,j):
            return math.dist(self.vertices[i*SIDES+j],self.center(i))
        for ring in (4,8):
            self.assertGreater(radius(ring,4)/radius(ring,12),1.12)
        self.assertGreater(radius(4,4),radius(6,4)*1.12)
        self.assertGreater(radius(8,4),radius(6,4)*.95)
        # The two end rings must remain smooth at the bone joints.
        for ring in (0,RINGS-1):
            self.assertLess(abs(radius(ring,4)-radius(ring,12)),self.radius*.18)

    def test_all_six_limb_bands_preserve_clearance(self):
        # Check relief against every actual approved limb span, not just a
        # representative upper leg; no self-intersections in ring ordering.
        for side in (-1,1):
            for y0,y1 in ((.38,.58),(-.05,-.02),(-.48,-.62)):
                hip=(side*.52,y0,1.56)
                elbow=(side*1.02,y1,1.22)
                wrist=(side*1.34,y1+.08,.78)
                tip=(side*1.48,y1+.12,.54)
                for a,b,r in ((hip,elbow,.115),(elbow,wrist,.085),(wrist,tip,.06)):
                    vertices,faces=limb_mesh_data(a,b,r)
                    centers=[tuple(sum(vertices[i*SIDES+j][k] for j in range(SIDES))/SIDES
                                   for k in range(3)) for i in range(RINGS)]
                    self.assertLess(math.dist(centers[0],a),1e-10)
                    self.assertLess(math.dist(centers[-1],b),1e-10)
                    for ring in (4,8):
                        dr=math.dist(vertices[ring*SIDES+4],centers[ring])
                        vr=math.dist(vertices[ring*SIDES+12],centers[ring])
                        self.assertGreater(dr/vr,1.12)
                    self.assertEqual(sum(len(f)-2 for f in faces),412)

    def test_mirrored_six_limb_variants(self):
        for side in (-1, 1):
            for hip_y,elbow_y in ((.38,.58),(-.05,-.02),(-.48,-.62)):
                hip=(side*.52,hip_y,1.56)
                elbow=(side*1.02,elbow_y,1.22)
                wrist=(side*1.34,elbow_y+.08,.78)
                tip=(side*1.48,elbow_y+.12,.54)
                for a,b,r in ((hip,elbow,.115),(elbow,wrist,.085),(wrist,tip,.060)):
                    verts,faces=limb_mesh_data(a,b,r)
                    self.assertEqual(len(verts),208)
                    self.assertEqual(sum(len(f)-2 for f in faces),412)

    def test_invalid_geometry_rejected(self):
        for a,b,r in ((self.a,self.a,.115),(self.a,self.b,-1),
                      (self.a,self.b,float('nan')),(self.a,(float('inf'),0,0),.115),
                      (self.a,(100,0,0),.115)):
            with self.subTest(a=a,b=b,r=r),self.assertRaises(ValueError):
                limb_mesh_data(a,b,r)


if __name__=='__main__':
    unittest.main()
