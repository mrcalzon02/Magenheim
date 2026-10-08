"""Blender-free geometry regressions for the existing Ashmite feeding pincers."""
import math
import unittest
from collections import Counter
from ashmite_mandible_geometry import CONTRACT, RINGS, SIDES, anchors, centerline, mandible_mesh_data


def volume(vertices, faces):
    v=0.
    for face in faces:
        a=vertices[face[0]]
        for j in range(1,len(face)-1):
            b,c=vertices[face[j]],vertices[face[j+1]]
            v+=(a[0]*(b[1]*c[2]-b[2]*c[1])+a[1]*(b[2]*c[0]-b[0]*c[2])+a[2]*(b[0]*c[1]-b[1]*c[0]))/6
    return v


class AshmiteMandibleTests(unittest.TestCase):
    def test_closed_outward_pair_and_triangle_budget(self):
        self.assertEqual(CONTRACT,'ashmite-paired-hooked-serrated-mandibles-17x24')
        for side in (-1,1):
            verts,faces,uvs=mandible_mesh_data(side)
            self.assertEqual(len(verts),RINGS*SIDES+2)
            self.assertEqual(len(faces),SIDES*(RINGS+1))
            self.assertEqual(sum(len(f)-2 for f in faces),2*SIDES*RINGS)
            self.assertEqual(len(faces),len(uvs))
            oriented=Counter()
            for f in faces:
                for a,b in zip(f,f[1:]+f[:1]): oriented[(a,b)]+=1
            self.assertTrue(all(oriented[(b,a)]==n==1 for (a,b),n in oriented.items()))
            self.assertGreater(volume(verts,faces),0)

    def test_anchors_preserved_and_inward_return_hook(self):
        for side in (-1,1):
            a,b=anchors(side)
            self.assertEqual(centerline(side,0),a)
            self.assertAlmostEqual(centerline(side,1)[0],b[0],places=12)
            self.assertAlmostEqual(centerline(side,1)[1],b[1],places=12)
            self.assertAlmostEqual(centerline(side,1)[2],b[2],places=12)
            self.assertGreater(side*(centerline(side,.70)[0]-b[0]),.007)
            self.assertGreater(centerline(side,.50)[2],(a[2]+b[2])/2)

    def test_bilateral_mirror_and_feeding_envelope(self):
        left,_,_=mandible_mesh_data(-1)
        right,_,_=mandible_mesh_data(1)
        for a,b in zip(left,right):
            self.assertAlmostEqual(a[0],-b[0],places=10)
            self.assertAlmostEqual(a[1],b[1],places=10)
            self.assertAlmostEqual(a[2],b[2],places=10)
        for side in (-1,1):
            start,_=anchors(side)
            verts,_,_=mandible_mesh_data(side)
            world=[(v[0]+start[0],v[1]+start[1],v[2]+start[2]) for v in verts]
            self.assertLess(max(abs(v[0]) for v in world),.095)
            self.assertGreater(min(v[1] for v in world),.128)
            self.assertLess(max(v[1] for v in world),.210)

    def test_physical_denticles_dorsal_keel_and_taper(self):
        for side in (-1,1):
            verts,_,_=mandible_mesh_data(side)
            start,_=anchors(side)
            def radial(r,j):
                c=centerline(side,r/(RINGS-1))
                return math.dist(tuple(verts[r*SIDES+j][k]+start[k] for k in range(3)),c)
            # Three distinct inner tooth ridges must be raised above adjacent rings.
            for ring in (4,8,12):
                tooth=radial(ring,SIDES//2)/radial(ring,0)
                before=radial(ring-1,SIDES//2)/radial(ring-1,0)
                after=radial(ring+1,SIDES//2)/radial(ring+1,0)
                self.assertGreater(tooth,max(before,after)+.10)
            # Dorsal keratin keel is physical height relief, not a normal map.
            self.assertGreater(radial(8,3*SIDES//4)/radial(8,SIDES//4),1.25)
            # Actual tip cross-section must taper well below the base.
            def diameter(r):
                return math.dist(verts[r*SIDES],verts[r*SIDES+SIDES//2])
            self.assertLess(diameter(RINGS-1),diameter(0)*.25)

    def test_authored_uv_seam_and_invalid_inputs(self):
        for side in (-1,1):
            _,faces,uvs=mandible_mesh_data(side)
            self.assertTrue(all(len(f)==len(uv) for f,uv in zip(faces,uvs)))
            self.assertTrue(all(0<=u<=1 and 0<=v<=1 for uv in uvs for u,v in uv))
            seam=uvs[SIDES+SIDES-1]
            self.assertEqual(seam[0][0],(SIDES-1)/SIDES)
            self.assertEqual(seam[1][0],1)
        for side,t in ((0,.5),(2,.5),(1,-.1),(1,1.1),(1,float('nan'))):
            with self.assertRaises(ValueError): centerline(side,t)

if __name__=='__main__': unittest.main()
