# Magenheim — The Nowhere King Implementation Plan

Status: Durable implementation authority

**2026-09-30 progression correction (0.0.158):** The final encounter belongs to the native
Underworld Great Decay, after all six Deepstones are mounted and awakened. Surface Queen defeat
opens Deep Gate entry; historical King victories preserve access for existing saves. The older
Mistlands-first placement and King-before-Underworld wording below is superseded. Throne and
discovery prefab IDs remain unchanged for save readability. Replacement rewards consume a real
King trophy at the Crown Reliquary; trophies themselves are encounter-only.
Scope: Dark Throne location, Nowhere King boss, encounter, persistence, combat, presentation, drops, trophy, Null Mantle, and Scepter of Inversion
Related authority: `MISTLANDS_CRYSTAL_FOES_PLAN.md` owns the reusable Mistlands crystal-spawner system consumed by the Dark Throne.

## Core encounter fantasy

The Nowhere King is Magenheim's final boss. He is a king, not a giant monster: approximately 3.6–4.0 meters tall, with the crown reaching roughly 4.2 meters. He remains close enough to human scale that players can read his posture, attention, weapon motion, and deliberate approach.

His combat identity is Authority, Precision, Spatial Control, and Relentless Pressure. He normally walks. Violence occurs in short, extremely fast bursts separated by readable recovery windows. The encounter must punish permanent ranged kiting, standing underneath him, and endless one-sided circling without simply invalidating those tactics through arbitrary immunity or teleport spam.

The King wears ancient extremely dark iron ceremonial plate with damaged old-gold ornament. Sections are not broken but absent, revealing impossible empty space. His crown is a tall, narrow, damaged black royal crown whose fragments float slightly above the helmet. His ragged mantle has deliberately uncanny delayed motion.

The King's royal armament is the paired **Last Argument**: two approximately 2.8–3.1 meter swords built as mirrored but unmistakably different weapons. **Firmament** is the galaxy blade: a black royal hilt around a blade that appears to contain depth, stars and slow nebular colour rather than ordinary metal. **Null Gate** is the void blade: a dark framed aperture whose center reads as absence, edged by a restrained violet event-horizon glow. The pair share grip/guard ancestry and scale so they read as one royal set, but they must never be implemented as recolors of one mesh. Physical impacts sound enormously heavy. Firmament cuts carry a thin crystalline/astral report; Null Gate suppresses nearby ambience immediately before a pressure-like crack. **The Last Argument** names the pair collectively rather than one two-handed sword.

## The Dark Throne

The Dark Throne is a standalone, additive Mistlands location, not a Deep Fracture dungeon and not a replacement for any vanilla biome/location authority.

Default world limit: one Dark Throne per world, configurable for testing or alternate server rules. Placement is Mistlands-only by default, very rare, sufficiently distant from world spawn for final-game progression, and validated against unsuitable terrain and conflicting special locations.

The structure is a monumental basalt dais/throne-hall ruin approximately 52 x 60 meters, providing a 45–55 meter combat arena. It uses the basalt/black-stone architectural language already natural to the Mistlands: broad stairs, retaining walls, broken columns, ruined ceremonial architecture, eight large ceremonial candles/braziers, and an enormous black throne at the rear. The King stands before it. He never sits on the throne.

Suggested implementation authorities:

- `DarkThroneDefinition.cs`
- `DarkThroneLayout.cs`
- `DarkThronePrefabFactory.cs`
- `DarkThroneLocationRegistrar.cs`
- `DarkThroneArenaRuntime.cs`
- `DarkThroneEncounterState.cs`

The Dark Throne consumes the reusable `Magenheim_CrystalSpawner_DarkThrone` profile from `MISTLANDS_CRYSTAL_FOES_PLAN.md`. Crystal creatures populate the approaches/perimeter before engagement. Once the King encounter activates, throne-area creature spawners suspend so the boss remains the focus. The King does not continuously summon ordinary crystal adds.

## World safeguards, encounter persistence, and dais leash

