using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Composes the unique Deepstone Conclave presentation for the logical Underworld.
/// This is deliberately not a ZoneManager location: registering it as ordinary parent-world
/// worldgen would violate the reserved spatial-domain authority. Admission is owned by the
/// world-session lifecycle after the derived Underworld identity can be resolved.
/// </summary>
internal static class UnderworldWorldCenterRegistrar
{
    internal const string LocationName = "Magenheim_UnderworldWorldCenter";
    internal const string StandingStonesName = "Magenheim_UnderworldStandingStones";
    internal const string DescentMonolithName = "Magenheim_DescentMonolith";
    internal static readonly Vector3 ReturnGateOffset = new(18f, 0f, 0f);
    internal static readonly UnderworldAnchor LogicalCenter = new("magenheim.underworld.pending", 0d, 0d, 0d, 0f);

    private static readonly DeepstoneBinding[] Deepstones =
    {
        new("StoneOfBloom", "magenheim.underworld.deepstone.bloom"),
        new("StoneOfDeepTide", "magenheim.underworld.deepstone.tide"),
        new("StoneOfCinder", "magenheim.underworld.deepstone.cinder"),
        new("StoneOfRime", "magenheim.underworld.deepstone.rime"),
        new("StoneOfFracture", "magenheim.underworld.deepstone.fracture"),
        new("StoneOfDecay", "magenheim.underworld.deepstone.decay"),
    };

    internal static GameObject Create(UnderworldWorldIdentity identity, UnderworldSpatialDomainDefinition spatialDomain, ManualLogSource log)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (spatialDomain is null) throw new ArgumentNullException(nameof(spatialDomain));
        if (log is null) throw new ArgumentNullException(nameof(log));

        var logical = LogicalCenter with { WorldId = identity.DerivedWorldId };
        var host = UnderworldSpatialDomain.ToHostAnchor(spatialDomain, identity, UnderworldLayer.Underworld, logical);
        var root = new GameObject(LocationName);
        root.transform.position = new Vector3((float)host.X, (float)host.Y, (float)host.Z);
        root.transform.rotation = Quaternion.Euler(0f, host.HeadingDegrees, 0f);
        BuildStandingStones(root.transform);

        var gatePrefab = Jotunn.Managers.PrefabManager.Instance.GetPrefab(UnderworldDeepGateRegistrar.PrefabName)
            ?? throw new InvalidOperationException("Deep Gate must be registered before the Underworld world center is composed.");
        var gate = UnityEngine.Object.Instantiate(gatePrefab, root.transform, false);
        gate.name = UnderworldDeepGateRegistrar.PrefabName;
        gate.transform.localPosition = ReturnGateOffset;
        gate.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        var endpoint = gate.GetComponent<UnderworldGateEndpoint>() ?? gate.AddComponent<UnderworldGateEndpoint>();
        endpoint.Role = UnderworldGateRole.ReturnToSurface;
        if (gate.GetComponent<UnderworldDeepGateProgressionRuntime>() == null) gate.AddComponent<UnderworldDeepGateProgressionRuntime>();

        log.LogInfo($"Admitted Underworld center '{LocationName}' at reserved host ({host.X:0.##}, {host.Y:0.##}, {host.Z:0.##}).");
        return root;
    }

    private static void BuildStandingStones(Transform parent)
    {
        var root = new GameObject(StandingStonesName);
        root.transform.SetParent(parent, false);
        var material = CreateStoneMaterial();
        var box = RuntimeMeshPrimitives.Box("magenheim.underworld.standing-stone");
        var dais = RuntimeMeshPrimitives.Cylinder(32, "magenheim.underworld.center-dais");
        const float radius = 12f;

        for (var i = 0; i < Deepstones.Length; i++)
        {
            var binding = Deepstones[i];
            var angle = i * Mathf.PI * 2f / Deepstones.Length;
            var stone = AddMesh(
                root.transform,
                binding.ObjectName,
                box,
                new Vector3(Mathf.Cos(angle) * radius, 2.8f, Mathf.Sin(angle) * radius),
                Quaternion.Euler(i % 2 == 0 ? -3f : 4f, -angle * Mathf.Rad2Deg + 90f, i % 3 - 1),
                new Vector3(2.1f, 7.4f + (i % 3) * 0.7f, 1.25f),
                material,
                parent.gameObject.layer);
            stone.AddComponent<UnderworldDeepstoneRuntime>().Bind(binding.DeepstoneId);
        }

        AddMesh(root.transform, DescentMonolithName, box, new Vector3(0f, 3.9f, 0f), Quaternion.identity, new Vector3(2.8f, 8.2f, 2.8f), material, parent.gameObject.layer);
        AddMesh(root.transform, "CenterDais", dais, new Vector3(0f, 0.45f, 0f), Quaternion.identity, new Vector3(17f, 0.9f, 17f), material, parent.gameObject.layer);
    }

    private static Material CreateStoneMaterial()
    {
        var shader = Shader.Find("Standard") ?? throw new InvalidOperationException("Unity Standard shader unavailable for Underworld standing stones.");
        var material = new Material(shader) { name = "Magenheim_UnderworldStandingStone_Material", color = new Color(0.075f, 0.082f, 0.095f, 1f) };
        material.SetFloat("_Metallic", 0.12f);
        material.SetFloat("_Glossiness", 0.24f);
        return material;
    }

    private static GameObject AddMesh(Transform parent, string name, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material, int layer)
    {
        var node = new GameObject(name) { layer = layer };
        node.transform.SetParent(parent, false);
        node.transform.localPosition = position;
        node.transform.localRotation = rotation;
        node.transform.localScale = scale;
        node.AddComponent<MeshFilter>().sharedMesh = mesh;
        node.AddComponent<MeshRenderer>().sharedMaterial = material;
        node.AddComponent<MeshCollider>().sharedMesh = mesh;
        return node;
    }

    private sealed record DeepstoneBinding(string ObjectName, string DeepstoneId);
}
