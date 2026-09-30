using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Configuration;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Developer-only admission of exactly one Planned ordinary Underworld dungeon for DDE live gates.
/// Disabled by default. The policy is fingerprinted into multiplayer gameplay authority.
/// </summary>
internal static class UnderworldVanillaDungeonCandidatePolicy
{
    private const string Section = "Development.DeepDungeonExpansion";
    private static ConfigEntry<bool>? _enabled;
    private static ConfigEntry<string>? _candidate;

    internal static void Configure(ConfigFile config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));
        _enabled = config.Bind(
            Section,
            "EnablePlannedCandidateWorldgen",
            false,
            "Developer-only. Allows exactly one Planned Deep Dungeon Expansion donor derivative to register for disposable-world admission testing. Keep false outside DDE validation.");
        _candidate = config.Bind(
            Section,
            "CandidateDungeon",
            "fungal_forest",
            "Developer-only candidate suffix: fungal_forest, blackwater_deep, sulfurous_wastes, frozen_caverns, or great_decay.");

        // Fail at startup rather than silently ignoring an enabled invalid candidate.
        if (Enabled)
            _ = RequireCandidate();
    }

    internal static bool Enabled => _enabled?.Value == true;

    internal static string Fingerprint
    {
        get
        {
            var canonical = Enabled
                ? "dde-candidate-v1|enabled|" + RequireCandidate().DungeonId
                : "dde-candidate-v1|disabled";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            return string.Concat(bytes.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }

    internal static bool TryGetCandidate(out UnderworldVanillaDungeonReuseDefinition profile)
    {
        if (!Enabled)
        {
            profile = null!;
            return false;
        }

        profile = RequireCandidate();
        return true;
    }

    internal static string Describe() =>
        Enabled
            ? "ENABLED for " + RequireCandidate().DungeonId
            : "disabled";

    internal static bool IsCandidate(UnderworldDungeonDefinition definition)
    {
        if (definition is null || !Enabled) return false;
        var candidate = RequireCandidate();
        return string.Equals(candidate.DungeonId, definition.Id, StringComparison.Ordinal);
    }

    internal static bool IsWorldgenAdmitted(UnderworldDungeonDefinition definition) =>
        definition is not null &&
        (definition.Status == UnderworldDungeonStatus.RuntimeReady || IsCandidate(definition));


    private static UnderworldVanillaDungeonReuseDefinition RequireCandidate()
    {
        var raw = (_candidate?.Value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                "Deep Dungeon candidate worldgen is enabled but CandidateDungeon is empty.");

        var fullId = raw.StartsWith("magenheim.underworld.dungeon.", StringComparison.Ordinal)
            ? raw
            : "magenheim.underworld.dungeon." + raw;

        var match = UnderworldVanillaDungeonReuseCatalog.All.SingleOrDefault(value =>
            string.Equals(value.DungeonId, fullId, StringComparison.Ordinal));
        if (match is null)
            throw new InvalidOperationException(
                $"Unknown Deep Dungeon candidate '{raw}'. Expected one of: " +
                string.Join(", ", UnderworldVanillaDungeonReuseCatalog.All.Select(value =>
                    value.DungeonId.Substring("magenheim.underworld.dungeon.".Length))));

        var definition = UnderworldDungeonCatalog.All.Single(value =>
            string.Equals(value.Id, match.DungeonId, StringComparison.Ordinal));
        if (definition.Status != UnderworldDungeonStatus.Planned)
            throw new InvalidOperationException(
                $"Deep Dungeon candidate '{definition.DisplayName}' is already {definition.Status}; candidate override is only for Planned families.");

        return match;
    }
}
