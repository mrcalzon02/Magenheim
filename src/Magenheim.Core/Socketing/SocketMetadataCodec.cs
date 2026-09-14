using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Magenheim.Core.Socketing;

public static class SocketMetadataCodec
{
    public const string CustomDataKey = "magenheim.sockets.v1";
    private const int CurrentFormatVersion = 1;

    public static string Encode(SocketState state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        var crystals = string.Join(",", state.InstalledCrystals.Select(
            crystal => crystal.Element + ":" + crystal.Tier));
        return CurrentFormatVersion.ToString(CultureInfo.InvariantCulture) + "|" +
               state.UnlockedSlots.ToString(CultureInfo.InvariantCulture) + "|" + crystals;
    }

    public static bool TryDecode(string encoded, out SocketState state, out string diagnostic)
    {
        state = SocketState.Empty;
        diagnostic = string.Empty;
        if (string.IsNullOrWhiteSpace(encoded))
        {
            diagnostic = "Socket metadata is empty.";
            return false;
        }

        var parts = encoded.Split('|');
        if (parts.Length != 3)
        {
            diagnostic = "Socket metadata must contain exactly version, slot count, and crystal list fields.";
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var version) ||
            version != CurrentFormatVersion)
        {
            diagnostic = $"Unsupported socket metadata version '{parts[0]}'.";
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var slots) ||
            slots < 0 || slots > SocketState.MaximumSupportedSlots)
        {
            diagnostic = $"Socket count must be between 0 and {SocketState.MaximumSupportedSlots}.";
            return false;
        }

        var crystals = new List<Crystal>();
        if (!string.IsNullOrEmpty(parts[2]))
        {
            foreach (var token in parts[2].Split(','))
            {
                var crystalParts = token.Split(':');
                if (crystalParts.Length != 2 ||
                    !Enum.TryParse(crystalParts[0], ignoreCase: false, out ElementalAlignment element) ||
                    !Enum.IsDefined(typeof(ElementalAlignment), element) ||
                    !Enum.TryParse(crystalParts[1], ignoreCase: false, out CrystalTier tier) ||
                    !Enum.IsDefined(typeof(CrystalTier), tier))
                {
                    diagnostic = $"Invalid installed crystal token '{token}'.";
                    return false;
                }
                crystals.Add(new Crystal(element, tier));
            }
        }

        if (crystals.Count > slots)
        {
            diagnostic = "Installed crystal count exceeds unlocked socket count.";
            return false;
        }

        try
        {
            state = new SocketState(slots, crystals);
            return true;
        }
        catch (Exception exception)
        {
            diagnostic = exception.Message;
            return false;
        }
    }

    public static bool TryRead(
        IReadOnlyDictionary<string, string> customData,
        out SocketState state,
        out string diagnostic)
    {
        if (customData is null) throw new ArgumentNullException(nameof(customData));
        if (!customData.TryGetValue(CustomDataKey, out var encoded))
        {
            state = SocketState.Empty;
            diagnostic = string.Empty;
            return true;
        }
        return TryDecode(encoded, out state, out diagnostic);
    }

    public static void Write(IDictionary<string, string> customData, SocketState state)
    {
        if (customData is null) throw new ArgumentNullException(nameof(customData));
        if (state is null) throw new ArgumentNullException(nameof(state));

        if (state.UnlockedSlots == 0 && state.InstalledCrystals.Count == 0)
        {
            customData.Remove(CustomDataKey);
            return;
        }

        customData[CustomDataKey] = Encode(state);
    }
}
