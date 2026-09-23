using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Unique Fungal Forest boss location for The First Bloom. Residency is restricted to the native
/// Underworld instance chunk selected by unique-location authority for the canonical boss identity.
/// The arena geometry is deliberately donor-backed until authored Motherbed pieces replace it;
/// location identity, deterministic residency and arbitration are production architecture.
/// </summary>
internal sealed class FungalMotherbedFamily : IUnderworldLandmarkStructureFamily
{
    internal const string CanonicalLocationId = "magenheim.underworld.location.first_bloom";
    private readonly UnderworldRuntimeServices _services;

    public UnderworldLandmarkReservationPolicy.Profile ReservationProfile { get; } =
        new(salt: 0, cellSizeChunks: 1, exclusionRadiusChunks: 2);

    internal FungalMotherbedFamily(UnderworldRuntimeServices services) =>
        _services = services ?? throw new ArgumentNullException(nameof(services));

    public string Kind => "fungal-motherbed";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FungalForest;
    public int PlacementSlot => 20;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        var anchor = _services.UniqueLocationAnchors.Resolve(CanonicalLocationId, Biome);
        return anchor.Chunk.X == key.X && anchor.Chunk.Z == key.Z;
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        var anchor = _services.UniqueLocationAnchors.Resolve(CanonicalLocationId, Biome);
        if (anchor.Chunk.X != key.X || anchor.Chunk.Z != key.Z)
            throw new InvalidOperationException("The Motherbed may only materialize at its resolved unique-location anchor.");

        var root = new GameObject($"Magenheim_FungalMotherbed_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663 ^ 0x31B1006;
        var random = new System.Random(seed);

        try
        {
            var heading = (float)(random.NextDouble() * 360d);
            root.transform.rotation = Quaternion.Euler(0f, heading, 0f);

            // Broad low center keeps First Bloom readable while establishing the rooted hollow floor.
            var bed = UnderworldDonorVisualFactory.Create(Biome, seed ^ 0x13579BDF, "MotherbedHeart");
            bed.transform.SetParent(root.transform, false);
            bed.transform.localPosition += new Vector3(0f, -0.15f, 0f);
            bed.transform.localScale = Vector3.Scale(bed.transform.localScale, new Vector3(4.6f, 0.55f, 4.6f));

            // Rooted columns define the hollow without walling off sightlines across the boss arena.
            for (var i = 0; i < 8; i++)
            {
                var angle = (i * 45f + (float)(random.NextDouble() * 8d - 4d)) * Mathf.Deg2Rad;
                var radius = 9.5f + (float)random.NextDouble() * 2.2f;
                var column = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 32452843), $"RootedColumn_{i}");
                column.transform.SetParent(root.transform, false);
                column.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, 1.1f, Mathf.Sin(angle) * radius);
                column.transform.localRotation *= Quaternion.Euler(i % 2 == 0 ? 5f : -7f, -angle * Mathf.Rad2Deg + 90f, (i - 3) * 1.8f);
                column.transform.localScale = Vector3.Scale(column.transform.localScale, new Vector3(1.2f, 3.2f + (i % 3) * 0.35f, 1.2f));
            }

            // Four luminous navigation markers preserve the plan's safe-route language around the hollow.
            for (var i = 0; i < 4; i++)
            {
                var angle = (45f + i * 90f) * Mathf.Deg2Rad;
                var marker = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 17) * 49979687), $"LuminousMarker_{i}");
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition += new Vector3(Mathf.Cos(angle) * 6.4f, 0.35f, Mathf.Sin(angle) * 6.4f);
                marker.transform.localScale *= 0.72f;
            }

            root.name += "_" + CanonicalLocationId.Replace('.', '_');
            return root;
        }
        catch
        {
            UnityEngine.Object.Destroy(root);
            throw;
        }
    }
}
