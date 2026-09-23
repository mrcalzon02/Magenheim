using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Canonical Fracture Zones navigation landmark: an ancient bridge spanning a fault,
/// with a collapsed central crossing that makes the fracture itself legible at range.
/// Uses the shared native-instance landmark reservation/arbitration authority.
/// </summary>
internal sealed class BrokenAncientBridgeLandmarkFamily : IUnderworldLandmarkStructureFamily
{
    private const int FamilySalt = 0x3A71C5E2;

    // The bridge is a long macro-navigation silhouette. Keep it uncommon and reserve
    // enough surrounding territory that ordinary structures cannot clutter the span.
    public UnderworldLandmarkReservationPolicy.Profile ReservationProfile { get; } =
        new(FamilySalt, cellSizeChunks: 16, exclusionRadiusChunks: 3);

    public string Kind => "broken-ancient-bridge-landmark";
    public UnderworldTerrainBiome Biome => UnderworldTerrainBiome.FractureZones;
    public int PlacementSlot => 12;

    public bool Eligible(UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key) =>
        UnderworldLandmarkReservationPolicy.IsAnchor(identity, key, ReservationProfile);

    public GameObject Compose(Vector3 position, UnderworldWorldIdentity identity, UnderworldInstanceChunkKey key)
    {
        var root = new GameObject($"Magenheim_BrokenAncientBridge_{key.X}_{key.Z}");
        root.transform.position = position;
        var seed = identity.DerivedSeed32 ^ key.X * 73856093 ^ key.Z * 19349663 ^ FamilySalt;
        var random = new System.Random(seed);

        try
        {
            // Two surviving approaches establish a strong east-west crossing line while
            // deliberately leaving the middle absent. The inner pieces sag and cant into
            // the fault so this reads as a collapsed bridge, not two unrelated roads.
            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 5; i++)
                {
                    var inward = i / 4f;
                    var deck = UnderworldDonorVisualFactory.Create(
                        Biome,
                        seed ^ ((side < 0 ? 101 : 211) + i * 86069),
                        $"BridgeDeck_{(side < 0 ? "West" : "East")}_{i}");
                    deck.transform.SetParent(root.transform, false);
                    var x = side * (8.8f - i * 1.65f);
                    var lateralDamage = ((float)random.NextDouble() - 0.5f) * inward * 0.55f;
                    deck.transform.localPosition += new Vector3(x, 1.15f - inward * inward * 1.15f, lateralDamage);
                    deck.transform.localRotation *= Quaternion.Euler(
                        inward * (4f + (float)random.NextDouble() * 10f),
                        ((float)random.NextDouble() - 0.5f) * (3f + inward * 9f),
                        -side * inward * (5f + (float)random.NextDouble() * 11f));
                    deck.transform.localScale = Vector3.Scale(
                        deck.transform.localScale,
                        new Vector3(1.45f - inward * 0.28f, 0.42f, 2.05f));
                }
            }

            // Four heavy surviving supports give the crossing an ancient engineered
            // silhouette. The inner pair lean toward the missing span as if the fault
            // movement tore the bridge apart rather than the bridge simply ending.
            for (var i = 0; i < 4; i++)
            {
                var side = i < 2 ? -1f : 1f;
                var inner = (i & 1) == 1;
                var support = UnderworldDonorVisualFactory.Create(
                    Biome,
                    seed ^ ((i + 401) * 104729),
                    $"BridgeSupport_{i}");
                support.transform.SetParent(root.transform, false);
                support.transform.localPosition += new Vector3(side * (inner ? 3.0f : 7.0f), -1.55f, 0f);
                support.transform.localRotation *= Quaternion.Euler(0f, 0f, inner ? -side * (8f + (float)random.NextDouble() * 7f) : side * 2f);
                support.transform.localScale = Vector3.Scale(support.transform.localScale, new Vector3(1.35f, 3.8f, 1.55f));
            }

            // Fallen fragments below the absent center reinforce that the gap is authored
            // damage and provide close-range visual evidence without restoring a crossing.
            for (var i = 0; i < 3; i++)
            {
                var rubble = UnderworldDonorVisualFactory.Create(Biome, seed ^ ((i + 701) * 65537), $"BridgeFall_{i}");
                rubble.transform.SetParent(root.transform, false);
                rubble.transform.localPosition += new Vector3(-1.25f + i * 1.2f, -2.6f - i * 0.35f, ((float)random.NextDouble() - 0.5f) * 1.5f);
                rubble.transform.localRotation *= Quaternion.Euler(55f + (float)random.NextDouble() * 70f, (float)random.NextDouble() * 360f, 25f + (float)random.NextDouble() * 80f);
                rubble.transform.localScale *= 0.65f + (float)random.NextDouble() * 0.45f;
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
