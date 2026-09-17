using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Magenheim.Core.Underworld;

public sealed record UnderworldFloraDefinition(
    string Id, string PrefabName, string BiomeId,
    int MinHeightMeters, int MaxHeightMeters, int MaxSlopeDegrees,
    bool RequiresRockFace);

/// <summary>Immutable terrain eligibility data. The shared cavern skybox is not a placement surface.</summary>
public sealed class UnderworldFloraDefinitionSet
{
    public int SchemaVersion { get; }
    public IReadOnlyList<UnderworldFloraDefinition> Species { get; }
    public string Fingerprint { get; }

    public UnderworldFloraDefinitionSet(int schemaVersion, IEnumerable<UnderworldFloraDefinition> species)
    {
        if (schemaVersion != 1) throw new InvalidOperationException("Unsupported flora schema.");
        if (species is null) throw new ArgumentNullException(nameof(species));
        var frozen = species.ToArray();
        if (frozen.Length == 0) throw new InvalidOperationException("Flora requires species.");
        foreach (var value in frozen)
        {
            if (value is null) throw new InvalidOperationException("Null flora species.");
            RequireIdentity(value.Id, "magenheim.underworld.flora.");
            RequireIdentity(value.PrefabName, "Magenheim_Underworld_");
            RequireIdentity(value.BiomeId, "magenheim.underworld.biome.");
            if (value.MinHeightMeters <= 0 || value.MaxHeightMeters < value.MinHeightMeters ||
                value.MaxSlopeDegrees < 0 || value.MaxSlopeDegrees > 90)
                throw new InvalidOperationException("Invalid flora dimensions or slope.");
        }
        if (frozen.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != frozen.Length ||
            frozen.Select(x => x.PrefabName).Distinct(StringComparer.Ordinal).Count() != frozen.Length)
            throw new InvalidOperationException("Duplicate flora identity.");
        SchemaVersion = schemaVersion;
        Species = Array.AsReadOnly(frozen);
        var text = new StringBuilder("underworld-flora-schema=1\n");
        foreach (var value in frozen.OrderBy(x => x.Id, StringComparer.Ordinal))
            text.Append(value.Id).Append('|').Append(value.PrefabName).Append('|').Append(value.BiomeId)
                .Append('|').Append(value.MinHeightMeters.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(value.MaxHeightMeters.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(value.MaxSlopeDegrees.ToString(CultureInfo.InvariantCulture))
                .Append('|').Append(value.RequiresRockFace ? "1" : "0").Append('\n');
        using var hash = SHA256.Create();
        Fingerprint = string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))
            .Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static void RequireIdentity(string value, string prefix)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(prefix, StringComparison.Ordinal) ||
            value.Length == prefix.Length || value.Any(c => !(char.IsLetterOrDigit(c) || c == '.' || c == '_')))
            throw new InvalidOperationException("Invalid flora identity.");
    }
}

public sealed record UnderworldFloraTerrainSample(
    UnderworldLayer Layer, string BiomeId, bool HasGround, double HeightAboveWaterMeters,
    double SlopeDegrees, bool HasExposedRockFace, bool IsObstructed);

public static class UnderworldFloraPlacement
{
    // The adapter supplies a terrain sample in realm-local coordinates. No raycast to a roof,
    // per-biome skybox, or surface-biome fallback is part of this authority.
    public static bool CanPlace(UnderworldFloraDefinitionSet definitions, string speciesId,
        UnderworldFloraTerrainSample sample)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        if (sample is null) return false;
        var species = definitions.Species.SingleOrDefault(x => x.Id == speciesId);
        return species is not null && sample.Layer == UnderworldLayer.Underworld &&
            string.Equals(sample.BiomeId, species.BiomeId, StringComparison.Ordinal) &&
            sample.HasGround && !sample.IsObstructed &&
            !double.IsNaN(sample.HeightAboveWaterMeters) && !double.IsInfinity(sample.HeightAboveWaterMeters) &&
            sample.HeightAboveWaterMeters > 0 &&
            !double.IsNaN(sample.SlopeDegrees) && !double.IsInfinity(sample.SlopeDegrees) &&
            sample.SlopeDegrees >= 0 && sample.SlopeDegrees <= species.MaxSlopeDegrees &&
            (!species.RequiresRockFace || sample.HasExposedRockFace);
    }
}
