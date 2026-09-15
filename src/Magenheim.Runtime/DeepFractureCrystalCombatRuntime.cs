using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime
{
    internal static class DeepFractureCrystalCombatRuntime
    {
        private const float ArmorDamageMultiplier = 0.72f;
        private const float ResistanceDamageMultiplier = 0.82f;
        private const float RegenerationInterval = 2f;
        private const float RegenerationFraction = 0.0125f;

        internal static void ModifyIncomingDamage(Character character, HitData hit)
        {
            if (character == null || hit == null) return;
            var components = character.GetComponentsInChildren<DeepFractureCrystalComponent>(true);
            var multiplier = 1f;
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null || !component.IsFunctionIntact()) continue;
                if (component.Function == DeepFractureCrystalFunction.Armor) multiplier *= ArmorDamageMultiplier;
                else if (component.Function == DeepFractureCrystalFunction.Resistance) multiplier *= ResistanceDamageMultiplier;
            }
            if (multiplier < 1f) hit.ApplyModifier(multiplier);
        }

        internal static void Regenerate(Character character)
        {
            if (character == null || character.IsDead()) return;
            var components = character.GetComponentsInChildren<DeepFractureCrystalComponent>(true);
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component != null && component.Function == DeepFractureCrystalFunction.Regeneration && component.IsFunctionIntact())
                {
                    var maximum = character.GetMaxHealth();
                    if (character.GetHealth() < maximum) character.Heal(maximum * RegenerationFraction, true);
                    return;
                }
            }
        }

        internal static float RegenerationPeriod { get { return RegenerationInterval; } }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    internal static class DeepFractureCrystalDamagePatch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            DeepFractureCrystalCombatRuntime.ModifyIncomingDamage(__instance, hit);
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.CustomFixedUpdate))]
    internal static class DeepFractureCrystalRegenerationPatch
    {
        private static readonly System.Collections.Generic.Dictionary<int, float> NextTick = new System.Collections.Generic.Dictionary<int, float>();
        private static void Postfix(Character __instance)
        {
            if (__instance == null) return;
            var id = __instance.GetInstanceID();
            float next;
            if (NextTick.TryGetValue(id, out next) && Time.time < next) return;
            NextTick[id] = Time.time + DeepFractureCrystalCombatRuntime.RegenerationPeriod;
            DeepFractureCrystalCombatRuntime.Regenerate(__instance);
        }
    }
}
