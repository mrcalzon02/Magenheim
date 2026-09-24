using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Unique Great Decay boss location for the Carrion Crown. Residency is restricted to the
/// native Underworld-instance anchor selected for the canonical Carrion Crown location identity.
/// Donor geometry is provisional; identity, deterministic residency and arbitration are durable.
/// </summary>
internal sealed class GreatDecayCarrionCrownFamily : IUnderworldLandmarkStructureFamily
{
    internal const string CanonicalLocationId = "magenheim.underworld.location.carrion_crown";
    private readonly UnderworldRuntimeServices _services;

    public UnderworldLandmarkReservationPolicy.Profile ReservationProfile { get; } =
        new(salt: 0, cellSizeChunks: 1, exclusionRadiusChunks: 2);

    internal GreatDecayCarrionCrownFamily(UnderworldRuntimeServices services) =>
        _services = services ?? throw new ArgumentNullException(nameof(services));

    public string Kind => "great-decay-carrion-crown";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.GreatDecay;
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
            throw new InvalidOperationException("The Carrion Crown arena may only materialize at its resolved unique-location anchor.");

        var root = new GameObject($"Magenheim_GreatDecayCarrionCrown_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663 ^ 0x2D7A91B;
        var random = new System.Random(seed);

        try
        {
            root.transform.rotation = Quaternion.Euler(0f, (float)(random.NextDouble() * 360d), 0f);

            // A low central carrion bed keeps the final arena readable while authored biomass can replace donors later.
            var bed = UnderworldDonorVisualFactory.Create(Biome, seed ^ 0x19B74D3, "CarrionBed");
            bed.transform.SetParent(root.transform, false);
            bed.transform.localPosition += new Vector3(0f, -0.55f, 0f);
            bed.transform.localScale = Vector3.Scale(bed.transform.localScale, new Vector3(4.2f, 0.3f, 4.2f));

            // Twelve asymmetrical crown-spines create the arena silhouette without closing sightlines.
            for (var i = 0; i < 12; i++)
            {
                var angle = (i * 30f) * Mathf.Deg2Rad;
                var radius = 11.5f + (float)random.NextDouble() * 2.5f;
                var spine = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 32452843), $"CarrionSpine_{i}");
                spine.transform.SetParent(root.transform, false);
                spine.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, -0.1f, Mathf.Sin(angle) * radius);
                spine.transform.localRotation *= Quaternion.Euler(i % 2 == 0 ? -9f : 11f, -angle * Mathf.Rad2Deg + 90f, 0f);
                spine.transform.localScale = Vector3.Scale(spine.transform.localScale, new Vector3(1.0f, 2.4f + (i % 3) * 0.35f, 1.0f));
            }

            // Four ruptured approach lanes keep cardinal ingress legible through Great Decay's persistent miasma.
            for (var i = 0; i < 4; i++)
            {
                var angle = (i * 90f) * Mathf.Deg2Rad;
                var marker = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 17) * 49979687), $"CarrionApproach_{i}");
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition += new Vector3(Mathf.Cos(angle) * 16f, -0.2f, Mathf.Sin(angle) * 16f);
                marker.transform.localRotation *= Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                marker.transform.localScale = Vector3.Scale(marker.transform.localScale, new Vector3(1.7f, 1.2f, 1.7f));
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
