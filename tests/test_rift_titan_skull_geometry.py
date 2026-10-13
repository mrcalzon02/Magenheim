"""Blender-independent Rift Titan recessed eye socket geometry regressions."""
import unittest
from collections import defaultdict
from rift_titan_head_layout import head_parts
from rift_titan_skull_geometry import eye_socket_geometry


class RiftTitanSkullGeometryTests(unittest.TestCase):
    def setUp(self):
        stones, eyes, _ = head_parts()
        self.wells = {part[0]: part for part in stones if part[0].startswith('EyeWell')}
        self.eyes = {eye[0]: eye for eye in eyes}

    def test_closed_manifold_and_outward_winding(self):
        for part in self.wells.values():
            vertices, faces = eye_socket_geometry(*part[1:6])
            self.assertEqual((len(vertices), sum(len(face)-2 for face in faces)), (16, 28))
            edges = defaultdict(list)
            for face in faces:
                for a,b in zip(face,face[1:]+face[:1]):
                    edges[tuple(sorted((a,b)))].append((a,b))
            self.assertTrue(all(len(directions)==2 and directions[0] != directions[1]
                                for directions in edges.values()))
            volume = sum((vertices[a][0]*(vertices[b][1]*vertices[c][2]-vertices[b][2]*vertices[c][1])
                          +vertices[a][1]*(vertices[b][2]*vertices[c][0]-vertices[b][0]*vertices[c][2])
                          +vertices[a][2]*(vertices[b][0]*vertices[c][1]-vertices[b][1]*vertices[c][0]))/6
                         for face in faces for i in range(1,len(face)-1)
                         for a,b,c in [(face[0],face[i],face[i+1])])
            self.assertGreater(volume,0)

    def test_eyes_recess_behind_lips_without_burying_in_back_wall(self):
        for suffix in ('L','R'):
            well = self.wells['EyeWell'+suffix]
            eye = self.eyes['EyeGlow'+suffix]
            vertices,_ = eye_socket_geometry(*well[1:6])
            front_lip = min(v[1] for v in vertices[:4])
            cavity_back = vertices[8][1]
            # Prism's radial ring extends 80% of its radius in Y.
            eye_front = eye[1][1] - eye[2]*.8
            eye_back = eye[1][1] + eye[2]*.8
            self.assertLess(front_lip,eye_front)
            self.assertLess(eye_front,cavity_back)
            self.assertLessEqual(eye_back,cavity_back+.002)
            self.assertAlmostEqual(well[1][0],eye[1][0])
            self.assertAlmostEqual(well[1][2],eye[1][2])

    def test_sockets_are_mirrored_and_clear_core(self):
        left = self.wells['EyeWellL']; right = self.wells['EyeWellR']
        self.assertAlmostEqual(left[1][0],-right[1][0])
        self.assertEqual(left[1][1:3],right[1][1:3])
        for well in (left,right):
            verts,_ = eye_socket_geometry(*well[1:6])
            self.assertGreater(min(v[2] for v in verts),2.8)

    def test_invalid_inputs_fail_closed(self):
        for center,width,height,depth,tilt in [((0,0,0),0,.1,.1,0),
                                                 ((0,0,0),.1,-1,.1,0),
                                                 ((0,0,0),.1,.1,float('nan'),0),
                                                 ((float('inf'),0,0),.1,.1,.1,0)]:
            with self.assertRaises(ValueError):
                eye_socket_geometry(center,width,height,depth,tilt)

if __name__ == '__main__':
    unittest.main()
