"""Offline source-geometry acceptance for the four existing Capcrawler shelf plates."""
import collections
import math
import sys
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
import capcrawler_shelf_geometry as geo


def triangles(face):
    for i in range(1, len(face)-1):
        yield face[0], face[i], face[i+1]


def signed_volume(vertices, faces):
    total=0.0
    for face in faces:
        for a,b,c in triangles(face):
            x,y,z=vertices[a],vertices[b],vertices[c]
            total+=(x[0]*(y[1]*z[2]-y[2]*z[1])
                    +x[1]*(y[2]*z[0]-y[0]*z[2])
                    +x[2]*(y[0]*z[1]-y[1]*z[0]))/6
    return total


class ShelfGeometryTests(unittest.TestCase):
    def test_roster_and_anchor_contract(self):
        self.assertEqual(geo.POSITIONS, ((-.16,.130,.075),(.04,.145,.085)))
        self.assertEqual(geo.SECTORS,64)
        for side in (-1,1):
            for index in (1,2):
                vertices,faces,uvs=geo.shelf_mesh_data(side,index)
                self.assertEqual(len(vertices),770)
                self.assertEqual(len(faces),832)
                self.assertEqual(len(uvs),len(faces))

    def test_watertight_and_winding(self):
        for side in (-1,1):
            for index in (1,2):
                vertices,faces,_=geo.shelf_mesh_data(side,index)
                directed=collections.defaultdict(list)
                for face in faces:
                    for a,b in zip(face,face[1:]+face[:1]):
                        directed[tuple(sorted((a,b)))].append((a,b))
                self.assertTrue(all(len(d)==2 and d[0]==d[1][::-1]
                                    for d in directed.values()))
                self.assertGreater(signed_volume(vertices,faces),.0003)

    def test_non_degenerate_faces_and_uv_loops(self):
        for side in (-1,1):
            for index in (1,2):
                vertices,faces,uvs=geo.shelf_mesh_data(side,index)
                for face,face_uv in zip(faces,uvs):
                    self.assertEqual(len(face),len(face_uv))
                    self.assertEqual(len(set(face)),len(face))
                    for u,v in face_uv:
                        self.assertTrue(math.isfinite(u) and math.isfinite(v))
                        self.assertGreaterEqual(u,0)
                        self.assertLessEqual(u,1)
                        self.assertGreaterEqual(v,0)
                        self.assertLessEqual(v,1)
                    for a,b,c in triangles(face):
                        ab=[vertices[b][k]-vertices[a][k] for k in range(3)]
                        ac=[vertices[c][k]-vertices[a][k] for k in range(3)]
                        cross=(ab[1]*ac[2]-ab[2]*ac[1],
                               ab[2]*ac[0]-ab[0]*ac[2],
                               ab[0]*ac[1]-ab[1]*ac[0])
                        self.assertGreater(sum(k*k for k in cross),1e-14)

    def test_separate_dorsal_ventral_and_rim_islands(self):
        for side in (-1,1):
            for index in (1,2):
                _,faces,uvs=geo.shelf_mesh_data(side,index)
                dorsal=uvs[:geo.SECTORS*geo.RINGS]
                ventral=uvs[geo.SECTORS*geo.RINGS:-geo.SECTORS]
                rim=uvs[-geo.SECTORS:]
                self.assertEqual(len(dorsal),384)
                self.assertEqual(len(ventral),384)
                self.assertEqual(len(rim),64)
                self.assertLess(max(u for face in dorsal for u,v in face),.5)
                self.assertGreater(min(u for face in ventral for u,v in face),.5)
                self.assertLess(max(v for face in dorsal+ventral for u,v in face),.5)
                self.assertGreater(min(v for face in rim for u,v in face),.6)

    def test_physical_fluting_and_scalloped_lip(self):
        for side in (-1,1):
            for index in (1,2):
                vertices,_,_=geo.shelf_mesh_data(side,index)
                rim=vertices[1+(geo.RINGS-1)*geo.SECTORS:1+geo.RINGS*geo.SECTORS]
                zs=[p[2] for p in rim]
                self.assertGreater(max(zs)-min(zs),.004)
                _,sx,sy=geo.POSITIONS[index-1]
                radii=[math.hypot((side*p[0]-.045)/sx,
                                  (p[1]-.006*((side*p[0]-.045)/sx)**2)/sy)
                       for p in rim]
                self.assertGreater(max(radii)-min(radii),.08)
                self.assertGreater(vertices[0][2]-min(zs),.03)
                self.assertGreater(min(p[2] for p in rim)-min(p[2] for p in vertices),.004)

    def test_mirrored_anatomy_and_plate_proportions(self):
        for index in (1,2):
            left,_,_=geo.shelf_mesh_data(-1,index)
            right,_,_=geo.shelf_mesh_data(1,index)
            for a,b in zip(left,right):
                self.assertAlmostEqual(a[0],-b[0],places=9)
                self.assertAlmostEqual(a[1],b[1],places=9)
                self.assertAlmostEqual(a[2],b[2],places=9)
        for side in (-1,1):
            first,_,_=geo.shelf_mesh_data(side,1)
            second,_,_=geo.shelf_mesh_data(side,2)
            self.assertGreater(max(p[1] for p in second)-min(p[1] for p in second),
                               max(p[1] for p in first)-min(p[1] for p in first))

    def test_invalid_plate_rejected(self):
        for args in ((0,1),(-1,0),(1,3),(2,2)):
            with self.assertRaises(ValueError): geo.shelf_mesh_data(*args)


if __name__ == '__main__': unittest.main()
