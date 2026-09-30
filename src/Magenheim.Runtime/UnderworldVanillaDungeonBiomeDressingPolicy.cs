using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Converts enlarged vanilla donor rooms from bare scale/population swaps into recognizably
/// Underworld-native spaces without replacing their donor room grammar. Dressing is visual-only
/// unless a deliberately bounded biome hazard volume is attached.
/// </summary>
internal static class UnderworldVanillaDungeonBiomeDressingPolicy
{
    internal readonly record struct Stats(
        int ArchitectureMaterials,
        int MajorProps,
        int GroundProps,
        int LocalLights,
        int AtmosphereVolumes,
        int ThermalVolumes)
    {
        internal static Stats Empty => new(0, 0, 0, 0, 0, 0);

        public static Stats operator +(Stats left, Stats right) =>
            new(
                left.ArchitectureMaterials + right.ArchitectureMaterials,
                left.MajorProps + right.MajorProps,
                left.GroundProps + right.GroundProps,
                left.LocalLights + right.LocalLights,
                left.AtmosphereVolumes + right.AtmosphereVolumes,
                left.ThermalVolumes + right.ThermalVolumes);
    }

    private readonly struct Palette
    {
        internal Palette(
            Color architectureTint,
            Color lightColor,
            float smoothnessDelta,
            float lightIntensity,
            float lightRangeScale,
            UnderworldAtmosphereEvent atmosphereEvent,
            double atmosphereIntensity,
            double hazardFloor)
        {
            ArchitectureTint = architectureTint;
            LightColor = lightColor;
            SmoothnessDelta = smoothnessDelta;
            LightIntensity = lightIntensity;
            LightRangeScale = lightRangeScale;
            AtmosphereEvent = atmosphereEvent;
            AtmosphereIntensity = atmosphereIntensity;
            HazardFloor = hazardFloor;
        }

        internal Color ArchitectureTint { get; }
        internal Color LightColor { get; }
        internal float SmoothnessDelta { get; }
        internal float LightIntensity { get; }
        internal float LightRangeScale { get; }
        internal UnderworldAtmosphereEvent AtmosphereEvent { get; }
        internal double AtmosphereIntensity { get; }
        internal double HazardFloor { get; }
    }

    internal static Stats Apply(
        GameObject roomObject,
        UnderworldVanillaDungeonReuseDefinition profile,
        string donorRoomName,
        int donorRoomIndex)
    {
        if (!roomObject) throw new ArgumentNullException(nameof(roomObject));
        if (profile is null) throw new ArgumentNullException(nameof(profile));
        if (string.IsNullOrWhiteSpace(donorRoomName))
            throw new ArgumentException("Donor room identity is required.", nameof(donorRoomName));

        var room = roomObject.GetComponent<Room>()
            ?? throw new InvalidOperationException("DDE biome dressing received a room clone without Room.");

        var palette = PaletteFor(profile.Biome);
        var band = UnderworldVanillaDungeonRoomPolicy.RiskFor(
            room, profile.DungeonId, donorRoomName, donorRoomIndex);

        var scaleRoot = FindDirectChild(roomObject.transform, "Magenheim_DDE_ScaleRoot")
            ?? throw new InvalidOperationException(
                "DDE biome dressing requires the connection-safe scale root.");

        var architectureMaterials = ApplyArchitecturePalette(
            scaleRoot,
            palette,
            profile.Biome,
            donorRoomIndex);

        var bounds = StructuralBounds(roomObject.transform, scaleRoot, room.m_size);
        var dressingRoot = new GameObject("Magenheim_DDE_BiomeDressing");
        dressingRoot.transform.SetParent(roomObject.transform, false);

        var majorProps = AddMajorProps(
            dressingRoot.transform,
            room,
            profile,
            donorRoomName,
            donorRoomIndex,
            band,
            bounds);

        var groundProps = AddGroundProps(
            dressingRoot.transform,
            room,
            profile,
            donorRoomName,
            donorRoomIndex,
            band,
            bounds);

        var lights = AddLocalLight(
            dressingRoot.transform,
            room,
            profile,
            donorRoomName,
            donorRoomIndex,
            band,
            bounds,
            palette);

        var atmosphere = AddAtmosphere(
            dressingRoot.transform,
            room,
            profile,
            donorRoomName,
            donorRoomIndex,
            band,
            bounds,
            palette);

        var thermal = AddSulfurThermalPocket(
            dressingRoot.transform,
            room,
            profile,
            donorRoomName,
            donorRoomIndex,
            band,
            bounds);

        return new Stats(
            architectureMaterials,
            majorProps,
            groundProps,
            lights,
            atmosphere,
            thermal);
    }

