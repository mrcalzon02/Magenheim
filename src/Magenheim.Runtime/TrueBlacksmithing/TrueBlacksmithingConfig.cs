using System;
using BepInEx.Configuration;

namespace Magenheim.Runtime.TrueBlacksmithing;

internal static class TrueBlacksmithingConfig
{
    private const string Section = "Modules.TrueBlacksmithing";
    private static ConfigEntry<bool>? _enabled;
    private static bool _startupEnabled;

    internal static bool Enabled => _startupEnabled;

    internal static void Configure(ConfigFile config)
    {
        if (config is null) throw new ArgumentNullException(nameof(config));

        _enabled = config.Bind(
            Section,
            "Enabled",
            false,
            "Enable the optional True Blacksmithing manufacturing overhaul. Restart required. "
            + "The fail-safe fallback is mandatory: if the subsystem cannot activate cleanly, "
            + "Magenheim must retain or restore its baseline crafting behavior for that session.");
        _startupEnabled = _enabled.Value;
    }
}
