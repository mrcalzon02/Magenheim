using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class UnderworldBiomeStructureResidencyRuntime
{
    private readonly UnderworldRuntimeServices _services;
    private readonly List<IUnderworldBiomeStructureFamily> _families = new();
    private readonly Dictionary<string, GameObject> _resident = new(StringComparer.Ordinal);

    internal UnderworldBiomeStructureResidencyRuntime(UnderworldRuntimeServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        Register(new FungalSporeCairnFamily());
        Register(new FungalRootMassFamily());
        Register(new SulfurVentCairnFamily());
        Register(new FrozenShardFanFamily());
        Register(new DecayRootCorridorFamily());
    }

    internal int ResidentCount => _resident.Count;

    internal void Register(IUnderworldBiomeStructureFamily family)
    {
        if (family is null) throw new ArgumentNullException(nameof(family));
        foreach (var existing in _families)
        {
            if (string.Equals(existing.Kind, family.Kind, StringComparison.Ordinal))
                throw new InvalidOperationException($"Underworld structure family '{family.Kind}' is already registered.");
            if (existing.Biome == family.Biome && existing.PlacementSlot == family.PlacementSlot)
                throw new InvalidOperationException($"Underworld biome '{family.Biome}' already owns placement slot {family.PlacementSlot} through '{existing.Kind}'.");
        }
        _families.Add(family);
    }

    internal void Reconcile()
    {
        var identity = _services.InstanceLifecycle.Identity;
        if (identity is null || _services.InstanceLifecycle.Phase != UnderworldInstancePhase.Active) { Clear(); return; }
        var wanted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in _services.ChunkStreaming.LoadedChunks)
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
                var root = _services.StructureAdmission.Admit(identity, family.Kind, pair.Key, family.PlacementSlot, () => family.Compose(position, identity, pair.Key));
                _resident.Add(residencyKey, root);
            }
        }
        var stale = new List<string>();
        foreach (var pair in _resident) if (!wanted.Contains(pair.Key)) stale.Add(pair.Key);
        foreach (var key in stale) Release(key);
    }

    internal void Clear() { foreach (var root in _resident.Values) if (root) UnityEngine.Object.Destroy(root); _resident.Clear(); }
    private void Release(string key) { if (!_resident.TryGetValue(key, out var root)) return; if (root) UnityEngine.Object.Destroy(root); _resident.Remove(key); }
    private static string ResidencyKey(string kind, UnderworldInstanceChunkKey key, int slot) => kind + "\n" + key.X + "," + key.Z + "\n" + slot;
}

internal interface IUnderworldBiomeStructureFamily
{
    string Kind { get; }
    UnderworldTerrainBiome Biome { get; }
    int PlacementSlot { get; }
    bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key);
    GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key);
}

internal sealed class FungalSporeCairnFamily : IUnderworldBiomeStructureFamily
{
    public string Kind => "fungal-spore-cairn";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FungalForest;
    public int PlacementSlot => 0;
    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked { var hash = identity.DerivedSeed32; hash = (hash * 397) ^ key.X; hash = (hash * 397) ^ key.Z; hash ^= hash >> 16; return (hash & 3) == 0; }
    }
    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FungalSporeCairn_{key.X}_{key.Z}"); root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663; var random = new System.Random(seed);
        try
        {
            for (var i = 0; i < 4; i++)
            {
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ i * 486187739, $"SporeDonor_{i}"); donor.transform.SetParent(root.transform, false);
                if (i == 0) continue;
                var angle = (float)(random.NextDouble() * Math.PI * 2d); var radius = 1.1f + (float)random.NextDouble() * 2.2f;
                donor.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                donor.transform.localRotation *= Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
                donor.transform.localScale *= 0.72f + (float)random.NextDouble() * 0.48f;
            }
            return root;
        }
        catch { UnityEngine.Object.Destroy(root); throw; }
    }
}

internal sealed class FungalRootMassFamily : IUnderworldBiomeStructureFamily
{
    public string Kind => "fungal-root-mass";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FungalForest;
    public int PlacementSlot => 2;
    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked { var hash = identity.DerivedSeed32 ^ 0x2F6E2B1D; hash = (hash * 397) ^ key.X; hash = (hash * 397) ^ key.Z; hash ^= hash >> 15; return (hash & 7) <= 2; }
    }
    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FungalRootMass_{key.X}_{key.Z}"); root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 92837111 ^ key.Z * 689287499 ^ 0x2F6E2B1D; var random = new System.Random(seed);
        try
        {
            var heart = UnderworldDonorVisualFactory.Create(Biome, seed ^ 0x13A5C7D, "RootHeartDonor");
            heart.transform.SetParent(root.transform, false);
            heart.transform.localPosition += new Vector3(0f, 0.35f, 0f);
            heart.transform.localScale *= 1.45f;
            for (var i = 0; i < 6; i++)
            {
                var angle = (float)(random.NextDouble() * Math.PI * 2d);
                var radius = 1.8f + (float)random.NextDouble() * 3.4f;
                var branch = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 92821), $"RootDonor_{i}");
                branch.transform.SetParent(root.transform, false);
                branch.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius);
                branch.transform.localRotation *= Quaternion.Euler(68f + (float)random.NextDouble() * 28f, angle * Mathf.Rad2Deg, 0f);
                branch.transform.localScale *= 0.62f + (float)random.NextDouble() * 0.58f;
            }
            return root;
        }
        catch { UnityEngine.Object.Destroy(root); throw; }
    }
}

