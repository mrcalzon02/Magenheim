"""Blender-free regression tests for Abyss Shellback elite LOD0 budgeting."""
import importlib.util
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parent
def load(filename,name):
    spec=importlib.util.spec_from_file_location(name,ROOT/filename)
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module
lod=load("abyss_shellback_runtime_lod.py","shellback_lod")
gate=load("verify-abyss-shellback-runtime-budget.py","shellback_gate")

def fixture(count):
    names=sorted(gate.REQUIRED)
    parts=[]
    for i,name in enumerate(names):
        n=count//len(names)+(i<count%len(names))
        parts.append(dict(name=name,path="creaturebone:Body/"+name,
                          vertices=[[0,0,0]]*(3*n),uv=[[0,0]]*(3*n),
                          triangles=list(range(3*n))))
    return dict(parts=parts,creatureRig=dict(kind="rigid-segment-creature-v1"))

class ShellbackTests(unittest.TestCase):
    def test_reduction_and_silhouette_priority(self):
        counts={f"ShellPlate_{i}":20480 for i in range(1,6)}
        counts.update(BodyMass=20480,VulnerableUnderside=20480,Eye_L=1280,Eye_R=1280)
        ratios=lod.allocate(counts)
        self.assertLessEqual(sum(n*ratios.get(name,1) for name,n in counts.items()),lod.CAP)
        self.assertGreater(ratios["ShellPlate_1"],ratios["BodyMass"])
        self.assertNotIn("Eye_L",ratios)
    def test_no_reduction_below_budget(self):
        self.assertEqual(lod.allocate({"ShellPlate_1":4000,"BodyMass":2000,"Eye_L":1280}),{})
    def test_protect_leg_eye_finger(self):
        for name in ("Eye_L","Leg_L_1_Upper","Claw_L_Finger_1"):
            self.assertTrue(lod.protected(name,1280))
    def test_excessive_reduction_fails(self):
        with self.assertRaises(ValueError):lod.allocate({"ShellPlate_1":10000000,"Eye_L":1280})
    def test_existing_50088_runtime_rejected(self):
        with self.assertRaisesRegex(ValueError,"outside elite"):
            gate.validate(fixture(50088))
    def test_valid_elite_band(self):
        self.assertEqual(gate.validate(fixture(20000)),20000)
    def test_missing_plate_rejected(self):
        doc=fixture(20000)
        doc["parts"]=[p for p in doc["parts"] if p["name"]!="ShellPlate_3"]
        with self.assertRaisesRegex(ValueError,"missing anatomy"):gate.validate(doc)
    def test_invalid_uv_rejected(self):
        doc=fixture(20000);doc["parts"][0]["uv"]=[]
        with self.assertRaisesRegex(ValueError,"UV failure"):gate.validate(doc)

if __name__=="__main__":unittest.main()
