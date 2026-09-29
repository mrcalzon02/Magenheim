using System;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Composes the unique Deepstone Conclave presentation at the native Underworld instance origin.
/// This is deliberately not a ZoneManager location: admission is owned by the world-session
/// lifecycle after the derived Underworld identity and native terrain authority are active.
/// </summary>
internal static class UnderworldWorldCenterRegistrar
{
    internal const string LocationName = "Magenheim_UnderworldWorldCenter";
    internal const string StandingStonesName = "Magenheim_UnderworldStandingStones";
    internal const string DescentMonolithName = "Magenheim_DescentMonolith";
    internal const string GeneratedObjectKind = "deepstone-conclave";
    internal static readonly Vector3 ReturnGateOffset = new(
        (float)UnderworldTerrainLifecycle.GateFoundationCenterXMeters,
        0f,
        (float)UnderworldTerrainLifecycle.GateFoundationCenterZMeters);
    internal static readonly Vector3 LogicalCenter = Vector3.zero;
    private static Transform? _returnGate;
    private static float _gateApproachDistance = 15f;

    private static readonly DeepstoneBinding[] Deepstones =
    {
        new("StoneOfBloom", "magenheim.underworld.deepstone.bloom", "underworld-deepstone-bloom", new Vector3(2.35f, 7.7f, 1.55f)),
        new("StoneOfDeepTide", "magenheim.underworld.deepstone.tide", "underworld-deepstone-tide", new Vector3(2.45f, 7.9f, 1.65f)),
        new("StoneOfCinder", "magenheim.underworld.deepstone.cinder", "underworld-deepstone-cinder", new Vector3(2.55f, 8.1f, 1.65f)),
        new("StoneOfRime", "magenheim.underworld.deepstone.rime", "underworld-deepstone-rime", new Vector3(2.45f, 8.0f, 1.70f)),
        new("StoneOfFracture", "magenheim.underworld.deepstone.fracture", "underworld-deepstone-fracture", new Vector3(2.70f, 8.3f, 1.85f)),
        new("StoneOfDecay", "magenheim.underworld.deepstone.decay", "underworld-deepstone-decay", new Vector3(2.65f, 8.2f, 1.80f)),
    };

    internal static GameObject Create(UnderworldWorldIdentity identity, ManualLogSource log)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (log is null) throw new ArgumentNullException(nameof(log));

        var logical = LogicalCenter;
        var root = new GameObject(LocationName);
        var ownership = root.AddComponent<UnderworldGeneratedObjectIdentity>();
        ownership.Bind(identity, GeneratedObjectKind);
        root.transform.position = ResolveGroundedCenterPosition(logical);
        root.transform.rotation = Quaternion.identity;
        BuildStandingStones(root.transform);

        var gatePrefab = Jotunn.Managers.PrefabManager.Instance.GetPrefab(UnderworldDeepGateRegistrar.PrefabName)
            ?? throw new InvalidOperationException("Deep Gate must be registered before the Underworld world center is composed.");
        var gate = UnityEngine.Object.Instantiate(gatePrefab, root.transform, false);
        gate.name = UnderworldDeepGateRegistrar.PrefabName;
        gate.transform.position = ResolveTerrainPosition(ReturnGateOffset, -0.08f);
        gate.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        _returnGate = gate.transform;
        _gateApproachDistance = 3f;
        foreach (var renderer in gate.GetComponentsInChildren<MeshRenderer>(true))
        {
            var b = renderer.bounds;
            for (var x = -1; x <= 1; x += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var corner = b.center + new Vector3(x*b.extents.x, 0f, z*b.extents.z);
                _gateApproachDistance = Mathf.Max(_gateApproachDistance,
                    gate.transform.InverseTransformPoint(corner).z + 3f);
            }
        }
        var endpoint = gate.GetComponent<UnderworldGateEndpoint>() ?? gate.AddComponent<UnderworldGateEndpoint>();
        endpoint.Role = UnderworldGateRole.ReturnToSurface;
        if (gate.GetComponent<UnderworldDeepGateProgressionRuntime>() == null) gate.AddComponent<UnderworldDeepGateProgressionRuntime>();

