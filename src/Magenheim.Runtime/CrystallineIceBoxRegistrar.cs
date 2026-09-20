using System;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers a persistent crystalline refrigeration chest that slowly condenses vanilla Ice.
/// Production uses Valheim's world-time producer state so progress survives unloaded zones.
/// </summary>
internal sealed class CrystallineIceBoxRegistrar : IDisposable
{
    internal const string PrefabName = "Magenheim_CrystallineIceBox";
    internal const int DefaultGrowthSecondsPerIce = 600;
    internal const int DefaultStoredIceLimit = 10;

    private readonly ManualLogSource _log;
    private readonly int _growthSecondsPerIce;
    private readonly int _storedIceLimit;
    private bool _subscribed;
    private bool _registered;

    internal CrystallineIceBoxRegistrar(ManualLogSource log, ConfigFile config)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        if (config is null) throw new ArgumentNullException(nameof(config));

        _growthSecondsPerIce = config.Bind(
            "Balance.CrystallineIceBox",
            "GrowthSecondsPerIce",
            DefaultGrowthSecondsPerIce,
            new ConfigDescription(
                "Seconds required for the Crystalline Ice Box to condense one Ice. Requires restart because the producer prefab is configured during bootstrap.",
                new AcceptableValueRange<int>(30, 86400))).Value;

        _storedIceLimit = config.Bind(
            "Balance.CrystallineIceBox",
            "MaxStoredIce",
            DefaultStoredIceLimit,
            new ConfigDescription(
                "Maximum Ice the Crystalline Ice Box can accumulate before harvesting is required. Requires restart because the producer prefab is configured during bootstrap.",
                new AcceptableValueRange<int>(1, 100))).Value;
    }

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterIceBox;
        _subscribed = true;
    }

    private void RegisterIceBox()
    {
        if (_registered) return;
        try
        {
            if (PrefabManager.Instance.GetPrefab(PrefabName))
                throw new InvalidOperationException($"Cannot replace occupied Crystalline Ice Box identity '{PrefabName}'.");
            if (PrefabManager.Instance.GetPrefab("piece_beehive") is null)
                throw new InvalidOperationException("Vanilla persistent producer source 'piece_beehive' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(CrystalEnchantingDaisRegistrar.PrefabName) is null)
                throw new InvalidOperationException("Crystal Enchanting Dais must be registered before Crystalline Ice Box construction is enabled.");
            if (PrefabManager.Instance.GetPrefab("Magenheim_Crystal_Frost_Master") is null)
                throw new InvalidOperationException("Crystalline Ice Box requires the registered Frost Master crystal.");

            var icePrefab = PrefabManager.Instance.GetPrefab("Ice")
                ?? throw new InvalidOperationException("Vanilla Ice prefab 'Ice' is unavailable.");
            var iceDrop = icePrefab.GetComponent<ItemDrop>()
                ?? throw new InvalidOperationException("Vanilla Ice prefab has no ItemDrop component.");

            var pieceConfig = new PieceConfig
            {
                Name = "Crystalline Ice Box",
                Description =
                    "A black-stone refrigeration chest wrapped in iron and Frost crystal. " +
                    "Focused through a Crystal Enchanting Dais, its cold lattice slowly condenses harvestable Ice from ambient moisture.",
                PieceTable = "Hammer",
                Category = "Crafting",
                CraftingStation = CrystalEnchantingDaisRegistrar.PrefabName,
                Icon = EarthAssets.Icon(CrystallineIceBoxVisuals.ModelId),
                Requirements = new[]
                {
                    Cost("BlackMarble", 12),
                    Cost("Iron", 8),
                    Cost("Ice", 5),
                    Cost("Magenheim_Crystal_Frost_Master", 1),
                }
            };

            var custom = new CustomPiece(PrefabName, "piece_beehive", pieceConfig);
            custom.Piece.m_dlc = string.Empty;
            var prefab = custom.PiecePrefab;
            var visual = CrystallineIceBoxVisuals.Apply(prefab);
            InheritedPrefabSanitizer.DisableInheritedAudio(prefab);

            var producer = prefab.GetComponent<Beehive>()
                ?? throw new InvalidOperationException("Crystalline Ice Box lost its persistent producer component.");
            producer.m_name = "Crystalline Ice Box";
            producer.m_honeyItem = iceDrop;
            producer.m_secPerUnit = _growthSecondsPerIce;
            producer.m_maxHoney = _storedIceLimit;
            producer.m_biome = Heightmap.Biome.All;
            producer.m_effectOnlyInDaylight = false;
            producer.m_maxCover = 0f;
            producer.m_extractText = "Harvest Ice";
            producer.m_checkText = "Check ice formation";
            producer.m_areaText = "The cold lattice cannot stabilize here";
            producer.m_freespaceText = "The Ice Box needs clear ventilation";
            producer.m_sleepText = "The Frost crystal lattice is dormant";
            producer.m_happyText = "The cold lattice is slowly condensing fresh Ice";
            producer.m_spawnEffect = new EffectList();

            var ambient = new GameObject("magenheim.crystalline-ice-box.ambient") { layer = prefab.layer };
            ambient.transform.SetParent(prefab.transform, false);
            producer.m_beeEffect = ambient;

            var harvestPoint = new GameObject("magenheim.crystalline-ice-box.harvest-point") { layer = prefab.layer };
            harvestPoint.transform.SetParent(prefab.transform, false);
            harvestPoint.transform.localPosition = new Vector3(0f, 1.05f, -.88f);
            producer.m_spawnPoint = harvestPoint.transform;
            producer.m_coverPoint = harvestPoint.transform;

            ConfigureCollider(prefab);
            PlacementSnapAuthority.FitRectangle(prefab, 2.55f, 1.62f);
            var wear = prefab.GetComponent<WearNTear>();
            if (wear)
            {
                wear.m_health = Mathf.Max(wear.m_health, 1100f);
                wear.m_new = visual;
            }

            if (!PieceManager.Instance.AddPiece(custom))
                throw new InvalidOperationException($"Jotunn refused Crystalline Ice Box piece '{PrefabName}'.");

            _registered = true;
            _log.LogInfo(
                $"Registered Crystalline Ice Box behind the Crystal Enchanting Dais: one Ice every {_growthSecondsPerIce / 60f:0.##} minutes, storing up to {_storedIceLimit}.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Crystalline Ice Box registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void ConfigureCollider(GameObject prefab)
    {
        var solids = prefab.GetComponentsInChildren<Collider>(true)
            .Where(collider => !collider.isTrigger)
            .ToArray();
        var layer = solids.Length > 0 ? solids[0].gameObject.layer : prefab.layer;
        foreach (var collider in solids) collider.enabled = false;

        var holder = new GameObject("magenheim.crystalline-ice-box.collision") { layer = layer };
        holder.transform.SetParent(prefab.transform, false);
        var box = holder.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, .72f, 0f);
        box.size = new Vector3(2.55f, 1.44f, 1.62f);
    }

    private static RequirementConfig Cost(string item, int amount) =>
        new RequirementConfig(item, amount, 0, true);

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterIceBox;
        _subscribed = false;
    }
}
