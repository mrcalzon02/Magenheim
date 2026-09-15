using System;
using System.Collections.Generic;
using Magenheim.Core.DeepFractures;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Consumes the pure-core collision-safe passage assembly plan and realizes it with the
/// registered Magenheim passage Room prefab. Runtime owns placement only; topology and routing
/// remain pure-core authority.
/// </summary>
internal static class DeepFracturePassageAssembler
{
    internal static IReadOnlyList<Room> Assemble(
        Transform interiorRoot,
        DeepFractureInteriorBlueprint blueprint,
        DeepFractureInteriorProjectionPolicy? policyOverride = null)
    {
        if (interiorRoot is null)
            throw new ArgumentNullException(nameof(interiorRoot));
        if (blueprint is null)
            throw new ArgumentNullException(nameof(blueprint));

        var roomData = DeepFractureRoomRegistrar.ResolvePassage();
        var source = roomData.m_loadedRoom
            ?? throw new InvalidOperationException($"Registered Deep Fracture passage '{DeepFractureRoomVisuals.PassagePrefabName}' has no loaded Room prefab.");
        var plan = DeepFracturePassageAssemblyCompiler.Build(blueprint, policyOverride);
        var placed = new List<Room>(plan.Segments.Count);

        foreach (var segment in plan.Segments)
        {
            var instance = UnityEngine.Object.Instantiate(source.gameObject, interiorRoot, false);
            instance.name = $"{DeepFractureRoomVisuals.PassagePrefabName}_{segment.ConnectionId}_{segment.SegmentIndex:000}";
            instance.transform.localPosition = new Vector3((float)segment.Center.X, (float)segment.Center.Y, (float)segment.Center.Z);
            instance.transform.localRotation = Quaternion.Euler(0f, (float)segment.YawDegrees, 0f);

            // The registered template is 16m long. Scale only its longitudinal axis for the
            // final fractional segment; width/height remain authoritative room geometry.
            var longitudinalScale = (float)(segment.Length / DeepFracturePassageAssemblyCompiler.RuntimePassageLength);
            instance.transform.localScale = new Vector3(1f, 1f, longitudinalScale);
            instance.SetActive(true);

            var room = instance.GetComponent<Room>()
                ?? throw new InvalidOperationException($"Instantiated Deep Fracture passage segment '{instance.name}' has no Room component.");
            placed.Add(room);
        }

        return placed.AsReadOnly();
    }
}