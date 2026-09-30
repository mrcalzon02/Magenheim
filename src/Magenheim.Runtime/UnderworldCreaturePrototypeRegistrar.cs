using System;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Native Underworld fauna reusing Valheim rig/clip/AI machinery with Magenheim balance, loot and presentation.</summary>
internal sealed class UnderworldCreaturePrototypeRegistrar : IDisposable
{
    private readonly ManualLogSource _log;
    internal UnderworldCreaturePrototypeRegistrar(ManualLogSource log) => _log = log;
    internal void Register() => PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;

    private void RegisterContent()
    {
        var registered = 0;
        foreach (var entry in UnderworldCreaturePrototypes.All)
        {
            // A missing donor must not suppress the other 41 review creatures.
            try
            {
                var source = PrefabManager.Instance.GetPrefab(entry.Donor);
                if (!source || !source.GetComponent<Character>() || !source.GetComponent<BaseAI>() ||
                    !source.GetComponent<ZNetView>())
                    throw new InvalidOperationException($"Donor {entry.Donor} lacks its native creature components.");
                var animator = source.GetComponentInChildren<Animator>(true);
                if (!animator || !animator.runtimeAnimatorController)
                    throw new InvalidOperationException($"Donor {entry.Donor} has no animation controller.");
                if (PrefabManager.Instance.GetPrefab(entry.Prefab))
                    throw new InvalidOperationException($"Prefab identity {entry.Prefab} is already occupied.");
                var clone = PrefabManager.Instance.CreateClonedPrefab(entry.Prefab, source);
                clone.GetComponent<Character>().m_name = entry.Name;
                // Uniform scale keeps the donor's bone hierarchy and attack sockets together.
                clone.transform.localScale *= entry.Scale;

                var authoredVisual = string.Empty;
                var materialCount = 0;
                if (!string.IsNullOrWhiteSpace(entry.AuthoredModelId) &&
                    ModelAssets.Exists(entry.AuthoredModelId))
                {
                    authoredVisual = RigidCreatureSegmentBinder.Apply(clone, entry.AuthoredModelId);
                }
                else
                {
                    materialCount = UnderworldCreatureBiomeVisuals.Apply(clone, entry);
                    authoredVisual = "donor visual fallback";
                }

                var identity = UnderworldCreatureIdentityPass.Apply(clone, entry);
                var creatureConfig = new CreatureConfig { Name = entry.Name };
                foreach (var drop in UnderworldCreatureLootRuntime.OrdinaryDrops(entry))
                    creatureConfig.AddDropConfig(drop);
                creatureConfig.AddSpawnConfig(BuildSpawnConfig(entry));
                if (!CreatureManager.Instance.AddCreature(new CustomCreature(clone, true, creatureConfig)))
                    throw new InvalidOperationException($"Jotunn refused creature registration for '{entry.Prefab}'.");
                registered++;
                _log.LogDebug($"Underworld creature {entry.Prefab}: intact {entry.Donor} gameplay chassis; visual={authoredVisual}; {materialCount} donor renderer material(s) retextured when fallback is active; {identity}; native biome spawn enabled; {entry.Limit}");
            }
            catch (Exception exception)
            {
                _log.LogWarning($"Underworld prototype {entry.Name} unavailable: {exception.Message}");
            }
        }
        _log.LogInfo($"Registered {registered}/{UnderworldCreaturePrototypes.All.Length} Underworld creatures with native spawns, owned material/VFX identities, combat scaling, home-biome tolerances, progression-resource loot and species trophies. Donor rigs, animations and attack definitions remain intact; donor loot is replaced.");
        RegisterInfrastructure();
        Dispose();
    }

    private static SpawnConfig BuildSpawnConfig(UnderworldCreaturePrototypes.Entry entry)
    {
        if (!TryBiome(entry.Biome, out var biome))
            throw new InvalidOperationException($"No native Underworld biome flag exists for '{entry.Biome}'.");

        var tuning = TuningFor(entry);
        var aquatic = entry.Donor == "Serpent" || entry.Donor == "Leech";
        var shoreline = entry.Biome == "Blackwater Deep" &&
                        (entry.Donor == "Neck" || entry.Name == "Abyss Shellback" || entry.Name == "Shoreclaw");

        return new SpawnConfig
        {
            Name = entry.Prefab + "_Natural_" + entry.Biome.Replace(" ", string.Empty),
            Biome = biome,
            BiomeArea = Heightmap.BiomeArea.Everything,
            SpawnChance = tuning.Chance,
            SpawnInterval = tuning.Interval,
            SpawnDistance = 24f,
            MinSpawnRadius = 36f,
            MaxSpawnRadius = 88f,
            MaxSpawned = tuning.MaxSpawned,
            MinGroupSize = tuning.MinGroup,
            MaxGroupSize = tuning.MaxGroup,
            GroupRadius = tuning.GroupRadius,
            MinAltitude = aquatic ? -30f : shoreline ? -2f : 0.5f,
            MaxAltitude = aquatic ? 2.5f : shoreline ? 10f : 1000f,
            MinTilt = 0f,
            MaxTilt = aquatic ? 48f : shoreline ? 30f : 42f,
            SpawnAtDay = true,
            SpawnAtNight = true,
            HuntPlayer = UnderworldCreatureIdentityPass.HuntsPlayer(entry),
        };
    }

