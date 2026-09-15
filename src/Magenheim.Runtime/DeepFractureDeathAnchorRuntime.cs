using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime
{
    internal static class DeepFractureDeathAnchorRuntime
    {
        private const string RecoveryKey = "magenheim_death_anchor_recoveries";
        private const float RecoveryHealthFraction = 0.35f;

        internal static bool TryPreventDeath(Character character)
        {
            if (character == null) return false;
            var anchor = character.GetComponentInChildren<DeepFractureCrystalComponent>(true);
            if (anchor == null || !anchor.IsDeathAnchorIntact()) return false;

            var view = character.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner()) return false;
            var zdo = view.GetZDO();
            if (zdo == null) return false;

            var recoveryHealth = Mathf.Max(1f, character.GetMaxHealth() * RecoveryHealthFraction);
            character.SetHealth(recoveryHealth);
            zdo.Set(RecoveryKey, zdo.GetInt(RecoveryKey, 0) + 1);
            return true;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    internal static class DeepFractureDeathAnchorPatch
    {
        private static bool Prefix(Character __instance)
        {
            return !DeepFractureDeathAnchorRuntime.TryPreventDeath(__instance);
        }
    }
}