The Nowhere King is a unique persistent world encounter and must never depend on ordinary creature despawn rules. Once the Dark Throne location has generated, its durable encounter record becomes the authority for whether the King must exist. The King may be unloaded with the surrounding zone when no players are nearby, but **unloading is not defeat and must never become permanent despawning**. When the Dark Throne zone loads again, the encounter authority reconstructs or resolves the same living King from durable state unless the encounter is explicitly `Defeated`.

The durable Dark Throne record stores at minimum the encounter schema/version, unique encounter/location identity, generated throne anchor, `NeverEncountered`/`Engaged`/`Disengaged`/`Defeated` state, boss health required for the selected reset policy, reward-completion guard, and any other persistent facts required to reconstruct the fight safely. Ordinary AI despawn flags, distance cleanup, day/night cleanup, biome spawn cleanup, or temporary absence of players must never mark the King defeated or permanently remove him.

**Running away counts as disengaging from the encounter, not defeating or deleting it.** When no valid player remains inside the encounter participation boundary for a configurable grace period, the server transitions the encounter to `Disengaged`. The King stops pursuing, cancels transient attacks, clears temporary spatial hazards and target state, and returns to his throne-side home position. The intended default is a full encounter reset after successful disengagement: restore boss health, phase, Null Mantle rolling resistance, Royal Stagger, attack cooldown state, candles, and arena presentation to the pre-engagement baseline. This prevents ranged attrition by repeatedly entering and fleeing. If later design testing prefers health persistence, that becomes an explicit configuration/policy change rather than an accidental consequence of unload behavior.

The King is **hard-leashed to the Dark Throne dais/arena**. `DarkThroneArenaRuntime` owns an authoritative arena boundary derived from the generated location anchor rather than from the current player position. The AI never selects navigation destinations outside that boundary. Royal Advance, Step Between, King's Grasp positioning, lunges, knockback recovery, and every other movement-producing ability must clamp/validate destinations against the same arena authority.

If physics, knockback, navigation failure, ownership transfer, or another mod nevertheless places the King outside the legal dais boundary, the server does not allow him to wander into the Mistlands. It cancels the current attack, clears invalid velocity/path state, and performs a safe authoritative return to the nearest legal recovery point or throne-side home anchor. This correction is an invariant recovery path, not a normal combat teleport and cannot be used by the King to attack players outside the arena.

Players may leave the dais. The King may target players near its edge while they remain valid encounter participants, but he cannot cross the arena boundary to chase them. Once all valid participants have remained outside the disengagement boundary/grace period, the encounter resets as described above. Anti-kiting mechanics therefore operate **inside** the designed arena; they do not justify breaking the leash.

Multiplayer uses the union of valid living encounter participants. One player leaving does not reset the encounter while another valid participant remains inside. Reset/disengagement decisions, boss reconstruction, health restoration, leash corrections, and defeat/reward transitions are server-authoritative. Clients must never independently respawn, reset, or reposition the King.

Suggested condensed authority: `DarkThroneEncounterState.cs` owns durable lifecycle state; `DarkThroneArenaRuntime.cs` owns arena geometry, participant tests, legal-position validation, and recovery anchors; `NowhereKingPersistence.cs` serializes/reconstructs the unique boss. Do not implement separate per-attack leash logic or per-client despawn prevention.

## Boss implementation architecture

Avoid a monolithic AI switch tree and avoid one Harmony patch per attack. Condense decision-making behind a single attack director and shared spatial/physics authorities.

Suggested authorities:

- `NowhereKingRegistrar.cs`
- `NowhereKingPrefabFactory.cs`
- `NowhereKingModelBuilder.cs`
- `NowhereKingAnimator.cs`
- `NowhereKingBrain.cs`
- `NowhereKingCombatState.cs`
- `NowhereKingAttackDirector.cs`
- `NowhereKingSpatialRuntime.cs`
- `GravityInversionRuntime.cs`
- `GravityInversionProfile.cs`
- `AdaptiveResistanceRuntime.cs`
- `NowhereKingNullMantle.cs`
- `NowhereKingRoyalStagger.cs`
- `NowhereKingPersistence.cs`

The attack director owns phase eligibility, tactical intent, cooldowns, target weighting, recovery windows, multiplayer behavioral scaling, anti-kiting pressure, and attack chaining. Individual attacks execute bounded mechanics but do not independently invent global encounter policy.