    private static SpawnTuning TuningFor(UnderworldCreaturePrototypes.Entry entry)
    {
        // Hero/heavy silhouettes should punctuate a biome, not turn every chunk into an arena.
        switch (entry.Name)
        {
            case "Crowncap Brute":
            case "Deep Hunter":
            case "Abyss Shellback":
            case "Furnace Golem":
            case "Cryolith Guardian":
            case "Rift Colossus":
            case "Graft Warden":
            case "Corpse Orchard":
                return new SpawnTuning(3.5f, 210f, 1, 1, 1, 1f);
            case "Puffback":
            case "Basalt Crawler":
            case "Stonebound":
                return new SpawnTuning(5f, 165f, 1, 1, 1, 2f);
            case "Lantern Moth":
            case "Sporeling":
            case "Ashmite":
            case "Frost Tick":
            case "Fracture Wisp":
            case "Rotling":
                return new SpawnTuning(12f, 90f, 4, 1, 3, 6f);
            case "Cave Ray":
            case "Gloomfin":
            case "Blackwater Lamprey":
            case "Lantern Angler":
            case "Gravity Leech":
                return new SpawnTuning(8f, 125f, 2, 1, 2, 7f);
            case "Rime Moth":
            case "Rimewing":
            case "Shardwing":
            case "Fume Wraith":
                return new SpawnTuning(7f, 135f, 2, 1, 2, 8f);
            default:
                return new SpawnTuning(7.5f, 120f, 2, 1, 2, 5f);
        }
    }

    private static bool TryBiome(string name, out Heightmap.Biome biome)
    {
        switch (name)
        {
            case "Fungal Forest":
                biome = UnderworldTerrainRuntime.FungalForestBiome; return true;
            case "Blackwater Deep":
                biome = UnderworldTerrainRuntime.BlackwaterDeepBiome; return true;
            case "Sulfurous Wastes":
                biome = UnderworldTerrainRuntime.SulfurousWastesBiome; return true;
            case "Frozen Caverns":
                biome = UnderworldTerrainRuntime.FrozenCavernsBiome; return true;
            case "Fracture Zones":
                biome = UnderworldTerrainRuntime.FractureZonesBiome; return true;
            case "Great Decay":
                biome = UnderworldTerrainRuntime.GreatDecayBiome; return true;
            default:
                biome = Heightmap.Biome.None; return false;
        }
    }

    private readonly struct SpawnTuning
    {
        internal SpawnTuning(float chance, float interval, int maxSpawned, int minGroup, int maxGroup, float groupRadius)
        {
            Chance = chance;
            Interval = interval;
            MaxSpawned = maxSpawned;
            MinGroup = minGroup;
            MaxGroup = maxGroup;
            GroupRadius = groupRadius;
        }

        internal float Chance { get; }
        internal float Interval { get; }
        internal int MaxSpawned { get; }
        internal int MinGroup { get; }
        internal int MaxGroup { get; }
        internal float GroupRadius { get; }
    }

    private void RegisterInfrastructure()
    {
        var registered = 0;
        foreach (var entry in UnderworldCreaturePrototypes.Infrastructure)
        {
            try
            {
                var source = PrefabManager.Instance.GetPrefab(entry.Donor);
                if (!source || !source.GetComponent<Piece>() || !source.GetComponent<ZNetView>() ||
                    !source.GetComponent<WearNTear>())
                    throw new InvalidOperationException($"Donor {entry.Donor} lacks native building components.");
                if (PrefabManager.Instance.GetPrefab(entry.Prefab))
                    throw new InvalidOperationException($"Prefab identity {entry.Prefab} is already occupied.");
                // Legacy 0.0.89 review identities remain console-only for compatibility.
                var clone = PrefabManager.Instance.CreateClonedPrefab(entry.Prefab, source);
                clone.GetComponent<Piece>().m_name = entry.Name + " (prototype)";
                Tint(clone, entry.Color);
                PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, true));
                registered++;
            }
            catch (Exception exception)
            {
                _log.LogWarning($"Underworld infrastructure prototype {entry.Name} unavailable: {exception.Message}");
            }
        }
        _log.LogInfo($"Registered {registered}/{UnderworldCreaturePrototypes.Infrastructure.Length} legacy infrastructure review prefabs. Console-only; no Hammer registration.");
    }

    internal static void Tint(GameObject root, Color color)
    {
        // Property blocks do not recolor shared Valheim materials or allocate material copies.
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }
    }

    public void Dispose() => PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
}
