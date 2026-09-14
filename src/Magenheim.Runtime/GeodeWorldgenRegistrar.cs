using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Definitions;
using Magenheim.Core.Worldgen;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Creates and additively registers dedicated mineable geode world prefabs as Jotunn vegetation.
/// Planning and collision policy remain owned by Magenheim.Core; this runtime class only executes
/// approved Add decisions and never edits/removes vanilla or foreign vegetation.
/// </summary>
internal sealed class GeodeWorldgenRegistrar : IDisposable
{
    private const string VanillaWorldBasePrefab = "Rock_4";
    private const float DefaultHealth = 20f;
    private const int DefaultMinToolTier = 0;

    private readonly MagenheimDefinitionSet _definitions;
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal GeodeWorldgenRegistrar(MagenheimDefinitionSet definitions, ManualLogSource log)
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
            var desiredPlan = DefinitionWorldgenPlanner.Build(
                _definitions,
                Array.Empty<ObservedWorldgenRegistration>());

            if (desiredPlan.HasErrors)
                throw new InvalidOperationException(BuildPlanError("Validated definitions produced an invalid desired worldgen plan", desiredPlan));

            var desired = desiredPlan.Entries.Select(entry => entry.Desired).ToArray();
            var observed = JotunnWorldgenAdapter.ObserveDesiredHostIdentities(desired);
            var finalPlan = DefinitionWorldgenPlanner.Build(_definitions, observed);

            if (finalPlan.HasErrors)
                throw new InvalidOperationException(BuildPlanError("Worldgen compatibility planning rejected registration", finalPlan));

            foreach (var skipped in finalPlan.Entries.Where(entry => entry.Action == WorldgenPlanAction.Skip))
            {
                _log.LogWarning(
                    $"Skipped Magenheim worldgen addition '{skipped.Desired.RegistrationKey}' / '{skipped.Desired.PrefabName}': {skipped.Diagnostic}");
            }

            var additions = finalPlan.Additions.ToArray();
            PreflightAdditions(additions);

            foreach (var addition in additions)
                RegisterWorldVegetation(addition);

