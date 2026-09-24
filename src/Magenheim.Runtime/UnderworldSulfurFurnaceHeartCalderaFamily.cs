using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Unique Sulfurous Wastes boss location for the Furnace Heart. The location is resident only at
/// the native Underworld-instance anchor selected for the canonical Furnace Heart identity.
/// Donor geometry is provisional; identity, deterministic residency and arbitration are durable.
/// </summary>
internal sealed class SulfurFurnaceHeartCalderaFamily : IUnderworldLandmarkStructureFamily
{
    internal const string CanonicalLocationId = "magenheim.underworld.location.furnace_heart";
    private readonly UnderworldRuntimeServices _services;

    public UnderworldLandmarkReservationPolicy.Profile ReservationProfile { get; } =
        new(salt: 0, cellSizeChunks: 1, exclusionRadiusChunks: 2);

    internal SulfurFurnaceHeartCalderaFamily(UnderworldRuntimeServices services) =>
        _services = services ?? throw new ArgumentNullException(nameof(services));

    public string Kind => "sulfur-furnace-heart-caldera";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.SulfurousWastes;
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
            throw new InvalidOperationException("The Furnace Heart caldera may only materialize at its resolved unique-location anchor.");

        var root = new GameObject($"Magenheim_SulfurFurnaceHeartCaldera_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663 ^ 0x5F17A21;
        var random = new System.Random(seed);

        try
        {
            root.transform.rotation = Quaternion.Euler(0f, (float)(random.NextDouble() * 360d), 0f);

            // Broad low encounter floor: later authored vent/lavafall anchors can occupy the perimeter
            // without changing the canonical location or deterministic residency contract.
            var floor = UnderworldDonorVisualFactory.Create(Biome, seed ^ 0x13579BDF, "FurnaceHeartFloor");
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition += new Vector3(0f, -0.45f, 0f);
            floor.transform.localScale = Vector3.Scale(floor.transform.localScale, new Vector3(3.8f, 0.38f, 3.8f));

            // Twelve broken basalt teeth define a porous caldera rim while retaining clear combat exits.
            for (var i = 0; i < 12; i++)
            {
                var angle = (i * 30f + (float)(random.NextDouble() * 8d - 4d)) * Mathf.Deg2Rad;
                var radius = 11.5f + (float)random.NextDouble() * 1.8f;
                var tooth = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 32452843), $"CalderaTooth_{i}");
                tooth.transform.SetParent(root.transform, false);
                tooth.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, -0.1f + (i % 3) * 0.2f, Mathf.Sin(angle) * radius);
                tooth.transform.localRotation *= Quaternion.Euler(i % 2 == 0 ? 9f : -7f, -angle * Mathf.Rad2Deg + 90f, (i - 5) * 1.4f);
                tooth.transform.localScale = Vector3.Scale(tooth.transform.localScale, new Vector3(1.2f, 1.8f + (i % 4) * 0.25f, 1.2f));
            }

            // Three larger perimeter anchors reserve the authored phase-two lavafall/vent language
            // already specified for Furnace Heart without pretending donor props are real fluids.
            for (var i = 0; i < 3; i++)
            {
                var angle = (30f + i * 120f) * Mathf.Deg2Rad;
                var vent = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 31) * 49979687), $"FurnaceVentAnchor_{i}");
                vent.transform.SetParent(root.transform, false);
                vent.transform.localPosition += new Vector3(Mathf.Cos(angle) * 15.5f, 0.15f, Mathf.Sin(angle) * 15.5f);
                vent.transform.localRotation *= Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, i % 2 == 0 ? 6f : -6f);
                vent.transform.localScale = Vector3.Scale(vent.transform.localScale, new Vector3(1.7f, 2.5f, 1.7f));
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
