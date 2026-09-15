using System;
using Magenheim.Core.Definitions;
using Magenheim.Core.Networking;

internal static class DefinitionAuthorityTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException($"Definition authority assertion {assertions} failed: {message}");
        }

        var currentSchema = MagenheimDefinitionValidator.CurrentSchemaVersion;
        var fingerprintA = new string('a', DefinitionAuthorityHandshake.Sha256HexLength);
        var fingerprintB = new string('b', DefinitionAuthorityHandshake.Sha256HexLength);
        var local = new DefinitionAuthorityDescriptor(currentSchema, fingerprintA);

        var pending = DefinitionAuthorityResult.Pending;
        Assert(pending.Status == DefinitionAuthorityStatus.Pending, "Pending must report Pending status.");
        Assert(!pending.MutationAuthorized, "Pending authority must fail closed for mutation.");

        var compatible = DefinitionAuthorityHandshake.Compare(local, new DefinitionAuthorityDescriptor(currentSchema, fingerprintA));
        Assert(compatible.Status == DefinitionAuthorityStatus.Compatible, "Identical schema/fingerprint must be compatible.");
        Assert(compatible.MutationAuthorized, "Only a compatible descriptor pair may authorize mutation.");

        var schemaMismatch = DefinitionAuthorityHandshake.Compare(local, new DefinitionAuthorityDescriptor(currentSchema + 1, fingerprintA));
        Assert(schemaMismatch.Status == DefinitionAuthorityStatus.SchemaMismatch, "Different schema versions must report SchemaMismatch.");
        Assert(!schemaMismatch.MutationAuthorized, "Schema mismatch must fail closed.");

        var fingerprintMismatch = DefinitionAuthorityHandshake.Compare(local, new DefinitionAuthorityDescriptor(currentSchema, fingerprintB));
        Assert(fingerprintMismatch.Status == DefinitionAuthorityStatus.FingerprintMismatch, "Different fingerprints must report FingerprintMismatch.");
        Assert(!fingerprintMismatch.MutationAuthorized, "Fingerprint mismatch must fail closed.");

        var invalidLocal = DefinitionAuthorityHandshake.Compare(
            new DefinitionAuthorityDescriptor(0, fingerprintA),
            new DefinitionAuthorityDescriptor(currentSchema, fingerprintA));
        Assert(invalidLocal.Status == DefinitionAuthorityStatus.InvalidDescriptor, "Invalid local authority must be rejected before comparison.");
        Assert(!invalidLocal.MutationAuthorized, "Invalid local authority must never authorize mutation.");

        var invalidRemote = DefinitionAuthorityHandshake.Compare(
            local,
            new DefinitionAuthorityDescriptor(currentSchema, fingerprintA.ToUpperInvariant()));
        Assert(invalidRemote.Status == DefinitionAuthorityStatus.InvalidDescriptor, "Uppercase fingerprints must be rejected as non-canonical descriptors.");
        Assert(!invalidRemote.MutationAuthorized, "Invalid remote authority must never authorize mutation.");

        Assert(!DefinitionAuthorityHandshake.IsValid(null, out _), "Null authority descriptor must be invalid.");
        Assert(!DefinitionAuthorityHandshake.IsValid(new DefinitionAuthorityDescriptor(currentSchema, "abc"), out _), "Wrong-length fingerprints must be invalid.");
        Assert(!DefinitionAuthorityHandshake.IsValid(new DefinitionAuthorityDescriptor(currentSchema, new string('g', 64)), out _), "Non-hexadecimal fingerprints must be invalid.");
        Assert(DefinitionAuthorityHandshake.IsValid(local, out var validError) && validError.Length == 0, "Canonical descriptor must validate without an error diagnostic.");

        assertions += DeepFractureCatalogTests.Run();
        assertions += DeepFractureEncounterPlannerTests.Run();
        assertions += DeepFractureLocationRegistrationTests.Run();
        assertions += GeodeCrackingTests.Run();
        assertions += GeodeOpeningTransactionTests.Run();
        return assertions;
    }
}
