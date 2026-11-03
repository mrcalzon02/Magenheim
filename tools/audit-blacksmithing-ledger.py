#!/usr/bin/env python3
"""Read-only, fail-closed audit of the forged-cutting True Blacksmithing route.

This is an offline diagnostic, not a recipe converter or an activation gate.
The native census has no authored recipe-to-route bindings or redistributed costs;
therefore a clean native baseline MUST NOT be reported as conserved manufacturing.
"""
import argparse
import hashlib
import json
from pathlib import Path


def load(path):
    def unique_keys(pairs):
        value = {}
        for key, item in pairs:
            if key in value:
                raise ValueError(f"duplicate JSON property: {key}")
            value[key] = item
        return value
    return json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=unique_keys)


def unique(records, field, kind, errors):
    found = {}
    for record in records:
        key = record.get(field)
        if not isinstance(key, str) or not key or key in found:
            errors.append(f"missing/duplicate {kind}: {key!r}")
        else:
            found[key] = record
    return found


def native_amount(cost, quality):
    base = cost["amount"]
    if quality == 1:
        return base
    per = cost["amountPerLevel"]
    # Native Piece.Requirement.GetAmount, including quality-four discontinuity.
    doubled_multiplier = 2 * (quality - 1) if quality < 4 else 8 + quality - 4
    return (doubled_multiplier * per) // 2 + (base if cost["upgraderResource"] else 0)


