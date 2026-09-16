using System;
using Magenheim.Core.DarkThrone;
using Magenheim.Core.Underworld;

internal static class UnderworldTransitionTests
{
    private const string Authority = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException(
                    $"Underworld transition assertion {assertions} failed: {message}");
        }

        var identity = UnderworldWorldIdentityFactory.Derive("world-123", "SeedAlpha");
        var surfaceAnchor = new UnderworldAnchor(identity.ParentWorldId, 12d, 30d, -8d, 90f);
        var underworldAnchor = new UnderworldAnchor(identity.DerivedWorldId, 4d, 18d, 7d, 180f);
        var surface = UnderworldTransitionRules.CreateSurfaceState("player-1", identity, surfaceAnchor);

        Assert(surface.CurrentLayer == UnderworldLayer.Surface, "New player state must begin on the surface.");
        Assert(surface.ActiveTransition is null, "New stable surface state must not have an active transition.");
        UnderworldTransitionRules.ValidatePersistedState(surface, identity);
        assertions++;

        var alive = new DarkThroneEncounterSnapshot(
            DarkThroneEncounterSnapshot.CurrentSchemaVersion,
            "magenheim.dark_throne",
            EncounterPosition.Zero,
            DarkThroneEncounterLifecycle.Disengaged,
            1d,
            false);
        AssertThrows(
            Assert,
            () => UnderworldTransitionRules.BeginEnter(
                surface,
                identity,
                alive,
                surfaceAnchor,
                underworldAnchor,
                Authority,
                "enter-locked"),
            "Entry must fail closed before Nowhere King defeat.");

        var defeated = DarkThroneEncounterTransitions.Defeat(alive);
        var entering = UnderworldTransitionRules.BeginEnter(
            surface,
            identity,
            defeated,
            surfaceAnchor,
            underworldAnchor,
            Authority,
            "enter-1");

        Assert(entering.ActiveTransition is not null, "Successful entry planning must create an active transaction.");
        Assert(entering.SurfaceReturnAnchor == surfaceAnchor, "Entry planning must persist the source return anchor before transfer.");
        Assert(entering.CurrentLayer == UnderworldLayer.Surface, "Preparing entry must not move the stable player layer early.");
        UnderworldTransitionRules.ValidatePersistedState(entering, identity);
        assertions++;

        AssertThrows(
            Assert,
            () => UnderworldTransitionRules.Commit(entering, "enter-1", Authority),
            "Entry cannot commit before target initialization is acknowledged.");

        AssertThrows(
            Assert,
            () => UnderworldTransitionRules.MarkTargetReady(entering, "wrong-operation", Authority),
            "Transition advancement must reject a mismatched operation id.");

        AssertThrows(
            Assert,
            () => UnderworldTransitionRules.MarkTargetReady(
                entering,
                "enter-1",
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
            "Transition advancement must reject authority drift.");

        var entryReady = UnderworldTransitionRules.MarkTargetReady(entering, "enter-1", Authority);
        Assert(
            entryReady.ActiveTransition?.Phase == UnderworldTransitionPhase.TargetReady,
            "Prepared entry must advance to target-ready only after initialization acknowledgement.");

        var below = UnderworldTransitionRules.Commit(entryReady, "enter-1", Authority);
        Assert(below.CurrentLayer == UnderworldLayer.Underworld, "Committed entry must switch the stable layer to Underworld.");
        Assert(below.CurrentWorldId == identity.DerivedWorldId, "Committed entry must bind the derived world identity.");
        Assert(below.SurfaceReturnAnchor == surfaceAnchor, "Committed entry must retain the surface return anchor while below.");
        Assert(below.ActiveTransition is null, "Committed entry must clear its active transaction.");
        UnderworldTransitionRules.ValidatePersistedState(below, identity);
        assertions++;

        var returning = UnderworldTransitionRules.BeginReturn(
            below,
            identity,
            underworldAnchor,
            Authority,
            "return-1");
        Assert(returning.ActiveTransition?.Direction == UnderworldTransitionDirection.Return, "Return planning must create a return transaction.");
        Assert(returning.ActiveTransition?.TargetAnchor == surfaceAnchor, "Return planning must target the persisted surface anchor.");

        var returnReady = UnderworldTransitionRules.MarkTargetReady(returning, "return-1", Authority);
        var returned = UnderworldTransitionRules.Commit(returnReady, "return-1", Authority);
        Assert(returned.CurrentLayer == UnderworldLayer.Surface, "Committed return must restore the surface layer.");
        Assert(returned.CurrentWorldId == identity.ParentWorldId, "Committed return must restore the parent world identity.");
        Assert(returned.SurfaceReturnAnchor is null, "Successful return may clear the no-longer-needed recovery anchor.");
        UnderworldTransitionRules.ValidatePersistedState(returned, identity);
        assertions++;

        var enteringFailure = UnderworldTransitionRules.BeginEnter(
            surface,
            identity,
            defeated,
            surfaceAnchor,
            underworldAnchor,
            Authority,
            "enter-fail");
        var recovery = UnderworldTransitionRules.RequireRecovery(
            enteringFailure,
            "enter-fail",
            Authority,
            "target initialization failed");
        Assert(recovery.ActiveTransition?.Phase == UnderworldTransitionPhase.RecoveryRequired, "Failed entry must persist a recovery-required transaction.");
        Assert(recovery.SurfaceReturnAnchor == surfaceAnchor, "Failed entry must retain the recoverable surface anchor.");

        var recoveredSurface = UnderworldTransitionRules.RecoverToSource(
            recovery,
            "enter-fail",
            Authority);
        Assert(recoveredSurface.CurrentLayer == UnderworldLayer.Surface, "Entry recovery must restore the source surface layer.");
        Assert(recoveredSurface.ActiveTransition is null, "Successful recovery must clear the active transaction.");
        Assert(recoveredSurface.SurfaceReturnAnchor is null, "Recovered surface state must not retain a stale entry anchor.");

        var returnFailure = UnderworldTransitionRules.BeginReturn(
            below,
            identity,
            underworldAnchor,
            Authority,
            "return-fail");
        var returnRecovery = UnderworldTransitionRules.RequireRecovery(
            returnFailure,
            "return-fail",
            Authority,
            "surface load failed");
        var recoveredBelow = UnderworldTransitionRules.RecoverToSource(
            returnRecovery,
            "return-fail",
            Authority);
        Assert(recoveredBelow.CurrentLayer == UnderworldLayer.Underworld, "Return recovery must restore the source Underworld layer.");
        Assert(recoveredBelow.SurfaceReturnAnchor == surfaceAnchor, "Failed return recovery must preserve the surface anchor for a later retry.");
        UnderworldTransitionRules.ValidatePersistedState(recoveredBelow, identity);
        assertions++;

        var wrongWorldAnchor = surfaceAnchor with { WorldId = "wrong-world" };
        AssertThrows(
            Assert,
            () => UnderworldTransitionRules.CreateSurfaceState("player-1", identity, wrongWorldAnchor),
            "Surface state creation must reject an anchor from a foreign world.");

        var invalidBelow = below with { SurfaceReturnAnchor = null };
        AssertThrows(
            Assert,
            () => UnderworldTransitionRules.ValidatePersistedState(invalidBelow, identity),
            "Persisted Underworld state without a return anchor must fail closed.");

        var nonFiniteAnchor = surfaceAnchor with { X = double.NaN };
        AssertThrows(
            Assert,
            () => UnderworldTransitionRules.CreateSurfaceState("player-1", identity, nonFiniteAnchor),
            "Transition anchors must reject non-finite coordinates.");

        return assertions;
    }

    private static void AssertThrows(
        Action<bool, string> assert,
        Action action,
        string message)
    {
        var threw = false;
        try
        {
            action();
        }
        catch (Exception exception) when (
            exception is InvalidOperationException ||
            exception is ArgumentException)
        {
            threw = true;
        }

        assert(threw, message);
    }
}
