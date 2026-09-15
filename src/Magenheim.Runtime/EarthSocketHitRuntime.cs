using System;
using System.Collections.Generic;
using HarmonyLib;
using Magenheim.Core.Socketing;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Executes Earth socket resonance as kinetic control rather than another elemental damage proc.
/// One Earth activation creates one short-range seismic impulse centered on the struck target.
/// Additional Earth crystals strengthen the same impulse through resonance; they never create
/// additional per-socket processors.
/// </summary>
internal static class EarthSocketHitRuntime
{
    [ThreadStatic]
    private static bool _executingSeismicImpulse;

    internal static void ApplyOnHit(Character target, HitData hit)
    {
        if (_executingSeismicImpulse || target is null || hit is null || target.IsDead() || !hit.HaveAttacker())
            return;

        var attacker = hit.GetAttacker() as Humanoid;
        if (attacker is null || ReferenceEquals(attacker, target))
            return;

        var weapon = attacker.GetCurrentWeapon();
        if (weapon is null || !SocketBehaviorRuntime.TryResolveEarth(weapon, out var activation))
            return;

        var radius = Radius(activation);
        var pushForce = PushForce(activation);
        var staggerDamage = StaggerDamage(activation);
        var origin = target.transform.position;
        var colliders = Physics.OverlapSphere(origin, radius, LayerMask.GetMask("character"), QueryTriggerInteraction.Collide);
        var struck = new HashSet<Character>();

        try
        {
            _executingSeismicImpulse = true;
            foreach (var collider in colliders)
            {
                var affected = collider.GetComponentInParent<Character>();
                if (affected is null || affected.IsDead() || ReferenceEquals(affected, attacker))
                    continue;
                if (!struck.Add(affected))
                    continue;
                if (attacker is Player player && !BaseAI.IsEnemy(player, affected))
                    continue;

                var direction = affected.transform.position - origin;
                if (direction.sqrMagnitude < .001f)
                    direction = affected.transform.position - attacker.transform.position;
                direction.y = Mathf.Max(.12f, direction.y);
                direction.Normalize();

                var seismicHit = new HitData
                {
                    m_damage = new HitData.DamageTypes { m_blunt = staggerDamage },
                    m_point = affected.transform.position,
                    m_dir = direction,
                    m_pushForce = pushForce,
                    m_staggerMultiplier = StaggerMultiplier(activation),
                };
                seismicHit.SetAttacker(attacker);
                affected.Damage(seismicHit);
            }
        }
        finally
        {
            _executingSeismicImpulse = false;
        }
    }

    private static float Radius(BehavioralResonanceActivation activation) =>
        Mathf.Clamp(1.75f + .28f * (float)activation.EffectiveTierPower, 1.75f, 4.25f);

    private static float PushForce(BehavioralResonanceActivation activation) =>
        Mathf.Clamp(10f + 3.5f * (float)activation.EffectiveTierPower, 10f, 38f);

    private static float StaggerDamage(BehavioralResonanceActivation activation)
    {
        var tierBase = activation.HighestTier switch
        {
            CrystalTier.Rough => 1.5f,
            CrystalTier.Simple => 2f,
            CrystalTier.Crystal => 3f,
            CrystalTier.Advanced => 4f,
            CrystalTier.Master => 5.5f,
            _ => 0f,
        };
        return tierBase * Mathf.Clamp((float)activation.EffectiveTierPower, .5f, 8f);
    }

    private static float StaggerMultiplier(BehavioralResonanceActivation activation) =>
        Mathf.Clamp(1.15f + .18f * (float)activation.EffectiveTierPower, 1.15f, 2.75f);
}

[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
internal static class EarthSocketHitPatch
{
    private static void Postfix(Character __instance, HitData hit) =>
        EarthSocketHitRuntime.ApplyOnHit(__instance, hit);
}
