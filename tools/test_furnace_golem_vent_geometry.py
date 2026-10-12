"""Blender-independent tests for the Furnace Golem's open exhaust geometry."""
import unittest
from collections import Counter
from math import hypot
from furnace_golem_vent_geometry import PROFILES, SECTORS, make_vent

def signed_volume(vertices, faces):
    volume=0.0
    for face in faces:
        for i in range(1,len(face)-1):
            a,b,c=(vertices[j] for j in (face[0],face[i],face[i+1]))
            volume+=(a[0]*(b[1]*c[2]-b[2]*c[1])+a[1]*(b[2]*c[0]-b[0]*c[2])+a[2]*(b[0]*c[1]-b[1]*c[0]))/6
    return volume

class FurnaceVentTests(unittest.TestCase):
    def check(self,kind):
        verts,faces,uv,regions=make_vent(kind)
        self.assertEqual(len(faces),len(uv))
        self.assertEqual(len(faces),len(regions))
        self.assertEqual(len(verts),2*SECTORS*len(PROFILES[kind]))
        self.assertEqual(len(faces),SECTORS*(2*len(PROFILES[kind])))
        self.assertTrue(all(len(f)==4 and len(set(f))==4 for f in faces))
        self.assertTrue(all(len(f)==len(q) for f,q in zip(faces,uv)))
        self.assertTrue(all(0<=u<=1 and 0<=v<=1 for face in uv for u,v in face))
        edges=Counter(tuple(sorted((a,b))) for face in faces for a,b in zip(face,face[1:]+face[:1]))
        self.assertEqual(set(edges.values()),{2})
        self.assertGreater(signed_volume(verts,faces),.001)
        return verts,faces,regions
    def test_stack_closed_manifold_with_open_bore(self):
        self.check('stack')
    def test_cowl_closed_manifold_with_open_bore(self):
        self.check('cowl')
    def test_open_mouth_has_no_sealing_faces(self):
        for kind in PROFILES:
            verts,faces,regions=self.check(kind)
            z=PROFILES[kind][-1][0]
            mouth=[f for f,r in zip(faces,regions) if r=='mouth_rim']
            self.assertEqual(len(mouth),SECTORS)
            for face in mouth:
                self.assertTrue(all(hypot(verts[i][0],verts[i][1])>.04 for i in face))
                self.assertTrue(all(abs(verts[i][2]-z)<1e-8 for i in face))
    def test_cowl_flares_over_stack_without_obstructing_shaft(self):
        self.assertGreater(PROFILES['cowl'][-2][1],PROFILES['stack'][-1][1]*1.5)
        self.assertGreater(PROFILES['cowl'][-1][2],PROFILES['stack'][-1][2])
        self.assertLess(2.18+PROFILES['cowl'][0][0],1.90+PROFILES['stack'][-1][0])
        self.assertGreater(2.18+PROFILES['cowl'][-1][0],1.90+PROFILES['stack'][-1][0])
    def test_bore_and_exterior_have_separate_uv_islands(self):
        _,_,uv,regions=make_vent('cowl')
        outer=[u for face,r in zip(uv,regions) if r=='outer' for u,v in face]
        bore=[u for face,r in zip(uv,regions) if r=='bore' for u,v in face]
        self.assertLess(max(outer),min(bore))
        self.assertEqual(regions.count('mouth_rim'),SECTORS)
        self.assertEqual(regions.count('base_rim'),SECTORS)
    def test_invalid_profile_fails(self):
        with self.assertRaises(ValueError):make_vent('solid_cap')

if __name__=='__main__':unittest.main()