Durable state machine:

`Dormant -> Engaged -> PhaseOne -> TransitionOne -> PhaseTwo -> TransitionTwo -> PhaseThree -> Disengaged -> Defeated`

`Disengaged` returns to the pre-engagement baseline and can re-enter `Engaged` when a player validly enters/activates the arena. `Defeated` is terminal for boss reconstruction and enables the unique reward-completion state.

The fight has exactly three combat phases. Health boundaries are 70% and 35%. Low-health aggression inside Phase Three may increase without creating a hidden fourth phase.

## Phase One — The King Remains — 100% to 70%

Phase One is a disciplined **twin-sword duel**. The King does not expose his gravity kit yet. Firmament and Null Gate must be visible in separate hands and the hit language must correspond to the weapon motion rather than abstract damage calls.

**Royal Combination:** Firmament opens with a broad horizontal cut, Null Gate answers with a reverse diagonal, and the optional third beat is a clearly telegraphed crossing thrust. Blocking with true endgame equipment is viable; parrying is possible but demanding.

**Crown Breaker:** the King raises both blades and brings them down in a crossed execution strike with enormous direct damage/stagger. Its short-range stone rupture is physical impact from the paired swords, not a gravity spell.

**King's Reach:** a wide scissoring sweep in which the two blades cover different timing windows. The rear quarter remains the intended positional answer.

**Royal Advance:** anti-kiting authority. If the selected player remains beyond melee distance too long, the King walks toward them and gradually accelerates, finishing with a two-blade lunge. It remains clamped to the Dark Throne arena and terminates rather than carrying the King beyond the dais.

At 70%, the King crosses Firmament and Null Gate in front of his body. Three candles extinguish, loose debris begins orbiting upward, and the first visible gravity distortion passes across the arena. From this point onward the swords remain present, but gravity rather than swordsmanship becomes the primary attack language.

## Phase Two — The Weight of Nowhere — 70% to 35%

Phase Two is the dedicated **gravity-control phase**. The King stops trying to win a conventional duel and uses the arena's mass, trajectories and player positioning as weapons. Sword strikes become recovery/spacing actions rather than the core rotation.

**Gravity Inversion — The World Falls Upward:** signature formation breaker. The King crosses the paired Last Argument and charges for approximately 2–2.5 seconds. Violet energy crawls upward, dust and debris lift, the mantle pulls upward, and an expanding gravitational shockwave violently launches affected players. Direct damage is moderate; normal fall physics should determine the dangerous consequence wherever practical. Distance from the King controls vertical/horizontal impulse. Blocking with an ordinary shield does not negate gravity. Correct responses are leaving the radius, precisely dodging the wave, or legitimately triggering Royal Stagger before release.

**King's Grasp:** a telegraphed focused pull that drags a distant target toward the King with little direct damage. It is the principal response to stationary ranged play.

**Crownfall:** a marked player or circular area receives a strong downward gravity pulse after a readable delay. Players already airborne are driven toward the ground; grounded players suffer a heavy stagger/impact rather than an arbitrary unavoidable one-shot.

**Royal Repulse:** a short-range outward gravity burst used when players crowd the King after a recovery window. It creates space without replacing melee hit detection.

**Event Horizon:** a temporary localized gravity well forms at a telegraphed arena point, pulling nearby players toward its center for a short duration before collapsing. It is route pressure, not permanent arena denial.

`NowhereKingSpatialRuntime` owns transient gravity markers, wells and their cleanup so individual attacks do not leak independent GameObject trees. Gravity destinations, forced movement and any King repositioning remain constrained by the shared arena authority.

## Adaptive resistance — The Null Mantle

The Nowhere King's adaptive resistance and the wearable reward must use one authoritative `AdaptiveResistanceRuntime`.

Maintain a rolling window of incoming elemental damage using Magenheim's authoritative elemental identities: Fire, Frost, Storm, Earth, Venom, Radiance, Spirit, and later alignments added through the same registry. When roughly 40% or more of recent elemental damage is dominated by one alignment, the King develops temporary resistance to it. This is resistance, never immunity. Resistance decays when that alignment stops dominating.

