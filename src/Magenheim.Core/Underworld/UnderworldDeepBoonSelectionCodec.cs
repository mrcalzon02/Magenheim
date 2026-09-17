using System;
using System.Text;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Versioned canonical persistence codec for one player's selected Deep Boon.
/// Selection validity against current world progression is deliberately re-established by
/// UnderworldDeepBoonSelection.Reconstruct after decoding; persisted bytes never grant a boon.
/// </summary>
public static class UnderworldDeepBoonSelectionCodec
{
    public const int CurrentFormatVersion = 1;
    private const string Header = "magenheim-underworld-deep-boon-selection";

    public static string Encode(UnderworldDeepBoonSelectionState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        var value = state.SelectedDeepBoonId;
        if (value is not null && string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Selected Deep Boon id cannot be blank.");
        var encoded = value is null ? "-" : Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        return Header + "\nversion=" + CurrentFormatVersion + "\nselected=" + encoded + "\n";
    }

    public static UnderworldDeepBoonSelectionState Decode(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) throw new InvalidOperationException("Deep Boon selection payload is empty.");
        var lines = payload.Replace("\r\n", "\n").Split('\n');
        if (lines.Length != 4 || lines[0] != Header || lines[3].Length != 0)
            throw new InvalidOperationException("Deep Boon selection payload shape is invalid.");
        if (lines[1] != "version=" + CurrentFormatVersion)
            throw new InvalidOperationException("Deep Boon selection payload version is unsupported.");
        const string prefix = "selected=";
        if (!lines[2].StartsWith(prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Deep Boon selection payload is missing selected state.");
        var encoded = lines[2].Substring(prefix.Length);
        if (encoded == "-") return UnderworldDeepBoonSelection.CreateInitialState();
        if (encoded.Length == 0) throw new InvalidOperationException("Deep Boon selection payload contains an empty selection token.");
        try
        {
            var selected = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            if (string.IsNullOrWhiteSpace(selected)) throw new InvalidOperationException("Decoded Deep Boon selection is blank.");
            return new UnderworldDeepBoonSelectionState(selected);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Deep Boon selection payload contains invalid base64.", exception);
        }
    }
}
