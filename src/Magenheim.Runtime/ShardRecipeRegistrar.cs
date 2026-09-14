using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;

namespace Magenheim.Runtime;

/// <summary>
/// Deterministic workshop recipes that do not require a transactional failure roll.
/// Probabilistic geode opening/refinement remain in WorkshopOperations; this registrar
/// only owns static material conversions that vanilla/Jotunn crafting can execute safely.
/// </summary>
internal sealed class ShardRecipeRegistrar : IDisposable
{
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
            if (PrefabManager.Instance.GetPrefab(WorkshopRegistrar.StationPrefab) is null)
                throw new InvalidOperationException("Geologist's Workstation must be registered before shard recombination recipes.");

            var count = 0;
            foreach (ElementalAlignment element in Enum.GetValues(typeof(ElementalAlignment)))
            {
                var shardPrefab = $"Magenheim_Shard_{element}";
                var simplePrefab = $"Magenheim_Crystal_{element}_Simple";
                var recipeName = $"Magenheim_Recipe_{element}Shards_To_Simple";

                if (PrefabManager.Instance.GetPrefab(shardPrefab) is null)
                    throw new InvalidOperationException($"Shard recipe source prefab '{shardPrefab}' is unavailable.");
                if (PrefabManager.Instance.GetPrefab(simplePrefab) is null)
                    throw new InvalidOperationException($"Shard recipe output prefab '{simplePrefab}' is unavailable.");

                var config = new RecipeConfig
                {
                    Name = recipeName,
                    Item = simplePrefab,
                    Amount = 1,
                    CraftingStation = WorkshopRegistrar.StationPrefab,
                    MinStationLevel = 1,
                    Enabled = true,
                };
                config.AddRequirement(shardPrefab, ShardsPerSimpleCrystal);

                if (!ItemManager.Instance.AddRecipe(new CustomRecipe(config)))
                    throw new InvalidOperationException($"Jotunn refused shard recombination recipe '{recipeName}'.");
                count++;
            }

            _registered = true;
            _log.LogInfo(
                $"Registered {count} elemental shard recombination recipes: {ShardsPerSimpleCrystal} matching shards -> 1 Simple crystal.");
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