    private static int ApplyArchitecturePalette(
        Transform scaleRoot,
        Palette palette,
        UnderworldTerrainBiome biome,
        int roomIndex)
    {
        var changed = 0;
        var renderers = scaleRoot.GetComponentsInChildren<Renderer>(true);
        for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            var renderer = renderers[rendererIndex];
            if (!renderer || IsGameplayRenderer(renderer.transform, scaleRoot))
                continue;

            var originals = renderer.sharedMaterials;
            if (originals is null || originals.Length == 0)
                continue;

            var replacements = new Material[originals.Length];
            var replacedAny = false;
            for (var materialIndex = 0; materialIndex < originals.Length; materialIndex++)
            {
                var original = originals[materialIndex];
                if (!original)
                {
                    replacements[materialIndex] = original!;
                    continue;
                }

                var material = new Material(original)
                {
                    name = "Magenheim_DDE_" + biome + "_" + roomIndex + "_" +
                           rendererIndex + "_" + materialIndex + "_" + original.name,
                };

                var touched = false;
                if (material.HasProperty("_Color"))
                {
                    material.SetColor(
                        "_Color",
                        Multiply(material.GetColor("_Color"), palette.ArchitectureTint));
                    touched = true;
                }
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor(
                        "_BaseColor",
                        Multiply(material.GetColor("_BaseColor"), palette.ArchitectureTint));
                    touched = true;
                }
                if (material.HasProperty("_Glossiness"))
                {
                    material.SetFloat(
                        "_Glossiness",
                        Mathf.Clamp01(material.GetFloat("_Glossiness") + palette.SmoothnessDelta));
                    touched = true;
                }
                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat(
                        "_Smoothness",
                        Mathf.Clamp01(material.GetFloat("_Smoothness") + palette.SmoothnessDelta));
                    touched = true;
                }

