"""Offline forged-cutting audit regression tests; no Valheim installation required."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("blacksmithing_ledger_audit", ROOT / "tools/audit-blacksmithing-ledger.py")
AUDIT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(AUDIT)


class ForgedCuttingLedgerAuditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.grammar = AUDIT.load(ROOT / "true-blacksmithing/data/manufacturing-grammar.json")
        cls.native = {
            "items": [
                {"prefab": "SwordIron", "maxQuality": 4},
                {"prefab": "Iron", "maxQuality": 1},
            ],
            "recipes": [{
                "id": "Recipe_SwordIron",
                "outputPrefab": "SwordIron",
                "family": "Sword",
                "disposition": "Replace",
                "enabled": True,
                "upgradeOnly": False,
                "costs": [{
                    "prefab": "Iron", "amount": 20, "amountPerLevel": 10,
                    "upgraderResource": False, "extraAmountOnlyOneIngredient": 0,
                    "qualityCosts": [20, 10, 20, 40],
                }],
            }],
        }

    def test_real_forged_cutting_grammar_and_fail_closed_baseline(self):
        result = AUDIT.audit(copy.deepcopy(self.native), self.grammar)
        self.assertEqual(result["status"], "BLOCKED_MISSING_AUTHORED_LEDGER")
        self.assertFalse(result["conservation_verified"])
        self.assertEqual((result["grammar_route_families"], result["grammar_operations"],
                          result["grammar_components"]), (7, 7, 7))
        self.assertEqual((result["candidate_native_recipes"], result["native_cost_rows_checked"],
                          result["native_quality_entries_checked"]), (1, 1, 4))
        self.assertEqual({entry["component"] for entry in result["external_component_producers"]},
                         {"wood-forms", "hide-forms"})
        self.assertEqual(len(result["missing_conservation_inputs"]), 4)

    def test_quality_four_budget_corruption_rejected(self):
        fixture = copy.deepcopy(self.native)
        fixture["recipes"][0]["costs"][0]["qualityCosts"][3] = 39
        self.assertEqual(AUDIT.audit(fixture, self.grammar)["status"], "INVALID_NATIVE_LEDGER")

    def test_zero_cost_output_rejected(self):
        fixture = copy.deepcopy(self.native)
        fixture["recipes"][0]["costs"] = []
        self.assertEqual(AUDIT.audit(fixture, self.grammar)["status"], "INVALID_NATIVE_LEDGER")

    def test_duplicate_resource_rejected(self):
        fixture = copy.deepcopy(self.native)
        fixture["recipes"][0]["costs"].append(copy.deepcopy(fixture["recipes"][0]["costs"][0]))
        self.assertEqual(AUDIT.audit(fixture, self.grammar)["status"], "INVALID_NATIVE_LEDGER")

    def test_missing_component_owner_rejected(self):
        grammar = copy.deepcopy(self.grammar)
        next(route for route in grammar["routes"] if route["id"] == "forged-cutting")["components"].append("unowned-form")
        self.assertEqual(AUDIT.audit(self.native, grammar)["status"], "INVALID_NATIVE_LEDGER")

    def test_duplicate_json_property_rejected(self):
        import tempfile
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "bad.json"
            path.write_text('{"schemaVersion":1,"schemaVersion":1}', encoding="utf-8")
            with self.assertRaises(ValueError):
                AUDIT.load(path)


if __name__ == "__main__":
    unittest.main()
