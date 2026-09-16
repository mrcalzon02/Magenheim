using System;
using Magenheim.Core.DarkThrone;
using Magenheim.Core.Underworld;

internal static class UnderworldTransitionStateCodecTests
{
    private const string Authority = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException($"Underworld transition codec assertion {assertions} failed: {message}");
        }

        var identity = UnderworldWorldIdentityFactory.Derive("world:persistence", "CodecSeed");
        var surfaceAnchor = new UnderworldAnchor(identity.ParentWorldId, 12.25d, 31.5d, -8.75d, 91.5f);
        var underworldAnchor = new UnderworldAnchor(identity.DerivedWorldId, -4.125d, 18.75d, 7.5d, 181.25f);
        var surface = UnderworldTransitionRules.CreateSurfaceState("player|codec=1", identity, surfaceAnchor);

        var surfacePayload = UnderworldTransitionStateCodec.Encode(surface, identity);
        var decodedSurface = UnderworldTransitionStateCodec.Decode(surfacePayload, identity);
        Assert(decodedSurface == surface, "Stable surface state must round-trip exactly.");
        Assert(surfacePayload == UnderworldTransitionStateCodec.Encode(decodedSurface, identity), "Stable encoding must be canonical and repeatable.");

        var alive = new DarkThroneEncounterSnapshot(
            DarkThroneEncounterSnapshot.CurrentSchemaVersion,
            "magenheim.dark_throne",
            EncounterPosition.Zero,
            DarkThroneEncounterLifecycle.Disengaged,
            1d,
            false);
        var defeated = DarkThroneEncounterTransitions.Defeat(alive);
        var prepared = UnderworldTransitionRules.BeginEnter(
            surface, identity, defeated, surfaceAnchor, underworldAnchor, Authority, "enter|codec=2");
        var preparedPayload = UnderworldTransitionStateCodec.Encode(prepared, identity);
        var decodedPrepared = UnderworldTransitionStateCodec.Decode(preparedPayload, identity);
        Assert(decodedPrepared == prepared, "Prepared transition state must round-trip exactly, including anchors and operation identity.");

        var recovery = UnderworldTransitionRules.RequireRecovery(
            prepared, "enter|codec=2", Authority, "target failed: retry=required | preserve source");
        var recoveryPayload = UnderworldTransitionStateCodec.Encode(recovery, identity);
        var decodedRecovery = UnderworldTransitionStateCodec.Decode(recoveryPayload, identity);
        Assert(decodedRecovery == recovery, "Recovery diagnostic text must survive canonical persistence encoding.");

        AssertThrows(Assert,
            () => UnderworldTransitionStateCodec.Decode(surfacePayload.Replace("format=1", "format=2"), identity),
            "Unknown persistence format versions must fail closed.");
        AssertThrows(Assert,
            () => UnderworldTransitionStateCodec.Decode(surfacePayload + "unknown=value\n", identity),
            "Unknown persistence fields must fail closed.");
        AssertThrows(Assert,
            () => UnderworldTransitionStateCodec.Decode(surfacePayload.Replace("active=0", "active=3"), identity),
            "Invalid active markers must fail closed.");
        AssertThrows(Assert,
            () => UnderworldTransitionStateCodec.Decode(preparedPayload.Replace("authority=" + Authority, "authority=BAD"), identity),
            "Corrupted authority fingerprints must fail closed through state validation.");

        var foreignIdentity = UnderworldWorldIdentityFactory.Derive("other-world", "CodecSeed");
        AssertThrows(Assert,
            () => UnderworldTransitionStateCodec.Decode(surfacePayload, foreignIdentity),
            "Persisted state must not be accepted for a different parent/derived world pair.");

        return assertions;
    }

    private static void AssertThrows(Action<bool, string> assert, Action action, string message)
    {
        var threw = false;
        try { action(); }
        catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException || exception is FormatException)
        {
            threw = true;
        }
        assert(threw, message);
    }
}
