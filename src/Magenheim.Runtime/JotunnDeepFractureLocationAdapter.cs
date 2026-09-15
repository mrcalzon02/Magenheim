using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.DeepFractures;

namespace Magenheim.Runtime;

internal sealed class DeepFractureInteriorBinding
{
    internal DeepFractureInteriorBinding(bool hasInterior, float interiorRadius, string interiorEnvironment)
    {
        HasInterior = hasInterior;
        InteriorRadius = interiorRadius;
        InteriorEnvironment = interiorEnvironment;
    }

    internal bool HasInterior { get; }
    internal float InteriorRadius { get; }
    internal string InteriorEnvironment { get; }

    internal void Validate()
    {
        if (!HasInterior)
            throw new InvalidOperationException("Deep Fracture surface locations may not be registered until a real interior binding is attached.");
        if (float.IsNaN(InteriorRadius) || float.IsInfinity(InteriorRadius) || InteriorRadius <= 0f)
            throw new InvalidOperationException("Deep Fracture interior radius must be finite and greater than zero.");
        if (string.IsNullOrWhiteSpace(InteriorEnvironment))
            throw new InvalidOperationException("Deep Fracture interior binding requires an environment identity.");
    }
}

internal interface IDeepFractureInteriorBinder
{
    DeepFractureInteriorBinding AttachInterior(UnityEngine.GameObject locationContainer);
}

internal static class JotunnDeepFractureLocationAdapter
{
    internal static IReadOnlyList<ObservedDeepFractureLocation> ObserveDesiredHostIdentities(
        IEnumerable<DeepFractureLocationDefinition> desired)
    {
        if (desired is null)
            throw new ArgumentNullException(nameof(desired));

        var observed = new List<ObservedDeepFractureLocation>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var location in desired)
        {
            if (location is null || string.IsNullOrWhiteSpace(location.PrefabName) || !seen.Add(location.PrefabName))
                continue;

            var existing = ZoneManager.Instance.GetZoneLocation(location.PrefabName);
            var isJotunnCustom = CustomLocation.IsCustomLocation(location.PrefabName);
            if (existing is not null || isJotunnCustom)
            {
                observed.Add(new ObservedDeepFractureLocation(
                    location.PrefabName,
                    isJotunnCustom
                        ? "jotunn-custom-location"
                        : "host-vanilla-or-foreign-location"));
            }
        }

        return observed.AsReadOnly();
    }

    internal static LocationConfig BuildLocationConfig(
        DeepFractureLocationDefinition definition,
        DeepFractureInteriorBinding interior)
    {
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));
        if (interior is null)
            throw new ArgumentNullException(nameof(interior));

        definition.Validate();
        interior.Validate();

        var runtimeBiomes = definition.Biomes.Select(JotunnWorldgenAdapter.MapBiome).ToArray();
        if (runtimeBiomes.Length == 0)
            throw new InvalidOperationException("Deep Fracture location definition resolved to no runtime biomes.");

        return new LocationConfig
        {
            Biome = ZoneManager.AnyBiomeOf(runtimeBiomes),
            BiomeArea = JotunnWorldgenAdapter.MapArea(definition.BiomeArea),
            Quantity = definition.Quantity,
            Priotized = definition.Prioritized,
            ExteriorRadius = ToRuntimeFloat(definition.ExteriorRadius, nameof(definition.ExteriorRadius)),
            MinAltitude = ToRuntimeFloat(definition.MinAltitude, nameof(definition.MinAltitude)),
            MinTerrainDelta = 0f,
            MaxTerrainDelta = ToRuntimeFloat(definition.MaxTerrainDelta, nameof(definition.MaxTerrainDelta)),
            MinDistanceFromSimilar = ToRuntimeFloat(definition.MinDistanceFromSimilar, nameof(definition.MinDistanceFromSimilar)),
            Group = definition.Group,
            ClearArea = definition.ClearArea,
            RandomRotation = definition.RandomRotation,
            HasInterior = interior.HasInterior,
            InteriorRadius = interior.InteriorRadius,
            InteriorEnvironment = interior.InteriorEnvironment,
        };
    }

    private static float ToRuntimeFloat(double value, string field)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < -float.MaxValue || value > float.MaxValue)
            throw new InvalidOperationException($"Deep Fracture location field '{field}' cannot be represented by Jotunn's float API.");
        return (float)value;
    }
}
