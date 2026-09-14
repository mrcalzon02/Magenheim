using System;
using System.IO;
using BepInEx;
using HarmonyLib;
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
    internal const string PluginVersion = "0.0.16";

    private RuntimeServices? _services;
    private DefinitionAuthoritySynchronizer? _authoritySynchronizer;
    private GeodeItemRegistrar? _geodeItemRegistrar;
    private GeodeWorldgenRegistrar? _geodeWorldgenRegistrar;
    private EarthContentRegistrar? _earthContentRegistrar;
    private WorkshopRegistrar? _workshopRegistrar;
    private WorkshopOperationRegistrar? _workshopOperationRegistrar;
    private ShardRecipeRegistrar? _shardRecipeRegistrar;
    private Harmony? _harmony;

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
                _services.RefinementOperations,
                _services.SocketOperations,
                _services.SocketExtractionOperations,
                Logger);

            WorkshopOperationsRuntime.Configure(_services, _authoritySynchronizer, Logger);
            SocketEffectsRuntime.Configure(effectiveDefinitions, Logger);
            _harmony = new Harmony(PluginGuid + ".gameplay");
            _harmony.PatchAll(typeof(WorkshopCraftingPatch));
            _harmony.PatchAll(typeof(SocketDamagePatch));
            _harmony.PatchAll(typeof(SocketArmorPatch));
            _harmony.PatchAll(typeof(SocketBlockPowerPatch));
            _harmony.PatchAll(typeof(SocketCarryWeightPatch));

            _earthContentRegistrar = new EarthContentRegistrar(Logger);
            _earthContentRegistrar.Register();
            _workshopRegistrar = new WorkshopRegistrar(Logger);
            _workshopRegistrar.Register();
            _geodeItemRegistrar = new GeodeItemRegistrar(effectiveDefinitions, Logger);
            _geodeItemRegistrar.Register();
            _geodeWorldgenRegistrar = new GeodeWorldgenRegistrar(effectiveDefinitions, Logger);
            _geodeWorldgenRegistrar.Register();
            _workshopOperationRegistrar = new WorkshopOperationRegistrar(Logger);
            _workshopOperationRegistrar.Register();
            _shardRecipeRegistrar = new ShardRecipeRegistrar(Logger);
            _shardRecipeRegistrar.Register();

            Logger.LogInfo(
                $"{PluginName} {PluginVersion} loaded definition schema {effectiveDefinitions.SchemaVersion}. " +
                $"Baseline fingerprint {baselineDefinitions.Fingerprint}; effective fingerprint {effectiveDefinitions.Fingerprint}. " +
                "Definition authority synchronization, session-scoped geode/refinement/socket/extraction replay protection, definition-driven intact geode items, " +
                "fingerprinted geode placement configuration, additive geode vegetation registration, local-host geology workshop operations, " +
                "deterministic Earth shard recombination, and per-item socket damage/armor/block/carry/mining effect adapters are configured. " +
                "Remote-client operation RPC, socket workstation mutation UI, knockback/stagger runtime channels, and persistence multiplayer validation remain gated pending implementation/validation.");
        }
        catch (Exception exception)
        {
            Logger.LogFatal($"{PluginName} definition bootstrap failed: {exception}");
            throw;
        }
    }

    private void OnDestroy()
    {
        _shardRecipeRegistrar?.Dispose();
        _workshopOperationRegistrar?.Dispose();
        _earthContentRegistrar?.Dispose();
        _workshopRegistrar?.Dispose();
        _geodeWorldgenRegistrar?.Dispose();
        _geodeItemRegistrar?.Dispose();
        _harmony?.UnpatchSelf();
    }
}
