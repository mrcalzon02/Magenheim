"""Fail-closed grammar-shape cases for the original forged-cutting auditor."""
import copy
import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location('native_ledger_route', ROOT / 'tools/audit-blacksmithing-ledger.py')
AUDIT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(AUDIT)

class RouteContractAuditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.grammar = AUDIT.load(ROOT / 'true-blacksmithing/data/manufacturing-grammar.json')
        cls.native = {'items': [{'prefab': 'SwordIron', 'maxQuality': 1},
                                {'prefab': 'Iron', 'maxQuality': 1}],
                      'recipes': [{'id':'Recipe_SwordIron', 'outputPrefab':'SwordIron',
                                   'family':'Sword', 'enabled':True, 'upgradeOnly':False,
                                   'disposition':'Replace', 'costs':[{'prefab':'Iron',
                                   'amount':20, 'amountPerLevel':10, 'qualityCosts':[20],
                                   'extraAmountOnlyOneIngredient':0,'upgraderResource':False}]}]}

    def get_route(self, grammar):
        return next(r for r in grammar['routes'] if r['id'] == 'forged-cutting')

    def test_real_grammar_preserved_and_not_conservation_admitted(self):
        actual = AUDIT.audit(copy.deepcopy(self.native), copy.deepcopy(self.grammar))
        self.assertEqual(actual['status'], 'BLOCKED_MISSING_AUTHORED_LEDGER')
        self.assertEqual(actual['errors'], [])
        self.assertFalse(actual['conservation_verified'])

    def test_shape_rejection_before_family_membership_or_iteration(self):
        for field, malformed in (
            ('families','Sword'), ('families', {'Sword': True}),
            ('families', ['Sword', 2]), ('families', [['Sword']]),
            ('families', ['Sword', '']),
            ('operations','Heating'), ('operations', {'Heating': 1}),
            ('components','metal-stock'), ('components', [['metal-stock']]),
        ):
            with self.subTest(field=field, malformed=malformed):
                grammar = copy.deepcopy(self.grammar)
                self.get_route(grammar)[field] = malformed
                actual = AUDIT.audit(copy.deepcopy(self.native), grammar)
                self.assertEqual(actual['status'], 'INVALID_GRAMMAR')
                self.assertFalse(actual['conservation_verified'])
                self.assertIn(field, ' '.join(actual['errors']))

if __name__ == '__main__': unittest.main()
