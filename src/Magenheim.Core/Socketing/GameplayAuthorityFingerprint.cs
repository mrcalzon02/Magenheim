using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Magenheim.Core.Socketing;

/// <summary>
/// Extends the validated definition fingerprint with socket-compatibility policy so
/// peers cannot admit persistent socket mutations while using different eligibility rules.
/// </summary>
public static class GameplayAuthorityFingerprint
{
    public const int Sha256HexLength = 64;

    public static string Compute(string definitionFingerprint, SocketEligibilityPolicy socketPolicy)
    {
        if (!IsCanonicalSha256(definitionFingerprint))
            throw new ArgumentException("Definition fingerprint must be canonical lowercase SHA-256 hex.", nameof(definitionFingerprint));
        if (socketPolicy is null)
            throw new ArgumentNullException(nameof(socketPolicy));

        var builder = new StringBuilder();
        builder.Append("definition|").Append(definitionFingerprint).Append('\n');
        builder.Append("socket-policy-v1|")
            .Append(socketPolicy.WeaponMaxSlots).Append('|')
            .Append(socketPolicy.ArmorMaxSlots).Append('|')
            .Append(socketPolicy.ShieldMaxSlots).Append('|')
            .Append(socketPolicy.ToolMaxSlots).Append('|')
            .Append(socketPolicy.UtilityMaxSlots).Append('|')
            .Append(socketPolicy.ExplicitIncludeMaxSlots).Append('|')
            .Append(socketPolicy.IdentityComparison).Append('\n');

        AppendIdentities(builder, "socket-include-item", socketPolicy.IncludedItemNames, socketPolicy.IdentityComparison);
        AppendIdentities(builder, "socket-exclude-item", socketPolicy.ExcludedItemNames, socketPolicy.IdentityComparison);
        AppendIdentities(builder, "socket-include-prefab", socketPolicy.IncludedPrefabNames, socketPolicy.IdentityComparison);
        AppendIdentities(builder, "socket-exclude-prefab", socketPolicy.ExcludedPrefabNames, socketPolicy.IdentityComparison);
        AppendIdentities(builder, "socket-include-origin", socketPolicy.IncludedModOrigins, socketPolicy.IdentityComparison);
        AppendIdentities(builder, "socket-exclude-origin", socketPolicy.ExcludedModOrigins, socketPolicy.IdentityComparison);

        foreach (var category in socketPolicy.ExcludedCategories.OrderBy(value => (int)value))
            builder.Append("socket-exclude-category|").Append((int)category).Append('\n');

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return string.Concat(hash.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static void AppendIdentities(
        StringBuilder builder,
        string field,
        System.Collections.Generic.IEnumerable<string> values,
        SocketIdentityComparison comparison)
    {
        var comparer = comparison == SocketIdentityComparison.CaseInsensitive
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        foreach (var value in values.OrderBy(value => value, comparer))
        {
            var canonical = comparison == SocketIdentityComparison.CaseInsensitive
                ? value.ToUpperInvariant()
                : value;
            builder.Append(field).Append('|').Append(canonical).Append('\n');
        }
    }

    private static bool IsCanonicalSha256(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != Sha256HexLength)
            return false;

        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (!((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f')))
                return false;
        }

        return true;
    }
}
