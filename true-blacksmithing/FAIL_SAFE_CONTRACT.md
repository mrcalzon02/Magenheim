# True Blacksmithing Fail-Safe Contract

This contract is mandatory for every implementation stage.

## Baseline authority

Valheim/Magenheim baseline crafting remains authoritative until a complete True Blacksmithing activation succeeds.

If the subsystem is disabled, configuration cannot be read, required assets are missing, target recipes cannot be resolved, compatibility validation fails, or activation throws, Magenheim must continue without the True Blacksmithing overhaul.

A True Blacksmithing defect must not become a Magenheim startup defect.

## Startup sequence

1. Read the startup-scoped master switch.
2. If disabled, perform no True Blacksmithing recipe mutations.
3. If enabled, construct the complete activation plan.
4. Preflight all required recipe targets, station targets, item prefabs, asset references, localization keys, and compatibility conditions.
5. Only after preflight succeeds may recipe replacement begin.
6. Every reversible mutation must be registered with the mutation journal before it is applied.
7. Any activation exception trips the circuit breaker and rolls reversible mutations back in reverse order.
8. The subsystem enters `Faulted` for the remainder of the session rather than retrying repeatedly.

## Runtime fault behavior

A systemic True Blacksmithing runtime error must fail the requested operation safely, trip the subsystem circuit breaker when continuing would threaten recipe integrity, and restore baseline recipe mutations at the next safe boundary.

Do not repeatedly throw from a broken station every frame.

Do not silently continue with half of the recipe graph converted.

## Additive registration

Custom items, prefabs, icons, and stations are safer to register additively than to unregister during a live session.

If fallback occurs after additive registration:

- leave registered prefabs inert if removing them could invalidate inventories or world objects;
- restore baseline crafting routes;
- stop True Blacksmithing process handlers;
- prevent new subsystem-only transformations;
- preserve save readability.

Fallback is about restoring playable progression, not pretending the subsystem never existed in memory.

## Recipe replacement rules

Before replacing a vanilla or Magenheim recipe, capture enough baseline state to restore it.

Never:

- remove the original route and then begin validating the replacement;
- mutate a recipe without a rollback entry;
- leave an output unreachable if a replacement fails;
- consume materials before the authoritative operation has succeeded;
- make a Magenheim recipe depend on True Blacksmithing when the subsystem is disabled.

## Multiplayer

The server/host is authoritative for whether True Blacksmithing is active.

Before gameplay mutations are enabled, peers must agree on the subsystem activation state and compatible content definition. If authority cannot be established safely, True Blacksmithing remains inactive for that session.

A disagreement must not create client-only recipes or divergent item transformations.

## Save safety

True Blacksmithing fallback must never:

- delete inventory items;
- rewrite existing equipment into a different prefab merely to disable the subsystem;
- destroy placed stations;
- remove world objects from saves;
- perform destructive migration automatically.

Intermediate components already owned by a player may remain inert/recoverable after fallback. Baseline recipes must remain available so progression cannot be trapped.

## Permanent requirement

The master feature switch may eventually default on after the full integrated test campaign passes.

The fail-safe requirement never turns off.
