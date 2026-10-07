using System;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Presents the mortal-scale Last Argument as a true paired weapon: Firmament rides the normal
/// right-hand item attachment while Null Gate is reconstructed on the left hand. Gameplay remains
/// one authoritative item, so durability, sockets, networking and hit resolution cannot diverge
/// between two independently equipped weapon instances.
/// </summary>
internal sealed class LastArgumentPlayerVisualRuntime : MonoBehaviour
{
    internal const float PlayerBladeScale = 0.43f;

    private Player _player = null!;
    private GameObject? _offHand;
    private Transform? _leftHand;
    private Transform? _leftForearm;

    private void Awake() => _player = GetComponent<Player>();

    internal void Sync()
    {
        if (_player is null) return;

        var weapon = _player.GetCurrentWeapon();
        var active = weapon?.m_dropPrefab is not null &&
                     string.Equals(
                         weapon.m_dropPrefab.name,
                         NowhereKingRewardRegistrar.PairedLastArgumentPrefabName,
                         StringComparison.Ordinal);

        if (!active)
        {
            Clear();
            return;
        }

        if (!_offHand) BuildOffHand();
    }

    private void BuildOffHand()
    {
        // Valheim's rig is not guaranteed to use Unity's Humanoid avatar mode.
        // VisEquipment already owns the authoritative hand attachment transforms.
        var equipment = GetComponentInChildren<VisEquipment>(true);
        if (!equipment || !equipment.m_leftHand) return;
        _leftHand = equipment.m_leftHand;
        _leftForearm = _leftHand.parent;
        if (!_leftForearm)
            throw new InvalidOperationException(
                "The Last Argument requires the native left-hand attachment parent.");

        _offHand = ModelAssets.Load(
            gameObject,
            NowhereKingRewardRegistrar.NullGateModelId,
            item: false,
            scale: PlayerBladeScale,
            hideOriginal: false,
            parent: _leftHand);
        _offHand.name = "Magenheim_LastArgument_NullGate_LeftHand";
        Orient(_offHand.transform, _leftHand, _leftForearm);
    }

    private static void Orient(Transform blade, Transform hand, Transform forearm)
    {
        var outward = hand.position - forearm.position;
        if (outward.sqrMagnitude < 1e-5f) outward = hand.forward;
        outward.Normalize();

        var up = Vector3.up - Vector3.Dot(Vector3.up, outward) * outward;
        if (up.sqrMagnitude < 1e-5f) up = hand.up;

        var world = Quaternion.LookRotation(outward, up.normalized);
        blade.localPosition = Vector3.zero;
        blade.localRotation = Quaternion.Inverse(hand.rotation) * world;
        blade.localScale = Vector3.one * PlayerBladeScale;
        // The paired left blade bypasses held-item alignment. Its authored origin is at
        // the guard; move the grip centre onto the hand using the same seating as the right.
        HeldModelAlignment.SeatGrip(hand, blade.gameObject, NowhereKingRewardRegistrar.NullGateModelId);
    }

    private void Clear()
    {
        if (_offHand is not null) Destroy(_offHand);
        _offHand = null;
        _leftHand = null;
        _leftForearm = null;
    }

    private void OnDestroy() => Clear();
}

[HarmonyPatch(typeof(Player), "Update", new Type[0])]
internal static class LastArgumentPlayerVisualPatch
{
    [HarmonyPostfix]
    private static void Postfix(Player __instance)
    {
        if (__instance is null) return;
        var runtime = __instance.GetComponent<LastArgumentPlayerVisualRuntime>();
        if (runtime is null) runtime = __instance.gameObject.AddComponent<LastArgumentPlayerVisualRuntime>();
        runtime.Sync();
    }
}
