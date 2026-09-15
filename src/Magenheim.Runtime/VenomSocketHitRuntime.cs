using System;
using System.Collections.Generic;
using Magenheim.Core.Socketing;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Executes one contamination processor for the aggregate Venom resonance on the attacking item.
/// The primary victim receives the strongest exposure; nearby enemies receive a weaker lingering
/// contamination pulse. Repeated Venom crystals strengthen this one processor instead of adding
/// independent poison procs.
/// </summary>
internal static class VenomSocketHitRuntime
{
    [ThreadStatic]
    private static bool _executingContamination;

    internal static void ApplyOnHit(Character target, HitData hit)
    {
        if (_executingContamination || target is null || hit is null || target.IsDead() || !hit.HaveAttacker())
            return;

        var attacker = hit.GetAttacker() as Humanoid;
        if (attacker is null || ReferenceEquals(attacker, target))
            return;

        var weapon = attacker.GetCurrentWeapon();
        if (weapon is null || !SocketBehaviorRuntime.TryResolve(weapon, ElementalAlignment.Venom, out var activation))
            return;

        var primaryPoison = PoisonDamage(activation);
        if (primaryPoison <= 0f)
            return;

        var origin = target.transform.position;
        var radius = ContaminationRadius(activation);
        var colliders = Physics.OverlapSphere(origin, radius, LayerMask.GetMask("character"), QueryTriggerInteraction.Collide);
        var affected = new HashSet<Character>();

        try
        {
            _executingContamination = true;
            ApplyExposure(target, attacker, primaryPoison, activation, true);
            affected.Add(target);

            foreach (var collider in colliders)
            {
                var nearby = collider.GetComponentInParent<Character>();
                if (nearby is null || nearby.IsDead() || ReferenceEquals(nearby, attacker) || !affected.Add(nearby))
                    continue;
                if (attacker is Player player && !BaseAI.IsEnemy(player, nearby))
                    continue;

                ApplyExposure(nearby, attacker, primaryPoison * SecondaryExposure(activation), activation, false);
            }
        }
        finally
        {
            _executingContamination = false;
        }
    }

    private static void ApplyExposure(
        Character victim,
        Humanoid attacker,
        float poisonDamage,
        BehavioralResonanceActivation activation,
        bool primary)
    {
        var direction = victim.transform.position - attacker.transform.position;
        if (direction.sqrMagnitude < .001f)
            direction = attacker.transform.forward;
        direction.Normalize();

        var contamination = new HitData
        {
            m_damage = new HitData.DamageTypes { m_poison = poisonDamage },
            m_point = victim.transform.position,
            m_dir = direction,
            m_pushForce = 0f,
            m_staggerMultiplier = primary ? CorrosionStagger(activation) : 1f,
        };
        contamination.SetAttacker(attacker);
        victim.Damage(contamination);
    }

    private static float PoisonDamage(BehavioralResonanceActivation activation)
    {
        var tierBase = activation.HighestTier switch
        {
            CrystalTier.Rough => 1f,
            CrystalTier.Simple => 2.25f,
            CrystalTier.Crystal => 3.75f,
            CrystalTier.Advanced => 5.5f,
            CrystalTier.Master => 8f,
            _ => 0f,
        };
        return tierBase * Mathf.Clamp((float)activation.EffectiveTierPower, .5f, 8f);
    }

    private static float ContaminationRadius(BehavioralResonanceActivation activation) =>
        Mathf.Clamp(1.5f + .24f * (float)activation.EffectiveTierPower, 1.5f, 3.75f);

    private static float SecondaryExposure(BehavioralResonanceActivation activation) =>
        Mathf.Clamp(.30f + .035f * activation.ContributingCrystalCount, .30f, .55f);

    private static float CorrosionStagger(BehavioralResonanceActivation activation) =>
        Mathf.Clamp(1f + .08f * (float)activation.EffectiveTierPower, 1f, 1.65f);
}