def audit(catalog, grammar):
    errors = []
    workshops = unique(grammar.get("workshops", []), "id", "workshop", errors)
    components = unique(grammar.get("components", []), "id", "component", errors)
    routes = unique(grammar.get("routes", []), "id", "route", errors)
    owners = {}
    for workshop in workshops.values():
        for operation in workshop.get("operations", []):
            if operation in owners:
                errors.append(f"operation has multiple owners: {operation}")
            owners[operation] = workshop["id"]
    route = routes.get("forged-cutting")
    if route is None:
        errors.append("forged-cutting route is missing")
        return {"status": "INVALID_GRAMMAR", "errors": errors, "conservation_verified": False}
    for operation in route.get("operations", []):
        if operation not in owners:
            errors.append(f"forged-cutting operation has no owner: {operation}")
    external = []
    for component_id in route.get("components", []):
        component = components.get(component_id)
        if component is None:
            errors.append(f"forged-cutting references missing component: {component_id}")
            continue
        workshop = workshops.get(component.get("workshop"))
        if workshop is None:
            errors.append(f"component {component_id} has no workshop owner")
        elif not any(op in route.get("operations", []) for op in workshop.get("operations", [])):
            # Valid reusable upstream production; quantities/links are NOT authored yet.
            external.append({"component": component_id, "producer_workshop": workshop["id"]})
    for field in ("families", "operations", "components"):
        values = route.get(field, [])
        if not values or len(values) != len(set(values)):
            errors.append(f"forged-cutting has missing/duplicate {field}")
    items = unique(catalog.get("items", []), "prefab", "item", errors)
    unique(catalog.get("recipes", []), "id", "recipe", errors)
    # Native Valheim classifies named pickaxes as Hammer. A family-only route
    # filter would silently omit them from forged-cutting and could later send
    # them through the forged-striking route. Report, never auto-reclassify.
    pickaxe_exclusions = [
        {"recipe": recipe.get("id"), "output_prefab": recipe["outputPrefab"],
         "native_family": recipe.get("family")}
        for recipe in catalog.get("recipes", [])
        if recipe.get("enabled") and recipe.get("disposition") in ("Replace", "Adapt")
        and isinstance(recipe.get("outputPrefab"), str)
        and recipe["outputPrefab"].startswith("Pickaxe")
        and recipe.get("family") not in route["families"]
    ]
    candidate_count = 0
    checked_costs = 0
    checked_quality_entries = 0
    zero_cost = []
    # Family membership identifies candidates only. A sword can also use a cast or
    # exotic route; no material-condition routing can be proven from this grammar.
    for recipe in catalog.get("recipes", []):
        if not recipe.get("enabled") or recipe.get("disposition") not in ("Replace", "Adapt"):
            continue
        if recipe.get("family") not in route["families"]:
            continue
        candidate_count += 1
        rid = recipe.get("id", "<missing>")
        output = items.get(recipe.get("outputPrefab"))
        if output is None:
            errors.append(f"{rid}: missing output item")
            continue
        quality = output.get("maxQuality", 0)
        if not isinstance(quality, int) or quality < 1:
            errors.append(f"{rid}: invalid output quality")
            continue
        costs = unique(recipe.get("costs", []), "prefab", f"{rid} resource", errors)
        total_base = 0
        for resource, cost in costs.items():
            checked_costs += 1
            if resource not in items:
                errors.append(f"{rid}: unknown resource {resource}")
            if any(type(cost.get(field)) is not int or cost[field] < 0
                   for field in ("amount", "amountPerLevel", "extraAmountOnlyOneIngredient")):
                errors.append(f"{rid}/{resource}: invalid nonnegative native amounts")
                continue
            if not isinstance(cost.get("upgraderResource"), bool):
                errors.append(f"{rid}/{resource}: missing upgrader flag")
                continue
            actual = cost.get("qualityCosts")
            if not isinstance(actual, list) or len(actual) != quality:
                errors.append(f"{rid}/{resource}: expected {quality} native quality costs")
                continue
            for q, observed in enumerate(actual, 1):
                checked_quality_entries += 1
                if type(observed) is not int or observed != native_amount(cost, q):
                    errors.append(f"{rid}/{resource}: native quality {q} differs")
            total_base += cost["amount"]
        if not recipe.get("upgradeOnly") and total_base <= 0:
            zero_cost.append(rid)
    for rid in zero_cost:
        errors.append(f"{rid}: enabled replacement/adaptation has zero native base input")
    if candidate_count == 0:
        errors.append("no enabled replacement/adaptation candidates in forged-cutting families")
    missing = [
        "Per-recipe authored route ID and material-condition resolution (family overlap is not ownership)",
        "Per-recipe redistributed material/quality/upgrader budgets with explicit native-to-new conservation mapping",
        "Quantified intermediate component inputs, outputs, yields and upstream workshop dependencies",
        "Registered-session Magenheim/vanilla target and resource reconciliation (Gate 0)",
    ]
    return {
        "route": "forged-cutting", "grammar_route_families": len(route["families"]),
        "grammar_operations": len(route["operations"]), "grammar_components": len(route["components"]),
        "external_component_producers": external,
        "native_pickaxe_family_exclusions": pickaxe_exclusions,
        "candidate_native_recipes": candidate_count, "native_cost_rows_checked": checked_costs,
        "native_quality_entries_checked": checked_quality_entries,
        "errors": errors, "missing_conservation_inputs": missing,
        "conservation_verified": False,
        "status": "INVALID_NATIVE_LEDGER" if errors else "BLOCKED_MISSING_AUTHORED_LEDGER",
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    root = Path(__file__).resolve().parent.parent
    parser.add_argument("--catalog", type=Path, default=root / "true-blacksmithing/data/vanilla-catalog.json")
    parser.add_argument("--grammar", type=Path, default=root / "true-blacksmithing/data/manufacturing-grammar.json")
    args = parser.parse_args()
    try:
        catalog, grammar = load(args.catalog), load(args.grammar)
        result = audit(catalog, grammar)
        result["sources"] = {
            str(path): hashlib.sha256(path.read_bytes()).hexdigest()
            for path in (args.catalog, args.grammar)
        }
    except (OSError, ValueError, KeyError, TypeError) as error:
        result = {"status": "INVALID_INPUT", "conservation_verified": False, "errors": [str(error)]}
    print(json.dumps(result, indent=2, sort_keys=True))
    # Never silently turn an unbound native census into a passing conservation gate.
    return 2


if __name__ == "__main__":
    raise SystemExit(main())
