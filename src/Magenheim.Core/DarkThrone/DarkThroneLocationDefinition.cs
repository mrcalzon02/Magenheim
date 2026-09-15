using System;
using Magenheim.Core.Worldgen;

namespace Magenheim.Core.DarkThrone;

public sealed record DarkThroneLocationDefinition(
    string RegistrationKey,
    string PrefabName,
    string Group,
    string Biome,
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
            throw new InvalidOperationException("Dark Throne registration key must use the 'magenheim.location.' namespace.");
        if (string.IsNullOrWhiteSpace(PrefabName) || !PrefabName.StartsWith("Magenheim_", StringComparison.Ordinal))
            throw new InvalidOperationException("Dark Throne prefab must use a Magenheim-owned identity.");
        if (string.IsNullOrWhiteSpace(Group) || !Group.StartsWith("magenheim.", StringComparison.Ordinal))
            throw new InvalidOperationException("Dark Throne group must use a Magenheim-owned identity.");
        if (!string.Equals(Biome, "Mistlands", StringComparison.Ordinal))
            throw new InvalidOperationException("The Dark Throne is a Mistlands-only location.");
        var area = SpawnAreaValidator.Normalize(BiomeArea);
        if (!area.IsValid || area.Area != BiomeArea)
            throw new InvalidOperationException("Dark Throne biome area must be normalized.");
        if (Quantity != 1)
            throw new InvalidOperationException("The Dark Throne location definition must remain unique per generated world.");
        ValidatePositive(ExteriorRadius, nameof(ExteriorRadius));
        ValidateFinite(MinAltitude, nameof(MinAltitude));
        ValidateNonNegative(MaxTerrainDelta, nameof(MaxTerrainDelta));
        ValidateNonNegative(MinDistanceFromSimilar, nameof(MinDistanceFromSimilar));
    }

    private static void ValidateFinite(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException(name + " must be finite.");
    }

    private static void ValidatePositive(double value, string name)
    {
        ValidateFinite(value, name);
        if (value <= 0d) throw new InvalidOperationException(name + " must be greater than zero.");
    }

    private static void ValidateNonNegative(double value, string name)
    {
        ValidateFinite(value, name);
        if (value < 0d) throw new InvalidOperationException(name + " cannot be negative.");
    }
}

public static class DarkThroneLocationCatalog
{
    public static DarkThroneLocationDefinition DarkThrone { get; } = new(
        RegistrationKey: "magenheim.location.dark_throne",
        PrefabName: "Magenheim_DarkThrone",
        Group: "magenheim.dark_throne",
        Biome: "Mistlands",
        BiomeArea: SpawnArea.All,
        Quantity: 1,
        ExteriorRadius: 44d,
        MinAltitude: 2d,
        MaxTerrainDelta: 18d,
        MinDistanceFromSimilar: 8192d,
        Prioritized: true,
        ClearArea: true,
        RandomRotation: true);
}
