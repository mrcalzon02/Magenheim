using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Magenheim.Runtime;

/// <summary>
/// Deterministic workshop recipes that do not require a transactional failure roll.
/// Probabilistic geode opening/refinement remain in WorkshopOperations; this registrar
/// only owns static material conversions that vanilla/Jotunn crafting can execute safely.
/// </summary>
internal sealed class ShardRecipeRegistrar : IDisposable
{
    private const string EarthShardPrefab = "Magenheim_Shard_Earth";
    private const string EarthSimplePrefab = "Magenheim_Crystal_Earth_Simple";
    private const string EarthShardRecipe = "Magenheim_Recipe_EarthShards_To_Simple";
    internal const int ShardsPerSimpleCrystal = 5;

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal ShardRecipeRegistrar(ManualLogSource log) =>
        _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterRecipes;
        _subscribed = true;
    }

    private void RegisterRecipes()
    {
        if (_registered) return;
        try
        {
            if (PrefabManager.Instance.GetPrefab(EarthShardPrefab) is null)
                throw new InvalidOperationException($"Shard recipe source prefab '{EarthShardPrefab}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(EarthSimplePrefab) is null)
                throw new InvalidOperationException($"Shard recipe output prefab '{EarthSimplePrefab}' is unavailable.");
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must be registered before shard recombination recipes.");

            var config = new RecipeConfig
            {
                Name = EarthShardRecipe,
                Item = EarthSimplePrefab,
                Amount = 1,
                CraftingStation = WorkshopRegistrar.StationPrefab,
                MinStationLevel = 1,
                Enabled = true,
            };
            config.AddRequirement(EarthShardPrefab, ShardsPerSimpleCrystal);

            if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
                throw new InvalidOperationException($"Jotunn refused shard recombination recipe '{EarthShardRecipe}'.");

            _registered = true;
            _log.LogInfo($"Registered Earth shard recombination: {ShardsPerSimpleCrystal} Earth Crystal Shards -> 1 Simple Earth Crystal.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Shard recombination recipe registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterRecipes;
        _subscribed = false;
    }
}