This system must observe Magenheim elemental damage independently of whether an equipment crystal is stored by native Magenheim sockets or Jewelcrafting. Never create boss-only duplicate elemental enums or effect definitions.

Server owns the rolling damage window and resistance state. Clients receive presentation state only.

## Transition at 35%

The King genuinely staggers for the first time and drops to one knee. Remaining ceremonial candles extinguish one by one, leaving the final candle beside the throne. He looks toward it; it dies. His crown fractures further and fragments remain floating. The absence inside his armor spreads.

He then **abandons the duel**: Firmament and Null Gate are driven into the dais as visible inert/unstable anchors and cease to be his primary attack tools. The phase transition must make the rules change visually obvious before he begins moving again.

## Phase Three — The Last Candle — 35% to death

Phase Three is a **body-impact phase** built around leaps, ground pounds and readable catastrophic landings. Gravity magic is no longer cast as a standing spell rotation; its residue explains the impossible mass and hang-time of the King's movement, but the player is now reading his body rather than purple floor effects.

**King's Descent:** the King crouches, visibly commits to a target, launches high, and crashes onto the predicted landing point. The telegraph is long enough that a player who keeps moving can escape. The center hit is extremely dangerous; damage and stagger fall sharply with radius.

**Thronebreaker:** a deliberate stationary ground pound. The King raises both arms/body, pauses at the apex of the tell, and slams the dais, producing a radial stone shockwave. The attack is slow and punishing rather than fast and cheap.

**Ruinous Pursuit:** a sequence of two or three shorter jumps used to chase a retreating target. Each hop has lower damage than King's Descent and a visible landing marker. The sequence stops if continuing would cross the arena leash.

**No Kingdom Remains:** the desperation signature. The King performs a short chain of increasingly heavy ground pounds while advancing toward the current target, with distinct expanding shock fronts and recovery after the final impact. It is a movement/spacing examination, not a gravity-wave re-skin.

At very low health Phase Three may shorten recovery windows and increase willingness to chain the existing leap/slam attacks, but it gains no fourth-phase mechanics and does not return to the Phase Two standing gravity rotation.

## Royal Stagger

The King is not permanently staggerable and not immune to Valheim's combat language. Successful difficult parries and qualifying coordinated heavy melee damage build hidden **Royal Stagger**. Reaching threshold causes a brief genuine stagger/opening, then grants temporary stagger resistance to prevent multiplayer stun-locking.

Gravity Inversion cannot be parried. Its Phase Two charge may be interrupted only if players legitimately reach the Royal Stagger threshold before impact. Phase Three leap/ground-pound commitments use their own recovery windows and cannot be canceled by ordinary light stagger once airborne.

Server owns the stagger meter and resistance window.

## Multiplayer behavior

Scale primarily through behavior, not absurd health/damage multiplication. With more players the King changes targets more often; Event Horizon may admit an additional well only when the arena remains readable; King's Grasp increasingly favors players who remain outside melee range; Gravity Inversion naturally disrupts clustered formations; Phase Three leap targeting rotates among participants instead of repeatedly deleting one player. Damage scaling remains modest and health scaling conservative.

The server/authoritative owner controls boss AI, attack selection, phase transitions, Null Mantle calculations, Royal Stagger, spawner suspension, Gravity Inversion hit determination, persistent encounter state, disengagement/reset, dais-leash correction, death, and rewards. Clients own presentation such as particles, audio suppression, distortion, candle visuals, floating debris, and crown presentation.

## Persistence and serialization

Persistence is part of the initial implementation rather than a late retrofit. Use normal Valheim ZDO/network authority where possible.

Durable encounter data includes a schema/version, location/encounter identity, generated throne/home anchor, `NeverEncountered`/`Engaged`/`Disengaged`/`Defeated` state, phase-relevant recovery state where required, candle progression, reward-completion guard, and only those Null Mantle/cooldown facts necessary to prevent save/reload exploits or broken recovery.

Do not serialize transient tears, hitboxes, debris objects, individual animation frames, attack GameObjects, current navigation path, or temporary leash correction. On load, reconstruct presentation and transient state from durable encounter state. If the encounter is not `Defeated`, the Dark Throne authority must be capable of reconstructing the King when the location becomes active even if the creature object itself was previously unloaded or lost.

