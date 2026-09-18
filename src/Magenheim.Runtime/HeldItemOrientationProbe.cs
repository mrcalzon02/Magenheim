using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Temporary diagnostic. Reports the attach-space transform of whatever the local player is
/// holding, once per distinct item, so a vanilla weapon and a Magenheim weapon can be compared
/// directly in the log.
/// </summary>
/// <remarks>
/// The held-model gates assert a convention — long axis local +Y, grip at −Y, working end at +Y —
/// that has never been checked against how Valheim actually presents a held item. Measuring the
/// committed crossbow payload disproved the convention rather than the asset: its prod, the 1.32m
/// limb span, already sits at the +Y end, which is the end the convention calls "working", and it
/// still points its rear at the target in play. Every weapon passes both gates and more than half
/// are visibly wrong, so the gates are measuring self-consistency and the real convention is
/// unknown.
///
/// Guessing a rotation from that position would be a coin flip that also breaks the gates. This
/// reads the answer out of the running game instead: equip a vanilla sword, bow and crossbow, then
/// the Magenheim equivalents, and the log states each one's local position, rotation and scale
/// under the same attach transform. The difference between a vanilla entry and a Magenheim entry is
/// the correction the sources need.
///
/// Remove this once the convention is recorded in the gates.
/// </remarks>
internal sealed class HeldItemOrientationProbe : MonoBehaviour
{
    private const float PollSeconds = 0.5f;
    private static readonly HashSet<string> Reported = new(StringComparer.Ordinal);
    private static readonly System.Reflection.FieldInfo? RightInstance = AccessTools.Field(typeof(VisEquipment), "m_rightItemInstance");
    private static readonly System.Reflection.FieldInfo? LeftInstance = AccessTools.Field(typeof(VisEquipment), "m_leftItemInstance");
    private ManualLogSource? _log;
    private float _next;

    internal void Configure(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));

    private void Update()
    {
        if (_log is null || Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + PollSeconds;

        var player = Player.m_localPlayer;
        if (player is null) return;
        var visual = player.GetComponentInChildren<VisEquipment>();
        if (visual is null) return;

        // VisEquipment keeps the spawned held visuals private, so they are read through the
        // declared reflection boundary the rest of the runtime uses and the binding gate checks.
        Report("right", RightInstance?.GetValue(visual) as GameObject);
        Report("left", LeftInstance?.GetValue(visual) as GameObject);
    }

    private void Report(string hand, GameObject? instance)
    {
        if (_log is null || instance is null) return;
        var attach = instance.transform.parent;
        if (attach is null) return;

        // The instance name carries the prefab identity, which is what distinguishes a vanilla
        // weapon from the Magenheim clone standing in the same hand.
        var identity = instance.name.Replace("(Clone)", string.Empty).Trim();
        var key = hand + ":" + identity;
        if (!Reported.Add(key)) return;

        var local = instance.transform;
        var euler = local.localRotation.eulerAngles;
        var bounds = MeasureLocalBounds(instance);
        _log.LogWarning(
            $"[held probe] hand={hand} item='{identity}' attach='{attach.name}' " +
            $"localPos=({local.localPosition.x:0.###},{local.localPosition.y:0.###},{local.localPosition.z:0.###}) " +
            $"localEuler=({euler.x:0.#},{euler.y:0.#},{euler.z:0.#}) " +
            $"localScale=({local.localScale.x:0.###},{local.localScale.y:0.###},{local.localScale.z:0.###}) " +
            $"localBoundsCenter=({bounds.center.x:0.###},{bounds.center.y:0.###},{bounds.center.z:0.###}) " +
            $"localBoundsSize=({bounds.size.x:0.###},{bounds.size.y:0.###},{bounds.size.z:0.###})");
    }

    /// <summary>Renderer bounds expressed in the item's own local space, so the numbers are comparable to the source payload.</summary>
    private static Bounds MeasureLocalBounds(GameObject instance)
    {
        var root = instance.transform;
        var found = false;
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer || renderer is ParticleSystemRenderer) continue;
            var world = renderer.bounds;
            var center = root.InverseTransformPoint(world.center);
            var extents = root.InverseTransformVector(world.extents);
            var local = new Bounds(center, new Vector3(Mathf.Abs(extents.x) * 2f, Mathf.Abs(extents.y) * 2f, Mathf.Abs(extents.z) * 2f));
            if (!found) { bounds = local; found = true; }
            else bounds.Encapsulate(local);
        }
        return bounds;
    }
}
