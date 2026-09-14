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
    internal const string PluginVersion = "0.0.32";

    private RuntimeServices? _services;
    private DefinitionAuthoritySynchronizer? _authoritySynchronizer;
    private GeodeItemRegistrar? _geodeItemRegistrar;
    private GeodeWorldgenRegistrar? _geodeWorldgenRegistrar;
    private EarthContentRegistrar? _earthContentRegistrar;
    private WorkshopRegistrar? _workshopRegistrar;
    private FurnitureRegistrar? _furnitureRegistrar;
    private WorkshopOperationRegistrar? _workshopOperationRegistrar;
    private ShardRecipeRegistrar? _shardRecipeRegistrar;
    private FireStaffRegistrar? _fireStaffRegistrar;
    private FrostStaffRegistrar? _frostStaffRegistrar;
    private StormStaffRegistrar? _stormStaffRegistrar;
    private EarthStaffRegistrar? _earthStaffRegistrar;
    private VenomStaffRegistrar? _venomStaffRegistrar;
    private RadianceStaffRegistrar? _radianceStaffRegistrar;
    private SeidrStaffRegistrar? _seidrStaffRegistrar;
    private SpiritStaffRegistrar? _spiritStaffRegistrar;
    private SocketWorkstationOverlay? _socketWorkstationOverlay;
    private Harmony? _harmony;

    private void Awake()
    {
        try
        {
            ModQuery.Enable();

            var assemblyDirectory = Path.GetDirectoryName(typeof(MagenheimPlugin).Assembly.Location)
                ?? throw new InvalidOperationException("Unable to determine the Magenheim plugin directory.");
            var definitionPath = Path.Combine(assemblyDirectory, "default-data", "foundation.json");
            var baselineDefinitions = MagenheimDefinitionLoader.LoadFromFile(definitionPath);
            var effectiveDefinitions = MagenheimBalanceConfig.Apply(Config, baselineDefinitions);
            var socketPolicy = SocketCompatibilityConfig.Read(Config);

            _services = RuntimeServices.Create(effectiveDefinitions, socketPolicy);
            _authoritySynchronizer = new DefinitionAuthoritySynchronizer(
                effectiveDefinitions,
                socketPolicy,
                _services.GeodeOpeningOperations,
                _services.RefinementOperations,
                _services.SocketOperations,
                _services.SocketExtractionOperations,
                Logger);

            WorkshopOperationCatalog.Configure(effectiveDefinitions);
            WorkshopOperationsRuntime.Configure(_services, _authoritySynchronizer, Logger);
            WorkshopOperationRpc.Register(_services, _authoritySynchronizer, Logger);
            SocketEffectsRuntime.Configure(effectiveDefinitions, Logger);
            SocketTooltipRuntime.Configure(effectiveDefinitions);

            _socketWorkstationOverlay = gameObject.AddComponent<SocketWorkstationOverlay>();
            _socketWorkstationOverlay.Configure(_services, _authoritySynchronizer, Logger);

            _harmony = new Harmony(PluginGuid + ".gameplay");
            _harmony.PatchAll(typeof(WorkshopCraftingPatch));
            _harmony.PatchAll(typeof(SocketDamagePatch));
            _harmony.PatchAll(typeof(SocketArmorPatch));
            _harmony.PatchAll(typeof(SocketBlockPowerPatch));
            _harmony.PatchAll(typeof(SocketCarryWeightPatch));
            _harmony.PatchAll(typeof(SocketTooltipPatch));
            _harmony.PatchAll(typeof(EarthAbilityHitPatch));

            _earthContentRegistrar = new EarthContentRegistrar(Logger);
            _earthContentRegistrar.Register();
            _workshopRegistrar = new WorkshopRegistrar(Logger);
            _workshopRegistrar.Register();
            _furnitureRegistrar = new FurnitureRegistrar(Logger);
            _furnitureRegistrar.Register();
            _geodeItemRegistrar = new GeodeItemRegistrar(effectiveDefinitions, Logger);
            _geodeItemRegistrar.Register();
            _geodeWorldgenRegistrar = new GeodeWorldgenRegistrar(effectiveDefinitions, Logger);
            _geodeWorldgenRegistrar.Register();
            _workshopOperationRegistrar = new WorkshopOperationRegistrar(Logger);
            _workshopOperationRegistrar.Register();
            _shardRecipeRegistrar = new ShardRecipeRegistrar(Logger);
            _shardRecipeRegistrar.Register();
            _fireStaffRegistrar = new FireStaffRegistrar(Logger);
            _fireStaffRegistrar.Register();
            _frostStaffRegistrar = new FrostStaffRegistrar(Logger);
            _frostStaffRegistrar.Register();
            _stormStaffRegistrar = new StormStaffRegistrar(Logger);
            _stormStaffRegistrar.Register();
            _earthStaffRegistrar = new EarthStaffRegistrar(Logger);
            _earthStaffRegistrar.Register();
            _venomStaffRegistrar = new VenomStaffRegistrar(Logger);
            _venomStaffRegistrar.Register();
            _radianceStaffRegistrar = new RadianceStaffRegistrar(Logger);
            _radianceStaffRegistrar.Register();
            _seidrStaffRegistrar = new SeidrStaffRegistrar(Logger);
            _seidrStaffRegistrar.Register();
            _spiritStaffRegistrar = new SpiritStaffRegistrar(Logger);
            _spiritStaffRegistrar.Register();

            Logger.LogInfo(
                $"{PluginName} {PluginVersion} loaded definition schema {effectiveDefinitions.SchemaVersion}. " +
                $"Baseline definition fingerprint {baselineDefinitions.Fingerprint}; effective definition fingerprint {effectiveDefinitions.Fingerprint}; " +
                $"effective gameplay authority {_authoritySynchronizer.GameplayFingerprint}. " +
                "Player content includes eight biome geodes, eight five-tier elemental crystal families, the full geology workstation refinement ladder, " +
                "a 10-piece geology/crystal furniture collection with dedicated Hammer icons, resolved socket bonuses, and eight four-tier staff families with distinct runtime effects. " +
                "Fire owns fireburst/scorch/meteor burn terrain; Frost owns Brittle and Rime fields; Storm owns secondary discharges; " +
                "Earth owns Fractured/Shattered Armor and Tremor; Venom owns corrosion; Radiance owns hard-light/flash/sanctuary payloads; " +
                "Seidr owns binding hexes; and Spirit owns spectral echo fields.");
        }
        catch (Exception exception)
        {
            Logger.LogFatal($"{PluginName} definition bootstrap failed: {exception}");
            throw;
        }
    }

    private void OnDestroy()
    {
        _spiritStaffRegistrar?.Dispose();
        _seidrStaffRegistrar?.Dispose();
        _radianceStaffRegistrar?.Dispose();
        _venomStaffRegistrar?.Dispose();
        _earthStaffRegistrar?.Dispose();
        _stormStaffRegistrar?.Dispose();
        _frostStaffRegistrar?.Dispose();
        _fireStaffRegistrar?.Dispose();
        _shardRecipeRegistrar?.Dispose();
        _workshopOperationRegistrar?.Dispose();
        _furnitureRegistrar?.Dispose();
        _earthContentRegistrar?.Dispose();
        _workshopRegistrar?.Dispose();
        _geodeWorldgenRegistrar?.Dispose();
        _geodeItemRegistrar?.Dispose();
        if (_socketWorkstationOverlay is not null)
            Destroy(_socketWorkstationOverlay);
        _harmony?.UnpatchSelf();
    }
}
