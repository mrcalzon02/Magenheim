using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Shared Underworld creature balance authority. Biome controls progression; role controls the
/// damage/mobility/telegraph tradeoff. Donor rigs and attacks remain the implementation chassis.
/// </summary>
internal static class UnderworldCreatureCombatBalance
{
    internal enum Role
    {
        Swarm,
        Skirmisher,
        Hunter,
        Bruiser,
        Heavy,
        Apex,
    }

    internal static string Apply(GameObject prefab, UnderworldCreaturePrototypes.Entry entry)
    {
        var role = RoleFor(entry.Name);
        var profile = Build(entry.Biome, role);
        Apply(prefab, profile);
        return Describe(profile);
    }

    internal static string ApplySurtling(GameObject prefab, UnderworldSurtlings.Entry entry)
    {
        var role = entry.Element switch
        {
            "Wind" => Role.Skirmisher,
            "Fire" => Role.Hunter,
            "Water" => Role.Hunter,
            "Earth" => Role.Heavy,
            "Radiance" => Role.Bruiser,
            "Umbral" => Role.Hunter,
            _ => throw new InvalidOperationException($"Unknown Surtling element '{entry.Element}' has no combat role."),
        };

        var progressionHome = entry.Home switch
        {
            "Sulfurous Wastes" => "Sulfurous Wastes",
            "Blackwater Deep" => "Blackwater Deep",
            "Fracture Zones" => "Fracture Zones",
            "Luminous crystal sites" => "Fracture Zones",
            "Low-luminosity deeps" => "Great Decay",
            _ => throw new InvalidOperationException($"Unknown Surtling home '{entry.Home}' has no progression band."),
        };

        var profile = Build(progressionHome, role);
        Apply(prefab, profile);
        return Describe(profile);
    }

    private static void Apply(GameObject prefab, Profile profile)
    {
        var character = prefab.GetComponent<Character>()
            ?? throw new InvalidOperationException($"{prefab.name} lost Character authority before combat balance.");

        character.m_health = profile.Health;

        // Heavy damage buys the player more readable positioning time. Fast creatures deliberately
        // stay cheap per hit rather than becoming tiny high-DPS blenders.
        character.m_walkSpeed *= profile.MovementMultiplier;
        character.m_runSpeed *= profile.MovementMultiplier;
        character.m_swimSpeed *= profile.MovementMultiplier;
        character.m_flySlowSpeed *= profile.MovementMultiplier;
        character.m_flyFastSpeed *= profile.MovementMultiplier;
        character.m_acceleration *= Mathf.Lerp(.82f, 1.10f, Mathf.InverseLerp(.60f, 1.15f, profile.MovementMultiplier));
        character.m_turnSpeed *= Mathf.Lerp(.84f, 1.08f, Mathf.InverseLerp(.60f, 1.15f, profile.MovementMultiplier));

        var ai = prefab.GetComponent<MonsterAI>();
        if (ai)
            ai.m_minAttackInterval = Mathf.Max(ai.m_minAttackInterval, profile.MinimumAttackInterval);

        var scaler = prefab.GetComponent<MagenheimCreatureCombatScaling>();
        if (!scaler) scaler = prefab.AddComponent<MagenheimCreatureCombatScaling>();
        scaler.Configure(profile.DamageMultiplier, profile.DamageCap);
    }

    private static Profile Build(string biome, Role role)
    {
        var (baseHealth, biomeDamage, baseDamageCap) = biome switch
        {
            "Fungal Forest" => (180f, .90f, 36f),
            "Blackwater Deep" => (220f, 1.00f, 44f),
            "Sulfurous Wastes" => (270f, 1.10f, 54f),
            "Frozen Caverns" => (330f, 1.20f, 66f),
            "Fracture Zones" => (410f, 1.32f, 80f),
            "Great Decay" => (500f, 1.45f, 96f),
            _ => throw new InvalidOperationException($"Unknown Underworld biome '{biome}' has no combat progression band."),
        };

        var (healthScale, roleDamage, capScale, movement, attackInterval) = role switch
        {
            Role.Swarm => (.55f, .45f, .55f, 1.15f, .75f),
            Role.Skirmisher => (.80f, .65f, .80f, 1.08f, 1.00f),
            Role.Hunter => (1.10f, .85f, 1.00f, 1.00f, 1.40f),
            Role.Bruiser => (1.55f, 1.05f, 1.25f, .88f, 2.00f),
            Role.Heavy => (2.15f, 1.25f, 1.55f, .72f, 2.80f),
            Role.Apex => (3.00f, 1.45f, 1.90f, .60f, 3.60f),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };

        return new Profile(
            role,
            Mathf.Round(baseHealth * healthScale),
            movement,
            biomeDamage * roleDamage,
            baseDamageCap * capScale,
            attackInterval);
    }

