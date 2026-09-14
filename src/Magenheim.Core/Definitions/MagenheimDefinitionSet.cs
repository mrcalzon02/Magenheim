using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Magenheim.Core.Worldgen;

namespace Magenheim.Core.Definitions;

public sealed record ElementWeight(ElementalAlignment Element, double Weight);

public sealed record GeodeDefinition(
    string Id,
    string Biome,
    string PrefabName,
    SpawnArea Area,
    int GuaranteedCrystalCount,
    double SecondCrystalChance,
    double ThirdCrystalChance,
    IReadOnlyList<ElementWeight> ElementWeights)
{
    public GeodePlacementDefinition Placement { get; init; } = GeodePlacementDefinition.ConservativeMeadows;
}

public sealed record MagenheimDefinitionSet(
    int SchemaVersion,
    IReadOnlyList<RefinementRule> RefinementRules,
    IReadOnlyList<GeodeDefinition> Geodes,
    WorldgenCompatibilityPolicy WorldgenCompatibility,
    string Fingerprint);

public static class MagenheimDefinitionValidator
{
    public const int CurrentSchemaVersion = 3;

    private static readonly HashSet<string> SupportedBiomes = new(StringComparer.Ordinal)
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

    public static MagenheimDefinitionSet ValidateAndFreeze(
        int schemaVersion,
        IEnumerable<RefinementRule> refinementRules,
        IEnumerable<GeodeDefinition> geodes,
        WorldgenCompatibilityPolicy worldgenCompatibility)
    {
        if (schemaVersion != CurrentSchemaVersion)
            throw new InvalidOperationException($"Unsupported definition schema {schemaVersion}. Expected {CurrentSchemaVersion}.");
        if (refinementRules is null)
            throw new ArgumentNullException(nameof(refinementRules));
        if (geodes is null)
            throw new ArgumentNullException(nameof(geodes));
        if (worldgenCompatibility is null)
            throw new ArgumentNullException(nameof(worldgenCompatibility));

        var frozenCompatibility = ValidateAndFreezeWorldgenCompatibility(worldgenCompatibility);
        var identityComparer = GetIdentityComparer(frozenCompatibility.IdentityComparison);
        var frozenRules = refinementRules.ToArray();
        var frozenGeodes = geodes
            .Select(geode => ValidateAndFreezeGeode(geode, frozenCompatibility.InvalidAreaBehavior))
            .ToArray();

        ValidateRefinementRules(frozenRules);
        if (frozenGeodes.Length == 0)
            throw new InvalidOperationException("At least one geode definition is required.");

        var duplicateGeodeId = frozenGeodes
            .GroupBy(geode => geode.Id, identityComparer)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateGeodeId is not null)
            throw new InvalidOperationException($"Duplicate geode id '{duplicateGeodeId.Key}' under {frozenCompatibility.IdentityComparison} identity comparison.");

