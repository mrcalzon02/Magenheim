using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// The unique Fracture boss location. Unlike repeatable biome landmarks, this family is admitted
/// only at the native instance chunk selected by UnderworldUniqueLocationAnchorResolver for the
/// canonical Fracture boss-location identity. It participates in landmark arbitration with highest
/// priority so ordinary ruins and navigation landmarks cannot displace the boss arena.
/// </summary>
internal sealed class FractureSuspendedCourtFamily : IUnderworldLandmarkStructureFamily
{
    internal const string CanonicalLocationId = "magenheim.underworld.location.fracture";
    private readonly UnderworldRuntimeServices _services;

    // cellSize=1 makes every native chunk a legal reservation anchor; Eligible then narrows that
    // set to the one world-specific chunk selected by unique-location authority. Salt zero gives
    // progression-critical boss locations priority over ambient navigation landmarks.
    public UnderworldLandmarkReservationPolicy.Profile ReservationProfile { get; } =
        new(salt: 0, cellSizeChunks: 1, exclusionRadiusChunks: 2);

    internal FractureSuspendedCourtFamily(UnderworldRuntimeServices services) =>
        _services = services ?? throw new ArgumentNullException(nameof(services));

    public string Kind => "fracture-suspended-court";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FractureZones;
    public int PlacementSlot => 20;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        var anchor = _services.UniqueLocationAnchors.Resolve(CanonicalLocationId, Biome);
        return anchor.Chunk.X == key.X && anchor.Chunk.Z == key.Z;
    }

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var anchor = _services.UniqueLocationAnchors.Resolve(CanonicalLocationId, Biome);
        if (anchor.Chunk.X != key.X || anchor.Chunk.Z != key.Z)
            throw new InvalidOperationException("The Suspended Court may only materialize at its resolved unique-location anchor.");

        var root = new GameObject($"Magenheim_FractureSuspendedCourt_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663 ^ 0x4C71A2D;
        var random = new System.Random(seed);

        try
        {
            var heading = (float)(random.NextDouble() * 360d);
            root.transform.rotation = Quaternion.Euler(0f, heading, 0f);

            // Raised central dais: this is the arena's stable gameplay center and future Rift Titan
            // encounter seam. Donor geometry is intentionally provisional; location authority is not.
            var dais = UnderworldDonorVisualFactory.Create(Biome, seed ^ 0x13579BDF, "CourtDais");
            dais.transform.SetParent(root.transform, false);
            dais.transform.localPosition += new Vector3(0f, 0.65f, 0f);
            dais.transform.localScale = Vector3.Scale(dais.transform.localScale, new Vector3(3.8f, 0.7f, 3.8f));

            // Six broken court pylons establish a broad boss-arena silhouette without filling the
            // combat floor. Alternating damage/lean prevents the ring reading as a generic circle.
            for (var i = 0; i < 6; i++)
            {
                var angle = i * 60f * Mathf.Deg2Rad;
                var pylon = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 1) * 32452843), $"CourtPylon_{i}");
                pylon.transform.SetParent(root.transform, false);
                pylon.transform.localPosition += new Vector3(Mathf.Cos(angle) * 8.4f, 1.2f, Mathf.Sin(angle) * 8.4f);
                pylon.transform.localRotation *= Quaternion.Euler(i % 2 == 0 ? 6f : -9f, -angle * Mathf.Rad2Deg + 90f, (i - 2) * 2.5f);
                pylon.transform.localScale = Vector3.Scale(pylon.transform.localScale, new Vector3(1.15f, 3.4f - (i % 3) * 0.35f, 1.15f));
            }

            // Four suspended approach fragments leave visible voids between the outer route and dais.
            // They communicate "Suspended Court" while keeping the arena center unobstructed.
            for (var i = 0; i < 4; i++)
            {
                var angle = (45f + i * 90f) * Mathf.Deg2Rad;
                for (var segment = 0; segment < 2; segment++)
                {
                    var approach = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 11) * 49979687) ^ segment, $"CourtApproach_{i}_{segment}");
                    approach.transform.SetParent(root.transform, false);
                    var radius = 11.2f + segment * 2.2f;
                    approach.transform.localPosition += new Vector3(Mathf.Cos(angle) * radius, 0.15f - segment * 0.25f, Mathf.Sin(angle) * radius);
                    approach.transform.localRotation *= Quaternion.Euler(segment * 4f, -angle * Mathf.Rad2Deg, (i % 2 == 0 ? 1f : -1f) * (5f + segment * 3f));
                    approach.transform.localScale = Vector3.Scale(approach.transform.localScale, new Vector3(1.8f, 0.42f, 1.0f));
                }
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