            _registered = true;
            _log.LogInfo(
                $"Registered {additions.Length} Magenheim geode vegetation addition(s); " +
                $"{finalPlan.Entries.Count(entry => entry.Action == WorldgenPlanAction.Skip)} skipped by compatibility policy.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Magenheim geode worldgen registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private void PreflightAdditions(WorldgenPlanEntry[] additions)
    {
        if (PrefabManager.Instance.GetPrefab(VanillaWorldBasePrefab) is null)
            throw new InvalidOperationException($"Vanilla geode world base prefab '{VanillaWorldBasePrefab}' is unavailable.");

        foreach (var addition in additions)
        {
            var geode = FindGeode(addition.Desired.RegistrationKey);
            if (PrefabManager.Instance.GetPrefab(geode.PrefabName) is null)
            {
                throw new InvalidOperationException(
                    $"Intact geode item prefab '{geode.PrefabName}' must be registered before worldgen addition '{addition.Desired.RegistrationKey}'.");
            }

            if (PrefabManager.Instance.GetPrefab(addition.Desired.PrefabName) is not null)
            {
                throw new InvalidOperationException(
                    $"Worldgen addition '{addition.Desired.RegistrationKey}' cannot safely add prefab '{addition.Desired.PrefabName}' because that prefab identity already exists.");
            }

            geode.Placement.Validate(geode.Id);
            _ = JotunnWorldgenAdapter.MapBiome(addition.Desired.Biome
                ?? throw new InvalidOperationException($"Worldgen addition '{addition.Desired.RegistrationKey}' has no biome."));
            _ = JotunnWorldgenAdapter.MapArea(addition.NormalizedArea);
        }
    }

    private void RegisterWorldVegetation(WorldgenPlanEntry addition)
    {
        var geode = FindGeode(addition.Desired.RegistrationKey);
        var placement = geode.Placement;
        var intactGeodePrefab = PrefabManager.Instance.GetPrefab(geode.PrefabName)
            ?? throw new InvalidOperationException($"Intact geode item prefab '{geode.PrefabName}' disappeared after preflight.");

        var worldPrefab = PrefabManager.Instance.CreateClonedPrefab(
            addition.Desired.PrefabName,
            VanillaWorldBasePrefab)
            ?? throw new InvalidOperationException(
                $"Unable to clone vanilla world prefab '{VanillaWorldBasePrefab}' for '{addition.Desired.PrefabName}'.");

        ConfigurePersistentNetworkState(worldPrefab, addition.Desired.PrefabName);
        ConfigureDestructible(worldPrefab, addition.Desired.PrefabName);
        ConfigureSingleIntactGeodeDrop(worldPrefab, intactGeodePrefab, addition.Desired.PrefabName);

        var vegetationConfig = new VegetationConfig
        {
            Biome = JotunnWorldgenAdapter.MapBiome(addition.Desired.Biome!),
            BiomeArea = JotunnWorldgenAdapter.MapArea(addition.NormalizedArea),
            BlockCheck = placement.BlockCheck,
            ForcePlacement = placement.ForcePlacement,
            Min = ToRuntimeFloat(placement.MinPerZone, nameof(placement.MinPerZone)),
            Max = ToRuntimeFloat(placement.MaxPerZone, nameof(placement.MaxPerZone)),
            MinAltitude = ToRuntimeFloat(placement.MinAltitude, nameof(placement.MinAltitude)),
            MaxAltitude = ToRuntimeFloat(placement.MaxAltitude, nameof(placement.MaxAltitude)),
            MinOceanDepth = ToRuntimeFloat(placement.MinOceanDepth, nameof(placement.MinOceanDepth)),
            MaxOceanDepth = ToRuntimeFloat(placement.MaxOceanDepth, nameof(placement.MaxOceanDepth)),
            MinTerrainDelta = ToRuntimeFloat(placement.MinTerrainDelta, nameof(placement.MinTerrainDelta)),
            MaxTerrainDelta = ToRuntimeFloat(placement.MaxTerrainDelta, nameof(placement.MaxTerrainDelta)),
            TerrainDeltaRadius = ToRuntimeFloat(placement.TerrainDeltaRadius, nameof(placement.TerrainDeltaRadius)),
            MinTilt = ToRuntimeFloat(placement.MinTilt, nameof(placement.MinTilt)),
            MaxTilt = ToRuntimeFloat(placement.MaxTilt, nameof(placement.MaxTilt)),
            InForest = placement.InForest,
            ForestThresholdMin = ToRuntimeFloat(placement.ForestThresholdMin, nameof(placement.ForestThresholdMin)),
            ForestThresholdMax = ToRuntimeFloat(placement.ForestThresholdMax, nameof(placement.ForestThresholdMax)),
            ScaleMin = ToRuntimeFloat(placement.ScaleMin, nameof(placement.ScaleMin)),
            ScaleMax = ToRuntimeFloat(placement.ScaleMax, nameof(placement.ScaleMax)),
            GroupSizeMin = placement.GroupSizeMin,
            GroupSizeMax = placement.GroupSizeMax,
            GroupRadius = ToRuntimeFloat(placement.GroupRadius, nameof(placement.GroupRadius)),
            GroundOffset = ToRuntimeFloat(placement.GroundOffset, nameof(placement.GroundOffset)),
        };

        var customVegetation = new CustomVegetation(worldPrefab, fixReference: false, vegetationConfig);
        if (!ZoneManager.Instance.AddCustomVegetation(customVegetation))
        {
            throw new InvalidOperationException(
                $"Jotunn refused additive vegetation registration for '{addition.Desired.PrefabName}'. Existing host content was not modified.");
        }

        _log.LogInfo(
            $"Added geode vegetation '{addition.Desired.PrefabName}' for biome '{addition.Desired.Biome}', " +
            $"area '{addition.NormalizedArea}', configured max-per-zone/chance {placement.MaxPerZone:0.###}.");
    }

    private GeodeDefinition FindGeode(string registrationKey)
    {
        return _definitions.Geodes.SingleOrDefault(
                geode => string.Equals(geode.Id, registrationKey, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Worldgen plan references unknown geode registration key '{registrationKey}'.");
    }

    private static string BuildPlanError(string prefix, WorldgenAdditionPlan plan)
    {
        var diagnostics = plan.Entries
            .Where(entry => entry.Action == WorldgenPlanAction.Error)
            .Select(entry => $"{entry.Desired.RegistrationKey}: {entry.Diagnostic}");
        return prefix + ": " + string.Join("; ", diagnostics);
    }

    private static float ToRuntimeFloat(double value, string field)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < -float.MaxValue || value > float.MaxValue)
            throw new InvalidOperationException($"Placement field '{field}' cannot be represented by Jotunn's float API.");
        return (float)value;
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

    private static void ConfigureSingleIntactGeodeDrop(
        GameObject prefab,
        GameObject intactGeodePrefab,
        string prefabName)
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
        {
            throw new InvalidOperationException(
                $"Geode world prefab '{prefabName}' base destruction drop table exposes no reusable drop entry.");
        }

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
