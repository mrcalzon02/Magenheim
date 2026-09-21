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

    // Separate fill palette preserves existing landmark proportions and index identities.
    internal sealed record GroundCover(string Name, Donor Donor, Color Color,
        float MinHeightAboveWater = 0f, float MaxHeightAboveWater = 1000f, float MaxSlope = 35f);
    private static readonly IReadOnlyDictionary<UnderworldTerrainBiome, GroundCover[]> Cover =
        new Dictionary<UnderworldTerrainBiome, GroundCover[]>
    {
        [UnderworldTerrainBiome.FungalForest] = new GroundCover[]
        {
            new("Jade fiddlefern", new("Fiddleheadfern", 1.105f, 1.495f, Vector3.one, -.03f), new Color(0.388f, 0.737f, 0.627f)),
            new("Violet sporebrush", new("shrub_2", 1.020f, 1.380f, Vector3.one, -.03f), new Color(0.710f, 0.537f, 0.792f)),
            new("Amber cap bed", new("Pickable_Mushroom_JotunPuffs", 1.870f, 2.530f, Vector3.one, -.03f), new Color(0.886f, 0.714f, 0.404f)),
            new("Blue lantern caps", new("Pickable_Mushroom_Magecap", 1.700f, 2.300f, Vector3.one, -.03f), new Color(0.463f, 0.729f, 0.800f)),
            new("Moss pillow stone", new("Rock_4", 0.272f, 0.368f, Vector3.one, -.03f), new Color(0.482f, 0.651f, 0.545f)),
            new("Young worldroot", new("YggaShoot1", 0.340f, 0.460f, Vector3.one, -.03f), new Color(0.580f, 0.737f, 0.635f)),
            new("Broad jade fern", new("Fiddleheadfern", 0.9f, 1.3f, new Vector3(1.6f, 0.65f, 1.3f), -.04f), new Color(0.42f, 0.76f, 0.58f), 0f, 1000f, 28f),
            new("Fungal stepping slab", new("Rock_4", 0.35f, 0.55f, new Vector3(1.7f, 0.4f, 1.2f), -.04f), new Color(0.6f, 0.63f, 0.5f), -1f, 1000f, 40f),
        },
        [UnderworldTerrainBiome.BlackwaterDeep] = new GroundCover[]
        {
            new("Brine fern", new("Fiddleheadfern", 0.680f, 0.920f, Vector3.one, -.03f), new Color(0.341f, 0.624f, 0.612f), 0f, 4f, 28f),
            new("Drowned bank brush", new("shrub_2", 0.680f, 0.920f, Vector3.one, -.03f), new Color(0.337f, 0.549f, 0.580f), 0f, 4f, 28f),
            new("Pearl bank caps", new("Pickable_Mushroom_blue", 1.275f, 1.725f, Vector3.one, -.03f), new Color(0.671f, 0.855f, 0.847f), 0f, 4f, 28f),
            new("Wet bank stone", new("Rock_4", 0.340f, 0.460f, Vector3.one, -.03f), new Color(0.424f, 0.529f, 0.573f), -25f, 6f, 48f),
            new("Fingerstone rubble", new("RockFingerBroken", 0.128f, 0.172f, Vector3.one, -.03f), new Color(0.549f, 0.600f, 0.655f), -25f, 6f, 48f),
            new("Drowned rootlet", new("YggdrasilRoot", 0.153f, 0.207f, Vector3.one, -.03f), new Color(0.388f, 0.549f, 0.553f), -6f, 3f, 32f),
            new("Blackwater root fan", new("root08", 0.18f, 0.28f, new Vector3(1.7f, 0.65f, 1.2f), -.04f), new Color(0.42f, 0.63f, 0.64f), -5f, 2f, 30f),
            new("Lakebed stone shelf", new("Rock_4", 0.45f, 0.7f, new Vector3(1.5f, 0.35f, 1.2f), -.04f), new Color(0.42f, 0.53f, 0.62f), -30f, -0.2f, 45f),
        },
        [UnderworldTerrainBiome.SulfurousWastes] = new GroundCover[]
        {
            new("Copper ashbrush", new("AshlandsBush1", 0.680f, 0.920f, Vector3.one, -.03f), new Color(0.773f, 0.545f, 0.357f)),
            new("Sulfur scrub", new("AshlandsBush2", 0.510f, 0.690f, Vector3.one, -.03f), new Color(0.831f, 0.749f, 0.408f)),
            new("Vent fringe fern", new("Fiddleheadfern", 0.468f, 0.632f, Vector3.one, -.03f), new Color(0.667f, 0.635f, 0.416f)),
            new("Obsidian cobble", new("Ashlands_rock1", 0.153f, 0.207f, Vector3.one, -.03f), new Color(0.420f, 0.400f, 0.443f)),
            new("Sulfur stone", new("Rock_4", 0.272f, 0.368f, Vector3.one, -.03f), new Color(0.753f, 0.647f, 0.325f)),
            new("Charred sapling", new("AshlandsTree6_big", 0.255f, 0.345f, Vector3.one, -.03f), new Color(0.573f, 0.490f, 0.459f)),
            new("Cinder thorn fan", new("AshlandsBush2", 0.6f, 0.85f, new Vector3(1.5f, 0.7f, 1.1f), -.04f), new Color(0.72f, 0.41f, 0.31f), 0f, 1000f, 25f),
            new("Basalt stepping slab", new("Ashlands_rock2", 0.2f, 0.35f, new Vector3(1.8f, 0.45f, 1.2f), -.04f), new Color(0.43f, 0.39f, 0.47f), -1f, 1000f, 45f),
        },
        [UnderworldTerrainBiome.FrozenCaverns] = new GroundCover[]
        {
            new("Silver shelter fern", new("Fiddleheadfern", 0.552f, 0.747f, Vector3.one, -.03f), new Color(0.690f, 0.839f, 0.855f)),
            new("Rime brush", new("shrub_2_heath", 0.595f, 0.805f, Vector3.one, -.03f), new Color(0.663f, 0.749f, 0.863f)),
            new("Iceblue caps", new("Pickable_Mushroom_blue", 1.275f, 1.725f, Vector3.one, -.03f), new Color(0.588f, 0.796f, 0.906f)),
            new("Glacial pebble", new("rock3_mountain_1", 0.153f, 0.207f, Vector3.one, -.03f), new Color(0.631f, 0.729f, 0.800f)),
            new("Rime crystal sprig", new("BlackIceShard_01", 0.383f, 0.517f, Vector3.one, -.03f), new Color(0.522f, 0.718f, 0.839f)),
            new("Sheltered rootlet", new("YggaShoot2", 0.212f, 0.287f, Vector3.one, -.03f), new Color(0.608f, 0.675f, 0.749f)),
            new("Low rime fern", new("Fiddleheadfern", 0.6f, 0.9f, new Vector3(1.45f, 0.55f, 1.2f), -.04f), new Color(0.7f, 0.85f, 0.89f), 0f, 1000f, 25f),
            new("Ice scree shelf", new("BlackIceShard_01", 0.4f, 0.7f, new Vector3(1.65f, 0.38f, 1.1f), -.04f), new Color(0.54f, 0.7f, 0.83f), -2f, 1000f, 48f),
        },
        [UnderworldTerrainBiome.FractureZones] = new GroundCover[]
        {
            new("Amethyst fault fern", new("Fiddleheadfern", 0.680f, 0.920f, Vector3.one, -.03f), new Color(0.714f, 0.596f, 0.812f)),
            new("Ridge brush", new("shrub_2_heath", 0.552f, 0.747f, Vector3.one, -.03f), new Color(0.635f, 0.616f, 0.733f)),
            new("Crevice caps", new("Pickable_Mushroom_Magecap", 1.190f, 1.610f, Vector3.one, -.03f), new Color(0.702f, 0.588f, 0.859f)),
            new("Fault scree", new("RockFingerBroken", 0.170f, 0.230f, Vector3.one, -.03f), new Color(0.647f, 0.580f, 0.682f)),
            new("Quartz cobble", new("Rock_4", 0.255f, 0.345f, Vector3.one, -.03f), new Color(0.761f, 0.710f, 0.788f)),
            new("Split ridge sapling", new("YggaShoot3", 0.238f, 0.322f, Vector3.one, -.03f), new Color(0.580f, 0.502f, 0.647f)),
            new("Fault fan scrub", new("shrub_2_heath", 0.75f, 1.05f, new Vector3(1.7f, 0.6f, 1.05f), -.04f), new Color(0.65f, 0.49f, 0.73f), 0f, 1000f, 28f),
            new("Fractured shale plate", new("RockFingerBroken", 0.25f, 0.4f, new Vector3(1.65f, 0.4f, 1.1f), -.04f), new Color(0.57f, 0.5f, 0.67f), -2f, 1000f, 50f),
        },
        [UnderworldTerrainBiome.GreatDecay] = new GroundCover[]
        {
            new("Sourgreen fern", new("Fiddleheadfern", 1.275f, 1.725f, Vector3.one, -.03f), new Color(0.655f, 0.725f, 0.404f)),
            new("Wine rotbrush", new("shrub_2", 1.105f, 1.495f, Vector3.one, -.03f), new Color(0.710f, 0.506f, 0.580f)),
            new("Ochre decay caps", new("Pickable_Mushroom_JotunPuffs", 1.870f, 2.530f, Vector3.one, -.03f), new Color(0.800f, 0.675f, 0.392f)),
            new("Marrow pale caps", new("Pickable_Mushroom_Magecap", 1.530f, 2.070f, Vector3.one, -.03f), new Color(0.831f, 0.784f, 0.631f)),
            new("Rotroot tangle", new("root07", 0.255f, 0.345f, Vector3.one, -.03f), new Color(0.565f, 0.604f, 0.416f)),
            new("Mossgrave stone", new("Rock_4", 0.297f, 0.402f, Vector3.one, -.03f), new Color(0.604f, 0.616f, 0.463f)),
            new("Rotroot fern fan", new("Fiddleheadfern", 1.1f, 1.6f, new Vector3(1.5f, 0.75f, 1.3f), -.04f), new Color(0.69f, 0.73f, 0.34f), 0f, 1000f, 30f),
            new("Carrion root mat", new("root11", 0.25f, 0.4f, new Vector3(1.65f, 0.45f, 1.3f), -.04f), new Color(0.62f, 0.52f, 0.36f), -1f, 1000f, 35f),
        },
    };

    internal static GroundCover SelectCover(UnderworldTerrainBiome biome, int variant) =>
        Cover[biome][(variant & int.MaxValue) % Cover[biome].Length];

    internal static int CoverCount(UnderworldTerrainBiome biome) => Cover[biome].Length;

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
        // same Valheim donor do not retain an identical outline. Vertical variation stays restrained.
        // Normalize the jitter volume so deformation changes proportion rather than accidentally
        // changing the donor's visual mass a second time on top of the explicit mass tier.
        var massRoll = Hash01(variant, 0x1C7);
        var mass = massRoll < .16f ? 1.28f : massRoll < .70f ? 1f : .72f;
        var scalar = Mathf.Lerp(donor.MinScale, donor.MaxScale, Hash01(variant, 0x2D3)) * mass;
        var xJitter = Mathf.Lerp(.92f, 1.08f, Hash01(variant, 0x51B));
        var yJitter = Mathf.Lerp(.96f, 1.04f, Hash01(variant, 0x63D));
        var zJitter = Mathf.Lerp(.92f, 1.08f, Hash01(variant, 0x7A9));
        var jitterVolume = xJitter * yJitter * zJitter;
        var correction = jitterVolume > .0001f ? Mathf.Pow(1f / jitterVolume, 1f / 3f) : 1f;
        var silhouette = new Vector3(xJitter * correction, yJitter * correction, zJitter * correction);
        return Vector3.Scale(donor.Shape * scalar, silhouette);
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