Death/reward state must be transactional enough that server restart, reconnect, ownership transfer, duplicate death callbacks, or reconstruction cannot create repeated unique rewards.

## Audio and presentation

Physical movement is heavy and restrained. Spatial manipulation briefly suppresses nearby environmental audio before a deep pressure crack. Gravity Inversion has its own bass-heavy buildup, thinning high-frequency sound before impact and a rising pressure tone across the wave.

Voice is sparse. Canonical encounter lines:

- `There is nowhere left.`
- `You still mistake distance for safety.`
- `Then let the last light die.`
- `There was never a throne.`

The King remains visually disciplined: roughly 1.8–2.0x player height and substantially broader. Large Valheim monsters remain physically larger. The room communicates his scale of power.

## Death sequence

The King does not explode. He stops. Firmament and Null Gate go dark where they were abandoned or fall inert if displaced by the final exchange. Crown fragments remain suspended while sections of armor simply cease to exist. The crown eventually falls. When it hits the ground, all extinguished ceremonial candles relight and remaining suspended debris falls. Several seconds of silence precede victory/reward presentation.

## Rewards — authoritative replacement for Fragment of Nowhere

There is **no Fragment of Nowhere capstone material** in this plan. The one-time victory transaction releases the complete royal set: **Firmament**, **Null Gate**, the **Null Mantle**, and the **Nowhere King trophy**. Trophy knowledge also gates later access to the Scepter of Inversion.

All four direct boss drops have post-victory replacement recipes at the **Mycelial Bench** using the first Underworld/Fungal Forest resource tier. Firmament and Null Gate each retain the project's dependency rule by consuming a `Magenheim_Weapon_CrystalSword` chassis plus Worldroot, Understone, Spire and Glowcap-derived materials. The Null Mantle and trophy use the same first-tier economy at costs appropriate to replacement/cosmetic production. This does not bypass the initial boss kill because those resources and the Mycelial Bench economy are reached only after the Dark Throne victory opens the Underworld.

A fifth Mycelial Bench recipe combines one Firmament and one Null Gate into the player-facing **The Last Argument** (`Magenheim_LastArgument_Paired`). The King keeps his enormous ~2.8 m royal swords; a mortal wielder carries the same paired identities at roughly 43% authored scale, about 0.68 m per blade, so they read and handle as an exotic matched knife set rather than comically oversized boss weapons. The item occupies both hands, advances the **Knives** skill, and borrows Valheim's native alternating-hand fast-melee choreography where available. It remains one authoritative item for durability, sockets, hit resolution and networking while Null Gate is presented in the left hand and Firmament in the right.

### The Null Mantle

Prefab identity: `Magenheim_NullMantle`.

The direct unique equipment drop is the King's **black crown**, named **The Null Mantle**. It should be based on/retextured from the appropriate base endgame crown asset where technically/licensing-compatible within the game runtime, using extremely dark damaged metal, old-gold remnants, and the King's visual language.

The Null Mantle is not merely a resistance helmet. **Equipping it visually makes the wearer the new Nowhere King.** While worn, a presentation controller applies the King's inherited appearance to the player without destructively modifying the underlying player prefab or equipment assets:

- a localized **Nowhere black fog** follows/envelops the wearer, using a restrained near-body volume/particle treatment rather than globally replacing biome fog or weather;
- the visible player body/armor receives a **darkened Nowhere material treatment**, driving the silhouette toward the King's black/void appearance while preserving enough equipment form to remain readable;
- the player's eyes become unmistakable **glowing red eyes**, visible through the dark treatment and readable in fog and low light;
- the black crown remains the defining head silhouette, so the transformation reads as succession rather than a generic shadow status effect.

This presentation is equipment-state-driven and reversible. Unequipping the crown restores the player's normal renderer/material state and removes the fog/eye effects. It must never permanently rewrite shared player, armor, or foreign equipment materials. Renderer/material overrides should be instance-local, cached, restored safely on unequip/death/respawn, and rebuilt after model/equipment refreshes where Valheim recreates renderers.

