using System;
using Magenheim.Core.Underworld;

internal static class UnderworldDeepBoonSelectionCodecTests
{
    internal static int Run()
    {
        var assertions = 0;
        var empty = UnderworldDeepBoonSelection.CreateInitialState();
        var emptyPayload = UnderworldDeepBoonSelectionCodec.Encode(empty);
        Assert(UnderworldDeepBoonSelectionCodec.Decode(emptyPayload) == empty, "Empty selection must round-trip."); assertions++;

        var selected = new UnderworldDeepBoonSelectionState("deep_spore/communion=α");
        var payload = UnderworldDeepBoonSelectionCodec.Encode(selected);
        var decoded = UnderworldDeepBoonSelectionCodec.Decode(payload);
        Assert(decoded == selected, "Selected boon identity must round-trip without delimiter ambiguity."); assertions++;
        Assert(UnderworldDeepBoonSelectionCodec.Encode(decoded) == payload, "Selection encoding must be canonical and repeatable."); assertions++;

        AssertThrows(() => UnderworldDeepBoonSelectionCodec.Decode(""), "Empty payload must fail closed."); assertions++;
        AssertThrows(() => UnderworldDeepBoonSelectionCodec.Decode(payload.Replace("version=1", "version=99", StringComparison.Ordinal)), "Unknown format version must fail closed."); assertions++;
        AssertThrows(() => UnderworldDeepBoonSelectionCodec.Decode("magenheim-underworld-deep-boon-selection\nversion=1\nselected=%%%\n"), "Invalid encoded identity must fail closed."); assertions++;
        AssertThrows(() => UnderworldDeepBoonSelectionCodec.Encode(new UnderworldDeepBoonSelectionState("   ")), "Blank selected identity must not persist."); assertions++;
        return assertions;
    }

    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void AssertThrows(Action action, string message)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException(message);
    }
}
