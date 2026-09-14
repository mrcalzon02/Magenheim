using System;
using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using Jotunn.Managers;
using Magenheim.Core.Definitions;
using Magenheim.Core.Worldgen;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Creates the dedicated networked/destructible world-object prefab for each authoritative
/// geode definition. Inventory geodes and mineable world objects deliberately remain separate
/// prefab identities: the world object drops exactly one intact inventory geode when destroyed.
/// </summary>
internal sealed class GeodeWorldPrefabRegistrar : IDisposable
{
    private const string VanillaWorldBasePrefab = "Rock_4";
    private const float DefaultHealth = 20f;
    private const int DefaultMinToolTier = 0;

    private readonly MagenheimDefinitionSet _definitions;
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal GeodeWorldPrefabRegistrar(MagenheimDefinitionSet definitions, ManualLogSource log)
    {
        _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal void Register()
    {
        if (_subscribed || _registered)
            return;

        PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
        _subscribed = true;
    }

    public void Dispose()
    {
        if (!_subscribed)
            return;

        PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        _subscribed = false;
    }

    private void OnVanillaPrefabsAvailable()
    {
        if (_registered)
            return;

        try
        {
            foreach (var geode in _definitions.Geodes)
                RegisterWorldPrefab(geode);

            _registered = true;
            _log.LogInfo($"Registered {_definitions.Geodes.Count} dedicated Magenheim geode world prefab(s).");
        }
        catch (Exception exception)
        {
            _log.LogError($"Magenheim geode world-prefab registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void RegisterWorldPrefab(GeodeDefinition geode)
    {
        var worldPrefabName = GeodeWorldPrefabIdentity.FromItemPrefabName(geode.PrefabName);
        if (PrefabManager.Instance.GetPrefab(worldPrefabName) != null)
            throw new InvalidOperationException($"Geode world prefab '{worldPrefabName}' is already registered.");

        var intactGeodePrefab = PrefabManager.Instance.GetPrefab(geode.PrefabName)
            ?? throw new InvalidOperationException(
                $"Intact geode item prefab '{geode.PrefabName}' must be registered before '{worldPrefabName}'.");

        var worldPrefab = PrefabManager.Instance.CreateClonedPrefab(worldPrefabName, VanillaWorldBasePrefab)
            ?? throw new InvalidOperationException(
                $"Unable to clone vanilla world prefab '{VanillaWorldBasePrefab}' for '{worldPrefabName}'.");

        ConfigurePersistentNetworkState(worldPrefab, worldPrefabName);
        ConfigureDestructible(worldPrefab, worldPrefabName);
        ConfigureSingleIntactGeodeDrop(worldPrefab, intactGeodePrefab, worldPrefabName);

        PrefabManager.Instance.AddPrefab(worldPrefab);
    }

    private static void ConfigurePersistentNetworkState(GameObject prefab, string prefabName)
    {
        var zNetView = prefab.GetComponent<ZNetView>()
            ?? throw new InvalidOperationException($"Geode world prefab '{prefabName}' has no ZNetView component.");

        SetRequiredField(zNetView, "m_persistent", true, prefabName);
    }

    private static void ConfigureDestructible(GameObject prefab, string prefabName)
    {
        var destructible = prefab.GetComponent<Destructible>()
            ?? throw new InvalidOperationException($"Geode world prefab '{prefabName}' has no Destructible component.");

        SetRequiredField(destructible, "m_health", DefaultHealth, prefabName);
        SetRequiredField(destructible, "m_minToolTier", DefaultMinToolTier, prefabName);
    }

    private static void ConfigureSingleIntactGeodeDrop(GameObject prefab, GameObject intactGeodePrefab, string prefabName)
    {
        var dropOnDestroyed = prefab.GetComponent<DropOnDestroyed>()
            ?? throw new InvalidOperationException($"Geode world prefab '{prefabName}' has no DropOnDestroyed component.");

        var dropTableField = GetRequiredField(dropOnDestroyed.GetType(), "m_dropWhenDestroyed", prefabName);
        var dropTable = dropTableField.GetValue(dropOnDestroyed)
            ?? throw new InvalidOperationException($"Geode world prefab '{prefabName}' has no destruction drop table.");

        SetRequiredField(dropTable, "m_dropMin", 1, prefabName);
        SetRequiredField(dropTable, "m_dropMax", 1, prefabName);
        SetRequiredField(dropTable, "m_dropChance", 1f, prefabName);
        SetRequiredField(dropTable, "m_onePerPlayer", false, prefabName);

        var dropsField = GetRequiredField(dropTable.GetType(), "m_drops", prefabName);
        if (!(dropsField.GetValue(dropTable) is IList drops) || drops.Count == 0)
            throw new InvalidOperationException(
                $"Geode world prefab '{prefabName}' base destruction drop table exposes no reusable drop entry.");

        while (drops.Count > 1)
            drops.RemoveAt(drops.Count - 1);

        var firstDrop = drops[0]
            ?? throw new InvalidOperationException($"Geode world prefab '{prefabName}' has a null destruction drop entry.");

        SetRequiredField(firstDrop, "m_item", intactGeodePrefab, prefabName);
        SetRequiredField(firstDrop, "m_stackMin", 1, prefabName);
        SetRequiredField(firstDrop, "m_stackMax", 1, prefabName);
        SetRequiredField(firstDrop, "m_weight", 1f, prefabName);
    }

    private static FieldInfo GetRequiredField(Type type, string fieldName, string prefabName)
    {
        return type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Geode world prefab '{prefabName}' requires runtime field '{type.FullName}.{fieldName}', but it was not found.");
    }

    private static void SetRequiredField(object target, string fieldName, object value, string prefabName)
    {
        var field = GetRequiredField(target.GetType(), fieldName, prefabName);
        field.SetValue(target, value);
    }
}
