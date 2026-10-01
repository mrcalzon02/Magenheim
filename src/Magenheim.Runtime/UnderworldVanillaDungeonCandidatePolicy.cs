using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Configuration;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Compatibility shell for the retired one-at-a-time DDE candidate switch.
/// All five ordinary expanded-vanilla dungeon families are now RuntimeReady and are admitted
/// together through normal catalog authority. Legacy config keys remain bound so old profiles
/// do not fail startup, but they no longer alter registration or worldgen.
/// </summary>
internal static class UnderworldVanillaDungeonCandidatePolicy
{
    private const string Section = "Development.DeepDungeonExpansion";

    internal static void Configure(ConfigFile config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));

        _ = config.Bind(
            Section,
            "EnablePlannedCandidateWorldgen",
            false,
            "Deprecated compatibility setting. All five ordinary Deep Dungeon Expansion families are runtime-admitted together; this value no longer changes worldgen.");

        _ = config.Bind(
            Section,
            "CandidateDungeon",
            "fungal_forest",
            "Deprecated compatibility setting. One-at-a-time candidate admission has been retired.");
    }

    internal static bool Enabled => false;

    internal static string Fingerprint
    {
        get
        {
            var canonical = "dde-candidate-v2|retired|all-ordinary-runtime-ready|release:" + MagenheimPlugin.PluginVersion
                + "|entry:queen-or-legacy-king|finale:great-decay-six-deepstones";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            return string.Concat(bytes.Select(value =>
                value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }

    internal static bool TryGetCandidate(out UnderworldVanillaDungeonReuseDefinition profile)
    {
        profile = null!;
        return false;
    }

    internal static string Describe() =>
        "retired; all ordinary expanded-vanilla dungeons are RuntimeReady";

    internal static bool IsCandidate(UnderworldDungeonDefinition definition) => false;

    internal static bool IsWorldgenAdmitted(UnderworldDungeonDefinition definition) =>
        definition is not null &&
        definition.Status == UnderworldDungeonStatus.RuntimeReady;
}
