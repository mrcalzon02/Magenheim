"""Body PBR atlas acceptance: nonoverlapping side, anterior and posterior islands."""
import importlib.util
import math
from pathlib import Path
import unittest

path=Path(__file__).resolve().parents[1]/'tools/ashmite_body_geometry.py'
spec=importlib.util.spec_from_file_location('ashmite_body_geometry',path)
g=importlib.util.module_from_spec(spec)
spec.loader.exec_module(g)

class BodyUvIslands(unittest.TestCase):
    def test_three_disjoint_islands_for_both_existing_body_segments(self):
        self.assertEqual(g.UV_CONTRACT,'ashmite-body-three-island-uv-v1')
        for kind in ('thorax','abdomen'):
            verts,faces,uvs=g.body_mesh_data(kind)
            rear=uvs[:g.SIDES]
            side=uvs[g.SIDES:-g.SIDES]
            front=uvs[-g.SIDES:]
            self.assertEqual((len(rear),len(side),len(front)),(80,2800,80))
            self.assertLess(max(v for poly in side for u,v in poly),
                            min(v for poly in rear+front for u,v in poly))
            self.assertLess(max(u for poly in rear for u,v in poly),
                            min(u for poly in front for u,v in poly))
            for island in (rear,side,front):
                for face_uv in island:
                    self.assertTrue(all(0<u<1 and 0<v<1 and math.isfinite(u+v)
                                        for u,v in face_uv))
                    signed_area=sum(face_uv[i][0]*face_uv[(i+1)%len(face_uv)][1]
                                    -face_uv[(i+1)%len(face_uv)][0]*face_uv[i][1]
                                    for i in range(len(face_uv)))/2
                    self.assertGreater(abs(signed_area),1e-8)

    def test_wrap_seam_is_local_and_capped_at_both_ends(self):
        for kind in g.BODIES:
            _,_,uvs=g.body_mesh_data(kind)
            side=uvs[g.SIDES:-g.SIDES]
            for ring in range(g.RINGS-1):
                first=side[ring*g.SIDES]
                last=side[ring*g.SIDES+g.SIDES-1]
                self.assertAlmostEqual(first[0][0],g.SIDE_U[0])
                self.assertAlmostEqual(last[2][0],g.SIDE_U[1])
                self.assertLess(last[2][0]-last[0][0],.02)
                self.assertAlmostEqual(first[0][1],last[0][1])
            self.assertEqual(uvs[0][0],g.CAP_CENTERS[0])
            self.assertEqual(uvs[-1][0],g.CAP_CENTERS[1])

    def test_geometry_unchanged_from_approved_body_envelopes(self):
        for kind in g.BODIES:
            verts,faces,_=g.body_mesh_data(kind)
            self.assertEqual((len(verts),len(faces),sum(len(f)-2 for f in faces)),
                             (2882,2960,5760))
            self.assertEqual((verts[0],verts[-1]),
                             ((0.,-g.specification(kind)[1][1],0.),
                              (0.,g.specification(kind)[1][1],0.)))

if __name__=='__main__':unittest.main()
