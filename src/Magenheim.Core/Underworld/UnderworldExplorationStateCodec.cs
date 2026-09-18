using System;
using System.Globalization;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Canonical, versioned persistence envelope for one logical map layer's fog-of-war state.
/// Dimensions and layer identity are authenticated by the envelope before packed cells are restored,
/// so stale or cross-layer payloads fail closed instead of silently revealing the wrong map.
/// </summary>
public static class UnderworldExplorationStateCodec
{
    private const string Magic = "MGEN-EXPLORATION";
    private const int Version = 1;

    public static string Encode(UnderworldExplorationState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        return string.Join("|",
            Magic,
            Version.ToString(CultureInfo.InvariantCulture),
            ((int)state.Layer).ToString(CultureInfo.InvariantCulture),
            state.Width.ToString(CultureInfo.InvariantCulture),
            state.Height.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(state.Pack()));
    }

    public static UnderworldExplorationState Decode(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) throw new InvalidOperationException("Exploration payload is empty.");
        var fields = payload.Split('|');
        if (fields.Length != 6 || !string.Equals(fields[0], Magic, StringComparison.Ordinal))
            throw new InvalidOperationException("Exploration payload header is invalid.");
        if (!int.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version != Version)
            throw new InvalidOperationException("Exploration payload version is unsupported.");
        if (!int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var layerValue)
            || !Enum.IsDefined(typeof(MagenheimMapLayer), layerValue))
            throw new InvalidOperationException("Exploration payload map layer is invalid.");
        if (!int.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out var width) || width <= 0
            || !int.TryParse(fields[4], NumberStyles.None, CultureInfo.InvariantCulture, out var height) || height <= 0)
            throw new InvalidOperationException("Exploration payload dimensions are invalid.");

        byte[] packed;
        try { packed = Convert.FromBase64String(fields[5]); }
        catch (FormatException exception) { throw new InvalidOperationException("Exploration payload cell data is invalid.", exception); }

        var state = new UnderworldExplorationState((MagenheimMapLayer)layerValue, width, height);
        state.Restore(packed);
        return state;
    }

    public static void RestoreInto(UnderworldExplorationState target, string payload)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        var decoded = Decode(payload);
        if (decoded.Layer != target.Layer || decoded.Width != target.Width || decoded.Height != target.Height)
            throw new InvalidOperationException("Exploration payload authority does not match the target logical map layer.");
        target.Restore(decoded.Pack());
    }
}
