using System;

namespace Magenheim.Core.Networking;

public enum DefinitionAuthorityStatus
{
    Pending = 0,
    Compatible = 1,
    InvalidDescriptor = 2,
    SchemaMismatch = 3,
    FingerprintMismatch = 4,
}

public sealed record DefinitionAuthorityDescriptor(int SchemaVersion, string Fingerprint);

public sealed record DefinitionAuthorityResult(
    DefinitionAuthorityStatus Status,
    bool MutationAuthorized,
    string Diagnostic)
{
    public static DefinitionAuthorityResult Pending { get; } =
        new(DefinitionAuthorityStatus.Pending, false, "Definition authority has not been synchronized yet.");
}

public static class DefinitionAuthorityHandshake
{
    public const int Sha256HexLength = 64;

    public static DefinitionAuthorityResult Compare(
        DefinitionAuthorityDescriptor local,
        DefinitionAuthorityDescriptor remote)
    {
        if (!IsValid(local, out var localError))
            return new DefinitionAuthorityResult(
                DefinitionAuthorityStatus.InvalidDescriptor,
                false,
                $"Local definition authority is invalid: {localError}");

        if (!IsValid(remote, out var remoteError))
            return new DefinitionAuthorityResult(
                DefinitionAuthorityStatus.InvalidDescriptor,
                false,
                $"Remote definition authority is invalid: {remoteError}");

        if (local.SchemaVersion != remote.SchemaVersion)
            return new DefinitionAuthorityResult(
                DefinitionAuthorityStatus.SchemaMismatch,
                false,
                $"Definition schema mismatch: local {local.SchemaVersion}, remote {remote.SchemaVersion}.");

        if (!string.Equals(local.Fingerprint, remote.Fingerprint, StringComparison.Ordinal))
            return new DefinitionAuthorityResult(
                DefinitionAuthorityStatus.FingerprintMismatch,
                false,
                $"Definition fingerprint mismatch: local {local.Fingerprint}, remote {remote.Fingerprint}.");

        return new DefinitionAuthorityResult(
            DefinitionAuthorityStatus.Compatible,
            true,
            $"Definition authority matches schema {local.SchemaVersion} fingerprint {local.Fingerprint}.");
    }

    public static bool IsValid(DefinitionAuthorityDescriptor? descriptor, out string error)
    {
        if (descriptor is null)
        {
            error = "descriptor is null";
            return false;
        }

        if (descriptor.SchemaVersion <= 0)
        {
            error = "schema version must be positive";
            return false;
        }

        if (string.IsNullOrWhiteSpace(descriptor.Fingerprint) || descriptor.Fingerprint.Length != Sha256HexLength)
        {
            error = $"fingerprint must contain exactly {Sha256HexLength} hexadecimal characters";
            return false;
        }

        for (var index = 0; index < descriptor.Fingerprint.Length; index++)
        {
            var character = descriptor.Fingerprint[index];
            var hexadecimal =
                character >= '0' && character <= '9' ||
                character >= 'a' && character <= 'f';

            if (!hexadecimal)
            {
                error = "fingerprint must be lowercase hexadecimal SHA-256 text";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }
}
