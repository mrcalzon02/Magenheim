using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Sparse Sulfurous Wastes navigation landmark described by the canonical
/// "sulfur coast below the three lavafalls" route language.  This deliberately
/// reuses the native Underworld landmark/residency authority: it is not a new
/// placement system and does not simulate a second fluid lifecycle.
/// </summary>
internal sealed class SulfurThreeLavafallsLandmarkFamily : IUnderworldLandmarkStructureFamily
{
    private const int FamilySalt = 0x53A17F2B;

    // A different cell/radius profile from the Fungal split pillar intentionally
    // exercises the shared symmetric arbitration path with real content.
    public UnderworldLandmarkReservationPolicy.Profile ReservationProfile { get; } =
        new(FamilySalt, cellSizeChunks: 10, exclusionRadiusChunks: 2);

    public string Kind => "sulfur-three-lavafalls-landmark";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.SulfurousWastes;
    public int PlacementSlot => 11;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key) =>
        UnderworldLandmarkReservationPolicy.IsAnchor(identity, key, ReservationProfile);

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_SulfurThreeLavafalls_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663 ^ FamilySalt;
        var random = new System.Random(seed);

        try
        {
            // Build a broad volcanic crown and three descending channels from the
            // existing Sulfurous Wastes donor palette.  The channels are visual
            // landmark geometry only until the native lava presentation primitive
            // is available; no custom fluid simulation is introduced here.
            for (var i = 0; i < 5; i++)
            {
                var crown = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 32452843), $"LavafallCrown_{i}");
                crown.transform.SetParent(root.transform, false);
                crown.transform.localPosition += new Vector3((i - 2) * 2.4f, 2.8f + (float)random.NextDouble() * 1.2f, 1.8f);
                crown.transform.localRotation *= Quaternion.Euler(72f + (float)random.NextDouble() * 12f, (float)random.NextDouble() * 360f, 0f);
                crown.transform.localScale = Vector3.Scale(crown.transform.localScale, new Vector3(1.8f, 2.2f, 1.6f));
            }

            for (var channel = -1; channel <= 1; channel++)
            {
                for (var step = 0; step < 4; step++)
                {
                    var fall = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((channel + 2) * 49999) ^ ((step + 1) * 86069), $"Lavafall_{channel + 1}_{step}");
                    fall.transform.SetParent(root.transform, false);
                    fall.transform.localPosition += new Vector3(channel * 3.2f, 2.1f - step * 1.35f, 0.8f - step * 1.15f);
                    fall.transform.localRotation *= Quaternion.Euler(80f, channel * 7f, channel * -5f);
                    fall.transform.localScale = Vector3.Scale(fall.transform.localScale, new Vector3(0.72f, 1.9f, 0.72f));
                }
            }

            for (var i = 0; i < 6; i++)
            {
                var angle = (float)(random.NextDouble() * Math.PI * 2d);
                var radius = 3.5f + (float)random.NextDouble() * 4.5f;
                var footing = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 104729), $"SulfurCoastFooting_{i}");
                footing.transform.SetParent(root.transform, false);
                footing.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, -0.45f, Mathf.Sin(angle) * radius);
                footing.transform.localRotation *= Quaternion.Euler(68f + (float)random.NextDouble() * 20f, angle * Mathf.Rad2Deg, 0f);
                footing.transform.localScale *= 0.9f + (float)random.NextDouble() * 0.7f;
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