                replacements[materialIndex] = material;
                if (touched)
                {
                    changed++;
                    replacedAny = true;
                }
            }

            if (replacedAny)
                renderer.sharedMaterials = replacements;
        }

        return changed;
    }

    private static int AddMajorProps(
        Transform parent,
        Room room,
        UnderworldVanillaDungeonReuseDefinition profile,
        string roomName,
        int roomIndex,
        UnderworldVanillaDungeonRiskBand band,
        Bounds bounds)
    {
        if (room.m_entrance) return 0;

        var minHorizontal = Mathf.Min(bounds.size.x, bounds.size.z);
        var desired = minHorizontal >= 18f ? 2 : minHorizontal >= 10f ? 1 : 0;
        if ((int)band >= (int)UnderworldVanillaDungeonRiskBand.Deep)
            desired++;
        desired = Mathf.Clamp(desired, 0, 3);

        var added = 0;
        for (var index = 0; index < desired; index++)
        {
            if (!UnderworldVanillaDungeonRoomPolicy.Roll(
                    .82d,
                    profile.DungeonId,
                    roomName,
                    roomIndex,
                    "biome-major-prop",
                    index))
                continue;

            var seed = StableSeed(profile.DungeonId, roomName, roomIndex, "major", index);
            GameObject visual;
            try
            {
                visual = UnderworldDonorVisualFactory.Create(
                    profile.Biome,
                    seed,
                    "Magenheim_DDE_Major_" + index);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            StripGameplayCollision(visual);
            visual.transform.SetParent(parent, false);

            var position = PerimeterPosition(bounds, seed, index, desired, .58f);
            position.y = bounds.min.y + visual.transform.localPosition.y;
            if (NearConnection(room, position, 2.8f))
            {
                UnityEngine.Object.DestroyImmediate(visual);
                continue;
            }

            visual.transform.localPosition = position;
            visual.transform.localScale = Vector3.Scale(
                visual.transform.localScale,
                new Vector3(.62f, .72f, .62f));
            added++;
        }

        return added;
    }

    private static int AddGroundProps(
        Transform parent,
        Room room,
        UnderworldVanillaDungeonReuseDefinition profile,
        string roomName,
        int roomIndex,
        UnderworldVanillaDungeonRiskBand band,
        Bounds bounds)
    {
        var area = Mathf.Max(1f, bounds.size.x * bounds.size.z);
        var desired = Mathf.Clamp(Mathf.RoundToInt(area / 95f), 2, 6);
        if ((int)band >= (int)UnderworldVanillaDungeonRiskBand.Deep)
            desired = Mathf.Min(7, desired + 1);

        var added = 0;
        for (var index = 0; index < desired; index++)
        {
            var seed = StableSeed(profile.DungeonId, roomName, roomIndex, "cover", index);
            GameObject? visual;
            try
            {
                visual = UnderworldDonorVisualFactory.CreateCover(
                    profile.Biome,
                    seed,
                    "Magenheim_DDE_Cover_" + index,
                    heightAboveWater: profile.Biome == UnderworldTerrainBiome.BlackwaterDeep ? .4d : 1d,
                    slopeDegrees: 0d,
                    groundNormal: Vector3.up);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            if (!visual) continue;
            StripGameplayCollision(visual);
            visual.transform.SetParent(parent, false);

            var position = PerimeterPosition(bounds, seed, index, desired, .74f);
            position.y = bounds.min.y + visual.transform.localPosition.y;
            if (NearConnection(room, position, 2.1f))
            {
                UnityEngine.Object.DestroyImmediate(visual);
                continue;
            }

            visual.transform.localPosition = position;
            visual.transform.localScale = Vector3.Scale(
                visual.transform.localScale,
                new Vector3(.72f, .72f, .72f));
            added++;
        }

        return added;
    }

    private static int AddLocalLight(
        Transform parent,
        Room room,
        UnderworldVanillaDungeonReuseDefinition profile,
        string roomName,
        int roomIndex,
        UnderworldVanillaDungeonRiskBand band,
        Bounds bounds,
        Palette palette)
    {
        if (room.m_entrance) return 0;

        var probability = (int)band >= (int)UnderworldVanillaDungeonRiskBand.Deep ? .72d : .48d;
        if (!UnderworldVanillaDungeonRoomPolicy.Roll(
                probability,
                profile.DungeonId,
                roomName,
                roomIndex,
                "biome-local-light",
                0))
            return 0;

        var root = new GameObject("Magenheim_DDE_BiomeLight");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(
            bounds.center.x,
            bounds.min.y + Mathf.Clamp(bounds.size.y * .58f, 1.8f, 5.5f),
            bounds.center.z);

        var light = root.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = palette.LightColor;
        light.intensity = palette.LightIntensity;
        light.range = Mathf.Clamp(
            Mathf.Min(bounds.size.x, bounds.size.z) * palette.LightRangeScale,
            4.5f,
            11f);
        light.shadows = LightShadows.None;
        return 1;
    }

    private static int AddAtmosphere(
        Transform parent,
        Room room,
        UnderworldVanillaDungeonReuseDefinition profile,
        string roomName,
        int roomIndex,
        UnderworldVanillaDungeonRiskBand band,
        Bounds bounds,
        Palette palette)
    {
        if (room.m_entrance) return 0;

        var probability = (int)band >= (int)UnderworldVanillaDungeonRiskBand.Deep ? .86d : .62d;
        if (!UnderworldVanillaDungeonRoomPolicy.Roll(
                probability,
                profile.DungeonId,
                roomName,
                roomIndex,
                "biome-atmosphere",
                0))
            return 0;

        var risk = Mathf.Clamp01(((int)band + 1f) / 4f);
        var root = new GameObject("Magenheim_DDE_BiomeAtmosphere")
        {
            layer = room.gameObject.layer,
        };
        root.transform.SetParent(parent, false);
        root.transform.localPosition = bounds.center;

        var collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = Vector3.zero;
        collider.size = new Vector3(
            Mathf.Max(3f, bounds.size.x * .78f),
            Mathf.Max(2.6f, bounds.size.y * .72f),
            Mathf.Max(3f, bounds.size.z * .78f));

        var volume = root.AddComponent<UnderworldVanillaDungeonAtmosphereVolume>();
        volume.Bind(
            profile.Biome,
            palette.AtmosphereEvent,
            Math.Min(1d, palette.AtmosphereIntensity + risk * .16d),
            Math.Min(1d, palette.HazardFloor + risk * .12d));
        return 1;
    }

    private static int AddSulfurThermalPocket(
        Transform parent,
        Room room,
        UnderworldVanillaDungeonReuseDefinition profile,
        string roomName,
        int roomIndex,
        UnderworldVanillaDungeonRiskBand band,
        Bounds bounds)
    {
        if (profile.Biome != UnderworldTerrainBiome.SulfurousWastes ||
            room.m_entrance ||
            (int)band < (int)UnderworldVanillaDungeonRiskBand.Deep)
            return 0;

        if (!UnderworldVanillaDungeonRoomPolicy.Roll(
                .58d,
                profile.DungeonId,
                roomName,
                roomIndex,
                "biome-thermal-pocket",
                0))
            return 0;

        var root = new GameObject("Magenheim_DDE_ThermalPocket")
        {
            layer = room.gameObject.layer,
        };
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(
            bounds.center.x + bounds.extents.x * .20f,
            bounds.min.y + 1.2f,
            bounds.center.z - bounds.extents.z * .18f);

        var collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(
            Mathf.Max(3.5f, bounds.size.x * .32f),
            3.0f,
            Mathf.Max(4f, bounds.size.z * .46f));

        var hazard = root.AddComponent<UnderworldGeothermalHazardVolume>();
        hazard.Bind(
            UnderworldGeothermalHazard.VentField,
            .55f + (int)band * .12f,
            .1f);
        return 1;
    }

    private static bool IsGameplayRenderer(Transform value, Transform scaleRoot)
    {
        for (var current = value; current && current != scaleRoot; current = current.parent)
        {
            var gameObject = current.gameObject;
            if (gameObject.GetComponent<ZNetView>() ||
                gameObject.GetComponent<Character>() ||
                gameObject.GetComponent<ItemDrop>() ||
                gameObject.GetComponent<Container>() ||
                gameObject.GetComponent<Pickable>() ||
                gameObject.GetComponent<CreatureSpawner>() ||
                gameObject.GetComponent<SpawnArea>())
                return true;
        }
        return false;
    }

    private static Bounds StructuralBounds(
        Transform roomRoot,
        Transform scaleRoot,
        Vector3Int fallbackSize)
    {
        var found = false;
        var minimum = Vector3.zero;
        var maximum = Vector3.zero;

        foreach (var renderer in scaleRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer || IsGameplayRenderer(renderer.transform, scaleRoot))
                continue;

            if (!TryRendererBounds(renderer, out var localBounds))
                continue;

            var min = localBounds.min;
            var max = localBounds.max;
            for (var corner = 0; corner < 8; corner++)
            {
                var local = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);
                var world = renderer.transform.TransformPoint(local);
                var point = roomRoot.InverseTransformPoint(world);
                if (!found)
                {
                    minimum = point;
                    maximum = point;
                    found = true;
                }
                else
                {
                    minimum = Vector3.Min(minimum, point);
                    maximum = Vector3.Max(maximum, point);
                }
            }
        }

        if (found)
        {
            var result = new Bounds();
            result.SetMinMax(minimum, maximum);
            return result;
        }

        var fallback = new Bounds(
            new Vector3(0f, fallbackSize.y * .5f, 0f),
            new Vector3(
                Mathf.Max(2f, fallbackSize.x),
                Mathf.Max(2f, fallbackSize.y),
                Mathf.Max(2f, fallbackSize.z)));
        return fallback;
    }

    private static bool TryRendererBounds(Renderer renderer, out Bounds bounds)
    {
        var filter = renderer.GetComponent<MeshFilter>();
        if (filter && filter.sharedMesh)
        {
            bounds = filter.sharedMesh.bounds;
            return true;
        }

        var skinned = renderer as SkinnedMeshRenderer;
        if (skinned)
        {
            bounds = skinned.localBounds;
            return true;
        }

        bounds = default;
        return false;
    }

    private static Vector3 PerimeterPosition(
        Bounds bounds,
        int seed,
        int index,
        int total,
        float radiusFactor)
    {
        var phase = Hash01(seed, 0x4A31) * Mathf.PI * 2f;
        var angle = phase + (Mathf.PI * 2f * index / Mathf.Max(1, total));
        var x = bounds.center.x + Mathf.Cos(angle) * bounds.extents.x * radiusFactor;
        var z = bounds.center.z + Mathf.Sin(angle) * bounds.extents.z * radiusFactor;
        return new Vector3(x, bounds.min.y, z);
    }

    private static bool NearConnection(Room room, Vector3 localPosition, float distance)
    {
        foreach (var connection in room.GetConnections())
        {
            if (!connection) continue;
            var offset = connection.transform.localPosition - localPosition;
            offset.y = 0f;
            if (offset.sqrMagnitude < distance * distance)
                return true;
        }
        return false;
    }

    private static void StripGameplayCollision(GameObject visual)
    {
        foreach (var collider in visual.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
    }

    private static Transform? FindDirectChild(Transform parent, string name)
    {
        for (var index = 0; index < parent.childCount; index++)
        {
            var child = parent.GetChild(index);
            if (string.Equals(child.name, name, StringComparison.Ordinal))
                return child;
        }
        return null;
    }

    private static Palette PaletteFor(UnderworldTerrainBiome biome)
    {
        switch (biome)
        {
            case UnderworldTerrainBiome.FungalForest:
                return new Palette(
                    new Color(.72f, .88f, .74f, 1f),
                    new Color(.40f, .94f, .58f),
                    .03f,
                    .62f,
                    .43f,
                    UnderworldAtmosphereEvent.Sporefall,
                    .42d,
                    0d);

            case UnderworldTerrainBiome.BlackwaterDeep:
                return new Palette(
                    new Color(.62f, .73f, .78f, 1f),
                    new Color(.22f, .72f, .86f),
                    .14f,
                    .58f,
                    .40f,
                    UnderworldAtmosphereEvent.DeepFog,
                    .46d,
                    .10d);

            case UnderworldTerrainBiome.SulfurousWastes:
                return new Palette(
                    new Color(.78f, .64f, .48f, 1f),
                    new Color(1f, .40f, .10f),
                    -.06f,
                    .72f,
                    .42f,
                    UnderworldAtmosphereEvent.Ashfall,
                    .44d,
                    .16d);

            case UnderworldTerrainBiome.FrozenCaverns:
                return new Palette(
                    new Color(.82f, .91f, 1f, 1f),
                    new Color(.56f, .84f, 1f),
                    .18f,
                    .64f,
                    .44f,
                    UnderworldAtmosphereEvent.Whiteout,
                    .40d,
                    .14d);

            case UnderworldTerrainBiome.GreatDecay:
                return new Palette(
                    new Color(.67f, .63f, .45f, 1f),
                    new Color(.62f, .70f, .18f),
                    -.08f,
                    .52f,
                    .38f,
                    UnderworldAtmosphereEvent.BlackBloom,
                    .48d,
                    .18d);

            default:
                throw new InvalidOperationException(
                    "Ordinary donor dungeon dressing is undefined for " + biome + ".");
        }
    }

    private static Color Multiply(Color value, Color tint) =>
        new(
            Mathf.Clamp01(value.r * tint.r),
            Mathf.Clamp01(value.g * tint.g),
            Mathf.Clamp01(value.b * tint.b),
            value.a);

    private static int StableSeed(
        string dungeonId,
        string roomName,
        int roomIndex,
        string channel,
        int index)
    {
        unchecked
        {
            uint hash = 2166136261u;
            Append(ref hash, dungeonId);
            Append(ref hash, roomName);
            Append(ref hash, channel);
            hash ^= (uint)roomIndex;
            hash *= 16777619u;
            hash ^= (uint)index;
            hash *= 16777619u;
            return (int)hash;
        }
    }

    private static void Append(ref uint hash, string value)
    {
        foreach (var character in value)
        {
            hash ^= character;
            hash *= 16777619u;
        }
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

internal sealed class UnderworldVanillaDungeonAtmosphereVolume : MonoBehaviour
{
    [SerializeField] private UnderworldTerrainBiome _biome;
    [SerializeField] private UnderworldAtmosphereEvent _event;
    [SerializeField] private double _intensity01;
    [SerializeField] private double _hazardFloor01;
    private bool _bound;

    internal void Bind(
        UnderworldTerrainBiome biome,
        UnderworldAtmosphereEvent atmosphereEvent,
        double intensity01,
        double hazardFloor01)
    {
        if (!UnderworldAtmosphere.EventApplies(biome, atmosphereEvent))
            throw new InvalidOperationException(
                "Dungeon atmosphere event " + atmosphereEvent + " is invalid for " + biome + ".");

        _biome = biome;
        _event = atmosphereEvent;
        _intensity01 = Math.Max(0d, Math.Min(1d, intensity01));
        _hazardFloor01 = Math.Max(0d, Math.Min(1d, hazardFloor01));
        _bound = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_bound) return;
        var player = other.GetComponentInParent<Player>();
        if (!player || player.IsDead()) return;

        UnderworldLocalWeatherOverride.Refresh(
            player,
            _biome,
            _event,
            _intensity01,
            _hazardFloor01,
            .75f);
    }
}
