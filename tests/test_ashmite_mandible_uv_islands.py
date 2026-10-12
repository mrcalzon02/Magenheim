"""Blender-free three-island UV and silhouette-preservation checks for Ashmite mandibles."""
import hashlib
import math
import unittest
from ashmite_mandible_geometry import (CONTRACT, UV_CONTRACT, RINGS, SIDES,
    ROOT_CAP, TIP_CAP, SIDE_U, SIDE_V, anchors, mandible_mesh_data)


def signed_uv_area(uv):
    return sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(uv,uv[1:]+uv[:1]))/2


def geometry_fingerprint(verts, faces):
    payload = ';'.join(','.join(f'{x:.8f}' for x in vertex) for vertex in verts)
    payload += '\n' + ';'.join(','.join(map(str, face)) for face in faces)
    return hashlib.sha256(payload.encode()).hexdigest()


class MandibleUVAtlasTests(unittest.TestCase):
    def test_contract_preserves_existing_anatomy_and_triangle_count(self):
        self.assertEqual(CONTRACT, 'ashmite-paired-hooked-serrated-mandibles-17x24')
        self.assertEqual(UV_CONTRACT, 'ashmite-mandible-three-island-uv-v1')
        for side in (-1,1):
            vertices, faces, uvs = mandible_mesh_data(side)
            self.assertEqual(len(vertices),410)
            self.assertEqual(len(faces),432)
            self.assertEqual(sum(len(f)-2 for f in faces),816)
            self.assertEqual(len(uvs),len(faces))
            self.assertEqual(vertices[408],(0.,0.,0.))
            start, end = anchors(side)
            self.assertTrue(all(abs(vertices[409][k]-(end[k]-start[k]))<1e-12 for k in range(3)))
            self.assertEqual(geometry_fingerprint(vertices,faces), {
                -1: 'bb8287195ff900a12d240548f5abf3581d12a64181b482cfcbe4a6b6ad327e81',
                 1: '1bc2d42ff6f6b2901d1bc228a22b3b267afa7ccd53fce68ea39f045bd5943ab5',
            }[side])

    def test_three_disjoint_islands_with_real_1024_pixel_gutters(self):
        self.assertGreater(SIDE_U[0],.03)
        self.assertLess(SIDE_U[1],.97)
        self.assertGreater(SIDE_V[0],.03)
        self.assertGreater(ROOT_CAP[1]-ROOT_CAP[2]-SIDE_V[1],32/1024)
        self.assertGreater(TIP_CAP[1]-TIP_CAP[2]-SIDE_V[1],32/1024)
        self.assertGreater(TIP_CAP[0]-TIP_CAP[2]-(ROOT_CAP[0]+ROOT_CAP[2]),32/1024)
        for side in (-1,1):
            _,_,uvs=mandible_mesh_data(side)
            root=uvs[:SIDES]
            flank=uvs[SIDES:-SIDES]
            tip=uvs[-SIDES:]
            self.assertEqual(len(flank),SIDES*(RINGS-1))
            self.assertTrue(all(ROOT_CAP[0]-ROOT_CAP[2]-1e-10<=u<=ROOT_CAP[0]+ROOT_CAP[2]+1e-10
                                and ROOT_CAP[1]-ROOT_CAP[2]-1e-10<=v<=ROOT_CAP[1]+ROOT_CAP[2]+1e-10
                                for face in root for u,v in face))
            self.assertTrue(all(TIP_CAP[0]-TIP_CAP[2]-1e-10<=u<=TIP_CAP[0]+TIP_CAP[2]+1e-10
                                and TIP_CAP[1]-TIP_CAP[2]-1e-10<=v<=TIP_CAP[1]+TIP_CAP[2]+1e-10
                                for face in tip for u,v in face))
            self.assertTrue(all(SIDE_U[0]-1e-10<=u<=SIDE_U[1]+1e-10
                                and SIDE_V[0]-1e-10<=v<=SIDE_V[1]+1e-10
                                for face in flank for u,v in face))

    def test_all_faces_have_positive_uv_area_and_bounded_coordinates(self):
        for side in (-1,1):
            _,faces,uvs=mandible_mesh_data(side)
            for face,uv in zip(faces,uvs):
                self.assertEqual(len(face),len(uv))
                self.assertGreater(abs(signed_uv_area(uv)),1e-8)
                self.assertTrue(all(math.isfinite(u) and math.isfinite(v)
                                    and 0<=u<=1 and 0<=v<=1 for u,v in uv))

    def test_sidewall_seam_and_endcaps_do_not_share_texels(self):
        for side in (-1,1):
            _,_,uvs=mandible_mesh_data(side)
            flank=uvs[SIDES:-SIDES]
            seam=flank[SIDES-1]
            self.assertEqual(sorted({round(u,12) for u,v in seam}),
                             sorted({round(SIDE_U[0]+(SIDE_U[1]-SIDE_U[0])*(SIDES-1)/SIDES,12),
                                     round(SIDE_U[1],12)}))
            self.assertLess(max(v for face in flank for u,v in face),
                            min(v for face in uvs[:SIDES]+uvs[-SIDES:] for u,v in face))

if __name__=='__main__': unittest.main()
