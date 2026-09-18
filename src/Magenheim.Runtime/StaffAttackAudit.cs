using System;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Temporary diagnostic. Once every Magenheim staff prefab is registered, reports the attack
/// configuration each one actually shipped with.
/// </summary>
/// <remarks>
/// Live play against 0.0.64 reported that the Master staves of Storm, Venom, Spirit, Fire and Frost
/// deplete stamina and do nothing, Earth fires projectiles that do nothing, and only Radiance works.
/// The log showed a clean load with zero Magenheim errors and every staff and projectile prefab
/// registered, and reading the installed assemblies ruled out the obvious explanations: the weapon's
/// damage does reach the projectile, because Attack.FireProjectileBurst sets
/// hitData.m_damage = weapon.GetDamage() and Projectile.Setup copies it over the prefab's own value;
/// and stamina is consumed inside FireProjectileBurst before the projectile is instantiated, so the
/// burst ran and a projectile object was created for every shot.
///
/// That leaves the shipped values themselves, which are worth reading rather than inferring from
/// the source tables — a registrar that overwrites a field, a payload that resolves to the wrong
/// tier, or a null projectile would all look identical from the outside. This reports what each
/// staff is actually holding, so one run distinguishes a data problem from a plumbing one.
///
/// Remove once the staff defect is understood.
/// </remarks>
internal static class StaffAttackAudit
{
    private static readonly string[] Families = { "Fire", "Frost", "Storm", "Earth", "Venom", "Radiance", "Seidr", "Spirit" };
    private static readonly string[] Tiers = { "Simple", "Crystal", "Advanced", "Master" };
    private static bool _armed;
    private static ManualLogSource? _log;

    internal static void Arm(ManualLogSource log)
    {
        if (_armed) return;
        _log = log ?? throw new ArgumentNullException(nameof(log));
        PrefabManager.OnPrefabsRegistered += Report;
        _armed = true;
    }

    private static void Report()
    {
        var log = _log;
        if (log is null) return;
        try
        {
            var names = (from family in Families from tier in Tiers select $"Magenheim_Staff_{family}_{tier}").ToArray();
            log.LogWarning($"[staff audit] inspecting {names.Length} expected staff identities.");
            foreach (var name in names)
            {
                var prefab = PrefabManager.Instance.GetPrefab(name);
                if (prefab is null)
                {
                    log.LogWarning($"[staff audit] {name}: NOT REGISTERED");
                    continue;
                }
                var drop = prefab.GetComponent<ItemDrop>();
                if (drop is null || drop.m_itemData?.m_shared is null)
                {
                    log.LogWarning($"[staff audit] {name}: no ItemDrop/shared data");
                    continue;
                }
                var shared = drop.m_itemData.m_shared;
                var attack = shared.m_attack;
                if (attack is null)
                {
                    log.LogWarning($"[staff audit] {name}: no attack");
                    continue;
                }
                var projectile = attack.m_attackProjectile;
                var projectileComponent = projectile ? projectile.GetComponent<Projectile>() : null;
                var spawnOnHit = projectileComponent ? projectileComponent.m_spawnOnHit : null;
                log.LogWarning(
                    $"[staff audit] {name} | dmg={Describe(shared.m_damages)} | mult={attack.m_damageMultiplier:0.##} " +
                    $"| stamina={attack.m_attackStamina:0.#} eitr={attack.m_attackEitr:0.#} " +
                    $"| vel={attack.m_projectileVel:0.#}/{attack.m_projectileVelMin:0.#} " +
                    $"acc={attack.m_projectileAccuracy:0.##}/{attack.m_projectileAccuracyMin:0.##} " +
                    $"| n={attack.m_projectiles} bursts={attack.m_projectileBursts} interval={attack.m_burstInterval:0.##} " +
                    $"| type={attack.m_attackType} anim='{attack.m_attackAnimation}' " +
                    $"| projectile='{(projectile ? projectile.name : "<null>")}' " +
                    $"spawnOnHit='{(spawnOnHit ? spawnOnHit.name : "<null>")}' " +
                    $"projDmg={(projectileComponent ? Describe(projectileComponent.m_damage) : "<none>")} " +
                    $"aoe={(projectileComponent ? projectileComponent.m_aoe.ToString("0.##") : "<none>")} " +
                    $"ttl={(projectileComponent ? projectileComponent.m_ttl.ToString("0.##") : "<none>")} " +
                    $"gravity={(projectileComponent ? projectileComponent.m_gravity.ToString("0.##") : "<none>")}");
            }
        }
        catch (Exception exception)
        {
            log.LogWarning("[staff audit] failed: " + exception.Message);
        }
        finally
        {
            PrefabManager.OnPrefabsRegistered -= Report;
            _armed = false;
        }
    }

    private static string Describe(HitData.DamageTypes damage)
    {
        var text = new StringBuilder();
        void Add(string name, float value)
        {
            if (value == 0f) return;
            if (text.Length > 0) text.Append('+');
            text.Append(name).Append(':').Append(value.ToString("0.##"));
        }

        Add("blunt", damage.m_blunt);
        Add("slash", damage.m_slash);
        Add("pierce", damage.m_pierce);
        Add("chop", damage.m_chop);
        Add("pickaxe", damage.m_pickaxe);
        Add("fire", damage.m_fire);
        Add("frost", damage.m_frost);
        Add("lightning", damage.m_lightning);
        Add("poison", damage.m_poison);
        Add("spirit", damage.m_spirit);
        return text.Length == 0 ? "none" : text.ToString();
    }
}