internal sealed class SulfurVentCairnFamily : IUnderworldBiomeStructureFamily
{
    public string Kind => "sulfur-vent-cairn";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.SulfurousWastes;
    public int PlacementSlot => 1;
    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked { var hash = identity.DerivedSeed32 ^ 0x51F15EED; hash = (hash * 397) ^ key.X; hash = (hash * 397) ^ key.Z; hash ^= hash >> 16; return (hash & 3) == 1; }
    }
    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_SulfurVentCairn_{key.X}_{key.Z}"); root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 83492791 ^ key.Z * 297121507 ^ 0x51F15EED; var random = new System.Random(seed);
        try
        {
            var basin = UnderworldDonorVisualFactory.Create(Biome, seed ^ 0x61B45A3, "VentBasinDonor");
            basin.transform.SetParent(root.transform, false);
            basin.transform.localPosition += new Vector3(0f, 0.15f, 0f);
            basin.transform.localScale *= 1.35f;
            for (var i = 0; i < 4; i++)
            {
                var angle = (float)(random.NextDouble() * Math.PI * 2d);
                var radius = 0.9f + (float)random.NextDouble() * 2.4f;
                var vent = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 193939), $"SulfurVentDonor_{i}");
                vent.transform.SetParent(root.transform, false);
                vent.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, 0.1f, Mathf.Sin(angle) * radius);
                vent.transform.localRotation *= Quaternion.Euler(-8f + (float)random.NextDouble() * 16f, angle * Mathf.Rad2Deg, 6f - (float)random.NextDouble() * 12f);
                vent.transform.localScale *= 0.68f + (float)random.NextDouble() * 0.72f;
            }
            return root;
        }
        catch { UnityEngine.Object.Destroy(root); throw; }
    }
}

internal sealed class FrozenShardFanFamily : IUnderworldBiomeStructureFamily
{
    public string Kind => "frozen-shard-fan";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FrozenCaverns;
    public int PlacementSlot => 0;
    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked { var hash = identity.DerivedSeed32 ^ 0x17C3A5D; hash = (hash * 397) ^ key.X; hash = (hash * 397) ^ key.Z; hash ^= hash >> 16; return (hash & 7) <= 2; }
    }
    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FrozenShardFan_{key.X}_{key.Z}"); root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 1640531513 ^ key.Z * 805459861 ^ 0x17C3A5D; var random = new System.Random(seed);
        try
        {
            var anchor = UnderworldDonorVisualFactory.Create(Biome, seed ^ 0x5A17E, "FrozenFanAnchor");
            anchor.transform.SetParent(root.transform, false); anchor.transform.localPosition += new Vector3(0f, -0.12f, 0f); anchor.transform.localScale *= 1.2f;
            var heading = (float)(random.NextDouble() * 360d);
            for (var i = 0; i < 5; i++)
            {
                var spread = -48f + i * 24f + ((float)random.NextDouble() - 0.5f) * 12f; var angle = (heading + spread) * Mathf.Deg2Rad;
                var radius = 1.15f + i * 0.52f + (float)random.NextDouble() * 0.45f;
                var shard = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 32452843), $"FrozenShardDonor_{i}");
                shard.transform.SetParent(root.transform, false);
                shard.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, 0.02f + i * 0.06f, Mathf.Sin(angle) * radius);
                shard.transform.localRotation *= Quaternion.Euler(58f + (float)random.NextDouble() * 24f, heading + spread, -10f + (float)random.NextDouble() * 20f);
                shard.transform.localScale *= 0.7f + i * 0.09f + (float)random.NextDouble() * 0.35f;
            }
            return root;
        }
        catch { UnityEngine.Object.Destroy(root); throw; }
    }
}

internal sealed class DecayRootCorridorFamily : IUnderworldBiomeStructureFamily
{
    public string Kind => "decay-root-corridor";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.GreatDecay;
    public int PlacementSlot => 0;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        unchecked
        {
            var hash = identity.DerivedSeed32 ^ 0x36D3CA7;
            hash = (hash * 397) ^ key.X;
            hash = (hash * 397) ^ key.Z;
            hash ^= hash >> 16;
            return (hash & 7) <= 2;
        }
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_DecayRootCorridor_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 83492791 ^ 0x36D3CA7;
        var random = new System.Random(seed);
        try
        {
            var heading = (float)(random.NextDouble() * Mathf.PI * 2f);
            var forward = new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading));
            var side = new Vector3(-forward.z, 0f, forward.x);

            for (var i = 0; i < 6; i++)
            {
                var pair = i / 2;
                var flank = (i & 1) == 0 ? 1f : -1f;
                var along = -2.8f + pair * 2.8f + ((float)random.NextDouble() - 0.5f) * 0.7f;
                var lateral = flank * (2.25f + (float)random.NextDouble() * 0.85f);
                var donor = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 49979687), $"DecayRootDonor_{i}");
                donor.transform.SetParent(root.transform, false);
                donor.transform.localPosition += forward * along + side * lateral + new Vector3(0f, -0.08f + (float)random.NextDouble() * 0.22f, 0f);
                donor.transform.localRotation *= Quaternion.Euler(62f + (float)random.NextDouble() * 24f, heading * Mathf.Rad2Deg + (flank > 0f ? 90f : -90f), -14f + (float)random.NextDouble() * 28f);
                donor.transform.localScale *= 0.82f + (float)random.NextDouble() * 0.62f;
            }

            return root;
        }
        catch { UnityEngine.Object.Destroy(root); throw; }
    }
}