        log.LogInfo($"Admitted Underworld center '{LocationName}' as native object '{ownership.StableObjectId}' at instance ({root.transform.position.x:0.##}, {root.transform.position.y:0.##}, {root.transform.position.z:0.##}).");
        return root;
    }

    // Resolve against the actual admitted gate footprint, beyond its frontmost geometry.
    // The console and real entry gate share this destination; never use the central monolith.
    internal static Vector3 ResolveEngineCenterPosition()
    {
        if (!_returnGate) throw new InvalidOperationException("Underworld return gate is not ready.");
        var point = _returnGate.TransformPoint(new Vector3(0f, 0f, _gateApproachDistance));
        var logical = point;
        logical.y = 0f;
        return ResolveTerrainPosition(logical, 0.08f);
    }

    // Entry resolves on the front side of the gate, so face the player back toward the arch rather
    // than pointing them farther into the approach corridor.
    internal static Quaternion ArrivalRotation => _returnGate
        ? Quaternion.LookRotation(-_returnGate.forward, Vector3.up)
        : Quaternion.identity;

    private static Vector3 ResolveGroundedCenterPosition(Vector3 logical) =>
        ResolveTerrainPosition(logical, 1.25f);

    private static Vector3 ResolveTerrainPosition(Vector3 logical, float verticalOffset)
    {
        var terrain = UnderworldTerrainRuntime.SampleInstanceTerrain(logical.x, logical.y, logical.z);
        if (!terrain.Admitted)
            throw new InvalidOperationException("Native Underworld terrain authority did not admit the requested world-center coordinate.");
        return new Vector3((float)logical.x, (float)terrain.Height + verticalOffset, (float)logical.z);
    }

    private static void BuildStandingStones(Transform parent)
    {
        var root = new GameObject(StandingStonesName);
        root.transform.SetParent(parent, false);
        var monolith = ModelAssets.LoadSingleMesh("underworld-descent-monolith");
        var dais = ModelAssets.LoadSingleMesh("underworld-dais");
        var monolithMaterial = ModelAssets.LoadSingleMaterial("underworld-descent-monolith");
        var daisMaterial = ModelAssets.LoadSingleMaterial("underworld-dais");
        const float radius = 12f;

        for (var i = 0; i < Deepstones.Length; i++)
        {
            var binding = Deepstones[i];
            var angle = i * Mathf.PI * 2f / Deepstones.Length;
            var stone = AddAuthoredDeepstone(
                root.transform,
                binding,
                new Vector3(Mathf.Cos(angle) * radius, binding.WorldScale.y * .46f, Mathf.Sin(angle) * radius),
                Quaternion.Euler(i % 2 == 0 ? -3f : 4f, -angle * Mathf.Rad2Deg + 90f, i % 3 - 1),
                parent.gameObject.layer);
            var progression = stone.AddComponent<UnderworldDeepstoneRuntime>();
            progression.Bind(binding.DeepstoneId);
            stone.AddComponent<UnderworldDeepstonePresentationRuntime>().Bind(progression);
        }

        AddMesh(root.transform, DescentMonolithName, monolith, new Vector3(0f, 3.9f, 0f), Quaternion.identity, new Vector3(2.8f, 8.2f, 2.8f), monolithMaterial, parent.gameObject.layer);
        AddMesh(root.transform, "CenterDais", dais, new Vector3(0f, 0.45f, 0f), Quaternion.identity, new Vector3(17f, 0.9f, 17f), daisMaterial, parent.gameObject.layer);
    }

    private static GameObject AddAuthoredDeepstone(
        Transform parent,
        DeepstoneBinding binding,
        Vector3 position,
        Quaternion rotation,
        int layer)
    {
        var node = new GameObject(binding.ObjectName) { layer = layer };
        node.transform.SetParent(parent, false);
        node.transform.localPosition = position;
        node.transform.localRotation = rotation;
        node.transform.localScale = binding.WorldScale;

        // Deepstones are no longer six instances of one slab. Each canonical stone owns a
        // multi-part UV-authored model with its own material families, emission and local light.
        // The model's primary stone part carries the MeshCollider; decorative roots/crystals/bone
        // remain visual so added silhouette detail does not create snaggy navigation collision.
        ModelAssets.Load(
            node,
            binding.ModelId,
            hideOriginal: false,
            parent: node.transform,
            materialSource: new Material(ModelAssets.ResolveSurfaceShader()));
        return node;
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

    private sealed record DeepstoneBinding(string ObjectName, string DeepstoneId, string ModelId, Vector3 WorldScale);
}
