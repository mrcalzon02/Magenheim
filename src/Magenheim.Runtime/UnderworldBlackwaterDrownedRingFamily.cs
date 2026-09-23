using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Unique Blackwater Deep boss location for the Blackwater Maw. Residency is restricted to the
/// native Underworld instance chunk selected by unique-location authority for the canonical boss
/// identity. Donor geometry is provisional; identity, deterministic residency and arbitration are
/// production architecture.
/// </summary>
internal sealed class BlackwaterDrownedRingFamily : IUnderworldLandmarkStructureFamily
{
    internal const string CanonicalLocationId = "magenheim.underworld.location.blackwater_maw";
    private readonly UnderworldRuntimeServices _services;

    public UnderworldLandmarkReservationPolicy.Profile ReservationProfile { get; } =
        new(salt: 0, cellSizeChunks: 1, exclusionRadiusChunks: 2);

    internal BlackwaterDrownedRingFamily(UnderworldRuntimeServices services) =>
        _services = services ?? throw new ArgumentNullException(nameof(services));

    public string Kind => "blackwater-drowned-ring";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.BlackwaterDeep;
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
            throw new InvalidOperationException("The Drowned Ring may only materialize at its resolved unique-location anchor.");

        var root = new GameObject($"Magenheim_BlackwaterDrownedRing_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663 ^ 0x2D70A11;
        var random = new System.Random(seed);

        try
        {
            root.transform.rotation = Quaternion.Euler(0f, (float)(random.NextDouble() * 360d), 0f);

            // Low central shelf establishes the Maw encounter seam while leaving the surrounding
            // wet arena open for shore/water movement and later authored water-boundary behavior.
            var center = UnderworldDonorVisualFactory.Create(Biome, seed ^ 0x13579BDF, "DrownedRingCenter");
            center.transform.SetParent(root.transform, false);
            center.transform.localPosition += new Vector3(0f, -0.35f, 0f);
            center.transform.localScale = Vector3.Scale(center.transform.localScale, new Vector3(3.2f, 0.42f, 3.2f));

            // Twelve broken perimeter stones form the drowned ring without making a solid wall.
            for (var i = 0; i < 12; i++)
            {
                var angle = (i * 30f + (float)(random.NextDouble() * 6d - 3d)) * Mathf.Deg2Rad;
                var radius = 10.5f + (float)random.NextDouble() * 1.4f;
                var stone = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 32452843), $"DrownedRingStone_{i}");
                stone.transform.SetParent(root.transform, false);
                stone.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, -0.15f + (i % 3) * 0.18f, Mathf.Sin(angle) * radius);
                stone.transform.localRotation *= Quaternion.Euler(i % 2 == 0 ? 7f : -10f, -angle * Mathf.Rad2Deg + 90f, (i - 5) * 1.7f);
                stone.transform.localScale = Vector3.Scale(stone.transform.localScale, new Vector3(1.15f, 1.7f + (i % 4) * 0.22f, 1.15f));
            }

            // Four interrupted approaches communicate a formerly traversable flooded civic ring and
            // leave broad channels for vessel and swimming interaction during later encounter work.
            for (var i = 0; i < 4; i++)
            {
                var angle = (45f + i * 90f) * Mathf.Deg2Rad;
                var approach = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 19) * 49979687), $"DrownedApproach_{i}");
                approach.transform.SetParent(root.transform, false);
                approach.transform.localPosition += new Vector3(Mathf.Cos(angle) * 14.2f, -0.42f, Mathf.Sin(angle) * 14.2f);
                approach.transform.localRotation *= Quaternion.Euler(4f, -angle * Mathf.Rad2Deg, i % 2 == 0 ? 5f : -5f);
                approach.transform.localScale = Vector3.Scale(approach.transform.localScale, new Vector3(2.2f, 0.35f, 1.05f));
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
