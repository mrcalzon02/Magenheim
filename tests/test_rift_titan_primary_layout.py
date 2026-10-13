import math
import unittest
from rift_titan_primary_layout import SHOULDER_STRATA, limb_strata


class RiftTitanPrimaryLayoutTests(unittest.TestCase):
    def test_shoulders_keep_reach_but_are_thinner(self):
        old = (.29, .25, .21)
        self.assertEqual(len(SHOULDER_STRATA), 3)
        for profile, original_depth in zip(SHOULDER_STRATA, old):
            x, z, w, h, d, tilt = profile
            self.assertGreater(x + w, .9)
            self.assertGreater(h, 0)
            self.assertLess(d, original_depth * .75)
            self.assertTrue(all(math.isfinite(v) for v in profile))

    def test_slim_plates_follow_diagonal_arm(self):
        plates = limb_strata((-.8, .0, 2.4), (-1.4, .15, 1.6), -1)
        self.assertEqual(len(plates), 3)
        axis = math.atan2(-.6, -.8)
        for center, width, height, depth, tilt in plates:
            self.assertLess(abs(tilt - axis), .04)
            self.assertGreater(height / (1.9 * width), 1.7)
            self.assertLess(depth, .111)
        self.assertLess(plates[0][0][2], 2.4)
        self.assertGreater(plates[2][0][0], -1.4)

    def test_mirror_rig_is_symmetric(self):
        left = limb_strata((-.6, 0, 2), (-1.0, 0, 1.3), -1)
        right = limb_strata((.6, 0, 2), (1.0, 0, 1.3), 1)
        for a, b in zip(left, right):
            self.assertAlmostEqual(a[0][0], -b[0][0])
            self.assertAlmostEqual(a[0][2], b[0][2])
            self.assertAlmostEqual(a[1], b[1])
            self.assertAlmostEqual(a[2], b[2])
            self.assertAlmostEqual(a[4], -b[4])

    def test_leg_strata_stay_under_primitive_cost(self):
        plates = limb_strata((.4, 0, 1.2), (.55, .15, .12), 1, leg=True)
        for _, w, h, d, _ in plates:
            self.assertLessEqual(w, .155)
            self.assertLessEqual(d, .125)
            self.assertGreater(h, 1.9 * w * 1.7)

    def test_bad_endpoints_fail_closed(self):
        with self.assertRaises(ValueError):
            limb_strata((0, 0, 0), (0, 0, 0), 1)
        with self.assertRaises(ValueError):
            limb_strata((0, 0, 0), (0, 1, 0), 1)
        with self.assertRaises(ValueError):
            limb_strata((0, 0, 0), (0, 0, 1), 0)
        with self.assertRaises(ValueError):
            limb_strata((0, 0, 0), (float('nan'), 0, 1), 1)


if __name__ == '__main__':
    unittest.main()
