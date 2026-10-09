"""Blender-independent geometry and identity regression for Ashmite tarsal tools."""
import math
import unittest
from collections import Counter
from ashmite_tarsal_geometry import CONTRACT, RINGS, SIDES, anchors, tarsal_mesh_data


def volume(vertices,faces):
    result=0.
    for face in faces:
        a=vertices[face[0]]
        for j in range(1,len(face)-1):
            b,c=vertices[face[j]],vertices[face[j+1]]
            result+=(a[0]*(b[1]*c[2]-b[2]*c[1])+a[1]*(b[2]*c[0]-b[0]*c[2])+a[2]*(b[0]*c[1]-b[1]*c[0]))/6
    return result


class AshmiteTarsalTests(unittest.TestCase):
    def test_all_twelve_meshes_closed_and_uv_complete(self):
        self.assertEqual(CONTRACT,'ashmite-tarsal-rasp-six-pairs-v1')
        total=0
        for pair in (1,2,3):
            for side in (-1,1):
                for kind in RINGS:
                    with self.subTest(side=side,pair=pair,kind=kind):
                        v,f,uv=tarsal_mesh_data(side,pair,kind)
                        self.assertEqual(len(v),RINGS[kind]*SIDES+2)
                        self.assertEqual(sum(len(p)-2 for p in f),2*RINGS[kind]*SIDES)
                        self.assertEqual(len(f),len(uv))
                        self.assertTrue(all(len(p)==len(u) for p,u in zip(f,uv)))
                        self.assertTrue(all(0<=a<=1 and 0<=b<=1 for q in uv for a,b in q))
                        edges=Counter((a,b) for face in f for a,b in zip(face,face[1:]+face[:1]))
                        self.assertTrue(all(n==1 and edges[(b,a)]==1 for (a,b),n in edges.items()))
                        self.assertGreater(volume(v,f),0)
                        total+=sum(len(p)-2 for p in f)
        self.assertEqual(total,6*(2*11*20+2*17*20))

    def test_anchors_exact_and_silhouette_envelopes(self):
        for pair in (1,2,3):
            for side in (-1,1):
                for kind in RINGS:
                    start,end=anchors(side,pair,kind)
                    v,_,_=tarsal_mesh_data(side,pair,kind)
                    self.assertEqual(v[-2],(0.,0.,0.))
                    for k in range(3): self.assertAlmostEqual(v[-1][k],end[k]-start[k])
                    self.assertTrue(all(math.isfinite(c) for point in v for c in point))
                    world=[tuple(point[k]+start[k] for k in range(3)) for point in v]
                    if kind=='scraper':
                        self.assertAlmostEqual(start[1]-end[1],.030)
                        self.assertLessEqual(max(abs(p[0]-side*.198) for p in world),.011)
                        self.assertLessEqual(max(abs(p[2]-.007) for p in world),.011)
                        self.assertLessEqual(max(p[1] for p in world)-min(p[1] for p in world),.032)
                    else:
                        self.assertAlmostEqual(start[2],.018)
                        self.assertAlmostEqual(end[2],.008)

    def test_bilateral_mirror_preserves_six_foot_silhouettes(self):
        for pair in (1,2,3):
            for kind in RINGS:
                left,_,_=tarsal_mesh_data(-1,pair,kind)
                right,_,_=tarsal_mesh_data(1,pair,kind)
                la,_=anchors(-1,pair,kind); ra,_=anchors(1,pair,kind)
                mirrored=sorted((round(-(p[0]+la[0]),7),round(p[1]+la[1],7),round(p[2]+la[2],7)) for p in left)
                expected=sorted((round(p[0]+ra[0],7),round(p[1]+ra[1],7),round(p[2]+ra[2],7)) for p in right)
                self.assertEqual(mirrored,expected)

    def test_physical_tarsal_sutures_and_scraper_rasps(self):
        for kind in RINGS:
            v,_,_=tarsal_mesh_data(1,2,kind)
            n=RINGS[kind]
            def diameter(i):
                ring=v[i*SIDES:(i+1)*SIDES]
                return math.dist(ring[0],ring[SIDES//2])
            self.assertGreater(diameter(0),diameter(n-1)*1.5)
            if kind=='tarsus':
                for i in (3,7):
                    self.assertLess(diameter(i),(diameter(i-1)+diameter(i+1))/2-.00002)
            else:
                start,end=anchors(1,2,kind)
                mid=v[(n//2)*SIDES:((n//2)+1)*SIDES]
                self.assertLess(sum(p[0] for p in mid)/SIDES,-.001)
                self.assertLess(sum(p[2] for p in mid)/SIDES,-.0005)
                def down(i):
                    ring=v[i*SIDES:(i+1)*SIDES]
                    return min(p[2] for p in ring)
                for i in (5,8,12):
                    self.assertLess(down(i),(down(i-1)+down(i+1))/2-.00002)

    def test_foot_assembly_contact_proximity_and_backward_rasp(self):
        for pair in (1,2,3):
            for side in (-1,1):
                a,_,_=tarsal_mesh_data(side,pair,'tarsus')
                b,_,_=tarsal_mesh_data(side,pair,'scraper')
                ao,_=anchors(side,pair,'tarsus')
                bo,tip=anchors(side,pair,'scraper')
                foot=[(v[0]+ao[0],v[1]+ao[1],v[2]+ao[2]) for v in a[:-2]]
                claw=[(v[0]+bo[0],v[1]+bo[1],v[2]+bo[2]) for v in b[:-2]]
                distance=min(math.dist(v,w) for v in foot for w in claw)
                self.assertLess(distance,.0025)
                self.assertLess(tip[1],bo[1])

    def test_rejects_invalid_anatomy(self):
        for case in ((0,1,'tarsus'),(1,0,'tarsus'),(1,4,'scraper'),(1,1,'unknown')):
            with self.assertRaises(ValueError): tarsal_mesh_data(*case)


if __name__=='__main__': unittest.main()
