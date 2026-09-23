using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Canonical Great Decay navigation landmark: an old road whose surviving course
/// visibly enters the biome and is progressively swallowed by Decay growth.
/// Uses the shared native-instance landmark reservation/arbitration authority.
/// </summary>
internal sealed class GreatDecayVanishingRoadLandmarkFamily : IUnderworldLandmarkStructureFamily
{
    private const int FamilySalt = 0x5EED4A31;

    // Long enough to read as a route rather than a point landmark, while remaining
    // sparse enough that the Great Decay does not acquire a procedural road grid.
    public UnderworldLandmarkReservationPolicy.Profile ReservationProfile { get; } =
        new(FamilySalt, cellSizeChunks: 14, exclusionRadiusChunks: 2);

    public string Kind => "great-decay-vanishing-road-landmark";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.GreatDecay;
    public int PlacementSlot => 11;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key) =>
        UnderworldLandmarkReservationPolicy.IsAnchor(identity, key, ReservationProfile);

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_GreatDecayVanishingRoad_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663 ^ FamilySalt;
        var random = new System.Random(seed);

        try
        {
            // Twenty metres of broken route gives the player a strong directional line.
            // The pieces become shorter, narrower, more sunken and more crooked toward
            // the Decay end so the road appears to be physically consumed rather than
            // simply terminating at an arbitrary generation boundary.
            const int segmentCount = 9;
            for (var i = 0; i < segmentCount; i++)
            {
                var progress = i / (float)(segmentCount - 1);
                var segment = UnderworldDonorVisualFactory.Create(
                    Biome,
                    seed ^ ((i + 31) * 86069),
                    $"VanishingRoadSegment_{i}");
                segment.transform.SetParent(root.transform, false);

                var lateralWander = ((float)random.NextDouble() - 0.5f) * (0.25f + progress * 0.9f);
                var sink = -0.12f - progress * progress * 1.15f;
                segment.transform.localPosition += new Vector3(lateralWander, sink, (i - 4) * 2.35f);
                segment.transform.localRotation *= Quaternion.Euler(
                    progress * (3f + (float)random.NextDouble() * 8f),
                    ((float)random.NextDouble() - 0.5f) * (4f + progress * 18f),
                    ((float)random.NextDouble() - 0.5f) * progress * 10f);

                var width = 1.65f - progress * 0.72f;
                var height = 0.34f - progress * 0.12f;
                var length = 1.35f - progress * 0.42f;
                segment.transform.localScale = Vector3.Scale(
                    segment.transform.localScale,
                    new Vector3(width, height, length));
            }

            // Encroaching Decay masses increase toward the disappearing end. They use
            // the biome donor pipeline, keeping this first implementation compatible
            // with later custom art replacement without changing placement semantics.
            for (var i = 0; i < 6; i++)
            {
                var progress = (i + 1) / 6f;
                var growth = UnderworldDonorVisualFactory.Create(
                    Biome,
                    seed ^ ((i + 73) * 104729),
                    $"RoadDecayGrowth_{i}");
                growth.transform.SetParent(root.transform, false);
                var side = (i & 1) == 0 ? -1f : 1f;
                growth.transform.localPosition += new Vector3(
                    side * (1.25f - progress * 0.55f),
                    -0.2f + progress * 0.45f,
                    1.5f + i * 1.45f);
                growth.transform.localRotation *= Quaternion.Euler(
                    18f + (float)random.NextDouble() * 35f,
                    (float)random.NextDouble() * 360f,
                    side * (12f + progress * 24f));
                growth.transform.localScale *= 0.45f + progress * 1.15f;
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
