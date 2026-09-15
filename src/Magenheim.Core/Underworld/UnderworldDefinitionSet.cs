using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Pure, validated Underworld content authority. This is a component of Magenheim's
/// definition authority and must be embedded into the canonical Magenheim definition
/// fingerprint before runtime Underworld mutations are admitted.
/// </summary>
public sealed record UnderworldBiomeDefinition(
    string Id,
    string DisplayName,
    string BossId);

public sealed record UnderworldBossDefinition(
    string Id,
    string BiomeId,
    string UniqueLocationId,
    string DeepstoneSlotId,
    string TrophyPrefabName,
    string DeepBoonId,
    IReadOnlyList<string> PrerequisiteBossIds);

public sealed record UnderworldDeepstoneDefinition(
    string Id,
    string BossId,
    string DeepBoonId);

public sealed record UnderworldDefinitionSet(
    int SchemaVersion,
    IReadOnlyList<UnderworldBiomeDefinition> Biomes,
    IReadOnlyList<UnderworldBossDefinition> Bosses,
    IReadOnlyList<UnderworldDeepstoneDefinition> Deepstones,
    string Fingerprint);

public static class UnderworldDefinitionValidator
{
    public const int CurrentSchemaVersion = 1;

    private const string BiomePrefix = "magenheim.underworld.biome.";
    private const string BossPrefix = "magenheim.underworld.boss.";
    private const string LocationPrefix = "magenheim.underworld.location.";
    private const string DeepstonePrefix = "magenheim.underworld.deepstone.";
    private const string BoonPrefix = "magenheim.underworld.boon.";
    private const string TrophyPrefix = "Magenheim_Underworld_Trophy_";

