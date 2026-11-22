"""Fail-closed forged-cutting native-cost shape and conservation regressions (offline)."""
import copy
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import test_blacksmithing_ledger_audit as baseline

ROOT = Path(__file__).resolve().parents[1]
AUDIT = baseline.AUDIT


class MalformedNativeCostAuditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        baseline.ForgedCuttingLedgerAuditTests.setUpClass()
        cls.grammar = baseline.ForgedCuttingLedgerAuditTests.grammar
        cls.native = baseline.ForgedCuttingLedgerAuditTests.native

    def evaluate(self, costs):
        catalog = copy.deepcopy(self.native)
        catalog["recipes"][0]["costs"] = costs
        return AUDIT.audit(catalog, self.grammar)

    def test_non_object_native_cost_rows_have_diagnostics_not_exceptions(self):
        for invalid in (None, 17, "Iron", [], True):
            with self.subTest(cost=invalid):
                row = copy.deepcopy(self.native["recipes"][0]["costs"][0])
                result = self.evaluate([row, invalid])
                self.assertEqual(result["status"], "INVALID_NATIVE_LEDGER")
                self.assertFalse(result["conservation_verified"])
                self.assertEqual(result["candidate_native_recipes"], 1)
                self.assertEqual(result["native_cost_rows_checked"], 1)
                self.assertEqual(result["native_quality_entries_checked"], 4)
                self.assertTrue(any("Recipe_SwordIron resource" in error and "entry" in error
                                    for error in result["errors"]), result["errors"])

    def test_non_list_cost_collections_rejected_without_iteration_crash(self):
        for invalid in (None, 17, "Iron", {"prefab": "Iron"}, False):
            with self.subTest(costs=invalid):
                result = self.evaluate(invalid)
                self.assertEqual(result["status"], "INVALID_NATIVE_LEDGER")
                self.assertFalse(result["conservation_verified"])
                self.assertEqual(result["native_cost_rows_checked"], 0)
                self.assertEqual(result["native_quality_entries_checked"], 0)
                self.assertTrue(any("Recipe_SwordIron resource" in error and "collection" in error
                                    for error in result["errors"]), result["errors"])

    def test_duplicate_native_resources_remain_rejected(self):
        row = copy.deepcopy(self.native["recipes"][0]["costs"][0])
        result = self.evaluate([row, copy.deepcopy(row)])
        self.assertEqual(result["status"], "INVALID_NATIVE_LEDGER")
        self.assertIn("missing/duplicate Recipe_SwordIron resource: 'Iron'", result["errors"])
        self.assertEqual(result["native_cost_rows_checked"], 1)
        self.assertEqual(result["native_quality_entries_checked"], 4)
        self.assertFalse(result["conservation_verified"])

    def test_genuine_baseline_preserves_native_quality_four_discontinuity(self):
        result = self.evaluate(copy.deepcopy(self.native["recipes"][0]["costs"]))
        self.assertEqual(result["status"], "BLOCKED_MISSING_AUTHORED_LEDGER")
        self.assertEqual(result["errors"], [])
        self.assertEqual(result["native_quality_entries_checked"], 4)
        self.assertEqual(result["native_cost_rows_checked"], 1)
        self.assertFalse(result["conservation_verified"])

    def test_true_upgrader_resource_uses_native_quality_four_60(self):
        row = copy.deepcopy(self.native["recipes"][0]["costs"][0])
        row["upgraderResource"] = True
        row["qualityCosts"] = [20, 30, 40, 60]
        result = self.evaluate([row])
        self.assertEqual(result["status"], "BLOCKED_MISSING_AUTHORED_LEDGER")
        self.assertEqual(result["errors"], [])
        self.assertEqual(result["native_quality_entries_checked"], 4)
        self.assertFalse(result["conservation_verified"])

    def test_corrupt_quality_four_still_invalid(self):
        row = copy.deepcopy(self.native["recipes"][0]["costs"][0])
        row["qualityCosts"][-1] = 39
        result = self.evaluate([row])
        self.assertEqual(result["status"], "INVALID_NATIVE_LEDGER")
        self.assertIn("Recipe_SwordIron/Iron: native quality 4 differs", result["errors"])

    def test_explicit_empty_collection_remains_zero_cost_error(self):
        result = self.evaluate([])
        self.assertEqual(result["status"], "INVALID_NATIVE_LEDGER")
        self.assertIn("Recipe_SwordIron: enabled replacement/adaptation has zero native base input", result["errors"])

    def test_cli_returns_json_fail_closed_instead_of_traceback(self):
        with tempfile.TemporaryDirectory() as directory:
            catalog = Path(directory) / "catalog.json"
            data = copy.deepcopy(self.native)
            data["recipes"][0]["costs"].append(None)
            catalog.write_text(json.dumps(data), encoding="utf-8")
            process = subprocess.run([sys.executable, str(ROOT / "tools/audit-blacksmithing-ledger.py"),
                                      "--catalog", str(catalog), "--grammar",
                                      str(ROOT / "true-blacksmithing/data/manufacturing-grammar.json")],
                                     capture_output=True, text=True, timeout=10)
            self.assertEqual(process.returncode, 2, process.stderr)
            result = json.loads(process.stdout)
            self.assertEqual(result["status"], "INVALID_NATIVE_LEDGER")
            self.assertFalse(result["conservation_verified"])
            self.assertNotIn("Traceback", process.stderr)


if __name__ == "__main__":
    unittest.main()
