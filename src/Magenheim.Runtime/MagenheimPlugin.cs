using BepInEx;
using Jotunn.Utils;

namespace Magenheim.Runtime;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
internal sealed class MagenheimPlugin : BaseUnityPlugin
{
    internal const string PluginGuid = "mrcalzon02.magenheim";
    internal const string PluginName = "Magenheim";
    internal const string PluginVersion = "0.0.3";

    private RuntimeServices? _services;

    private void Awake()
    {
        _services = RuntimeServices.Create();
        Logger.LogInfo($"{PluginName} {PluginVersion} runtime bootstrap initialized. Gameplay mutation remains disabled until server-authoritative transactions are implemented.");
    }
}
