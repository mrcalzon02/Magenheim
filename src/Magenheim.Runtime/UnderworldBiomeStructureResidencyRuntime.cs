using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Shared residency authority for repeatable structures inside the derived Underworld instance.
/// Families describe deterministic eligibility and composition; this runtime alone owns admission,
/// native-chunk residency, and presentation cleanup. Surface ZoneSystem placement is not involved.
/// </summary>
internal sealed class UnderworldBiomeStructureResidencyRuntime
{
    private readonly UnderworldRuntimeServices _services;
    private readonly List<IUnderworldBiomeStructureFamily> _families = new();
    private readonly Dictionary<string, GameObject> _resident = new(StringComparer.Ordinal);

    internal UnderworldBiomeStructureResidencyRuntime(UnderworldRuntimeServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        Register(new FungalSporeCairnFamily());
        Register(new SulfurVentCairnFamily());
    }

    internal int ResidentCount => _resident.Count;

    internal void Register(IUnderworldBiomeStructureFamily family)
    {
        if (family is null) throw new ArgumentNullException(nameof(family));
        foreach (var existing in _families)
            if (string.Equals(existing.Kind, family.Kind, StringComparison.Ordinal))
                throw new InvalidOperationException($"Underworld structure family '{family.Kind}' is already registered.");
        _families.Add(family);
    }

    internal void Reconcile()
    {
        var identity = _services.InstanceLifecycle.Identity;
        if (identity is null || _services.InstanceLifecycle.Phase != UnderworldInstancePhase.Active)
        {
            Clear();
            return;
        }

        var chunks = _services.ChunkStreaming.LoadedChunks;
        var wanted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in chunks)
        {
            var bounds = _services.ChunkStreaming.Grid.Bounds(pair.Key);
            var x = (bounds.MinimumX + bounds.MaximumX) * 0.5d;
            var z = (bounds.MinimumZ + bounds.MaximumZ) * 0.5d;
            var terrain = UnderworldTerrainRuntime.SampleInstanceTerrain(x, 0d, z);
            if (!terrain.Admitted) continue;

            foreach (var family in _families)
            {
                if (terrain.Biome != family.Biome || !family.Eligible(identity, pair.Key)) continue;
                var residencyKey = ResidencyKey(family.Kind, pair.Key, family.PlacementSlot);
                wanted.Add(residencyKey);
                if (_resident.ContainsKey(residencyKey)) continue;

                var position = new Vector3((float)x, (float)terrain.Height, (float)z);
                var root = _services.StructureAdmission.Admit(
                    identity, family.Kind, pair.Key, family.PlacementSlot,
                    () => family.Compose(position, identity, pair.Key));
                _resident.Add(residencyKey, root);
            }
        }

        var stale = new List<string>();
        foreach (var pair in _resident)
            if (!wanted.Contains(pair.Key)) stale.Add(pair.Key);
        foreach (var key in stale) Release(key);
    }

    internal void Clear()
    {
        foreach (var root in _resident.Values) if (root) UnityEngine.Object.Destroy(root);
        _resident.Clear();
    }

    private void Release(string key)
    {
        if (!_resident.TryGetValue(key, out var root)) return;
        if (root) UnityEngine.Object.Destroy(root);
        _resident.Remove(key);
    }

    private static string ResidencyKey(string kind, UnderworldInstanceChunkKey key, int slot) =>
        kind + "\n" + key.X + "," + key.Z + "\n" + slot;
}

internal interface IUnderworldBiomeStructureFamily
{
    string Kind { get; }
    UnderworldTerrainBiome Biome { get; }
    int PlacementSlot { get; }
    bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key);
    GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key);
}

/// <summary>First registered family; its stable kind/slot preserve all records made by the earlier dedicated runtime.</summary>
internal sealed class FungalSporeCairnFamily : IUnderworldBiomeStructureFamily
{
    public string Kind => "fungal-spore-cairn";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FungalForest;
    public int PlacementSlot => 0;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked
        {
            var hash = identity.DerivedSeed32;
            hash = (hash * 397) ^ key.X;
            hash = (hash * 397) ^ key.Z;
            hash ^= hash >> 16;
            return (hash & 3) == 0;
        }
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FungalSporeCairn_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663;
        var random = new System.Random(seed);
        try
        {
            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(root.transform, false);
            pedestal.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            pedestal.transform.localScale = new Vector3(2.8f, 0.7f, 2.8f);

            for (var i = 0; i < 3; i++)
            {
                var angle = (float)(random.NextDouble() * Math.PI * 2d);
                var radius = 1.1f + (float)random.NextDouble() * 1.4f;
                var height = 2.8f + (float)random.NextDouble() * 2.6f;
                var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = $"Stem_{i}";
                stem.transform.SetParent(root.transform, false);
                stem.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, height * 0.5f + 1.1f, Mathf.Sin(angle) * radius);
                stem.transform.localScale = new Vector3(0.45f + i * 0.08f, height * 0.5f, 0.45f + i * 0.08f);

                var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cap.name = $"Cap_{i}";
                cap.transform.SetParent(root.transform, false);
                cap.transform.localPosition = stem.transform.localPosition + Vector3.up * (height * 0.55f);
                var capScale = 1.8f + (float)random.NextDouble() * 1.3f;
                cap.transform.localScale = new Vector3(capScale, capScale * 0.38f, capScale);
            }
            return root;
        }
        catch
        {
            UnityEngine.Object.Destroy(root);
            throw;
        }
    }
}

/// <summary>
/// Second repeatable family proving that the shared registry can host an independent biome,
/// deterministic slot and durable generated-object identity without another residency runtime.
/// </summary>
internal sealed class SulfurVentCairnFamily : IUnderworldBiomeStructureFamily
{
    public string Kind => "sulfur-vent-cairn";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.SulfurousWastes;
    public int PlacementSlot => 1;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked
        {
            var hash = identity.DerivedSeed32 ^ 0x51F15EED;
            hash = (hash * 397) ^ key.X;
            hash = (hash * 397) ^ key.Z;
            hash ^= hash >> 16;
            return (hash & 3) == 1;
        }
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_SulfurVentCairn_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 83492791 ^ key.Z * 297121507 ^ 0x51F15EED;
        var random = new System.Random(seed);
        try
        {
            var basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basin.name = "VentBasin";
            basin.transform.SetParent(root.transform, false);
            basin.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            basin.transform.localScale = new Vector3(3.4f, 0.35f, 3.4f);

            for (var i = 0; i < 4; i++)
            {
                var angle = (float)(random.NextDouble() * Math.PI * 2d);
                var radius = 0.7f + (float)random.NextDouble() * 1.7f;
                var height = 1.8f + (float)random.NextDouble() * 3.4f;
                var vent = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                vent.name = $"SulfurVent_{i}";
                vent.transform.SetParent(root.transform, false);
                vent.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, height * 0.5f + 0.5f, Mathf.Sin(angle) * radius);
                var width = 0.35f + (float)random.NextDouble() * 0.35f;
                vent.transform.localScale = new Vector3(width, height * 0.5f, width);

                var throat = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                throat.name = $"VentThroat_{i}";
                throat.transform.SetParent(root.transform, false);
                throat.transform.localPosition = vent.transform.localPosition + Vector3.up * (height * 0.52f);
                throat.transform.localScale = new Vector3(width * 1.5f, 0.22f, width * 1.5f);
            }
            return root;
        }
        catch
        {
            UnityEngine.Object.Destroy(root);
            throw;
        }
    }
}
