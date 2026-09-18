using System;
using Magenheim.Core.Underworld;

internal static class UnderworldExplorationStateTests
{
    public static int Run()
    {
        var n = 0;
        void A(bool ok, string message) { n++; if (!ok) throw new InvalidOperationException("Underworld exploration assertion " + n + " failed: " + message); }

        var layers = new MagenheimExplorationLayers(16, 16, 16, 16);
        A(layers.Underworld.Reveal(8, 8), "first Underworld reveal changes state");
        A(layers.Underworld.IsExplored(8, 8), "Underworld cell is explored");
        A(!layers.Surface.IsExplored(8, 8), "identical Surface cell remains clouded");
        A(!layers.Underworld.Reveal(8, 8), "repeat reveal is idempotent");

        var changed = layers.Underworld.RevealCircle(2, 2, 1);
        A(changed == 5, "radius-one reveal covers center plus four cardinal cells");
        A(!layers.Surface.IsExplored(2, 2), "circle reveal cannot leak across layers");

        var packed = layers.Underworld.Pack();
        var restored = new UnderworldExplorationState(MagenheimMapLayer.Underworld, 16, 16);
        restored.Restore(packed);
        A(restored.IsExplored(8, 8) && restored.IsExplored(2, 2), "packed fog state round-trips");
        A(!restored.IsExplored(15, 15), "unexplored cells remain clouded after restore");

        var rejected = false;
        try { restored.Restore(new byte[1]); } catch (InvalidOperationException) { rejected = true; }
        A(rejected, "wrong-sized exploration payload fails closed");
        return n;
    }
}
