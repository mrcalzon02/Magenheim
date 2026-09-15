using System;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Registers eight color-locked late-game Crystal Beds. Each water-filled basin cultivates only
/// its matching alignment and uses Valheim's persistent producer state so growth survives unloads.
/// </summary>
internal sealed class CrystalBedRegistrar : IDisposable
{
    internal const int DefaultGrowthSecondsPerRoughCrystal = 900;
    internal const int DefaultStoredCrystalLimit = 8;

    private readonly ManualLogSource _log;
    private readonly int _growthSecondsPerRoughCrystal;
    private readonly int _storedCrystalLimit;
    private bool _subscribed;
    private bool _registered;

    internal CrystalBedRegistrar(ManualLogSource log, ConfigFile config)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        if (config is null) throw new ArgumentNullException(nameof(config));

        _growthSecondsPerRoughCrystal = config.Bind(
            "Balance.CrystalBed",
            "GrowthSecondsPerRoughCrystal",
            DefaultGrowthSecondsPerRoughCrystal,
            new ConfigDescription(
                "Seconds required for each alignment-specific Crystal Bed to grow one matching Rough crystal. Requires restart because producer prefabs are configured during bootstrap.",
                new AcceptableValueRange<int>(30, 86400))).Value;

        _storedCrystalLimit = config.Bind(
            "Balance.CrystalBed",
            "MaxStoredRoughCrystals",
            DefaultStoredCrystalLimit,
            new ConfigDescription(
                "Maximum matching Rough crystals a Crystal Bed can accumulate before harvesting is required. Requires restart because producer prefabs are configured during bootstrap.",
                new AcceptableValueRange<int>(1, 100))).Value;
    }

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterBeds;
        _subscribed = true;
    }

    private void RegisterBeds()
    {
        if (_registered) return;
        try
        {
            if (PrefabManager.Instance.GetPrefab("piece_beehive") is null)
                throw new InvalidOperationException("Vanilla persistent producer source 'piece_beehive' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(CrystalEnchantingDaisRegistrar.PrefabName) is null)
                throw new InvalidOperationException("Crystal Enchanting Dais must be registered before Crystal Bed construction is enabled.");

            var count = 0;
            foreach (ElementalAlignment element in Enum.GetValues(typeof(ElementalAlignment)))
            {
                RegisterBed(element);
                count++;
            }

            _registered = true;
            _log.LogInfo(
                $"Registered {count} alignment-locked Crystal Beds; each requires the Crystal Enchanting Dais, grows one matching Rough crystal every {_growthSecondsPerRoughCrystal / 60f:0.##} minutes, and stores up to {_storedCrystalLimit}.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Crystal Bed registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private void RegisterBed(ElementalAlignment element)
    {
        var prefabName = $"Magenheim_CrystalBed_{element}";
        var roughPrefab = $"Magenheim_Crystal_{element}_Rough";
        var masterPrefab = $"Magenheim_Crystal_{element}_Master";

        if (PrefabManager.Instance.GetPrefab(prefabName))
            throw new InvalidOperationException($"Cannot replace occupied Crystal Bed identity '{prefabName}'.");

        var rough = PrefabManager.Instance.GetPrefab(roughPrefab)
            ?? throw new InvalidOperationException($"Crystal Bed requires registered Rough crystal '{roughPrefab}'.");
        if (PrefabManager.Instance.GetPrefab(masterPrefab) is null)
            throw new InvalidOperationException($"Crystal Bed requires registered Master crystal '{masterPrefab}'.");
        var roughDrop = rough.GetComponent<ItemDrop>()
            ?? throw new InvalidOperationException($"Rough crystal '{roughPrefab}' has no ItemDrop component.");

        var config = new PieceConfig
        {
            Name = $"{element} Crystal Bed",
            Description =
                $"A high-tier mineral cultivation basin filled with {element}-aligned solution and seeded crystal growths. " +
                $"It is focused through a Crystal Enchanting Dais and slowly grows only {element} Rough crystals.",
            PieceTable = "Hammer",
            Category = "Crafting",
            CraftingStation = CrystalEnchantingDaisRegistrar.PrefabName,
            Icon = CrystalBedIcons.Icon(element),
            Requirements = new[]
            {
                Cost("BlackMarble", 20),
                Cost("YggdrasilWood", 10),
                Cost("RefinedEitr", 15),
                Cost("Sap", 10),
                Cost(masterPrefab, 1),
            }
        };

        var custom = new CustomPiece(prefabName, "piece_beehive", config);
        custom.Piece.m_dlc = string.Empty;
        var prefab = custom.PiecePrefab;
        var visual = CrystalBedVisuals.Apply(prefab, element);
        InheritedPrefabSanitizer.DisableInheritedAudio(prefab);

        var producer = prefab.GetComponent<Beehive>()
            ?? throw new InvalidOperationException($"Crystal Bed '{prefabName}' lost its persistent producer component.");
        producer.m_name = $"{element} Crystal Bed";
        producer.m_honeyItem = roughDrop;
        producer.m_secPerUnit = _growthSecondsPerRoughCrystal;
        producer.m_maxHoney = _storedCrystalLimit;
        producer.m_biome = Heightmap.Biome.All;
        producer.m_effectOnlyInDaylight = false;
        producer.m_maxCover = 0f;
        producer.m_extractText = "Harvest Rough crystals";
        producer.m_checkText = "Check crystal growth";
        producer.m_areaText = "The mineral solution cannot stabilize here";
        producer.m_freespaceText = "The Crystal Bed needs open space";
        producer.m_sleepText = "The crystal solution is dormant";
        producer.m_happyText = $"The {element} crystal growths are forming slowly";
        producer.m_spawnEffect = new EffectList();
        var ambient = new GameObject("magenheim.crystal-bed.ambient") { layer = prefab.layer };
        ambient.transform.SetParent(prefab.transform, false);
        producer.m_beeEffect = ambient;

        ConfigureCollider(prefab);
        var wear = prefab.GetComponent<WearNTear>();
        if (wear)
        {
            wear.m_health = Mathf.Max(wear.m_health, 1200f);
            wear.m_new = visual;
        }

        if (!PieceManager.Instance.AddPiece(custom))
            throw new InvalidOperationException($"Jotunn refused Crystal Bed piece '{prefabName}'.");
    }

    private static void ConfigureCollider(GameObject prefab)
    {
        var solids = prefab.GetComponentsInChildren<Collider>(true).Where(collider => !collider.isTrigger).ToArray();
        var layer = solids.Length > 0 ? solids[0].gameObject.layer : prefab.layer;
        foreach (var collider in solids) collider.enabled = false;

        var holder = new GameObject("magenheim.crystal-bed.collision") { layer = layer };
        holder.transform.SetParent(prefab.transform, false);
        var box = holder.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, .43f, 0f);
        box.size = new Vector3(3.10f, .86f, 2.20f);
    }

    private static RequirementConfig Cost(string item, int amount) =>
        new RequirementConfig(item, amount, 0, true);

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterBeds;
        _subscribed = false;
    }
}
