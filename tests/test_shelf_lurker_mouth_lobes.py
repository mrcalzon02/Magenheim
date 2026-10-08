"""Blender-free geometry and UV regression for the four existing feeding lobes."""
import math
import unittest
from collections import Counter
from shelf_lurker_mouth_lobe_geometry import (CONTRACT, RINGS, SIDES,
                                              lobe_mesh_data, lobe_uv)

TARGETS = ((-.23,.38,1.29),(.23,.38,1.29),(0,.17,1.29),(0,.60,1.29))


def centroid(vertices, ring):
    pts = vertices[ring*SIDES:(ring+1)*SIDES]
    return tuple(sum(p[k] for p in pts)/SIDES for k in range(3))


def signed_volume(vertices, faces):
    volume = 0.
    for face in faces:
        a = vertices[face[0]]
        for k in range(1, len(face)-1):
            b,c = vertices[face[k]],vertices[face[k+1]]
            volume += (a[0]*(b[1]*c[2]-b[2]*c[1])
                       +a[1]*(b[2]*c[0]-b[0]*c[2])
                       +a[2]*(b[0]*c[1]-b[1]*c[0]))/6
    return volume


class MouthLobeTests(unittest.TestCase):
    def test_four_petal_topology_and_winding(self):
        self.assertEqual(CONTRACT, 'downturned-feeding-lamella-25x32')
        for target in TARGETS:
            vertices, faces = lobe_mesh_data(target)
            self.assertEqual((len(vertices),len(faces),sum(len(f)-2 for f in faces)),
                             (800,770,1596))
            edges, oriented = Counter(), Counter()
            for face in faces:
                for a,b in zip(face,face[1:]+face[:1]):
                    edges[tuple(sorted((a,b)))] += 1
                    oriented[(a,b)] += 1
            self.assertTrue(all(v==2 for v in edges.values()))
            self.assertTrue(all(oriented[(b,a)]==1 for a,b in oriented))
            self.assertGreater(signed_volume(vertices,faces),0)

    def test_downturned_fluted_petal_and_preserved_fourfold_reach(self):
        for target in TARGETS:
            vertices,_ = lobe_mesh_data(target)
            a,b,m = centroid(vertices,0),centroid(vertices,RINGS-1),centroid(vertices,12)
            self.assertAlmostEqual(math.hypot(a[0],a[1]-.38),.10,places=5)
            self.assertAlmostEqual(math.hypot(b[0],b[1]-.38),.36,places=5)
            self.assertLess(m[2],(a[2]+b[2])/2-.03)
            self.assertLess(b[2],a[2]-.05)
            radial=lambda j: math.dist(vertices[12*SIDES+j],m)
            self.assertGreater(radial(0),math.dist(vertices[0],a)*1.8)
            self.assertGreater(radial(0)-radial(5),.018)
            ux,uy=(target[0]/math.hypot(target[0],target[1]-.38),
                   (target[1]-.38)/math.hypot(target[0],target[1]-.38))
            tx,ty=-uy,ux
            def transverse_width(j):
                p=vertices[12*SIDES+j]
                theta=2*math.pi*j/SIDES
                return ((p[0]-m[0])*tx+(p[1]-m[1])*ty)/math.cos(theta)
            # Compare a longitudinal ridge with a neighbouring valley after
            # removing the ellipse's ordinary cos(theta) width projection.
            self.assertGreater(transverse_width(0)-transverse_width(2),.015)
            self.assertTrue(all(1.18 <= p[2] <= 1.34 for p in vertices))

    def test_side_seams_and_planar_caps_are_uv_safe(self):
        for ring in range(RINGS):
            for j in range(SIDES):
                for cap in (False,True):
                    u,v = lobe_uv(ring*SIDES+j,cap,False)
                    self.assertTrue(0<=u<=1 and 0<=v<=1)
        self.assertEqual(lobe_uv(SIDES,False,True)[0],1.)
        self.assertEqual(lobe_uv(SIDES+SIDES-1,False,True)[0],(SIDES-1)/SIDES)
        cap=[lobe_uv(j,True,False) for j in range(SIDES)]
        area=abs(sum(cap[j][0]*cap[(j+1)%SIDES][1]-cap[(j+1)%SIDES][0]*cap[j][1]
                     for j in range(SIDES)))/2
        self.assertGreater(area,.62)

    def test_rejects_unapproved_anatomy(self):
        for target in ((0,.38,1.29),(.8,.38,1.29),(.23,.38,1.4),
                       (float('nan'),.38,1.29)):
            with self.assertRaises(ValueError): lobe_mesh_data(target)


if __name__=='__main__': unittest.main()