    private static Role RoleFor(string name)
    {
        switch (name)
        {
            case "Lantern Moth":
            case "Sporeling":
            case "Cave Ray":
            case "Ashmite":
            case "Rime Moth":
            case "Frost Tick":
            case "Fracture Wisp":
            case "Rift Skitter":
            case "Rotling":
                return Role.Swarm;

            case "Capcrawler":
            case "Gloomfin":
            case "Shoreclaw":
            case "Vent Spitter":
            case "Pale Burrower":
            case "Shardwing":
            case "Gravity Leech":
            case "Carrion Bloom":
            case "Marrow Creeper":
                return Role.Skirmisher;

            case "Mycelial Stalker":
            case "Shelf Lurker":
            case "Lantern Angler":
            case "Cinder Hound":
            case "Fume Wraith":
            case "Magma Leaper":
            case "Iceblind":
            case "Rimewing":
            case "Chasm Stalker":
            case "Spore Husk":
            case "Decay Hound":
                return Role.Hunter;

            case "Puffback":
            case "Blackwater Lamprey":
            case "Basalt Crawler":
            case "Graft Warden":
                return Role.Bruiser;

            case "Abyss Shellback":
            case "Stonebound":
            case "Corpse Orchard":
                return Role.Heavy;

            case "Crowncap Brute":
            case "Deep Hunter":
            case "Furnace Golem":
            case "Glacier Stalker":
            case "Cryolith Guardian":
            case "Rift Colossus":
                return Role.Apex;

            default:
                throw new InvalidOperationException($"Underworld creature '{name}' has no combat role. New fauna must be balanced explicitly.");
        }
    }

    private static string Describe(Profile profile) =>
        $"{profile.Role} balance hp={profile.Health:0}, move x{profile.MovementMultiplier:0.00}, " +
        $"damage x{profile.DamageMultiplier:0.00} cap={profile.DamageCap:0.#}, attack floor={profile.MinimumAttackInterval:0.00}s";

    private readonly struct Profile
    {
        internal Profile(Role role, float health, float movementMultiplier, float damageMultiplier, float damageCap, float minimumAttackInterval)
        {
            Role = role;
            Health = health;
            MovementMultiplier = movementMultiplier;
            DamageMultiplier = damageMultiplier;
            DamageCap = damageCap;
            MinimumAttackInterval = minimumAttackInterval;
        }

        internal Role Role { get; }
        internal float Health { get; }
        internal float MovementMultiplier { get; }
        internal float DamageMultiplier { get; }
        internal float DamageCap { get; }
        internal float MinimumAttackInterval { get; }
    }
}

internal sealed class MagenheimCreatureCombatScaling : MonoBehaviour
{
    [SerializeField] private float _damageMultiplier = 1f;
    [SerializeField] private float _damageCap = float.MaxValue;

    internal void Configure(float multiplier, float cap)
    {
        _damageMultiplier = Mathf.Clamp(multiplier, .20f, 3f);
        _damageCap = Mathf.Max(1f, cap);
    }

    internal void ScaleOutgoing(HitData hit)
    {
        if (hit == null || Mathf.Approximately(_damageMultiplier, 1f)) return;
        Scale(ref hit.m_damage, _damageMultiplier);
    }

    internal void CapOutgoing(HitData hit)
    {
        if (hit == null || _damageCap <= 0f) return;
        var total = hit.GetTotalDamage();
        if (total <= _damageCap || total <= 0f || float.IsNaN(total) || float.IsInfinity(total)) return;
        Scale(ref hit.m_damage, _damageCap / total);
    }

    private static void Scale(ref HitData.DamageTypes damage, float factor)
    {
        damage.m_damage *= factor;
        damage.m_blunt *= factor;
        damage.m_slash *= factor;
        damage.m_pierce *= factor;
        damage.m_chop *= factor;
        damage.m_pickaxe *= factor;
        damage.m_fire *= factor;
        damage.m_frost *= factor;
        damage.m_lightning *= factor;
        damage.m_poison *= factor;
        damage.m_spirit *= factor;
    }
}