        var duplicatePrefab = frozenGeodes
            .GroupBy(geode => geode.PrefabName, identityComparer)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicatePrefab is not null)
            throw new InvalidOperationException($"Duplicate geode prefab '{duplicatePrefab.Key}' under {frozenCompatibility.IdentityComparison} identity comparison.");

        var fingerprint = ComputeFingerprint(schemaVersion, frozenRules, frozenGeodes, frozenCompatibility);
        return new MagenheimDefinitionSet(
            schemaVersion,
            Array.AsReadOnly(frozenRules),
            Array.AsReadOnly(frozenGeodes),
            frozenCompatibility,
            fingerprint);
    }

    private static void ValidateRefinementRules(IReadOnlyList<RefinementRule> rules)
    {
        if (rules.Count != 4)
            throw new InvalidOperationException("Refinement definitions must contain exactly four canonical tier transitions.");

        foreach (var rule in rules)
            rule.Validate();

        var expectedSources = new[]
        {
            CrystalTier.Rough,
            CrystalTier.Simple,
            CrystalTier.Crystal,
            CrystalTier.Advanced,
        };

        foreach (var expectedSource in expectedSources)
        {
            var matchingRules = rules.Count(rule => rule.SourceTier == expectedSource);
            if (matchingRules != 1)
                throw new InvalidOperationException($"Expected exactly one refinement rule for {expectedSource}; found {matchingRules}.");
        }
    }

    private static GeodeDefinition ValidateAndFreezeGeode(
        GeodeDefinition geode,
        InvalidAreaBehavior invalidAreaBehavior)
    {
        if (geode is null)
            throw new InvalidOperationException("Geode definitions cannot contain null entries.");

        var id = geode.Id?.Trim() ?? string.Empty;
        var biome = geode.Biome?.Trim() ?? string.Empty;
        var prefabName = geode.PrefabName?.Trim() ?? string.Empty;

        if (!id.StartsWith("magenheim.geode.", StringComparison.Ordinal))
            throw new InvalidOperationException("Geode ids must use the 'magenheim.geode.' namespace.");
        if (!SupportedBiomes.Contains(biome))
            throw new InvalidOperationException($"Geode '{id}' references unsupported biome '{biome}'.");
        if (!prefabName.StartsWith("Magenheim_", StringComparison.Ordinal))
            throw new InvalidOperationException($"Geode '{id}' prefab '{prefabName}' must use the Magenheim_ namespace.");

        var area = SpawnAreaValidator.Normalize(geode.Area, invalidAreaBehavior);
        if (!area.IsValid)
            throw new InvalidOperationException($"Geode '{id}' has invalid area: {area.Diagnostic}");

        if (geode.GuaranteedCrystalCount != 1)
            throw new InvalidOperationException($"Geode '{id}' must currently produce exactly one guaranteed crystal.");
        ValidateChance(geode.SecondCrystalChance, id, nameof(geode.SecondCrystalChance));
        ValidateChance(geode.ThirdCrystalChance, id, nameof(geode.ThirdCrystalChance));

        if (geode.ElementWeights is null || geode.ElementWeights.Count == 0)
            throw new InvalidOperationException($"Geode '{id}' requires at least one elemental weight.");

        var weights = geode.ElementWeights.ToArray();
        foreach (var weight in weights)
        {
            if (weight is null)
                throw new InvalidOperationException($"Geode '{id}' contains a null elemental weight.");
            if (double.IsNaN(weight.Weight) || double.IsInfinity(weight.Weight) || weight.Weight <= 0d)
                throw new InvalidOperationException($"Geode '{id}' has non-positive or non-finite weight for {weight.Element}.");
        }

        var duplicateElement = weights
            .GroupBy(weight => weight.Element)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateElement is not null)
            throw new InvalidOperationException($"Geode '{id}' defines element {duplicateElement.Key} more than once.");

        var placement = geode.Placement
            ?? throw new InvalidOperationException($"Geode '{id}' requires a world-placement definition.");
        placement.Validate(id);

        return geode with
        {
            Id = id,
            Biome = biome,
            PrefabName = prefabName,
            Area = area.Area,
            ElementWeights = Array.AsReadOnly(weights),
            Placement = placement,
        };
    }

    private static WorldgenCompatibilityPolicy ValidateAndFreezeWorldgenCompatibility(WorldgenCompatibilityPolicy policy)
    {
        if (!policy.AdditiveOnly)
            throw new InvalidOperationException("Worldgen compatibility cannot disable additive-only behavior.");
        if (!Enum.IsDefined(typeof(InvalidAreaBehavior), policy.InvalidAreaBehavior))
            throw new InvalidOperationException($"Unknown invalid-area behavior '{policy.InvalidAreaBehavior}'.");
        if (!Enum.IsDefined(typeof(DuplicateRegistrationBehavior), policy.DuplicateRegistrationBehavior))
            throw new InvalidOperationException($"Unknown duplicate-registration behavior '{policy.DuplicateRegistrationBehavior}'.");
        if (!Enum.IsDefined(typeof(RegistrationIdentityComparison), policy.IdentityComparison))
            throw new InvalidOperationException($"Unknown registration identity comparison '{policy.IdentityComparison}'.");

        var identityComparer = GetIdentityComparer(policy.IdentityComparison);
        var excludedKeys = NormalizeDistinct(
            policy.ExcludedRegistrationKeys,
            "worldgen compatibility registration-key exclusion",
            value => value.StartsWith("magenheim.", StringComparison.Ordinal),
            "Excluded registration keys must use the 'magenheim.' namespace.",
            identityComparer);

        var excludedPrefabs = NormalizeDistinct(
            policy.ExcludedPrefabNames,
            "worldgen compatibility prefab exclusion",
            value => value.StartsWith("Magenheim_", StringComparison.Ordinal),
            "Excluded prefab names must use the 'Magenheim_' namespace.",
            identityComparer);

        return policy with
        {
            AdditiveOnly = true,
            ExcludedRegistrationKeys = Array.AsReadOnly(excludedKeys),
            ExcludedPrefabNames = Array.AsReadOnly(excludedPrefabs),
        };
    }

    private static StringComparer GetIdentityComparer(RegistrationIdentityComparison comparison) =>
        comparison switch
        {
            RegistrationIdentityComparison.Exact => StringComparer.Ordinal,
            RegistrationIdentityComparison.CaseInsensitive => StringComparer.OrdinalIgnoreCase,
            _ => throw new InvalidOperationException($"Unknown registration identity comparison '{comparison}'."),
        };

    private static string[] NormalizeDistinct(
        IEnumerable<string>? values,
        string field,
        Func<string, bool> validator,
        string validationMessage,
        IEqualityComparer<string> comparer)
    {
        if (values is null)
            return Array.Empty<string>();

        var normalized = values.Select(value => value?.Trim() ?? string.Empty).ToArray();
        if (normalized.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException($"{field} cannot contain empty values.");
        if (normalized.Any(value => !validator(value)))
            throw new InvalidOperationException(validationMessage);

        var duplicate = normalized.GroupBy(value => value, comparer).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate {field} '{duplicate.Key}' under configured identity comparison.");

        return normalized;
    }

    private static void ValidateChance(double value, string geodeId, string field)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1d)
            throw new InvalidOperationException($"Geode '{geodeId}' field {field} must be finite and between 0 and 1 inclusive.");
    }

    private static string ComputeFingerprint(
        int schemaVersion,
        IEnumerable<RefinementRule> rules,
        IEnumerable<GeodeDefinition> geodes,
        WorldgenCompatibilityPolicy worldgenCompatibility)
    {
        var builder = new StringBuilder();
        builder.Append("schema=").Append(schemaVersion).Append('\n');

        builder.Append("worldgen|")
            .Append(worldgenCompatibility.InvalidAreaBehavior).Append('|')
            .Append(worldgenCompatibility.DuplicateRegistrationBehavior).Append('|')
            .Append(worldgenCompatibility.AdditiveOnly ? "1" : "0").Append('|')
            .Append(worldgenCompatibility.DetectPrefabCollisions ? "1" : "0").Append('|')
            .Append(worldgenCompatibility.IdentityComparison).Append('\n');

        var fingerprintIdentityComparer = GetIdentityComparer(worldgenCompatibility.IdentityComparison);
        foreach (var excludedKey in worldgenCompatibility.ExcludedRegistrationKeys.OrderBy(value => value, fingerprintIdentityComparer))
            builder.Append("worldgen-exclude-key|")
                .Append(NormalizeIdentityForFingerprint(excludedKey, worldgenCompatibility.IdentityComparison))
                .Append('\n');
        foreach (var excludedPrefab in worldgenCompatibility.ExcludedPrefabNames.OrderBy(value => value, fingerprintIdentityComparer))
            builder.Append("worldgen-exclude-prefab|")
                .Append(NormalizeIdentityForFingerprint(excludedPrefab, worldgenCompatibility.IdentityComparison))
                .Append('\n');

        foreach (var rule in rules.OrderBy(rule => rule.SourceTier))
        {
            builder.Append("refinement|")
                .Append(rule.SourceTier).Append('|')
                .Append(rule.DestinationTier).Append('|')
                .Append(rule.BaseFailureChance.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                .Append(rule.MinimumSkillLevel).Append('|')
                .Append(rule.RequiredStation).Append('|')
                .Append(rule.FailureShardCount).Append('\n');
        }

        foreach (var geode in geodes.OrderBy(geode => geode.Id, StringComparer.Ordinal))
        {
            builder.Append("geode|")
                .Append(geode.Id).Append('|')
                .Append(geode.Biome).Append('|')
                .Append(geode.PrefabName).Append('|')
                .Append((int)geode.Area).Append('|')
                .Append(geode.GuaranteedCrystalCount).Append('|')
                .Append(geode.SecondCrystalChance.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                .Append(geode.ThirdCrystalChance.ToString("R", CultureInfo.InvariantCulture));

            foreach (var weight in geode.ElementWeights.OrderBy(weight => weight.Element))
            {
                builder.Append('|').Append(weight.Element).Append('=')
                    .Append(weight.Weight.ToString("R", CultureInfo.InvariantCulture));
            }
            builder.Append('\n');

            AppendPlacementFingerprint(builder, geode.Id, geode.Placement);
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return string.Concat(hash.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static void AppendPlacementFingerprint(
        StringBuilder builder,
        string geodeId,
        GeodePlacementDefinition placement)
    {
        builder.Append("geode-placement|").Append(geodeId).Append('|')
            .Append(placement.BlockCheck ? "1" : "0").Append('|')
            .Append(placement.ForcePlacement ? "1" : "0").Append('|')
            .Append(placement.MinPerZone.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.MaxPerZone.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.MinAltitude.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.MaxAltitude.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.MinOceanDepth.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.MaxOceanDepth.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.MinTerrainDelta.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.MaxTerrainDelta.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.TerrainDeltaRadius.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.MinTilt.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.MaxTilt.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.InForest ? "1" : "0").Append('|')
            .Append(placement.ForestThresholdMin.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.ForestThresholdMax.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.ScaleMin.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.ScaleMax.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.GroupSizeMin).Append('|')
            .Append(placement.GroupSizeMax).Append('|')
            .Append(placement.GroupRadius.ToString("R", CultureInfo.InvariantCulture)).Append('|')
            .Append(placement.GroundOffset.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
    }

    private static string NormalizeIdentityForFingerprint(
        string value,
        RegistrationIdentityComparison comparison) =>
        comparison == RegistrationIdentityComparison.CaseInsensitive
            ? value.ToUpperInvariant()
            : value;
}
