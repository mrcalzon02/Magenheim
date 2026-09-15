using System;
using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Managers;
using Magenheim.Core.Socketing;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Executes the Storm behavioral socket identity after a successful weapon hit. One resolved
/// Storm activation creates one Magenheim-owned electrical discharge at the struck target; same-
/// element sockets strengthen that discharge through resonance rather than creating extra procs.
/// </summary>
internal static class StormSocketHitRuntime
{
    [ThreadStatic]
    private static bool _executingSecondaryDischarge;

    internal static void ApplyOnHit(Character target, HitData hit)
    {
        if (_executingSecondaryDischarge || target is null || hit is null || target.IsDead() || !hit.HaveAttacker())
            return;

        var attacker = hit.GetAttacker() as Humanoid;
        if (attacker is null || ReferenceEquals(attacker, target))
            return;

        var weapon = attacker.GetCurrentWeapon();
        if (weapon is null || !SocketBehaviorRuntime.TryResolveStorm(weapon, out var activation))
            return;

        var damage = StormDamage(activation);
        if (damage <= 0f)
            return;

        var radius = StormRadius(activation);
        var attackForce = StormForce(activation);
        var origin = target.transform.position;
        var colliders = Physics.OverlapSphere(origin, radius, LayerMask.GetMask("character"), QueryTriggerInteraction.Collide);
        var struck = new HashSet<Character>();

        try
        {
            _executingSecondaryDischarge = true;
            foreach (var collider in colliders)
            {
                var secondaryTarget = collider.GetComponentInParent<Character>();
                if (secondaryTarget is null || secondaryTarget.IsDead() || ReferenceEquals(secondaryTarget, attacker) || ReferenceEquals(secondaryTarget, target))
                    continue;
                if (!struck.Add(secondaryTarget))
                    continue;
                if (attacker is Player player && !BaseAI.IsEnemy(player, secondaryTarget))
                    continue;

                var secondaryHit = new HitData
                {
                    m_damage = new HitData.DamageTypes { m_lightning = damage },
                    m_point = secondaryTarget.transform.position,
                    m_dir = (secondaryTarget.transform.position - origin).normalized,
                    m_pushForce = attackForce,
                };
                secondaryHit.SetAttacker(attacker);
                secondaryTarget.Damage(secondaryHit);
            }
        }
        finally
        {
            _executingSecondaryDischarge = false;
        }
    }

    private static float StormDamage(BehavioralResonanceActivation activation)
    {
        var tierBase = activation.HighestTier switch
        {
            CrystalTier.Rough => 2.5f,
            CrystalTier.Simple => 4f,
            CrystalTier.Crystal => 6f,
            CrystalTier.Advanced => 8.5f,
            CrystalTier.Master => 12f,
            _ => 0f,
        };
        return tierBase * Mathf.Clamp((float)activation.EffectiveTierPower, .5f, 8f);
    }

    private static float StormRadius(BehavioralResonanceActivation activation) =>
        Mathf.Clamp(2.25f + .32f * (float)activation.EffectiveTierPower, 2.25f, 5.5f);

    private static float StormForce(BehavioralResonanceActivation activation) =>
        Mathf.Clamp(6f + 2.25f * (float)activation.EffectiveTierPower, 6f, 28f);
}

[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
internal static class StormSocketHitPatch
{
    private static void Postfix(Character __instance, HitData hit)
    {
        StormSocketHitRuntime.ApplyOnHit(__instance, hit);
        VenomSocketHitRuntime.ApplyOnHit(__instance, hit);
    }
}
