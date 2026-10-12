"""Blender-free regression coverage for exported Abyss Shellback GLB parity."""
import json
import struct
import tempfile
import unittest
from pathlib import Path
from shellback_glb_parity import scene_triangle_count, validate_parity

def make_glb(doc):
    data = json.dumps(doc, separators=(',', ':')).encode()
    data += b' ' * (-len(data) % 4)
    return struct.pack('<4sII', b'glTF', 2, 20 + len(data)) + struct.pack('<I4s', len(data), b'JSON') + data

def doc(count=2):
    return {'scenes':[{'nodes':[0]}], 'scene':0,
            'nodes':[{'mesh':0}], 'meshes':[{'primitives':[{'indices':0,'mode':4}]}],
            'accessors':[{'count':count*3}]}

class Tests(unittest.TestCase):
    def write(self, raw):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        path = Path(tmp.name)/'test.glb'
        path.write_bytes(raw)
        return path
    def test_count(self):
        self.assertEqual(scene_triangle_count(self.write(make_glb(doc(7)))), 7)
    def test_instance_count(self):
        d=doc(3);d['nodes']=[{'mesh':0},{'mesh':0}];d['scenes'][0]['nodes']=[0,1]
        self.assertEqual(scene_triangle_count(self.write(make_glb(d))), 6)
    def test_nested_nodes(self):
        d=doc(2);d['nodes']=[{'children':[1]},{'mesh':0}]
        self.assertEqual(scene_triangle_count(self.write(make_glb(d))), 2)
    def test_json_parity(self):
        path=self.write(make_glb(doc(2)))
        self.assertEqual(validate_parity({'parts':[{'triangles':list(range(6))}]},path),2)
        with self.assertRaisesRegex(ValueError,'parity failure'):
            validate_parity({'parts':[{'triangles':list(range(9))}]},path)
    def test_non_triangles(self):
        d=doc();d['meshes'][0]['primitives'][0]['mode']=5
        with self.assertRaisesRegex(ValueError,'non-triangle'):
            scene_triangle_count(self.write(make_glb(d)))
    def test_bad_header(self):
        raw=bytearray(make_glb(doc()));raw[0]=0
        with self.assertRaisesRegex(ValueError,'header'):
            scene_triangle_count(self.write(raw))
    def test_cycle(self):
        d=doc();d['nodes'][0]['children']=[0]
        with self.assertRaisesRegex(ValueError,'cyclic'):
            scene_triangle_count(self.write(make_glb(d)))
    def test_invalid_accessor(self):
        d=doc();d['meshes'][0]['primitives'][0]['indices']=99
        with self.assertRaisesRegex(ValueError,'accessor'):
            scene_triangle_count(self.write(make_glb(d)))
    def test_nonindexed(self):
        d=doc(4);d['meshes'][0]['primitives']=[{'attributes':{'POSITION':0}}]
        self.assertEqual(scene_triangle_count(self.write(make_glb(d))),4)

if __name__=='__main__':unittest.main()