The transformation must replicate correctly in multiplayer: remote players should see the wearer as the new Nowhere King, while the local wearer must not receive a full-screen black-fog obstruction. Presentation may be client-rendered from synchronized equipment state; gameplay resistance remains authoritative through the normal status/equipment authority.

Wearing the Null Mantle also grants a player-scale version of adaptive elemental resistance through the same `AdaptiveResistanceRuntime` used by the boss. The player version is deliberately weaker/shorter-lived than the King's version and remains resistance rather than immunity. Configuration owns thresholds, maximum resistance, observation window, and decay rate.

Suggested authorities are `NullMantleStatusEffect.cs` for gameplay and `NullMantlePresentationRuntime.cs` for the reversible visual inheritance. Do not duplicate adaptation mathematics or mutate shared materials inside either implementation.

### Trophy — The Blackened King

Prefab identity: `Magenheim_TrophyNowhereKing`.

The trophy is the King's **blackened head and crown**. Trophy acquisition is the progression/knowledge trigger for Gravity Inversion technology. The trophy may retain the cosmetic placed behavior of occasionally extinguishing nearby light sources temporarily before allowing them to relight. This behavior has no combat benefit.

The unique trophy is not consumed merely to learn the recipe. The player should be able to display the one-per-world trophy and still craft the unlocked equipment.

### Scepter of Inversion

Prefab identity: `Magenheim_ScepterOfInversion`.
Suggested registrar: `ScepterOfInversionRegistrar.cs`.

Picking up/learning the Nowhere King trophy unlocks the Scepter of Inversion recipe. The recipe should require expensive late-Magenheim resources and Master Crystals; exact costs remain a balance/configuration decision during implementation. The trophy itself is an unlock/knowledge requirement rather than a consumed ingredient by default.

The Scepter grants a player-scale **Gravity Inversion** ability. It uses the same `GravityInversionRuntime` as the boss with a separate `GravityInversionProfile`. The scepter profile has smaller radius, lower impulse, appropriate eitr/stamina/cooldown costs, and hostile-target filtering. Its identity remains battlefield control: modest initial damage, upward launch, interruption, displacement, and normal fall consequences.

Do not copy the boss implementation into a second player implementation. The shared runtime owns wave intersection, radial falloff, vertical/horizontal impulse, physics safety, authority rules, and effect-event description; profiles provide King-versus-Scepter parameters.

## Boss stone requirement

The Nowhere King has a dedicated **boss-location stone** using Valheim's native Vegvisir discovery path. Five `Magenheim_NowhereKingBossStone` sites are distributed through the Mistlands. Their donor is a vanilla Vegvisir stone, recolored into the King's black/violet language without mutating the shared vanilla prefab.

Interacting with the stone calls the native boss-location discovery flow for the unique `Magenheim_DarkThrone` location, adds/shows the boss pin as **The Dark Throne**, and opens the map through normal Valheim behavior. The stone does not invent a second boss-progression flag, does not spawn the King, and does not grant the Underworld unlock; it only points the player toward the existing authoritative encounter.

## Implementation sequence

