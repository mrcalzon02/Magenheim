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
    IReadOnlyList<ElementWeight> ElementWeights);

public sealed record MagenheimDefinitionSet(
    int SchemaVersion,
    IReadOnlyList<RefinementRule> RefinementRules,
    IReadOnlyList<GeodeDefinition> Geodes,
    string Fingerprint);

public static class MagenheimDefinitionValidator
{
    public const int CurrentSchemaVersion = 1;

    public static MagenheimDefinitionSet ValidateAndFreeze(
        int schemaVersion,
        IEnumerable<RefinementRule> refinementRules,
        IEnumerable<GeodeDefinition> geodes)
    {
        if (schemaVersion != CurrentSchemaVersion)
            throw new InvalidOperationException($"Unsupported definition schema {schemaVersion}. Expected {CurrentSchemaVersion}.");

        if (refinementRules is null)
            throw new ArgumentNullException(nameof(refinementRules));
        if (geodes is null)
            throw new ArgumentNullException(nameof(geodes));

        var frozenRules = refinementRules.ToArray();
        var frozenGeodes = geodes.Select(ValidateAndFreezeGeode).ToArray();

        ValidateRefinementRules(frozenRules);
        if (frozenGeodes.Length == 0)
            throw new InvalidOperationException("At least one geode definition is required.");

        var duplicateGeodeId = frozenGeodes
            .GroupBy(geode => geode.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateGeodeId is not null)
            throw new InvalidOperationException($"Duplicate geode id '{duplicateGeodeId.Key}'.");

        var duplicatePrefab = frozenGeodes
            .GroupBy(geode => geode.PrefabName, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicatePrefab is not null)
            throw new InvalidOperationException($"Duplicate geode prefab '{duplicatePrefab.Key}'.");

        var fingerprint = ComputeFingerprint(schemaVersion, frozenRules, frozenGeodes);
        return new MagenheimDefinitionSet(
            schemaVersion,
            Array.AsReadOnly(frozenRules),
            Array.AsReadOnly(frozenGeodes),
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

    private static GeodeDefinition ValidateAndFreezeGeode(GeodeDefinition geode)
    {
        if (geode is null)
            throw new InvalidOperationException("Geode definitions cannot contain null entries.");
        if (string.IsNullOrWhiteSpace(geode.Id) || !geode.Id.StartsWith("magenheim.geode.", StringComparison.Ordinal))
            throw new InvalidOperationException("Geode ids must use the 'magenheim.geode.' namespace.");
        if (string.IsNullOrWhiteSpace(geode.Biome))
            throw new InvalidOperationException($"Geode '{geode.Id}' requires a biome.");
        if (string.IsNullOrWhiteSpace(geode.PrefabName))
            throw new InvalidOperationException($"Geode '{geode.Id}' requires a prefab name.");

        var area = SpawnAreaValidator.Normalize(geode.Area, InvalidAreaBehavior.Reject);
        if (!area.IsValid)
            throw new InvalidOperationException($"Geode '{geode.Id}' has invalid area: {area.Diagnostic}");

        if (geode.GuaranteedCrystalCount != 1)
            throw new InvalidOperationException($"Geode '{geode.Id}' must currently produce exactly one guaranteed crystal.");
        ValidateChance(geode.SecondCrystalChance, geode.Id, nameof(geode.SecondCrystalChance));
        ValidateChance(geode.ThirdCrystalChance, geode.Id, nameof(geode.ThirdCrystalChance));

        if (geode.ElementWeights is null || geode.ElementWeights.Count == 0)
            throw new InvalidOperationException($"Geode '{geode.Id}' requires at least one elemental weight.");

        var weights = geode.ElementWeights.ToArray();
        foreach (var weight in weights)
        {
            if (weight is null)
                throw new InvalidOperationException($"Geode '{geode.Id}' contains a null elemental weight.");
            if (double.IsNaN(weight.Weight) || double.IsInfinity(weight.Weight) || weight.Weight <= 0d)
                throw new InvalidOperationException($"Geode '{geode.Id}' has non-positive or non-finite weight for {weight.Element}.");
        }

        var duplicateElement = weights
            .GroupBy(weight => weight.Element)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateElement is not null)
            throw new InvalidOperationException($"Geode '{geode.Id}' defines element {duplicateElement.Key} more than once.");

        return geode with
        {
            Id = geode.Id.Trim(),
            Biome = geode.Biome.Trim(),
            PrefabName = geode.PrefabName.Trim(),
            Area = area.Area,
            ElementWeights = Array.AsReadOnly(weights),
        };
    }

    private static void ValidateChance(double value, string geodeId, string field)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1d)
            throw new InvalidOperationException($"Geode '{geodeId}' field {field} must be finite and between 0 and 1 inclusive.");
    }

    private static string ComputeFingerprint(
        int schemaVersion,
        IEnumerable<RefinementRule> rules,
        IEnumerable<GeodeDefinition> geodes)
    {
        var builder = new StringBuilder();
        builder.Append("schema=").Append(schemaVersion).Append('\n');

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
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return string.Concat(hash.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }
}
