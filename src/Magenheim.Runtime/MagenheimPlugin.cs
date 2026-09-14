using System;
using System.IO;
using BepInEx;
using Jotunn.Utils;
using Magenheim.Runtime.Definitions;

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
        try
        {
            var assemblyDirectory = Path.GetDirectoryName(typeof(MagenheimPlugin).Assembly.Location)
                ?? throw new InvalidOperationException("Unable to determine the Magenheim plugin directory.");
            var definitionPath = Path.Combine(assemblyDirectory, "default-data", "foundation.json");
            var definitions = MagenheimDefinitionLoader.LoadFromFile(definitionPath);

            _services = RuntimeServices.Create(definitions);
            Logger.LogInfo(
                $"{PluginName} {PluginVersion} loaded definition schema {definitions.SchemaVersion} " +
                $"with fingerprint {definitions.Fingerprint}. Gameplay mutation remains disabled until server-authoritative transactions are implemented.");
        }
        catch (Exception exception)
        {
            Logger.LogFatal($"{PluginName} definition bootstrap failed: {exception}");
            throw;
        }
    }
}
