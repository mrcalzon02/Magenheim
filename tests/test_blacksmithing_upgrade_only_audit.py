"""Fail-closed native upgradeOnly flag validation; offline synthetic audit fixtures."""
import copy
import importlib.util
import unittest
from pathlib import Path

PATH = Path(__file__).resolve().parents[1] / "tools/audit-blacksmithing-ledger.py"
SPEC = importlib.util.spec_from_file_location("blacksmithing_upgrade_only", PATH)
AUDIT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(AUDIT)

GRAMMAR = {
    "workshops": [{"id": "anvil", "operations": ["Forging"]}],
    "components": [{"id": "working-forms", "workshop": "anvil"}],
    "routes": [{"id": "forged-cutting", "families": ["Sword"],
                "operations": ["Forging"], "components": ["working-forms"]}],
}
CATALOG = {
    "items": [{"prefab": "SwordIron", "maxQuality": 1}],
    "recipes": [{
        "id": "Recipe_SwordIron", "outputPrefab": "SwordIron",
        "family": "Sword", "disposition": "Replace",
        "enabled": True, "upgradeOnly": False, "costs": [],
    }],
}


class UpgradeOnlyAuditTests(unittest.TestCase):
    def evaluate(self, **fields):
        native = copy.deepcopy(CATALOG)
        native["recipes"][0].update(fields)
        return AUDIT.audit(native, GRAMMAR)

    def test_real_false_zero_cost_is_rejected(self):
        self.assertEqual(self.evaluate()["status"], "INVALID_NATIVE_LEDGER")

    def test_string_false_does_not_bypass_zero_cost(self):
        result = self.evaluate(upgradeOnly="false")
        self.assertEqual(result["status"], "INVALID_NATIVE_LEDGER")
        self.assertIn("Recipe_SwordIron: invalid native upgradeOnly flag", result["errors"])

    def test_integer_one_does_not_bypass_zero_cost(self):
        result = self.evaluate(upgradeOnly=1)
        self.assertEqual(result["status"], "INVALID_NATIVE_LEDGER")
        self.assertIn("Recipe_SwordIron: invalid native upgradeOnly flag", result["errors"])

    def test_missing_upgrade_only_is_not_implicitly_false(self):
        native = copy.deepcopy(CATALOG)
        del native["recipes"][0]["upgradeOnly"]
        result = AUDIT.audit(native, GRAMMAR)
        self.assertEqual(result["status"], "INVALID_NATIVE_LEDGER")

    def test_disabled_recipe_still_requires_boolean_flag(self):
        self.assertEqual(self.evaluate(enabled=False, upgradeOnly="false")["status"],
                         "INVALID_NATIVE_LEDGER")

    def test_other_family_bad_flag_invalidates_entire_catalog(self):
        native = copy.deepcopy(CATALOG)
        extra = copy.deepcopy(native["recipes"][0])
        extra.update(id="Recipe_Unrelated", family="Armour", upgradeOnly="true")
        native["recipes"].append(extra)
        result = AUDIT.audit(native, GRAMMAR)
        self.assertEqual(result["status"], "INVALID_NATIVE_LEDGER")
        self.assertEqual(result["candidate_native_recipes"], 1)

    def test_genuine_boolean_true_remains_unverified_not_invalid(self):
        self.assertEqual(self.evaluate(upgradeOnly=True)["status"],
                         "BLOCKED_MISSING_AUTHORED_LEDGER")


if __name__ == "__main__":
    unittest.main()
