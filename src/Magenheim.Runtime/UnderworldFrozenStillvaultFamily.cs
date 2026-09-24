using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Unique Frozen Caverns boss location for the White Silence. Residency is restricted to the
/// native Underworld-instance anchor selected for the canonical White Silence location identity.
/// Donor geometry is provisional; identity, deterministic residency and arbitration are durable.
/// </summary>
internal sealed class FrozenStillvaultFamily : IUnderworldLandmarkStructureFamily
{
    internal const string CanonicalLocationId = "magenheim.underworld.location.white_silence";
    private readonly UnderworldRuntimeServices _services;

    public UnderworldLandmarkReservationPolicy.Profile ReservationProfile { get; } =
        new(salt: 0, cellSizeChunks: 1, exclusionRadiusChunks: 2);

    internal FrozenStillvaultFamily(UnderworldRuntimeServices services) =>
        _services = services ?? throw new ArgumentNullException(nameof(services));

    public string Kind => "frozen-stillvault";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FrozenCaverns;
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
            throw new InvalidOperationException("The Stillvault may only materialize at its resolved unique-location anchor.");

        var root = new GameObject($"Magenheim_FrozenStillvault_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663 ^ 0x41C3E17;
        var random = new System.Random(seed);

        try
        {
            root.transform.rotation = Quaternion.Euler(0f, (float)(random.NextDouble() * 360d), 0f);

            // Low, readable central court. Authored ice-vault geometry can replace these donors without
            // changing the unique-location identity or its deterministic anchor contract.
            var floor = UnderworldDonorVisualFactory.Create(Biome, seed ^ 0x1735A9D, "StillvaultFloor");
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition += new Vector3(0f, -0.45f, 0f);
            floor.transform.localScale = Vector3.Scale(floor.transform.localScale, new Vector3(3.6f, 0.35f, 3.6f));

            // Eight tall vault ribs establish a frozen nave while leaving broad sightlines and exits.
            for (var i = 0; i < 8; i++)
            {
                var angle = (i * 45f) * Mathf.Deg2Rad;
                var rib = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 32452843), $"VaultRib_{i}");
                rib.transform.SetParent(root.transform, false);
                rib.transform.localPosition += new Vector3(Mathf.Cos(angle) * 11.5f, 0.3f, Mathf.Sin(angle) * 11.5f);
                rib.transform.localRotation *= Quaternion.Euler(i % 2 == 0 ? 5f : -5f, -angle * Mathf.Rad2Deg + 90f, 0f);
                rib.transform.localScale = Vector3.Scale(rib.transform.localScale, new Vector3(1.15f, 2.8f, 1.15f));
            }

            // Four cardinal threshold markers reserve readable approach lanes for the later White Silence encounter.
            for (var i = 0; i < 4; i++)
            {
                var angle = (i * 90f) * Mathf.Deg2Rad;
                var threshold = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 17) * 49979687), $"StillvaultThreshold_{i}");
                threshold.transform.SetParent(root.transform, false);
                threshold.transform.localPosition += new Vector3(Mathf.Cos(angle) * 15f, -0.1f, Mathf.Sin(angle) * 15f);
                threshold.transform.localRotation *= Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                threshold.transform.localScale = Vector3.Scale(threshold.transform.localScale, new Vector3(1.5f, 1.6f, 1.5f));
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