    public static UnderworldDefinitionSet ValidateAndFreeze(
        int schemaVersion,
        IEnumerable<UnderworldBiomeDefinition> biomes,
        IEnumerable<UnderworldBossDefinition> bosses,
        IEnumerable<UnderworldDeepstoneDefinition> deepstones)
    {
        if (schemaVersion != CurrentSchemaVersion)
            throw new InvalidOperationException(
                $"Unsupported Underworld definition schema {schemaVersion}. Expected {CurrentSchemaVersion}.");
        if (biomes is null) throw new ArgumentNullException(nameof(biomes));
        if (bosses is null) throw new ArgumentNullException(nameof(bosses));
        if (deepstones is null) throw new ArgumentNullException(nameof(deepstones));

        var frozenBiomes = biomes.Select(ValidateBiome).ToArray();
        var frozenBosses = bosses.Select(ValidateBoss).ToArray();
        var frozenDeepstones = deepstones.Select(ValidateDeepstone).ToArray();

        if (frozenBiomes.Length == 0)
            throw new InvalidOperationException("At least one Underworld biome definition is required.");
        if (frozenBosses.Length == 0)
            throw new InvalidOperationException("At least one Underworld boss definition is required.");
        if (frozenDeepstones.Length == 0)
            throw new InvalidOperationException("At least one Underworld Deepstone definition is required.");

        RejectDuplicate(frozenBiomes.Select(value => value.Id), "Underworld biome id");
        RejectDuplicate(frozenBosses.Select(value => value.Id), "Underworld boss id");
        RejectDuplicate(frozenBosses.Select(value => value.UniqueLocationId), "Underworld boss location id");
        RejectDuplicate(frozenBosses.Select(value => value.TrophyPrefabName), "Underworld boss trophy prefab");
        RejectDuplicate(frozenBosses.Select(value => value.DeepBoonId), "Underworld Deep Boon id");
        RejectDuplicate(frozenDeepstones.Select(value => value.Id), "Underworld Deepstone id");

        var biomeById = frozenBiomes.ToDictionary(value => value.Id, StringComparer.Ordinal);
        var bossById = frozenBosses.ToDictionary(value => value.Id, StringComparer.Ordinal);
        var deepstoneById = frozenDeepstones.ToDictionary(value => value.Id, StringComparer.Ordinal);

        foreach (var biome in frozenBiomes)
        {
            if (!bossById.TryGetValue(biome.BossId, out var boss))
                throw new InvalidOperationException(
                    $"Underworld biome '{biome.Id}' references unknown boss '{biome.BossId}'.");
            if (!string.Equals(boss.BiomeId, biome.Id, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Underworld biome '{biome.Id}' and boss '{boss.Id}' do not agree on ownership.");
        }

        var duplicateBiomeOwner = frozenBosses
            .GroupBy(value => value.BiomeId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateBiomeOwner is not null)
            throw new InvalidOperationException(
                $"Underworld biome '{duplicateBiomeOwner.Key}' has more than one primary boss.");

        foreach (var boss in frozenBosses)
        {
            if (!biomeById.ContainsKey(boss.BiomeId))
                throw new InvalidOperationException(
                    $"Underworld boss '{boss.Id}' references unknown biome '{boss.BiomeId}'.");

            if (!deepstoneById.TryGetValue(boss.DeepstoneSlotId, out var deepstone))
                throw new InvalidOperationException(
                    $"Underworld boss '{boss.Id}' references unknown Deepstone '{boss.DeepstoneSlotId}'.");
            if (!string.Equals(deepstone.BossId, boss.Id, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Deepstone '{deepstone.Id}' does not reference its owning boss '{boss.Id}'.");
            if (!string.Equals(deepstone.DeepBoonId, boss.DeepBoonId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Deepstone '{deepstone.Id}' and boss '{boss.Id}' do not agree on Deep Boon identity.");

            foreach (var prerequisite in boss.PrerequisiteBossIds)
            {
                if (!bossById.ContainsKey(prerequisite))
                    throw new InvalidOperationException(
                        $"Underworld boss '{boss.Id}' references unknown prerequisite boss '{prerequisite}'.");
                if (string.Equals(prerequisite, boss.Id, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Underworld boss '{boss.Id}' cannot require itself.");
            }
        }

        if (frozenDeepstones.Length != frozenBosses.Length)
            throw new InvalidOperationException(
                "Every Underworld boss must have exactly one Deepstone and no unowned Deepstones may exist.");

        ValidateProgressionAcyclic(frozenBosses, bossById);

        var fingerprint = ComputeFingerprint(
            schemaVersion,
            frozenBiomes,
            frozenBosses,
            frozenDeepstones);

        return new UnderworldDefinitionSet(
            schemaVersion,
            Array.AsReadOnly(frozenBiomes),
            Array.AsReadOnly(frozenBosses),
            Array.AsReadOnly(frozenDeepstones),
            fingerprint);
    }

    private static UnderworldBiomeDefinition ValidateBiome(UnderworldBiomeDefinition biome)
    {
        if (biome is null)
            throw new InvalidOperationException("Underworld biome definitions cannot contain null entries.");

        return biome with
        {
            Id = RequireNamespaced(biome.Id, BiomePrefix, "Underworld biome id"),
            DisplayName = RequireText(biome.DisplayName, "Underworld biome display name"),
            BossId = RequireNamespaced(biome.BossId, BossPrefix, "Underworld biome boss id"),
        };
    }

    private static UnderworldBossDefinition ValidateBoss(UnderworldBossDefinition boss)
    {
        if (boss is null)
            throw new InvalidOperationException("Underworld boss definitions cannot contain null entries.");
        if (boss.PrerequisiteBossIds is null)
            throw new InvalidOperationException($"Underworld boss '{boss.Id}' requires a prerequisite collection.");

        var prerequisites = boss.PrerequisiteBossIds
            .Select(value => RequireNamespaced(value, BossPrefix, "Underworld prerequisite boss id"))
            .ToArray();
        RejectDuplicate(prerequisites, $"prerequisite boss for '{boss.Id}'");

        return boss with
        {
            Id = RequireNamespaced(boss.Id, BossPrefix, "Underworld boss id"),
            BiomeId = RequireNamespaced(boss.BiomeId, BiomePrefix, "Underworld boss biome id"),
            UniqueLocationId = RequireNamespaced(
                boss.UniqueLocationId,
                LocationPrefix,
                "Underworld boss location id"),
            DeepstoneSlotId = RequireNamespaced(
                boss.DeepstoneSlotId,
                DeepstonePrefix,
                "Underworld boss Deepstone id"),
            TrophyPrefabName = RequireNamespaced(
                boss.TrophyPrefabName,
                TrophyPrefix,
                "Underworld boss trophy prefab"),
            DeepBoonId = RequireNamespaced(
                boss.DeepBoonId,
                BoonPrefix,
                "Underworld boss Deep Boon id"),
            PrerequisiteBossIds = Array.AsReadOnly(prerequisites),
        };
    }

    private static UnderworldDeepstoneDefinition ValidateDeepstone(UnderworldDeepstoneDefinition deepstone)
    {
        if (deepstone is null)
            throw new InvalidOperationException("Underworld Deepstone definitions cannot contain null entries.");

        return deepstone with
        {
            Id = RequireNamespaced(deepstone.Id, DeepstonePrefix, "Underworld Deepstone id"),
            BossId = RequireNamespaced(deepstone.BossId, BossPrefix, "Underworld Deepstone boss id"),
            DeepBoonId = RequireNamespaced(
                deepstone.DeepBoonId,
                BoonPrefix,
                "Underworld Deepstone boon id"),
        };
    }

    private static void ValidateProgressionAcyclic(
        IReadOnlyList<UnderworldBossDefinition> bosses,
        IReadOnlyDictionary<string, UnderworldBossDefinition> bossById)
    {
        var state = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var boss in bosses)
            Visit(boss.Id, bossById, state);
    }

    private static void Visit(
        string bossId,
        IReadOnlyDictionary<string, UnderworldBossDefinition> bossById,
        IDictionary<string, int> state)
    {
        if (state.TryGetValue(bossId, out var existing))
        {
            if (existing == 1)
                throw new InvalidOperationException(
                    $"Underworld boss progression contains a cycle involving '{bossId}'.");
            if (existing == 2)
                return;
        }

        state[bossId] = 1;
        foreach (var prerequisite in bossById[bossId].PrerequisiteBossIds)
            Visit(prerequisite, bossById, state);
        state[bossId] = 2;
    }

    private static void RejectDuplicate(IEnumerable<string> values, string field)
    {
        var duplicate = values
            .GroupBy(value => value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate {field} '{duplicate.Key}'.");
    }

    private static string RequireNamespaced(string? value, string prefix, string field)
    {
        var normalized = RequireText(value, field);
        if (!normalized.StartsWith(prefix, StringComparison.Ordinal))
            throw new InvalidOperationException($"{field} '{normalized}' must use prefix '{prefix}'.");
        if (normalized.Length == prefix.Length)
            throw new InvalidOperationException($"{field} must contain an identity after prefix '{prefix}'.");
        return normalized;
    }

    private static string RequireText(string? value, string field)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new InvalidOperationException($"{field} is required.");
        return normalized;
    }

    private static string ComputeFingerprint(
        int schemaVersion,
        IEnumerable<UnderworldBiomeDefinition> biomes,
        IEnumerable<UnderworldBossDefinition> bosses,
        IEnumerable<UnderworldDeepstoneDefinition> deepstones)
    {
        var builder = new StringBuilder();
        builder.Append("underworld-schema=").Append(schemaVersion).Append('\n');

        foreach (var biome in biomes.OrderBy(value => value.Id, StringComparer.Ordinal))
        {
            builder.Append("underworld-biome|")
                .Append(biome.Id).Append('|')
                .Append(biome.BossId).Append('\n');
        }

        foreach (var boss in bosses.OrderBy(value => value.Id, StringComparer.Ordinal))
        {
            builder.Append("underworld-boss|")
                .Append(boss.Id).Append('|')
                .Append(boss.BiomeId).Append('|')
                .Append(boss.UniqueLocationId).Append('|')
                .Append(boss.DeepstoneSlotId).Append('|')
                .Append(boss.TrophyPrefabName).Append('|')
                .Append(boss.DeepBoonId).Append('\n');

            foreach (var prerequisite in boss.PrerequisiteBossIds.OrderBy(value => value, StringComparer.Ordinal))
                builder.Append("underworld-boss-prerequisite|")
                    .Append(boss.Id).Append('|')
                    .Append(prerequisite).Append('\n');
        }

        foreach (var deepstone in deepstones.OrderBy(value => value.Id, StringComparer.Ordinal))
        {
            builder.Append("underworld-deepstone|")
                .Append(deepstone.Id).Append('|')
                .Append(deepstone.BossId).Append('|')
                .Append(deepstone.DeepBoonId).Append('\n');
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return string.Concat(hash.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }
}
