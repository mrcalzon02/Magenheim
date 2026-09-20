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
    internal readonly record struct Donor(string PrefabName, float MinScale, float MaxScale, Vector3 Shape, float GroundOffset = 0f, bool Collidable = false);

    private static readonly IReadOnlyDictionary<UnderworldTerrainBiome, Donor[]> Donors = new Dictionary<UnderworldTerrainBiome, Donor[]>
    {
        [UnderworldTerrainBiome.FungalForest] = new Donor[]
        {
            new("YggaShoot1", .85f, 1.35f, new(1.15f, 1.35f, 1.15f), -.15f, true), new("YggaShoot2", .9f, 1.45f, new(1f, 1.55f, 1f), -.15f, true), new("YggaShoot3", .8f, 1.25f, new(1.25f, 1.25f, 1.25f), -.15f, true),
            new("YggaShoot1", 1f, 1.55f, new(1.55f, .82f, 1.35f), -.2f, true), new("YggaShoot2", .75f, 1.15f, new(.72f, 1.95f, .72f), -.15f, true),
            new("Pickable_Mushroom_Magecap", 6.5f, 10f, new(1.5f, 1.1f, 1.5f), -.05f), new("Pickable_Mushroom_Magecap", 5.5f, 8f, new(.82f, 1.75f, .82f), -.05f),
            new("Pickable_Mushroom_JotunPuffs", 5f, 8.5f, new(1.4f, 1.15f, 1.4f), -.05f), new("Pickable_Mushroom_JotunPuffs", 4.5f, 7f, new(1.8f, .72f, 1.55f), -.05f),
            new("Pickable_Mushroom_blue", 7f, 12f, new(1.25f, 1.45f, 1.25f), -.05f), new("Pickable_Mushroom_blue", 5.5f, 9f, new(.7f, 2.1f, .7f), -.05f),
        },
        [UnderworldTerrainBiome.BlackwaterDeep] = new Donor[]
        {
            new("cliff_mistlands1", 1.1f, 1.8f, new(1f, 1.45f, 1f), -.5f, true), new("cliff_mistlands1", .9f, 1.45f, new(1.75f, .72f, 1.35f), -.55f, true),
            new("RockFinger", 1.25f, 2.25f, new(.9f, 1.6f, .9f), -.35f, true), new("RockFinger", 1f, 1.7f, new(.58f, 2.25f, .58f), -.4f, true), new("RockThumb", 1.4f, 2.4f, new(1.1f, 1.35f, 1.1f), -.35f, true),
            new("YggdrasilRoot", 1.2f, 2f, new(1.4f, .9f, 1.4f), -.25f, true), new("YggdrasilRoot", 1f, 1.65f, new(2f, .55f, 1.45f), -.3f, true),
        },
        [UnderworldTerrainBiome.SulfurousWastes] = new Donor[]
        {
            new("Ashlands_rock1", 1.2f, 2.3f, new(1.15f, 1.25f, 1.15f), -.35f, true), new("Ashlands_rock1", .9f, 1.65f, new(1.7f, .7f, 1.3f), -.4f, true),
            new("Ashlands_rock2", 1.1f, 2.1f, new(1.3f, 1.1f, 1.3f), -.35f, true), new("Ashlands_rock2", 1f, 1.8f, new(.75f, 1.85f, .75f), -.35f, true),
            new("cliff_ashlands1", 1f, 1.7f, new(1f, 1.55f, 1f), -.55f, true), new("AshlandsTree6_big", .75f, 1.2f, new(.85f, 1.25f, .85f), -.15f, true),
            new("AshlandsTree6_big", .65f, 1f, new(.58f, 1.75f, .58f), -.15f, true), new("AshlandsBush1", 1.4f, 2.8f, new(1.1f, .8f, 1.1f), -.05f),
        },
        [UnderworldTerrainBiome.FrozenCaverns] = new Donor[]
        {
            new("rock3_mountain_1", 1.15f, 2.1f, new(1f, 1.45f, 1f), -.35f, true), new("rock3_mountain_1", .9f, 1.55f, new(1.65f, .72f, 1.4f), -.4f, true),
            new("Ice_floor", 2f, 4f, new(1.5f, .45f, 1.5f), -.12f, true), new("Ice_floor", 1.6f, 3f, new(2.2f, .22f, 1.35f), -.1f, true),
            new("BlackIceShard_01", 1.6f, 3.5f, new(.8f, 1.8f, .8f), -.15f, true), new("BlackIceShard_01", 1.2f, 2.5f, new(.48f, 2.65f, .48f), -.15f, true), new("RockFinger", .9f, 1.6f, new(.75f, 1.75f, .75f), -.35f, true),
        },
        [UnderworldTerrainBiome.FractureZones] = new Donor[]
        {
            new("cliff_mistlands1", 1.1f, 2f, new(1.15f, 1.35f, 1.15f), -.45f, true), new("cliff_mistlands1", .85f, 1.4f, new(1.9f, .65f, 1.25f), -.5f, true),
            new("RockFinger", 1.2f, 2.5f, new(.8f, 1.85f, .8f), -.35f, true), new("RockFinger", 1f, 1.8f, new(.48f, 2.7f, .48f), -.4f, true),
            new("RockFingerBroken", 1.3f, 2.8f, new(1.15f, 1.45f, 1.15f), -.35f, true), new("RockFingerBroken", 1f, 2f, new(1.7f, .8f, 1.3f), -.4f, true),
            new("RockThumb", 1.25f, 2.5f, new(1f, 1.5f, 1f), -.35f, true), new("rockformation1", 1.4f, 3f, new(1.2f, 1.35f, 1.2f), -.45f, true),
        },
        [UnderworldTerrainBiome.GreatDecay] = new Donor[]
        {
            new("root07", 1.4f, 2.8f, new(1.25f, 1f, 1.25f), -.2f, true), new("root07", 1.1f, 2f, new(1.9f, .58f, 1.45f), -.25f, true),
            new("root08", 1.5f, 3f, new(1.1f, 1.2f, 1.1f), -.2f, true), new("root08", 1.15f, 2.1f, new(.68f, 1.9f, .68f), -.2f, true), new("root11", 1.3f, 2.7f, new(1.35f, .9f, 1.35f), -.2f, true),
            new("root12", 1.4f, 2.9f, new(1.2f, 1.1f, 1.2f), -.2f, true), new("root12", 1f, 1.9f, new(2.1f, .52f, 1.55f), -.25f, true),
            new("YggdrasilRoot", 1.3f, 2.4f, new(1.55f, .85f, 1.55f), -.25f, true), new("YggdrasilRoot", 1f, 1.8f, new(2.25f, .48f, 1.5f), -.3f, true),
            new("AshlandsTree6_big", .8f, 1.35f, new(1f, 1.3f, 1f), -.15f, true), new("AshlandsTree6_big", .65f, 1f, new(.62f, 1.8f, .62f), -.15f, true), new("AshlandsBush2", 1.5f, 3f, new(1.2f, .75f, 1.2f), -.05f),
        },
    };

    internal static Donor Select(UnderworldTerrainBiome biome, int variant)
    {
        if (!Donors.TryGetValue(biome, out var donors) || donors.Length == 0) throw new InvalidOperationException($"No vanilla donor palette exists for {biome}.");
        return donors[(variant & int.MaxValue) % donors.Length];
    }

    internal static Vector3 Scale(Donor donor, int variant)
    {
        // Three deterministic visual-mass tiers make clustered ecology read as a composition:
        // sparse landmark forms, a dominant middle canopy/formation, and smaller understory/fill.
        // Each silhouette axis has independently salted micro-variation so repeated copies of the
        // same Valheim donor do not retain an identical outline. Vertical variation stays restrained
        // to preserve the donor's authored identity and conservative collider assumptions.
        var massRoll = Hash01(variant, 0x1C7);
        var mass = massRoll < .16f ? 1.28f : massRoll < .70f ? 1f : .72f;
        var scalar = Mathf.Lerp(donor.MinScale, donor.MaxScale, Hash01(variant, 0x2D3)) * mass;
        var xJitter = Mathf.Lerp(.92f, 1.08f, Hash01(variant, 0x51B));
        var yJitter = Mathf.Lerp(.96f, 1.04f, Hash01(variant, 0x63D));
        var zJitter = Mathf.Lerp(.92f, 1.08f, Hash01(variant, 0x7A9));
        return Vector3.Scale(donor.Shape * scalar, new Vector3(xJitter, yJitter, zJitter));
    }

    private static float Hash01(int value, int salt)
    {
        unchecked
        {
            var hash = (uint)value;
            hash ^= (uint)salt * 0x9E3779B9u;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;
            return (hash & 0x00FFFFFFu) / 16777215f;
        }
    }
}
