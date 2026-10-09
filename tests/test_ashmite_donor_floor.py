"""Blender-free fail-closed checks for actual Valheim Tick donor geometry."""
import importlib.util
import json
import struct
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / 'tools/verify-underworld-ashmite-donor-floor.py'
spec = importlib.util.spec_from_file_location('ashmite_floor', SCRIPT)
floor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(floor)


def png(path, w, h):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(b'\x89PNG\r\n\x1a\n' + b'\x00\x00\x00\x0dIHDR' + struct.pack('>II', w, h))


class AshmiteDonorFloorTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.donor = self.root / 'donor'
        self.donor.mkdir()
        (self.donor / 'Tick.obj').write_text('o Tick\nv 0 0 0\nv 1 0 0\nv 0 1 0\nv 0 0 1\nf 1 2 3\nf 1 3 4\n', encoding='utf-8')
        (self.donor / 'Tick.mtl').write_text('map_Kd Tick_albedo.png\nmap_Bump Tick_normal.png\n', encoding='utf-8')
        png(self.donor / 'Tick_albedo.png', 512, 1024)
        png(self.donor / 'Tick_normal.png', 1024, 512)
        self.manifest = {'schema': 1, 'source': 'registered Valheim runtime prefabs',
                         'models': [{'prefab': 'Tick', 'file': 'Tick.obj', 'meshes': 1, 'vertices': 4, 'triangles': 2}]}
        self.save_manifest()
        self.tex = self.root / 'ashmite'
        for stem in floor.STEMS:
            for kind in floor.KINDS:
                png(self.tex / f'{stem}-{kind}.png', 1024, 1024)

    def save_manifest(self):
        (self.donor / 'manifest.json').write_text(json.dumps(self.manifest), encoding='utf-8')

    def test_measured_obj_and_pbr_floor(self):
        self.assertEqual(floor.measured_donor(self.donor), (2, (1024, 1024)))
        floor.validate_texture_floor(self.tex, (1024, 1024))

    def test_rejects_manifest_without_matching_exported_geometry(self):
        self.manifest['models'][0]['triangles'] = 9000
        self.save_manifest()
        with self.assertRaisesRegex(RuntimeError, 'differs from OBJ'):
            floor.measured_donor(self.donor)
        self.manifest['models'][0]['triangles'] = 2
        self.manifest['models'][0]['vertices'] = 9000
        self.save_manifest()
        with self.assertRaisesRegex(RuntimeError, 'vertex count differs'):
            floor.measured_donor(self.donor)

    def test_rejects_empty_invalid_and_unmeasurable_obj(self):
        obj = self.donor / 'Tick.obj'
        obj.write_text('o Tick\n', encoding='utf-8')
        with self.assertRaisesRegex(RuntimeError, 'no measurable'):
            floor.measured_donor(self.donor)
        obj.write_text('v 0 0 0\nf 1 2 3\n', encoding='utf-8')
        with self.assertRaisesRegex(RuntimeError, 'missing vertex'):
            floor.measured_donor(self.donor)

    def test_rejects_missing_or_ambiguous_donor_and_untrusted_manifest(self):
        self.manifest['models'] = []
        self.save_manifest()
        with self.assertRaisesRegex(RuntimeError, 'exactly one'):
            floor.measured_donor(self.donor)
        self.manifest['models'] = [{'prefab': 'Tick', 'file': 'Tick.obj', 'triangles': 0}]
        self.save_manifest()
        with self.assertRaisesRegex(RuntimeError, 'triangle count'):
            floor.measured_donor(self.donor)
        self.manifest['schema'] = 99
        self.save_manifest()
        with self.assertRaisesRegex(RuntimeError, 'schema/source'):
            floor.measured_donor(self.donor)

    def test_rejects_missing_texture_and_small_maps(self):
        (self.tex / 'eye-normal.png').unlink()
        with self.assertRaises((RuntimeError, FileNotFoundError)):
            floor.validate_texture_floor(self.tex, (1024, 1024))
        png(self.tex / 'eye-normal.png', 512, 1024)
        with self.assertRaisesRegex(RuntimeError, 'below Tick donor'):
            floor.validate_texture_floor(self.tex, (1024, 1024))

    def test_rejects_unmeasured_or_unsafe_mtl_paths(self):
        (self.donor / 'Tick.mtl').write_text('newmtl no_texture\n')
        with self.assertRaisesRegex(RuntimeError, 'no measured'):
            floor.measured_donor(self.donor)
        (self.donor / 'Tick.mtl').write_text('map_Kd ../outside.png\n')
        with self.assertRaisesRegex(RuntimeError, 'escapes'):
            floor.measured_donor(self.donor)

    def test_rejects_invalid_png_header(self):
        (self.donor / 'Tick_albedo.png').write_bytes(b'\x89PNG\r\n\x1a\n' + b'\x00\x00\x00\x0dJUNK' + struct.pack('>II', 512, 1024))
        with self.assertRaisesRegex(RuntimeError, 'IHDR'):
            floor.measured_donor(self.donor)


if __name__ == '__main__':
    unittest.main()
