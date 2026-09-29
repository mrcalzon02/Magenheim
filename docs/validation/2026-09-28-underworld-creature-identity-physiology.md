# Underworld creature identity / physiology pass — 2026-09-28

Status: **source implemented on main; live compile/gameplay acceptance still required.**

## Intent

Keep the existing Valheim creature chassis, rig, controller, attack definitions, hitboxes, network
behavior and loot wherever the body plan is good enough, while making the Underworld derivative
read and behave as a biome-native species rather than a recolored donor.

## Implemented source

`UnderworldCreatureIdentityPass` now runs on every one of the 42 ordinary Underworld creature
prefabs after the owned material skin pass and before Jötunn creature registration.

### Presentation

Each biome supplies a restrained runtime particle language:

- Fungal Forest: spore drift
- Blackwater Deep: bioluminescent motes
- Sulfurous Wastes: sulfur smoke
- Frozen Caverns: rime mist
- Fracture Zones: rift sparks
- Great Decay: decay motes

Selected luminous, spectral, apex and construct identities also receive a local point light. Particle
counts and light ranges are capped so this remains an identity layer rather than a second weather
system.

### Temperament

The original `MonsterAI` remains authoritative. Magenheim adjusts native alert range, view range,
view angle, hearing range, target-circling timing and roaming pressure. Grounded non-constrained
donors receive modest run/acceleration/turn improvements proportional to their aggression profile.

Apex forms use the strongest pressure profile. Ambient forms remain close to donor pressure. Eight
explicit apex species opt into the existing Jötunn `SpawnConfig.HuntPlayer` flag.

### Environmental physiology

The existing `Character.m_damageModifiers` and environmental-tolerance fields are used directly.
Physical donor resistances are retained.

- Fungal: poison resistant, fire weak, water/tar tolerant
- Blackwater: frost resistant, lightning weak, slightly fire weak, water tolerant
- Sulfurous: very fire resistant, very frost weak, slightly poison resistant, fire/smoke tolerant
- Frozen: very frost resistant, very fire weak, water tolerant
- Fracture: lightning resistant, slightly spirit resistant, smoke tolerant
- Great Decay: very poison resistant, spirit/fire weak, water/smoke/tar tolerant

Named stronger overrides currently include Furnace Golem fire immunity, Cryolith Guardian frost
immunity, Fume Wraith/Corpse Orchard/Spore Husk poison immunity, and stronger Fracture Wisp
lightning resistance.

### Attack identity without donor mutation

`UnderworldCreatureElementalAttack` is attached to the derivative prefab. A shared
`Character.Damage` prefix resolves the actual attacker and adds a bounded elemental rider only
when the landed donor hit contains direct physical damage:

- Fungal / Great Decay: poison
- Blackwater / Frozen: frost
- Sulfurous: fire
- Fracture: lightning

The rider is calculated from physical damage only, specifically so elemental damage-over-time ticks
cannot recursively manufacture more rider damage. Vanilla donor attack/item prefabs are not cloned
or mutated.

## Static read-back requirements

Remote source acceptance requires all of the following to remain true:

- roster contains 42 ordinary creature entries;
- all six canonical Underworld biome names are handled by physiology/VFX configuration;
- registrar calls `UnderworldCreatureIdentityPass.Apply` before `CreatureManager.AddCreature`;
- natural spawn `HuntPlayer` is selected through the identity pass rather than enabled globally;
- MonsterAI tuning uses the existing native fields;
- environmental tolerance uses native Character fields;
- elemental rider resolves the landed hit attacker and is restricted to physical donor damage;
- vanilla donor attack prefabs are never mutated.

## Live acceptance still required

The source changes are not a claim of successful installed-game behavior. Complete the dedicated
Underworld creature section in `TESTING.md`: inspect effects under real biome fog/lighting, compare
ambient versus apex pursuit, exercise elemental resistances, compare derivative/vanilla donor hit
payloads, verify no Surface spawning or donor contamination, and repeat combat checks with a second
peer.
