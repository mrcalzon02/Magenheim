"""Blender-free checks for physical Ashmite scute UV island mapping."""
import math
import unittest
from ashmite_scute_geometry import (
    CONTRACT, UV_CONTRACT, SIDES, RINGS, RIM_U, RIM_V,
    scute_mesh_data, scute_face_uvs,
)


class AshmiteScuteUvIslands(unittest.TestCase):
    def test_all_five_scutes_have_bounded_noncollapsed_loop_uvs(self):
        self.assertEqual(CONTRACT,'ashmite-five-heat-fractured-scutella-16x64')
        self.assertEqual(UV_CONTRACT,'ashmite-scute-three-island-uv-v1')
        for plate in range(1,6):
            rx=.092 if plate<4 else .075
            vertices,faces=scute_mesh_data(rx,.047,.018,plate)
            loops=scute_face_uvs(vertices,faces,rx,.047)
            self.assertEqual(len(loops),len(faces))
            self.assertTrue(all(len(a)==len(f) for a,f in zip(loops,faces)))
            for poly in loops:
                self.assertTrue(all(0<u<1 and 0<v<1 and math.isfinite(u+v)
                                    for u,v in poly))
                area=sum(poly[i][0]*poly[(i+1)%len(poly)][1] -
                         poly[(i+1)%len(poly)][0]*poly[i][1]
                         for i in range(len(poly)))/2
                self.assertGreater(abs(area),1e-8)

    def test_upper_lower_and_exposed_rim_are_disjoint_islands(self):
        verts,faces=scute_mesh_data(.092,.047,.018,2)
        loops=scute_face_uvs(verts,faces,.092,.047)
        upper=loops[:11*SIDES]
        rim=loops[11*SIDES:12*SIDES]
        lower=loops[12*SIDES:]
        self.assertEqual((len(upper),len(rim),len(lower)),(704,64,320))
        self.assertLess(max(u for p in upper for u,v in p),.5)
        self.assertGreater(min(u for p in lower for u,v in p),.5)
        self.assertGreater(min(v for p in upper+lower for u,v in p),.52)
        self.assertLess(max(v for p in rim for u,v in p),.4)

    def test_last_perimeter_sector_has_no_wraparound_streak(self):
        verts,faces=scute_mesh_data(.092,.047,.018,3)
        loops=scute_face_uvs(verts,faces,.092,.047)
        rim=loops[11*SIDES:12*SIDES]
        self.assertAlmostEqual(rim[0][0][0],RIM_U[0])
        self.assertAlmostEqual(rim[-1][-1][0],RIM_U[1])
        for face in rim:
            self.assertAlmostEqual(face[0][0],face[1][0])
            self.assertAlmostEqual(face[2][0],face[3][0])
            self.assertAlmostEqual(face[2][0]-face[0][0],
                                   (RIM_U[1]-RIM_U[0])/SIDES)
            self.assertEqual((face[0][1],face[1][1]),(RIM_V[1],RIM_V[0]))

    def test_uv_seams_do_not_change_mesh_or_original_rig_anchors(self):
        for plate in range(1,6):
            rx=.092 if plate<4 else .075
            vertices,faces=scute_mesh_data(rx,.047,.018,plate)
            self.assertEqual((len(vertices),len(faces),
                              sum(len(f)-2 for f in faces)),(1026,1088,2048))
            loops=scute_face_uvs(vertices,faces,rx,.047)
            self.assertNotEqual(loops[10*SIDES][1],loops[11*SIDES][0])

    def test_rejects_stale_topology_or_invalid_uv_scale(self):
        v,f=scute_mesh_data(.092,.047,.018,1)
        for vertices,faces,rx,ry in ((v[:-1],f,.092,.047),
                                     (v,f[:-1],.092,.047),
                                     (v,f,0,.047),
                                     (v,f,float('nan'),.047)):
            with self.assertRaises(ValueError):
                scute_face_uvs(vertices,faces,rx,ry)

if __name__=='__main__': unittest.main()
