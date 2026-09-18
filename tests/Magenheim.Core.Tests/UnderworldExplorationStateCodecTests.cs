using System;
using Magenheim.Core.Underworld;

internal static class UnderworldExplorationStateCodecTests
{
    internal static int Run()
    {
        var assertions = 0;
        void A(bool ok, string message) { assertions++; if (!ok) throw new InvalidOperationException("Underworld exploration codec assertion " + assertions + " failed: " + message); }
        void Reject(Action action, string message) { var rejected = false; try { action(); } catch (InvalidOperationException) { rejected = true; } A(rejected, message); }

        var source = new UnderworldExplorationState(MagenheimMapLayer.Underworld, 32, 24);
        source.RevealCircle(10, 11, 3);
        source.Reveal(31, 23);
        var encoded = UnderworldExplorationStateCodec.Encode(source);
        var restored = UnderworldExplorationStateCodec.Decode(encoded);
        A(restored.Layer == MagenheimMapLayer.Underworld && restored.Width == 32 && restored.Height == 24, "authority round-trips");
        A(restored.IsExplored(10, 11) && restored.IsExplored(31, 23), "revealed cells round-trip");
        A(!restored.IsExplored(0, 0), "unexplored cells remain clouded");
        A(UnderworldExplorationStateCodec.Encode(restored) == encoded, "encoding is canonical");

        var target = new UnderworldExplorationState(MagenheimMapLayer.Underworld, 32, 24);
        UnderworldExplorationStateCodec.RestoreInto(target, encoded);
        A(target.IsExplored(10, 11), "matching target accepts persisted state");
        Reject(() => UnderworldExplorationStateCodec.RestoreInto(new UnderworldExplorationState(MagenheimMapLayer.Surface, 32, 24), encoded), "cross-layer restore fails closed");
        Reject(() => UnderworldExplorationStateCodec.RestoreInto(new UnderworldExplorationState(MagenheimMapLayer.Underworld, 16, 24), encoded), "dimension drift fails closed");
        Reject(() => UnderworldExplorationStateCodec.Decode(encoded.Replace("MGEN-EXPLORATION|1|", "MGEN-EXPLORATION|99|")), "unknown schema fails closed");
        Reject(() => UnderworldExplorationStateCodec.Decode("MGEN-EXPLORATION|1|1|32|24|%%%"), "corrupt packed data fails closed");
        return assertions;
    }
}
