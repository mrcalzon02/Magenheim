using System;
using System.IO;
using BepInEx;
using Jotunn.Utils;
using Magenheim.Runtime.Definitions;
using Magenheim.Runtime.Networking;

namespace Magenheim.Runtime;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
internal sealed class MagenheimPlugin : BaseUnityPlugin
{
    internal const string PluginGuid = "mrcalzon02.magenheim";
    internal const string PluginName = "Magenheim";
    internal const string PluginVersion = "0.0.12";

    private RuntimeServices? _services;
    private DefinitionAuthoritySynchronizer? _authoritySynchronizer;
    private GeodeItemRegistrar? _geodeItemRegistrar;
    private GeodeWorldgenRegistrar? _geodeWorldgenRegistrar;

    private void Awake()
    {
        try
        {
            var assemblyDirectory = Path.GetDirectoryName(typeof(MagenheimPlugin).Assembly.Location)
                ?? throw new InvalidOperationException("Unable to determine the Magenheim plugin directory.");
            var definitionPath = Path.Combine(assemblyDirectory, "default-data", "foundation.json");
            var baselineDefinitions = MagenheimDefinitionLoader.LoadFromFile(definitionPath);
            var effectiveDefinitions = MagenheimBalanceConfig.Apply(Config, baselineDefinitions);

            _services = RuntimeServices.Create(effectiveDefinitions);
            _authoritySynchronizer = new DefinitionAuthoritySynchronizer(
                effectiveDefinitions,
                _services.GeodeOpeningOperations,
                Logger);

            // Registration order is intentional. Worldgen creation needs the intact geode item as
            // its destruction drop, so the item registrar subscribes before the worldgen registrar.
            _geodeItemRegistrar = new GeodeItemRegistrar(effectiveDefinitions, Logger);
            _geodeItemRegistrar.Register();
            _geodeWorldgenRegistrar = new GeodeWorldgenRegistrar(effectiveDefinitions, Logger);
            _geodeWorldgenRegistrar.Register();

            Logger.LogInfo(
                $"{PluginName} {PluginVersion} loaded definition schema {effectiveDefinitions.SchemaVersion}. " +
                $"Baseline fingerprint {baselineDefinitions.Fingerprint}; effective fingerprint {effectiveDefinitions.Fingerprint}. " +
                "Definition authority synchronization, session-scoped geode replay protection, definition-driven intact geode items, " +
                "and additive geode vegetation registration are configured. Persistent inventory/socket mutations remain gated pending runtime validation.");
        }
        catch (Exception exception)
        {
            Logger.LogFatal($"{PluginName} definition bootstrap failed: {exception}");
            throw;
        }
    }

    private void OnDestroy()
    {
        _geodeWorldgenRegistrar?.Dispose();
        _geodeItemRegistrar?.Dispose();
    }
}
