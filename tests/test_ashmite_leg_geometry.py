"""Blender-free topology and anatomy tests for all twelve Ashmite leg sections."""
import math
import unittest
from collections import Counter
from ashmite_leg_geometry import CONTRACT, RINGS, SIDES, leg_mesh_data


def signed_volume(vertices, faces):
    volume = 0.0
    for f in faces:
        a = vertices[f[0]]
        for j in range(1, len(f)-1):
            b,c = vertices[f[j]],vertices[f[j+1]]
            volume += (a[0]*(b[1]*c[2]-b[2]*c[1]) +
                       a[1]*(b[2]*c[0]-b[0]*c[2]) +
                       a[2]*(b[0]*c[1]-b[1]*c[0]))/6
    return volume


def leg_cases():
    for pair,y in enumerate((.065,0,-.070),1):
        for side in (-1,1):
            hip=(side*.068,y,.070)
            knee=(side*.125,y+(.020 if pair==1 else -.008),.048)
            ankle=(side*.165,y-.012,.018)
            yield side,pair,'femur',hip,knee
            yield side,pair,'tibia',knee,ankle


class AshmiteLegTests(unittest.TestCase):
    def test_closed_outward_geometry_on_all_twelve_segments(self):
        self.assertEqual(CONTRACT,'ashmite-sculpted-hexapod-leg-13x24-v1')
        total=0
        for side,pair,kind,a,b in leg_cases():
            with self.subTest(side=side,pair=pair,kind=kind):
                vertices,faces,uvs=leg_mesh_data(a,b,kind)
                self.assertEqual(len(vertices),RINGS*SIDES+2)
                self.assertEqual(len(faces),RINGS*SIDES+SIDES)
                tris=sum(len(f)-2 for f in faces)
                self.assertEqual(tris,2*RINGS*SIDES)
                total+=tris
                edges=Counter((p,q) for f in faces for p,q in zip(f,f[1:]+f[:1]))
                self.assertTrue(all(n==1 and edges[(q,p)]==1 for (p,q),n in edges.items()))
                self.assertGreater(signed_volume(vertices,faces),0)
                self.assertEqual(len(uvs),len(faces))
                self.assertTrue(all(len(f)==len(uv) for f,uv in zip(faces,uvs)))
                self.assertTrue(all(0<=u<=1 and 0<=v<=1 for face in uvs for u,v in face))
        self.assertEqual(total,12*2*RINGS*SIDES)

    def test_original_endpoints_and_rigid_bone_anchor_contract(self):
        for _,_,kind,a,b in leg_cases():
            vertices,_,_=leg_mesh_data(a,b,kind)
            self.assertEqual(vertices[-2],(0.,0.,0.))
            for k in range(3): self.assertAlmostEqual(vertices[-1][k],b[k]-a[k])
            self.assertTrue(all(math.isfinite(c) for v in vertices for c in v))

    def test_bilateral_symmetry_and_leg_silhouette(self):
        cases=list(leg_cases())
        for i in range(0,len(cases),4):
            for j in range(2):
                _,_,kind,a,b=cases[i+j]
                _,_,kind2,c,d=cases[i+j+2]
                self.assertEqual(kind,kind2)
                lv,_,_=leg_mesh_data(a,b,kind)
                rv,_,_=leg_mesh_data(c,d,kind)
                # Mirror the world-space point cloud, not winding-dependent ring indices.
                left=sorted((round(-(v[0]+a[0]),7),round(v[1]+a[1],7),round(v[2]+a[2],7)) for v in lv)
                right=sorted((round(v[0]+c[0],7),round(v[1]+c[1],7),round(v[2]+c[2],7)) for v in rv)
                self.assertEqual(left,right)

    def test_distinct_femur_and_tibia_carapace_profiles(self):
        cases=list(leg_cases())
        for _,_,kind,a,b in cases:
            vertices,_,_=leg_mesh_data(a,b,kind)
            def width(i):
                ring=vertices[i*SIDES:(i+1)*SIDES]
                return math.dist(ring[0],ring[SIDES//2])
            self.assertGreater(width(0),width(RINGS-1)*1.22)
            self.assertGreater(width(0),.012)
            # Physical heat fractures reduce ring width near 35% and 70%.
            for i in (4,8):
                self.assertLess(width(i),(width(i-1)+width(i+1))/2 - .00004)
            # Dorsal ridge stands above adjacent shoulders in projected up frame.
            # Compare distances from the ring centroid (orientation-independent).
            ring=vertices[6*SIDES:7*SIDES]
            mid=tuple(sum(v[k] for v in ring)/SIDES for k in range(3))
            def radial(j): return math.dist(mid,ring[j])
            self.assertGreater(radial(SIDES//4),radial(SIDES//8)*.99)

    def test_stable_frame_for_nearly_vertical_segment(self):
        vertices,faces,_=leg_mesh_data((0.,0.,0.),(0.,0.,.1),'tibia')
        self.assertGreater(signed_volume(vertices,faces),0)
        self.assertTrue(all(math.isfinite(c) for v in vertices for c in v))

    def test_rejects_invalid_segment_data(self):
        a=(0.,0.,0.)
        for b,kind in ((a,'femur'),((0.,0.,1.),'wing'),((math.nan,0.,0.),'tibia')):
            with self.assertRaises(ValueError): leg_mesh_data(a,b,kind)

if __name__=='__main__': unittest.main()
