using System;
using System.Collections.Generic;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Vanilla-prefab donors used by the disposable Underworld ecology preview.
/// The names are runtime references only: Magenheim does not package Valheim meshes, textures,
/// materials or prefabs. Donors are copied into stripped visual-only scene objects after Valheim
/// has loaded its own assets.
/// </summary>
internal static class UnderworldVanillaDonorCatalog
{
    internal readonly record struct Donor(
        string PrefabName,
        float MinScale,
        float MaxScale,
        Vector3 Shape,
        float GroundOffset = 0f,
        bool Collidable = false);

    private static readonly IReadOnlyDictionary<UnderworldTerrainBiome, Donor[]> Donors =
        new Dictionary<UnderworldTerrainBiome, Donor[]>
        {
            [UnderworldTerrainBiome.FungalForest] =
            new[]
            {
                new("YggaShoot1", .85f, 1.35f, new Vector3(1.15f, 1.35f, 1.15f), -.15f, true),
                new("YggaShoot2", .9f, 1.45f, new Vector3(1.0f, 1.55f, 1.0f), -.15f, true),
                new("YggaShoot3", .8f, 1.25f, new Vector3(1.25f, 1.25f, 1.25f), -.15f, true),
                new("Pickable_Mushroom_Magecap", 6.5f, 10f, new Vector3(1.5f, 1.1f, 1.5f), -.05f),
                new("Pickable_Mushroom_JotunPuffs", 5f, 8.5f, new Vector3(1.4f, 1.15f, 1.4f), -.05f),
                new("Pickable_Mushroom_blue", 7f, 12f, new Vector3(1.25f, 1.45f, 1.25f), -.05f),
            },
            [UnderworldTerrainBiome.BlackwaterDeep] =
            new[]
            {
                new("cliff_mistlands1", 1.1f, 1.8f, new Vector3(1.0f, 1.45f, 1.0f), -.5f, true),
                new("RockFinger", 1.25f, 2.25f, new Vector3(.9f, 1.6f, .9f), -.35f, true),
                new("RockThumb", 1.4f, 2.4f, new Vector3(1.1f, 1.35f, 1.1f), -.35f, true),
                new("YggdrasilRoot", 1.2f, 2.0f, new Vector3(1.4f, .9f, 1.4f), -.25f, true),
            },
            [UnderworldTerrainBiome.SulfurousWastes] =
            new[]
            {
                new("Ashlands_rock1", 1.2f, 2.3f, new Vector3(1.15f, 1.25f, 1.15f), -.35f, true),
                new("Ashlands_rock2", 1.1f, 2.1f, new Vector3(1.3f, 1.1f, 1.3f), -.35f, true),
                new("cliff_ashlands1", 1.0f, 1.7f, new Vector3(1.0f, 1.55f, 1.0f), -.55f, true),
                new("AshlandsTree6_big", .75f, 1.2f, new Vector3(.85f, 1.25f, .85f), -.15f, true),
                new("AshlandsBush1", 1.4f, 2.8f, new Vector3(1.1f, .8f, 1.1f), -.05f),
            },
            [UnderworldTerrainBiome.FrozenCaverns] =
            new[]
            {
                new("rock3_mountain_1", 1.15f, 2.1f, new Vector3(1.0f, 1.45f, 1.0f), -.35f, true),
                new("Ice_floor", 2.0f, 4.0f, new Vector3(1.5f, .45f, 1.5f), -.12f, true),
                new("BlackIceShard_01", 1.6f, 3.5f, new Vector3(.8f, 1.8f, .8f), -.15f, true),
                new("RockFinger", .9f, 1.6f, new Vector3(.75f, 1.75f, .75f), -.35f, true),
            },
            [UnderworldTerrainBiome.FractureZones] =
            new[]
            {
                new("cliff_mistlands1", 1.1f, 2.0f, new Vector3(1.15f, 1.35f, 1.15f), -.45f, true),
                new("RockFinger", 1.2f, 2.5f, new Vector3(.8f, 1.85f, .8f), -.35f, true),
                new("RockFingerBroken", 1.3f, 2.8f, new Vector3(1.15f, 1.45f, 1.15f), -.35f, true),
                new("RockThumb", 1.25f, 2.5f, new Vector3(1.0f, 1.5f, 1.0f), -.35f, true),
                new("rockformation1", 1.4f, 3.0f, new Vector3(1.2f, 1.35f, 1.2f), -.45f, true),
            },
            [UnderworldTerrainBiome.GreatDecay] =
            new[]
            {
                new("root07", 1.4f, 2.8f, new Vector3(1.25f, 1.0f, 1.25f), -.2f, true),
                new("root08", 1.5f, 3.0f, new Vector3(1.1f, 1.2f, 1.1f), -.2f, true),
                new("root11", 1.3f, 2.7f, new Vector3(1.35f, .9f, 1.35f), -.2f, true),
                new("root12", 1.4f, 2.9f, new Vector3(1.2f, 1.1f, 1.2f), -.2f, true),
                new("YggdrasilRoot", 1.3f, 2.4f, new Vector3(1.55f, .85f, 1.55f), -.25f, true),
                new("AshlandsTree6_big", .8f, 1.35f, new Vector3(1.0f, 1.3f, 1.0f), -.15f, true),
                new("AshlandsBush2", 1.5f, 3.0f, new Vector3(1.2f, .75f, 1.2f), -.05f),
            },
        };

    internal static Donor Select(UnderworldTerrainBiome biome, int variant)
    {
        if (!Donors.TryGetValue(biome, out var donors) || donors.Length == 0)
            throw new InvalidOperationException($"No vanilla donor palette exists for {biome}.");

        var index = (variant & int.MaxValue) % donors.Length;
        return donors[index];
    }

    internal static Vector3 Scale(Donor donor, int variant)
    {
        var normalized = ((variant & int.MaxValue) % 997) / 996f;
        var scalar = Mathf.Lerp(donor.MinScale, donor.MaxScale, normalized);
        return donor.Shape * scalar;
    }
}
