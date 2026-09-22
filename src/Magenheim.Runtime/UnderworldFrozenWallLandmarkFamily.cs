using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Canonical Frozen Caverns navigation landmark: the frozen wall west of the
/// Blackwater Deep. It is deliberately authored through the same native-instance
/// landmark/reservation authority as the split pillar and three lavafalls.
/// </summary>
internal sealed class FrozenWallLandmarkFamily : IUnderworldLandmarkStructureFamily
{
    private const int FamilySalt = 0x27C4B16D;

    // Broader and rarer than the existing landmarks. This gives the arbitration
    // framework a third real reservation geometry without creating a new placer.
    public UnderworldLandmarkReservationPolicy.Profile ReservationProfile { get; } =
        new(FamilySalt, cellSizeChunks: 12, exclusionRadiusChunks: 2);

    public string Kind => "frozen-wall-landmark";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FrozenCaverns;
    public int PlacementSlot => 10;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key) =>
        UnderworldLandmarkReservationPolicy.IsAnchor(identity, key, ReservationProfile);

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FrozenWall_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663 ^ FamilySalt;
        var random = new System.Random(seed);

        try
        {
            // A long broken face reads as a wall at navigation distance while donor
            // visuals keep this first implementation inside the existing asset pipeline.
            for (var i = -3; i <= 3; i++)
            {
                var segment = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 5) * 86069), $"FrozenWallSegment_{i + 3}");
                segment.transform.SetParent(root.transform, false);
                segment.transform.localPosition += new Vector3(i * 2.25f, 1.1f + (float)random.NextDouble() * 0.65f, ((i & 1) == 0 ? -0.35f : 0.35f));
                segment.transform.localRotation *= Quaternion.Euler(-4f + (float)random.NextDouble() * 8f, -7f + (float)random.NextDouble() * 14f, i * 1.5f);
                segment.transform.localScale = Vector3.Scale(segment.transform.localScale, new Vector3(1.55f, 4.2f + (float)random.NextDouble() * 1.4f, 1.4f));
            }

            // Irregular buttresses stop the silhouette reading as a procedural fence.
            for (var i = 0; i < 4; i++)
            {
                var side = i < 2 ? -1f : 1f;
                var buttress = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 17) * 104729), $"FrozenWallButtress_{i}");
                buttress.transform.SetParent(root.transform, false);
                buttress.transform.localPosition += new Vector3(side * (4.6f + (i % 2) * 1.7f), -0.2f, 1.2f + (float)random.NextDouble() * 1.4f);
                buttress.transform.localRotation *= Quaternion.Euler(67f + (float)random.NextDouble() * 13f, side * 18f, 0f);
                buttress.transform.localScale *= 1.0f + (float)random.NextDouble() * 0.55f;
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
