using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// First sparse landmark consumer of the native Underworld reservation authority.
/// The split pillar gives Fungal Forest geography a large, repeatable navigation
/// silhouette without introducing a second world/zone placement system.
/// </summary>
internal sealed class FungalSplitPillarLandmarkFamily : IUnderworldLandmarkStructureFamily
{
    private const int FamilySalt = 0x6A31C47D;

    // One native chunk of breathing room is now safe because residency validates
    // each deterministic anchor against the landmark biome before ordinary
    // structures yield to it.
    public UnderworldLandmarkReservationPolicy.Profile ReservationProfile { get; } =
        new(FamilySalt, cellSizeChunks: 8, exclusionRadiusChunks: 1);

    public string Kind => "fungal-split-pillar-landmark";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FungalForest;
    public int PlacementSlot => 10;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key) =>
        UnderworldLandmarkReservationPolicy.IsAnchor(identity, key, ReservationProfile);

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_FungalSplitPillar_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663 ^ FamilySalt;
        var random = new System.Random(seed);

        try
        {
            for (var side = -1; side <= 1; side += 2)
            {
                var shaft = UnderworldDonorVisualFactory.Create(Biome, seed ^ side * 32452843, side < 0 ? "SplitPillarLeft" : "SplitPillarRight");
                shaft.transform.SetParent(root.transform, false);
                shaft.transform.localPosition += new Vector3(side * 2.1f, 1.4f, 0f);
                shaft.transform.localRotation *= Quaternion.Euler(side * -7f, side * 8f, side * -11f);
                shaft.transform.localScale *= new Vector3(1.65f, 4.8f, 1.65f);
            }

            for (var i = 0; i < 4; i++)
            {
                var angle = (float)(random.NextDouble() * Math.PI * 2d);
                var radius = 2.4f + (float)random.NextDouble() * 2.6f;
                var footing = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 49999), $"SplitPillarFooting_{i}");
                footing.transform.SetParent(root.transform, false);
                footing.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, -0.35f, Mathf.Sin(angle) * radius);
                footing.transform.localRotation *= Quaternion.Euler(70f + (float)random.NextDouble() * 18f, angle * Mathf.Rad2Deg, 0f);
                footing.transform.localScale *= 0.85f + (float)random.NextDouble() * 0.55f;
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
