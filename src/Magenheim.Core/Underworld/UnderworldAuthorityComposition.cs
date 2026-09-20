using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Canonical gameplay-significant Underworld authority envelope. Runtime Underworld
/// mutation must use this composite fingerprint rather than synchronizing content,
/// construction and native terrain authority independently.
/// </summary>
public sealed record UnderworldAuthoritySnapshot(
    int SchemaVersion,
    UnderworldDefinitionSet Content,
    UnderworldArchitectureDefinitionSet Architecture,
    UnderworldInstanceTerrainDomain TerrainDomain,
    string Fingerprint);

public static class UnderworldAuthorityComposer
{
    public const int CurrentSchemaVersion = 4;

    public static UnderworldAuthoritySnapshot Compose(
        UnderworldDefinitionSet content,
        UnderworldArchitectureDefinitionSet architecture,
        UnderworldInstanceTerrainDomain terrainDomain)
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));
        if (architecture is null)
            throw new ArgumentNullException(nameof(architecture));
        if (terrainDomain is null)
            throw new ArgumentNullException(nameof(terrainDomain));
        if (content.SchemaVersion != UnderworldDefinitionValidator.CurrentSchemaVersion)
            throw new InvalidOperationException(
                $"Unsupported Underworld content schema {content.SchemaVersion}.");
        if (architecture.SchemaVersion != UnderworldArchitectureValidator.CurrentSchemaVersion)
            throw new InvalidOperationException(
                $"Unsupported Underworld architecture schema {architecture.SchemaVersion}.");

        var validatedTerrainDomain = UnderworldInstanceTerrainDomain.ValidateAndFreeze(
            terrainDomain.RadiusMeters,
            terrainDomain.MinimumY,
            terrainDomain.MaximumY);
        if (validatedTerrainDomain != terrainDomain)
            throw new InvalidOperationException("Underworld native terrain domain must be canonical.");

        RequireSha256(content.Fingerprint, "Underworld content fingerprint");
        RequireSha256(architecture.Fingerprint, "Underworld architecture fingerprint");

        return new UnderworldAuthoritySnapshot(
            CurrentSchemaVersion,
            content,
            architecture,
            terrainDomain,
            ComputeFingerprint(content.Fingerprint, architecture.Fingerprint, terrainDomain));
    }

    private static string ComputeFingerprint(
        string contentFingerprint,
        string architectureFingerprint,
        UnderworldInstanceTerrainDomain terrainDomain)
    {
        var payload = new StringBuilder()
            .Append("underworld-authority-schema=").Append(CurrentSchemaVersion).Append('\n')
            .Append("instance-contract=").Append(UnderworldInstanceContract.ContractId).Append('\n')
            .Append("content=").Append(contentFingerprint).Append('\n')
            .Append("architecture=").Append(architectureFingerprint).Append('\n')
            .Append("terrain-radius=").Append(Canonical(terrainDomain.RadiusMeters)).Append('\n')
            .Append("terrain-min-y=").Append(Canonical(terrainDomain.MinimumY)).Append('\n')
            .Append("terrain-max-y=").Append(Canonical(terrainDomain.MaximumY)).Append('\n')
            .ToString();

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var result = new StringBuilder(hash.Length * 2);
        foreach (var value in hash)
            result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        return result.ToString();
    }

    private static string Canonical(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    private static void RequireSha256(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64)
            throw new InvalidOperationException($"{field} must be canonical SHA-256 hex.");

        foreach (var character in value)
        {
            if ((character < '0' || character > '9') && (character < 'a' || character > 'f'))
                throw new InvalidOperationException($"{field} must use lowercase hexadecimal characters.");
        }
    }
}
