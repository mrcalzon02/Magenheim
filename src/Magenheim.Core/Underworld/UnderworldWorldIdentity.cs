using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Magenheim.Core.DarkThrone;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Deterministic logical identity for the Underworld paired with one parent Valheim world.
/// Runtime loading/persistence adapters consume this identity; Core does not assume how
/// Valheim ultimately switches or hosts the derived world context.
/// </summary>
public sealed record UnderworldWorldIdentity(
    string ParentWorldId,
    string ParentSeed,
    string DerivedWorldId,
    string DerivedSeedFingerprint,
    int DerivedSeed32);

public static class UnderworldWorldIdentityFactory
{
    private const string IdentityVersion = "magenheim-underworld-world-v1";

    public static UnderworldWorldIdentity Derive(string parentWorldId, string parentSeed)
    {
        var normalizedWorldId = RequireText(parentWorldId, nameof(parentWorldId));
        var normalizedSeed = RequireText(parentSeed, nameof(parentSeed));

        var canonical = new StringBuilder()
            .Append(IdentityVersion).Append('\n')
            .Append("parent-world=").Append(normalizedWorldId).Append('\n')
            .Append("parent-seed=").Append(normalizedSeed).Append('\n')
            .ToString();

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        var fingerprint = string.Concat(
            hash.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));

        var seed32 = unchecked((int)(
            ((uint)hash[0] << 24) |
            ((uint)hash[1] << 16) |
            ((uint)hash[2] << 8) |
            hash[3]));

        return new UnderworldWorldIdentity(
            normalizedWorldId,
            normalizedSeed,
            normalizedWorldId + ":magenheim-underworld",
            fingerprint,
            seed32);
    }

    private static string RequireText(string? value, string field)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new ArgumentException("A non-empty value is required.", field);
        if (normalized.Any(char.IsControl))
            throw new ArgumentException("World identity and seed cannot contain control characters.", field);
        return normalized;
    }
}

/// <summary>
/// The unlock consumes the existing durable Dark Throne encounter lifecycle. It does not
/// introduce a second Nowhere King completion flag.
/// </summary>
public static class UnderworldUnlockRules
{
    public static bool IsUnlocked(DarkThroneEncounterSnapshot encounter)
    {
        if (encounter is null) throw new ArgumentNullException(nameof(encounter));
        encounter.Validate();
        return encounter.Lifecycle == DarkThroneEncounterLifecycle.Defeated;
    }
}
