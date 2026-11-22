"""Native amount-domain/float-precision limits for the existing offline ledger audit.

These fixtures exercise the authentic auditor against the checked-in manufacturing
route grammar. They do not activate or claim conservation of the manufacturing route.
"""
import copy
import unittest

import test_blacksmithing_ledger_audit as baseline

AUDIT = baseline.AUDIT
INT32_MAX = (1 << 31) - 1
FLOAT32_EXACT_INT_MAX = 1 << 24


class NativeBudgetBoundsAuditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        baseline.ForgedCuttingLedgerAuditTests.setUpClass()
        cls.native = baseline.ForgedCuttingLedgerAuditTests.native
        cls.grammar = baseline.ForgedCuttingLedgerAuditTests.grammar

    def evaluate(self, **cost_fields):
        native = copy.deepcopy(self.native)
        native['recipes'][0]['costs'][0].update(cost_fields)
        return AUDIT.audit(native, self.grammar)

    def test_valid_small_native_costs_and_quality_four_are_unchanged(self):
        result = self.evaluate()
        self.assertEqual(result['status'], 'BLOCKED_MISSING_AUTHORED_LEDGER')
        self.assertEqual(result['native_quality_entries_checked'], 4)
        self.assertEqual(result['errors'], [])
        self.assertFalse(result['conservation_verified'])

    def test_missing_zero_negative_fractional_and_boolean_amounts(self):
        for field in ('amount', 'amountPerLevel', 'extraAmountOnlyOneIngredient'):
            for invalid in (None, -1, 0.5, True, False):
                with self.subTest(field=field, invalid=invalid):
                    result = self.evaluate(**{field: invalid})
                    self.assertEqual(result['status'], 'INVALID_NATIVE_LEDGER')
                    self.assertFalse(result['conservation_verified'])
        # Zero is genuinely permitted for a single resource if other native
        # resources supply a positive non-upgrade crafting base.
        native = copy.deepcopy(self.native)
        native['recipes'][0]['costs'][0].update(amountPerLevel=0, qualityCosts=[20, 0, 0, 0])
        self.assertEqual(AUDIT.audit(native, self.grammar)['status'],
                         'BLOCKED_MISSING_AUTHORED_LEDGER')

    def test_invalid_cost_arrays_and_quality_values(self):
        for malformed in (None, 42, '20,10,20,40', [20], [20, 10, 20, 40, 40],
                          [20, 10, 20, True], [20, 10, 20, 40.0],
                          [20, 10, 20, -1], [[20], 10, 20, 40]):
            with self.subTest(qualityCosts=malformed):
                result = self.evaluate(qualityCosts=malformed)
                self.assertEqual(result['status'], 'INVALID_NATIVE_LEDGER')
                self.assertFalse(result['conservation_verified'])

    def test_large_native_int_fields_rejected_before_false_budget_acceptance(self):
        # These source fields are C# / Unity signed 32-bit integers, never arbitrary
        # precision Python integers. Explicitly supply internally self-consistent
        # numbers so ordinary 'quality differs' checks do not mask missing guards.
        for field in ('amount', 'amountPerLevel', 'extraAmountOnlyOneIngredient'):
            native = copy.deepcopy(self.native)
            cost = native['recipes'][0]['costs'][0]
            cost[field] = INT32_MAX + 1
            cost['qualityCosts'] = [AUDIT.native_amount(cost, q) for q in range(1, 5)]
            with self.subTest(field=field):
                result = AUDIT.audit(native, self.grammar)
                self.assertEqual(result['status'], 'INVALID_NATIVE_LEDGER')
                self.assertFalse(result['conservation_verified'])

    def test_quality_calculation_cannot_exceed_native_int32(self):
        # Individual field values fit int32; level-four calculation does not.
        result = self.evaluate(amountPerLevel=600_000_000,
                               qualityCosts=[20, 600_000_000,
                                             1_200_000_000, 2_400_000_000])
        self.assertEqual(result['status'], 'INVALID_NATIVE_LEDGER')
        self.assertFalse(result['conservation_verified'])

    def test_float32_precision_boundary_is_fail_closed_not_python_bigint_green(self):
        # C# MaterialBudget.GetNativeAmount uses a float multiplier and casts to
        # int. Python integer math over 2^24 is not a verified exact native twin.
        per = FLOAT32_EXACT_INT_MAX + 1
        result = self.evaluate(amountPerLevel=per,
                               qualityCosts=[20, per, per * 2, per * 4])
        self.assertEqual(result['status'], 'INVALID_NATIVE_LEDGER')
        self.assertFalse(result['conservation_verified'])

    def test_upgrade_only_and_native_int32_boundary_remain_unchanged(self):
        native = copy.deepcopy(self.native)
        native['items'][0]['maxQuality'] = 1
        cost = native['recipes'][0]['costs'][0]
        cost.update(amount=INT32_MAX, amountPerLevel=0,
                    extraAmountOnlyOneIngredient=0, qualityCosts=[INT32_MAX])
        native['recipes'][0]['upgradeOnly'] = True
        result = AUDIT.audit(native, self.grammar)
        self.assertEqual(result['status'], 'BLOCKED_MISSING_AUTHORED_LEDGER')
        self.assertFalse(result['conservation_verified'])

        upgraded = self.evaluate(upgraderResource=True, qualityCosts=[20, 30, 40, 60])
        self.assertEqual(upgraded['status'], 'BLOCKED_MISSING_AUTHORED_LEDGER')
        self.assertEqual(upgraded['errors'], [])


if __name__ == '__main__':
    unittest.main()
