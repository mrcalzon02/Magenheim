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

    /// <summary>
    /// Shards fused into one unaligned structural block. Four, against five for a Simple crystal,
    /// because the structural crystal is deliberately the cheaper thing to make: it is bulk stock
    /// and every Magenheim building, decor and weapon recipe is priced in it.
    /// </summary>
    internal const int ShardsPerStructuralCrystal = 4;

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

            if (PrefabManager.Instance.GetPrefab(StructuralCrystalRegistrar.PrefabName) is null)
                throw new InvalidOperationException("The structural crystal must be registered before its fusing recipes.");

            var count = 0;
            var fused = 0;
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

                // Every alignment fuses into the same unaligned block. Valheim's crafting UI cannot
                // express "any four shards" as one requirement, so this is one recipe per element
                // rather than a single mixed-input craft: the player may use whichever shards they
                // have, but a single craft draws on one alignment.
                var fuseName = $"Magenheim_Recipe_{element}Shards_To_StructuralCrystal";
                var fuseConfig = new RecipeConfig
                {
                    Name = fuseName,
                    Item = StructuralCrystalRegistrar.PrefabName,
                    Amount = 1,
                    CraftingStation = WorkshopRegistrar.StationPrefab,
                    MinStationLevel = 1,
                    Enabled = true,
                };
                fuseConfig.AddRequirement(shardPrefab, ShardsPerStructuralCrystal);

                if (!ItemManager.Instance.AddRecipe(new CustomRecipe(fuseConfig)))
                    throw new InvalidOperationException($"Jotunn refused structural fusing recipe '{fuseName}'.");
                fused++;
            }

            _registered = true;
            _log.LogInfo(
                $"Registered {count} elemental shard recombination recipes: {ShardsPerSimpleCrystal} matching shards -> 1 Simple crystal; " +
                $"and {fused} structural fusing recipes: {ShardsPerStructuralCrystal} shards of any one alignment -> 1 Structural Crystal.");
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