1. Reconcile current Mistlands location registration, creature registry, combat hooks, elemental authority, ZDO patterns, and existing model/prefab factories.
2. Implement Dark Throne definition, additive placement, arena prefab, basalt architecture, throne, candles, and consumption of the shared Dark Throne crystal-spawner profile.
3. Implement **Dark Throne durable lifecycle authority, non-despawn/reconstruction safeguards, encounter participation/disengagement reset, and the shared dais boundary/leash** before registering the combat-ready King.
4. Author and export the paired Last Argument assets first: `nowhere-king-sword-firmament` and `nowhere-king-sword-null-gate`. They share royal hilt ancestry but have distinct galaxy-versus-void silhouettes/material construction; neither may be a recolor of the other.
5. Implement Nowhere King model/prefab, twin-sword hand binding, networking components, animation controller, and durable encounter identity against that lifecycle authority.
6. Implement encounter persistence and server-authoritative three-phase state machine before complex combat.
7. Implement Phase One and validate readable twin-sword melee/anti-kite behavior plus leash compliance for Royal Advance/lunges.
8. Implement shared spatial/gravity runtime and Phase Two, including Gravity Inversion, King's Grasp, Crownfall, Royal Repulse and Event Horizon.
9. Implement `AdaptiveResistanceRuntime` and boss Null Mantle behavior using existing Magenheim elemental identities.
10. Implement Royal Stagger.
11. Implement Phase Three leap/ground-pound combat, including King's Descent, Thronebreaker, Ruinous Pursuit and No Kingdom Remains.
12. Implement low-health Phase Three aggression and multiplayer behavioral scaling without creating a fourth phase.
13. Implement death transaction and unique reward guard.
14. Implement `Magenheim_NullMantle` using the shared adaptive-resistance authority plus reversible `NullMantlePresentationRuntime` for Nowhere black fog, dark player rendering, glowing red eyes, and crown-driven succession appearance.
15. Implement `Magenheim_TrophyNowhereKing`, trophy knowledge unlock, and cosmetic placed behavior.
16. Implement `Magenheim_ScepterOfInversion` using the shared Gravity Inversion authority.
17. Complete audio/VFX/crown/mantle polish only after mechanics are authoritative and multiplayer-safe.

## Acceptance criteria

A fresh eligible world can generate the rare Dark Throne without replacing vanilla Mistlands content. The location persists and does not duplicate on reload. Crystal defenders use the shared Mistlands crystal-spawner authority and suspend during the boss encounter. Mistlands boss stones use the native Vegvisir discovery mechanism to reveal the unique Dark Throne and do not create a parallel encounter/unlock authority.

The Nowhere King **cannot permanently despawn while undefeated**. Leaving the area, unloading the zone, restarting the server, reconnecting, or ordinary creature cleanup cannot convert an undefeated encounter into an absent boss. Reloading the Dark Throne reconstructs/resolves the unique King from durable encounter state. Running away transitions the encounter to `Disengaged` after the grace period and returns/resets the King rather than killing or despawning him. In multiplayer, the fight remains active while any valid participant remains. The King cannot leave the dais through pathfinding, Royal Advance, Phase Three leaps, knockback, physics, ownership transfer, or foreign-mod interference; invalid positions recover server-authoritatively to a legal arena point without becoming an offensive teleport.

The Nowhere King persists correctly, transitions at the intended health boundaries, retains one authoritative AI/attack director, and cannot duplicate rewards through reconnect/reload/death callback races. All core attacks remain readable and preserve player agency. Null Mantle resistance is adaptive resistance, not immunity. Royal Stagger cannot become a permanent stun-lock. Gravity Inversion uses physical launch behavior where practical and is server-authoritative in multiplayer.

Defeating the King produces `Magenheim_LastArgument_Firmament`, `Magenheim_LastArgument_NullGate`, the wearable `Magenheim_NullMantle` black crown, and `Magenheim_TrophyNowhereKing` blackened head/crown trophy exactly once under the intended reward policy. After the Underworld opens, all four drops can be reproduced at the Mycelial Bench from the first Fungal Forest material economy; the two swords additionally consume Crystal Sword chassis. Firmament and Null Gate can then be combined into `Magenheim_LastArgument_Paired`, a two-hand Knives-skill weapon that displays both royal blades at mortal knife scale. Trophy acquisition unlocks the Scepter of Inversion recipe. Wearing the Null Mantle grants the player-scale adaptive resistance **and visibly transforms the wearer into the successor Nowhere King through localized black fog, darkened/void-like player materials, glowing red eyes, and the black crown silhouette**. Unequipping it completely and safely restores normal presentation, including after death/respawn and equipment renderer refresh. Other multiplayer clients see the transformation without the wearer suffering an obstructive first-person/local fog treatment. The Scepter produces the player-scale Gravity Inversion behavior through the shared runtime rather than copied boss code.

Final acceptance requires compile/runtime registration verification plus disposable-world generation, boss fight, deliberate flee/disengage/reset, zone unload/reload, boss reconstruction, attempted leash escapes, death/reward, Null Mantle equip/unequip/respawn/multiplayer visual tests, save/reload, reconnect, and dedicated-server multiplayer testing when the environment permits.
