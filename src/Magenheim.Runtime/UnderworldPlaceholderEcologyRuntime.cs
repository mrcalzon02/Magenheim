using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Disposable local Underworld ecology preview assembled from Valheim-owned donor prefabs.
/// Donor visuals are created exclusively through <see cref="UnderworldDonorVisualFactory"/>, so
/// ecology and persistent native structures share one non-destructive donor-copy authority.
/// Ecology consumes native Underworld instance coordinates only; Surface WorldGenerator state
/// and hidden host-band coordinates are not ecology authority.
/// </summary>
internal sealed class UnderworldPlaceholderEcologyRuntime : MonoBehaviour
{
    private UnderworldRuntimeServices? _services;
    private ManualLogSource? _log;
    private List<GameObject> _spawned = new();
    private readonly Dictionary<UnderworldInstanceChunkKey, List<GameObject>> _cells = new();
    private string _admitted = string.Empty;
    private float _nextAt;
    private const float CellSize = UnderworldEcologyCells.SizeMeters;
    private const int LoadRadius = UnderworldEcologyCells.LoadRadius;
    private const int EcologyClusterCount = 2;
    private const int PlacementsPerCluster = 7;

    internal void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextAt) return;
        _nextAt = Time.unscaledTime + .25f;
        if (_services is null || ZNet.instance is null || ZNet.World is null)
        {
            ClearMarkers(); _admitted = string.Empty; return;
        }
        var lifecycle = _services.InstanceLifecycle;
        if (lifecycle.Phase != UnderworldInstancePhase.Active || lifecycle.Identity is null)
        {
            ClearMarkers();
            _admitted = string.Empty;
            return;
        }
        if (!_services.TryResolveLocalSession(out var identity, out var layer, out _, out _) ||
            identity is null || layer != UnderworldLayer.Underworld ||
            !string.Equals(identity.DerivedWorldId, lifecycle.Identity.DerivedWorldId, StringComparison.Ordinal) ||
            !string.Equals(identity.ParentWorldId, lifecycle.Identity.ParentWorldId, StringComparison.Ordinal) ||
            !string.Equals(identity.DerivedSeedFingerprint, lifecycle.Identity.DerivedSeedFingerprint, StringComparison.Ordinal))
        {
            ClearMarkers();
            _admitted = string.Empty;
            return;
        }
        var player = Player.m_localPlayer;
        if (player is null) return;
        var instancePosition = UnderworldInstanceLayer.ToLogical(player.transform.position);
        if (!string.Equals(_admitted, identity.DerivedWorldId, StringComparison.Ordinal)) ClearMarkers();
        _admitted = identity.DerivedWorldId;
        var focus = UnderworldEcologyCells.KeyAt(instancePosition.x, instancePosition.z);
        var cx = focus.X; var cz = focus.Z;
        // Retain complete cells (including collidable rocks) behind the loading frontier.
        // No surviving object's transform depends on the camera/player position.
        var stale = new List<UnderworldInstanceChunkKey>();
        foreach (var pair in _cells)
            if (!UnderworldEcologyCells.Retain(pair.Key, focus))
                stale.Add(pair.Key);
        foreach (var key in stale)
        {
            foreach (var item in _cells[key]) if (item) Destroy(item);
            _cells.Remove(key);
        }
        var budget = 3;
        for (var ring = 0; ring <= LoadRadius; ring++)
        for (var z = cz - ring; z <= cz + ring; z++)
        for (var x = cx - ring; x <= cx + ring; x++)
        {
            if (Math.Max(Math.Abs(x-cx), Math.Abs(z-cz)) != ring || _cells.ContainsKey(new(x,z))) continue;
            BuildCell(x, z, identity);
            if (--budget == 0) return;
        }
    }

    private void BuildCell(int cellX, int cellZ, UnderworldWorldIdentity identity)
    {
        _spawned = new List<GameObject>();
        _cells.Add(new(cellX, cellZ), _spawned);
        var seed = UnderworldEcologyCells.Seed(identity.DerivedSeed32, new(cellX, cellZ));
        var random = new System.Random(seed);
        var center = new Vector3((cellX + .5f) * CellSize, 0f, (cellZ + .5f) * CellSize);

        for (var cluster = 0; cluster < EcologyClusterCount; cluster++)
        {
            var anchorX = cellX * CellSize + 20f + (float)random.NextDouble() * 24f;
            var anchorZ = cellZ * CellSize + 20f + (float)random.NextDouble() * 24f;
            var anchor = UnderworldTerrainRuntime.SampleInstanceTerrain(anchorX, center.y, anchorZ);
            if (!anchor.Admitted) continue;

            var radius = Math.Min(18f, ClusterRadius(anchor.Biome));
            var pocketAngle = (float)(random.NextDouble() * Math.PI * 2d);
            for (var member = 0; member < PlacementsPerCluster; member++)
            {
                var placement = UnderworldBiomeEcologyComposition.Compose(anchor.Biome, random, radius, member, pocketAngle);
                var x = anchorX + placement.x;
                var z = anchorZ + placement.y;
                if (x*x + z*z < 36f*36f) continue; // Keep the dais and gate approach clear.
                var sample = UnderworldTerrainRuntime.SampleInstanceTerrain(x, center.y, z);
                if (!sample.Admitted || sample.Biome != anchor.Biome) continue;
                var variant = seed + cluster * 101 + member * 17;
                SpawnDonor(
                    UnderworldInstanceLayer.ToEngine(new Vector3(x, (float)sample.Height, z)),
                    sample.Biome,
                    ComposeVariant(sample.Biome, variant, member));
                // Fill stays near admitted supports; sparse/hazard biomes keep open sightlines.
                var coverCount = sample.Biome == UnderworldTerrainBiome.FungalForest ||
                    sample.Biome == UnderworldTerrainBiome.GreatDecay ? 4 : 2;
                var coverRandom = new System.Random(unchecked(variant ^ 0x57A31));
                for (var fill = 0; fill < coverCount; fill++)
                {
                    var heading = (float)coverRandom.NextDouble() * Mathf.PI * 2f;
                    var distance = 2f + (float)coverRandom.NextDouble() * 4f;
                    var fx = x + Mathf.Cos(heading) * distance;
                    var fz = z + Mathf.Sin(heading) * distance;
                    var floor = UnderworldTerrainRuntime.SampleInstanceTerrain(fx, center.y, fz);
                    if (!floor.Admitted || floor.Biome != sample.Biome) continue;
                    var east = UnderworldTerrainRuntime.SampleInstanceTerrain(fx + 1f, center.y, fz);
                    var west = UnderworldTerrainRuntime.SampleInstanceTerrain(fx - 1f, center.y, fz);
                    var north = UnderworldTerrainRuntime.SampleInstanceTerrain(fx, center.y, fz + 1f);
                    var south = UnderworldTerrainRuntime.SampleInstanceTerrain(fx, center.y, fz - 1f);
                    if (!east.Admitted || !west.Admitted || !north.Admitted || !south.Admitted ||
                        east.Biome != floor.Biome || west.Biome != floor.Biome ||
                        north.Biome != floor.Biome || south.Biome != floor.Biome) continue;
                    var dx = Math.Max(Math.Abs(east.Height - floor.Height), Math.Abs(west.Height - floor.Height));
                    var dz = Math.Max(Math.Abs(north.Height - floor.Height), Math.Abs(south.Height - floor.Height));
                    var slope = Math.Atan(Math.Sqrt(dx * dx + dz * dz)) * 180d / Math.PI;
                    var waterLevel = ZoneSystem.instance is null ? 30d : ZoneSystem.instance.m_waterLevel;
                    try
                    {
                        var cover = UnderworldDonorVisualFactory.CreateCover(sample.Biome,
                            unchecked(variant * 31 + fill), "Magenheim_UnderworldCover", floor.Height - waterLevel, slope);
                        if (cover is null) continue;
                        cover.transform.position += UnderworldInstanceLayer.ToEngine(
                            new Vector3(fx, (float)floor.Height, fz));
                        _spawned.Add(cover);
                    }
                    catch (InvalidOperationException exception) { _log?.LogWarning(exception.Message); }
                }
            }
        }
        _log?.LogDebug($"Refreshed {_spawned.Count} spatially composed vanilla-donor Underworld ecology objects in instance space near ({center.x:0},{center.z:0}).");
    }

    private static float ClusterRadius(UnderworldTerrainBiome biome) => biome switch
    {
        UnderworldTerrainBiome.FungalForest => 24f,
        UnderworldTerrainBiome.GreatDecay => 22f,
        UnderworldTerrainBiome.FrozenCaverns => 18f,
        UnderworldTerrainBiome.SulfurousWastes => 16f,
        UnderworldTerrainBiome.FractureZones => 14f,
        UnderworldTerrainBiome.BlackwaterDeep => 12f,
        _ => 16f,
    };

    private static int ComposeVariant(UnderworldTerrainBiome biome, int variant, int member)
    {
        var role = member == 0 ? 0 : member <= 4 ? 1 : 2;
        var candidates = (biome, role) switch
        {
            (UnderworldTerrainBiome.FungalForest, 0) => new[] { 0, 1, 13, 16, 19, 22 },
            (UnderworldTerrainBiome.FungalForest, 1) => new[] { 5, 7, 11, 12, 15, 17, 18, 21 },
            (UnderworldTerrainBiome.FungalForest, _) => new[] { 6, 8, 10, 11, 14, 17, 18, 20, 21 },
            (UnderworldTerrainBiome.BlackwaterDeep, 0) => new[] { 0, 2, 4, 5 },
            (UnderworldTerrainBiome.BlackwaterDeep, 1) => new[] { 1, 2, 4, 5, 6 },
            (UnderworldTerrainBiome.BlackwaterDeep, _) => new[] { 1, 3, 6 },
            (UnderworldTerrainBiome.SulfurousWastes, 0) => new[] { 4, 5, 0, 2 },
            (UnderworldTerrainBiome.SulfurousWastes, 1) => new[] { 0, 1, 2, 3, 6 },
            (UnderworldTerrainBiome.SulfurousWastes, _) => new[] { 7, 1, 3 },
            (UnderworldTerrainBiome.FrozenCaverns, 0) => new[] { 4, 5, 0 },
            (UnderworldTerrainBiome.FrozenCaverns, 1) => new[] { 0, 2, 4, 6 },
            (UnderworldTerrainBiome.FrozenCaverns, _) => new[] { 1, 3, 5 },
            (UnderworldTerrainBiome.FractureZones, 0) => new[] { 7, 4, 2, 0 },
            (UnderworldTerrainBiome.FractureZones, 1) => new[] { 0, 2, 4, 6 },
            (UnderworldTerrainBiome.FractureZones, _) => new[] { 1, 3, 5 },
            (UnderworldTerrainBiome.GreatDecay, 0) => new[] { 0, 2, 4, 5, 7 },
            (UnderworldTerrainBiome.GreatDecay, 1) => new[] { 0, 2, 4, 5, 7, 9 },
            (UnderworldTerrainBiome.GreatDecay, _) => new[] { 1, 3, 6, 8, 10, 11 },
            _ => new[] { 0 },
        };
        var roll = variant == int.MinValue ? 0 : Math.Abs(variant);
        var preferred = candidates[roll % candidates.Length];
        var paletteSize = UnderworldVanillaDonorCatalog.Count(biome);
        return roll - roll % paletteSize + preferred;
    }

    private void SpawnDonor(Vector3 position, UnderworldTerrainBiome biome, int variant)
    {
        try
        {
            var node = UnderworldDonorVisualFactory.Create(
                biome,
                variant,
                $"Magenheim_UnderworldEcology_{biome}_{variant}");
            // Factory-local position contains the donor's catalogued ground offset. Add the native
            // instance-space terrain position instead of overwriting that offset.
            node.transform.position += position;
            _spawned.Add(node);
        }
        catch (InvalidOperationException exception)
        {
            // Runtime donor availability can legitimately lag prefab registration. Ecology is
            // disposable, so skip this placement and let the next rebuild retry through the factory.
            _log?.LogWarning(exception.Message);
        }
    }

    private void ClearMarkers()
    {
        foreach (var cell in _cells.Values)
            foreach (var item in cell) if (item) Destroy(item);
        _cells.Clear();
        _spawned = new List<GameObject>();
    }

    private void OnDisable() { ClearMarkers(); _admitted = string.Empty; }
    private void OnDestroy() => ClearMarkers();
}
