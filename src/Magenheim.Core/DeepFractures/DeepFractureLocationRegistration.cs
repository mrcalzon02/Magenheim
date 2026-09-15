using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.Worldgen;

namespace Magenheim.Core.DeepFractures;

public sealed record DeepFractureLocationDefinition(
    string RegistrationKey,
    string PrefabName,
    string Group,
    IReadOnlyList<string> Biomes,
    SpawnArea BiomeArea,
    int Quantity,
    double ExteriorRadius,
    double MinAltitude,
    double MaxTerrainDelta,
    double MinDistanceFromSimilar,
    bool Prioritized,
    bool ClearArea,
    bool RandomRotation)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RegistrationKey) || !RegistrationKey.StartsWith("magenheim.location.", StringComparison.Ordinal))
            throw new InvalidOperationException("Deep Fracture location registration key must use the 'magenheim.location.' namespace.");
        if (string.IsNullOrWhiteSpace(PrefabName) || !PrefabName.StartsWith("Magenheim_", StringComparison.Ordinal))
            throw new InvalidOperationException("Deep Fracture location prefab must use a Magenheim-owned prefab identity.");
        if (string.IsNullOrWhiteSpace(Group) || !Group.StartsWith("magenheim.", StringComparison.Ordinal))
            throw new InvalidOperationException("Deep Fracture location group must use a Magenheim-owned identity.");
        if (Biomes is null || Biomes.Count == 0)
            throw new InvalidOperationException("Deep Fracture location requires at least one biome.");
        if (Biomes.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Deep Fracture location biome names cannot be empty.");
        if (Biomes.Distinct(StringComparer.Ordinal).Count() != Biomes.Count)
            throw new InvalidOperationException("Deep Fracture location biome names must be unique.");

        var area = SpawnAreaValidator.Normalize(BiomeArea);
        if (!area.IsValid || area.Area != BiomeArea)
            throw new InvalidOperationException($"Deep Fracture location biome area '{BiomeArea}' is not a valid normalized area.");

        if (Quantity < 1 || Quantity > 128)
            throw new InvalidOperationException("Deep Fracture location quantity must be between one and 128.");
        ValidateFiniteNonNegative(ExteriorRadius, nameof(ExteriorRadius), requirePositive: true);
        ValidateFinite(MinAltitude, nameof(MinAltitude));
        ValidateFiniteNonNegative(MaxTerrainDelta, nameof(MaxTerrainDelta), requirePositive: false);
        ValidateFiniteNonNegative(MinDistanceFromSimilar, nameof(MinDistanceFromSimilar), requirePositive: false);
    }

    private static void ValidateFinite(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new InvalidOperationException($"{name} must be finite.");
    }

    private static void ValidateFiniteNonNegative(double value, string name, bool requirePositive)
    {
        ValidateFinite(value, name);
        if (value < 0d || (requirePositive && value <= 0d))
            throw new InvalidOperationException(requirePositive
                ? $"{name} must be greater than zero."
                : $"{name} cannot be negative.");
    }
}

public static class DeepFractureLocationCatalog
{
    private static readonly string[] LandBiomes =
    {
        "Meadows",
        "BlackForest",
        "Swamp",
        "Mountain",
        "Plains",
        "Mistlands",
        "Ashlands",
        "DeepNorth",
    };

    public static DeepFractureLocationDefinition SurfaceFractureEntrance { get; } = new(
        RegistrationKey: "magenheim.location.deep_fracture_entrance",
        PrefabName: "Magenheim_DeepFracture_Entrance",
        Group: "magenheim.deep_fracture",
        Biomes: Array.AsReadOnly(LandBiomes),
        BiomeArea: SpawnArea.All,
        Quantity: 24,
        ExteriorRadius: 12d,
        MinAltitude: 1d,
        MaxTerrainDelta: 28d,
        MinDistanceFromSimilar: 1024d,
        Prioritized: false,
        ClearArea: false,
        RandomRotation: true);
}

public sealed record ObservedDeepFractureLocation(
    string PrefabName,
    string Source);

public enum DeepFractureLocationRegistrationAction
{
    Add,
    Skip,
    Error
}

public sealed record DeepFractureLocationRegistrationEntry(
    DeepFractureLocationDefinition Desired,
    DeepFractureLocationRegistrationAction Action,
    string Diagnostic);

public sealed record DeepFractureLocationRegistrationPlan(
    IReadOnlyList<DeepFractureLocationRegistrationEntry> Entries)
{
    public bool HasErrors => Entries.Any(entry => entry.Action == DeepFractureLocationRegistrationAction.Error);
    public IEnumerable<DeepFractureLocationRegistrationEntry> Additions => Entries.Where(entry => entry.Action == DeepFractureLocationRegistrationAction.Add);
}

public static class DeepFractureLocationRegistrationPlanner
{
    public static DeepFractureLocationRegistrationPlan Build(
        IEnumerable<DeepFractureLocationDefinition> desired,
        IEnumerable<ObservedDeepFractureLocation> observed)
    {
        if (desired is null)
            throw new ArgumentNullException(nameof(desired));
        if (observed is null)
            throw new ArgumentNullException(nameof(observed));

        var observedSnapshot = observed.Where(entry => entry is not null).ToArray();
        var entries = new List<DeepFractureLocationRegistrationEntry>();
        var desiredPrefabs = new HashSet<string>(StringComparer.Ordinal);
        var desiredKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var location in desired)
        {
            if (location is null)
            {
                entries.Add(new DeepFractureLocationRegistrationEntry(
                    DeepFractureLocationCatalog.SurfaceFractureEntrance,
                    DeepFractureLocationRegistrationAction.Error,
                    "Desired Deep Fracture location cannot be null."));
                continue;
            }

            try
            {
                location.Validate();
            }
            catch (InvalidOperationException exception)
            {
                entries.Add(new DeepFractureLocationRegistrationEntry(
                    location,
                    DeepFractureLocationRegistrationAction.Error,
                    exception.Message));
                continue;
            }

            if (!desiredKeys.Add(location.RegistrationKey))
            {
                entries.Add(new DeepFractureLocationRegistrationEntry(
                    location,
                    DeepFractureLocationRegistrationAction.Error,
                    $"Duplicate desired Deep Fracture registration key '{location.RegistrationKey}'."));
                continue;
            }

            if (!desiredPrefabs.Add(location.PrefabName))
            {
                entries.Add(new DeepFractureLocationRegistrationEntry(
                    location,
                    DeepFractureLocationRegistrationAction.Error,
                    $"Duplicate desired Deep Fracture prefab identity '{location.PrefabName}'."));
                continue;
            }

            var collision = observedSnapshot.FirstOrDefault(entry =>
                string.Equals(entry.PrefabName, location.PrefabName, StringComparison.Ordinal));
            if (collision is not null)
            {
                entries.Add(new DeepFractureLocationRegistrationEntry(
                    location,
                    DeepFractureLocationRegistrationAction.Skip,
                    $"Host location identity '{location.PrefabName}' is already occupied by '{collision.Source}'. Existing content is left untouched."));
                continue;
            }

            entries.Add(new DeepFractureLocationRegistrationEntry(
                location,
                DeepFractureLocationRegistrationAction.Add,
                "Magenheim-owned location identity is unoccupied and may be added non-destructively."));
        }

        return new DeepFractureLocationRegistrationPlan(entries.AsReadOnly());
    }
}
