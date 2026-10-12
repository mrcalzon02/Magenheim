"""Blender-independent regression for the Furnace Golem's primary armor silhouette."""
import unittest
from collections import Counter
from furnace_golem_armor_geometry import make_armor


def triangles(vertices,faces):
    for face in faces:
        for j in range(1,len(face)-1):
            yield vertices[face[0]],vertices[face[j]],vertices[face[j+1]]


def signed_volume(vertices,faces):
    volume=0.
    for a,b,c in triangles(vertices,faces):
        volume+=(a[0]*(b[1]*c[2]-b[2]*c[1])+
                 a[1]*(b[2]*c[0]-b[0]*c[2])+
                 a[2]*(b[0]*c[1]-b[1]*c[0]))/6.
    return volume

class FurnaceArmorTests(unittest.TestCase):
    def check(self,profile,cavity=False):
        v,f,uv,regions=make_armor(profile,.72,.78,.42,cavity)
        self.assertEqual(len(f),len(uv))
        self.assertEqual(len(f),len(regions))
        self.assertTrue(all(len(a)==len(b) for a,b in zip(f,uv)))
        self.assertTrue(all(0<=u<=1 and 0<=w<=1 for face in uv for u,w in face))
        self.assertTrue(all(len(set(face))==len(face) for face in f))
        edges=Counter(tuple(sorted((a,b))) for face in f for a,b in zip(face,face[1:]+face[:1]))
        self.assertEqual(set(edges.values()),{2})
        self.assertGreater(signed_volume(v,f),.005)
        return v,f,regions
    def test_all_profiles_are_closed_outward_and_uv_authored(self):
        for name in ('torso','pelvis','helm','plate','pauldron'):
            with self.subTest(name=name):self.check(name)
    def test_furnace_throat_is_actual_recess_not_overlay(self):
        v,f,regions=self.check('torso',True)
        self.assertIn('throat_wall',regions)
        self.assertIn('throat_floor',regions)
        front=max(p[1] for p in v)
        floor=[v[i][1] for face,region in zip(f,regions) if region=='throat_floor' for i in face]
        self.assertLess(max(floor),front-.1)
        self.assertGreater(len(v),45)
    def test_silhouette_shoulder_overhang_and_taper(self):
        v,_,_=self.check('torso')
        self.assertGreater(max(x for x,y,z in v if z>.1),max(x for x,y,z in v if z<-.2))
    def test_invalid_throat_and_dimensions_fail(self):
        with self.assertRaises(ValueError):make_armor('helm',.5,.5,.5,True)
        with self.assertRaises(ValueError):make_armor('torso',0,.5,.5)
        with self.assertRaises(ValueError):make_armor('generic',.5,.5,.5)
    def test_throat_keeps_back_and_front_surface_regions(self):
        _,_,regions=self.check('torso',True)
        self.assertEqual(regions.count('throat_floor'),10)
        self.assertEqual(regions.count('throat_wall'),10)
        self.assertEqual(regions.count('front'),10)

if __name__=='__main__':unittest.main()
