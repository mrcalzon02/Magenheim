# Jewelcrafting Resonance Semantics

## Decision

Magenheim crystals are balanced around one elemental effect package per equipment item. Jewelcrafting permits multiple equipment sockets and therefore introduces an equipment state that standalone Magenheim cannot produce. Magenheim must not translate each occupied Jewelcrafting socket into an independent full-strength Magenheim effect package: doing so would turn three Master crystals into roughly three copies of a Master effect and would multiply proc/event processors that were designed to exist once.

Jewelcrafting remains the equipment socket storage, UI, insertion/removal and persistence authority when the integration provider selects it. Magenheim remains the authority for crystal identity, Crystal Shaping, tiers, elemental behavior, effect mathematics, architecture, furniture, rituals, production and all non-equipment crystal consumers.

The compatibility rule is therefore **aggregate resonance, not additive duplicate effects**.

## Resonance model

For every equipped item, Magenheim reads the Magenheim crystal prefabs present in Jewelcrafting sockets and groups them by elemental alignment. Each elemental family produces at most one runtime Magenheim effect package for that item.

Within an elemental family, crystals are ordered by their tier power from strongest to weakest. Their contribution multipliers default to:

- first crystal: 100%
- second crystal: 50%
- third crystal: 25%
- fourth and subsequent crystals: 12.5% each

The elemental resonance power is the sum of each crystal's normal tier power multiplied by the contribution for its ordinal position. Tier power remains authoritative: a Rough crystal is not treated as equivalent to a Master crystal merely because both occupy one socket.

Conceptually:

`element resonance = Σ(tier power × ordinal contribution)`

Standalone Magenheim naturally collapses to the original behavior because its one-crystal equipment state always uses the first 100% contribution.

## Runtime invariant

**A Magenheim elemental effect may scale from multiple crystals, but its runtime behavior is instantiated at most once per equipment item per elemental family.**

Repeated Storm crystals therefore strengthen one Storm package rather than creating multiple independent chain-lightning processors. Repeated Venom crystals strengthen one contamination package rather than running several poison/area-denial processors concurrently. The same rule applies to every Magenheim alignment.

Numerical effect components may consume the aggregate resonance power. Proc chance, cooldown, chaining, hazard creation, crowd-control and other behavioral mechanics must explicitly consume normalized resonance rather than being independently subscribed once per socket.

## Mixed elements

Diminishing returns are calculated independently per elemental family. This intentionally creates a specialization-versatility tradeoff.

Three same-element crystals specialize one family but experience diminishing contribution. Three different elemental crystals establish three first-position resonances, providing broader behavior without tripling any one effect. Mixed elements do not implicitly fuse into new effects unless Magenheim explicitly defines such a mechanic later.

## Scope and non-interference

The resonance curve applies only to Magenheim crystal families. It must not modify Jewelcrafting's own gem stacking, effect calculations or third-party gem behavior.

Magenheim crystals remain one authoritative set of item prefabs. The Jewelcrafting adapter registers/reads those same prefabs; it does not create parallel Jewelcrafting-only copies. Architecture, furniture, custom producers, rituals and other Magenheim systems continue consuming Magenheim crystals directly and never pass through Jewelcrafting compatibility.

Magenheim must not double-apply effects. When Jewelcrafting owns equipment socket storage, native `magenheim.sockets.v1` equipment effect application is disabled and the Jewelcrafting aggregate resonance reader is the equipment input to Magenheim effect calculation.

## Configuration

The default resonance curve is `1.0,0.5,0.25,0.125`. It is server/modpack configurable under `Socketing.Integration` as `MagenheimResonanceMultipliers` using a comma-separated list of non-negative finite multipliers.

If an item contains more Magenheim crystals of one alignment than configured entries, the final configured multiplier repeats for all remaining crystals. This makes the configuration stable against Jewelcrafting or third-party equipment exposing more sockets than anticipated.

Recommended default behavior is monotonic non-increasing multipliers. Validation rejects negative, NaN, infinity and empty curves. A server owner may deliberately configure another curve, including `1,1,1` for additive numerical stacking or `1,0,0` for strict one-effective-crystal behavior, without changing Jewelcrafting globally.

## Migration consequence

Existing `magenheim.sockets.v1` equipment migration must write Magenheim crystal prefab identities into Jewelcrafting through its supported API, read the resulting socket state back, verify the expected crystals, and retain Magenheim recovery metadata until conversion is verified. Migration does not convert a crystal into a new Jewelcrafting item family and does not precompute permanent resonance values; resonance is derived from current socket contents.
