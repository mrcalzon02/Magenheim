"""Blender-free PBR UV separation and silhouette-preservation for all twelve Ashmite feet."""
import hashlib
import math
import unittest
from ashmite_tarsal_geometry import (
    CONTRACT, UV_CONTRACT, RINGS, SIDES, SIDE_U, SIDE_V, ROOT_CAP, TIP_CAP,
    anchors, tarsal_mesh_data,
)


def fingerprint(vertices, faces):
    data = ';'.join(','.join(f'{x:.8f}' for x in vertex) for vertex in vertices)
    data += '\n' + ';'.join(','.join(map(str, face)) for face in faces)
    return hashlib.sha256(data.encode()).hexdigest()


def area(uv):
    return sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(uv,uv[1:]+uv[:1]))/2


EXPECTED = {
    ('tarsus',-1): '44072cc8e9058287ca3f67396fa43dfedf2b002e68b05194d091d122b1df016c',
    ('tarsus', 1): 'f43386727002d53926d26c1f5c48ba20fa06558abdbac48f5daab8748b63e066',
    ('scraper',-1): '86461bf9fc25b8b4868a2801de9991b9716cd6b537115a4e0d9d2d8708b72d05',
    ('scraper', 1): '059f2fbaafa782354b40a0410ab3d7eb2c54ef39e997e84b5b9f37f3952e25d9',
}


class TarsalUVIslandTests(unittest.TestCase):
    def test_existing_geometry_and_rig_anchors_unchanged(self):
        self.assertEqual(CONTRACT, 'ashmite-tarsal-rasp-six-pairs-v1')
        self.assertEqual(UV_CONTRACT, 'ashmite-tarsal-three-island-uv-v1')
        for pair in (1,2,3):
            for side in (-1,1):
                for kind,n in RINGS.items():
                    with self.subTest(pair=pair,side=side,kind=kind):
                        vertices, faces, uvs = tarsal_mesh_data(side,pair,kind)
                        self.assertEqual(len(vertices),n*SIDES+2)
                        self.assertEqual(sum(len(face)-2 for face in faces),2*n*SIDES)
                        self.assertEqual(len(faces),len(uvs))
                        self.assertEqual(fingerprint(vertices,faces),EXPECTED[(kind,side)])
                        start,end=anchors(side,pair,kind)
                        self.assertEqual(vertices[-2],(0.,0.,0.))
                        self.assertTrue(all(abs(vertices[-1][k]-(end[k]-start[k]))<1e-12 for k in range(3)))

    def test_caps_and_sidewalls_disjoint_with_mipmap_gutters(self):
        self.assertGreater(SIDE_U[0],32/1024)
        self.assertLess(SIDE_U[1],1-32/1024)
        self.assertGreater(SIDE_V[0],32/1024)
        self.assertGreater(ROOT_CAP[1]-ROOT_CAP[2]-SIDE_V[1],32/1024)
        self.assertGreater(TIP_CAP[1]-TIP_CAP[2]-SIDE_V[1],32/1024)
        self.assertGreater(TIP_CAP[0]-TIP_CAP[2]-(ROOT_CAP[0]+ROOT_CAP[2]),32/1024)
        for pair in (1,2,3):
            for side in (-1,1):
                for kind in RINGS:
                    with self.subTest(pair=pair,side=side,kind=kind):
                        _,_,uvs=tarsal_mesh_data(side,pair,kind)
                        root, flank, tip=uvs[:SIDES],uvs[SIDES:-SIDES],uvs[-SIDES:]
                        for face in root:
                            self.assertTrue(all((u-ROOT_CAP[0])**2+(v-ROOT_CAP[1])**2 <= ROOT_CAP[2]**2+1e-10 for u,v in face))
                        for face in tip:
                            self.assertTrue(all((u-TIP_CAP[0])**2+(v-TIP_CAP[1])**2 <= TIP_CAP[2]**2+1e-10 for u,v in face))
                        for face in flank:
                            self.assertTrue(all(SIDE_U[0]-1e-10<=u<=SIDE_U[1]+1e-10 and SIDE_V[0]-1e-10<=v<=SIDE_V[1]+1e-10 for u,v in face))

    def test_all_uv_faces_non_degenerate_and_in_bounds(self):
        for pair in (1,2,3):
            for side in (-1,1):
                for kind in RINGS:
                    _,faces,uvs=tarsal_mesh_data(side,pair,kind)
                    for face,uv in zip(faces,uvs):
                        self.assertEqual(len(face),len(uv))
                        self.assertGreater(abs(area(uv)),1e-8)
                        self.assertTrue(all(math.isfinite(u) and math.isfinite(v) and 0<=u<=1 and 0<=v<=1 for u,v in uv))

    def test_cylindrical_seam_is_deliberate_and_caps_do_not_bleed(self):
        for kind in RINGS:
            _,_,uvs=tarsal_mesh_data(1,2,kind)
            side=uvs[SIDES:-SIDES]
            self.assertEqual(len(side),SIDES*(RINGS[kind]-1))
            self.assertAlmostEqual(side[0][0][0],SIDE_U[0])
            self.assertAlmostEqual(side[SIDES-1][1][0],SIDE_U[1])
            self.assertAlmostEqual(side[SIDES][0][0],SIDE_U[0])
            self.assertLess(max(v for face in side for _,v in face),
                            min(v for face in uvs[:SIDES]+uvs[-SIDES:] for _,v in face))

    def test_uv_islands_distinguish_protected_tarsi_and_mouthpart_scrapers(self):
        for pair in (1,2,3):
            for side in (-1,1):
                for kind in RINGS:
                    _,_,uvs=tarsal_mesh_data(side,pair,kind)
                    self.assertEqual(len(uvs),SIDES*(RINGS[kind]+1))
                    # A root disk and tip disk must never share the same center.
                    self.assertNotEqual(uvs[0][0],uvs[-1][0])

if __name__=='__main__': unittest.main()
