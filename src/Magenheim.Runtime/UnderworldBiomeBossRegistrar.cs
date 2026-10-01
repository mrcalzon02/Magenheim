using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Six biome encounters reuse the biome's authored apex anatomy and native AI/loot.</summary>
internal static class UnderworldBiomeBossRegistrar
{
    internal sealed record Encounter(string Trophy, string Name, string Chassis, float Scale, float Health, float Damage, string Arena);
    internal static readonly IReadOnlyList<Encounter> Encounters = new[]
    {
        new Encounter("FirstBloom", "The First Bloom", "Crowncap Brute", 1.35f, 4800f, 1.65f, "first-bloom"),
        new Encounter("BlackwaterMaw", "The Blackwater Maw", "Deep Hunter", 1.30f, 5600f, 1.75f, "blackwater-maw"),
        new Encounter("FurnaceHeart", "The Furnace Heart", "Furnace Golem", 1.35f, 6600f, 1.90f, "furnace-heart"),
        new Encounter("WhiteSilence", "The White Silence", "Cryolith Guardian", 1.35f, 7600f, 2.00f, "white-silence"),
        new Encounter("RiftTitan", "The Rift Titan", "Rift Colossus", 1.40f, 9000f, 2.10f, "rift-titan"),
        new Encounter("CarrionCrown", "The Carrion Crown", "Corpse Orchard", 1.35f, 10400f, 2.20f, "carrion-crown"),
    };
    internal static string Prefab(Encounter encounter) => "Magenheim_Underworld_Encounter_" + encounter.Trophy;
    internal static string BossId(Encounter encounter) => "magenheim.underworld.boss." + encounter.Arena.Replace('-', '_');
    internal static string DefeatedKey(UnderworldBossDefinition boss) => "magenheim.underworld.boss.defeated." + boss.Id;
    internal static GameObject ComposeArena(string location, Vector3 position, Quaternion rotation)
    {
        var boss = UnderworldDeepstoneRuntimeAuthority.Bosses.Single(b => b.UniqueLocationId == location);
        var encounter = Encounters.Single(e => boss.Id == BossId(e));
        var root = new GameObject("Magenheim_Underworld_Arena_" + encounter.Trophy);
        try
        {
            root.transform.SetPositionAndRotation(position, rotation);
            ModelAssets.Load(root, "underworld-boss-arena-" + encounter.Arena, item: false, hideOriginal: false);
            return root;
        }
        catch { UnityEngine.Object.Destroy(root); throw; }
    }
    private static ManualLogSource? _log;

    internal static void Register(ManualLogSource log)
    {
        _log = log;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
    }

    private static void RegisterContent()
    {
        try
        {
            foreach (var encounter in Encounters)
            {
                var boss = UnderworldDeepstoneRuntimeAuthority.Bosses.Single(b => b.Id == BossId(encounter));
                var species = UnderworldCreaturePrototypes.All.Single(e => e.Name == encounter.Chassis);
                if (!ModelAssets.Exists("underworld-boss-arena-" + encounter.Arena))
                    throw new InvalidOperationException("Missing authored biome boss arena: " + encounter.Name);
                var source = PrefabManager.Instance.GetPrefab(species.Prefab)
                    ?? throw new InvalidOperationException("Missing biome encounter chassis: " + species.Prefab);
                var clone = PrefabManager.Instance.CreateClonedPrefab(Prefab(encounter), source);
                clone.transform.localScale *= encounter.Scale;
                var character = clone.GetComponent<Character>();
                character.m_name = encounter.Name;
                character.m_boss = true;
                character.m_bossOrder = 0;
                character.m_bossEvent = string.Empty;
                character.m_defeatSetGlobalKey = DefeatedKey(boss);
                character.m_health = encounter.Health;
                clone.GetComponent<MagenheimCreatureCombatScaling>().Configure(encounter.Damage, 140f);
                if (species.AuthoredModelId is null || !ModelAssets.Exists(species.AuthoredModelId))
                    throw new InvalidOperationException("Biome encounter is missing authored apex anatomy: " + encounter.Name);
                var config = new CreatureConfig { Name = encounter.Name };
                config.AddDropConfig(new DropConfig
                {
                    Item = boss.TrophyPrefabName, MinAmount = 1, MaxAmount = 1, Chance = 100f,
                    OnePerPlayer = false, LevelMultiplier = false,
                });
                if (!CreatureManager.Instance.AddCreature(new CustomCreature(clone, fixReference: false, config)))
                    throw new InvalidOperationException("Biome boss registration refused: " + encounter.Name);
            }
            _log?.LogInfo("Registered six biome boss encounters with native AI, canonical trophy drops and native defeat keys.");
        }
        finally { PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent; }
    }
}

/// <summary>Native location lifecycle admits one living native ZDO per canonical biome encounter.</summary>
internal sealed class UnderworldBiomeBossArenaRuntime : MonoBehaviour
{
    private UnderworldBossDefinition? _boss;
    private UnderworldBiomeBossRegistrar.Encounter? _encounter;
    private readonly List<ZDO> _existing = new();
    private int _cursor;
    private bool _searchComplete;
    private float _nextCheck;
    private ZDO? _living;

    internal void Bind(string locationId)
    {
        _boss = UnderworldDeepstoneRuntimeAuthority.Bosses.Single(b => b.UniqueLocationId == locationId);
        _encounter = UnderworldBiomeBossRegistrar.Encounters.Single(e =>
            _boss.Id == UnderworldBiomeBossRegistrar.BossId(e));
    }

    private void Update()
    {
        if (_boss is null || _encounter is null || ZNet.instance == null || !ZNet.instance.IsServer()) return;
        if (!ValheimWorldInstanceExecution.TryGetContextForScene(gameObject.scene.handle, out var context) ||
            context is null || !context.InstanceId.IsUnderworld) return;
        using (ValheimWorldInstanceExecution.Enter(context)) Tick();
    }

    private void Tick()
    {
        var boss = _boss!; var encounter = _encounter!;
        var zone = ZoneSystem.instance; var zdos = ZDOMan.instance;
        if (zone == null || zdos == null || zone.GetGlobalKey(UnderworldBiomeBossRegistrar.DefeatedKey(boss))) return;
        if (!_searchComplete)
        {
            // Search the native manager before spawning, including objects awaiting sector streaming.
            _searchComplete = zdos.GetAllZDOsWithPrefabIterative(UnderworldBiomeBossRegistrar.Prefab(encounter), _existing, ref _cursor);
            if (!_searchComplete) return;
            if (_existing.Count > 1) { Debug.LogError("Duplicate native biome boss ZDOs: " + boss.Id); enabled = false; return; }
            _living = _existing.FirstOrDefault();
            _existing.Clear();
        }
        if (_living != null && zdos.GetZDO(_living.m_uid) != null) return;
        if (Time.time < _nextCheck) return;
        _nextCheck = Time.time + 2f;
        var mounted = UnderworldDeepstoneRuntimeAuthority.ReconstructWorldState();
        if (boss.PrerequisiteBossIds.Any(required => !mounted.Any(stone =>
            stone.BossId == required && stone.TrophyMounted && stone.BoonUnlocked))) return;
        if (!Player.GetAllPlayers().Any(player => player && !player.IsDead() &&
            player.gameObject.scene.handle == gameObject.scene.handle &&
            Vector3.Distance(player.transform.position, transform.position) <= 40f)) return;
        var prefab = PrefabManager.Instance.GetPrefab(UnderworldBiomeBossRegistrar.Prefab(encounter));
        if (!prefab) { Debug.LogError("Biome boss prefab unavailable: " + encounter.Name); enabled = false; return; }
        var position = transform.position + Vector3.up * 1.5f;
        var instance = Instantiate(prefab, position, transform.rotation);
        var view = instance.GetComponent<ZNetView>();
        if (!view || !view.IsValid()) { Destroy(instance); Debug.LogError("Biome boss has no native persistent ZDO: " + encounter.Name); enabled = false; return; }
        _living = view.GetZDO();
        Debug.Log("Underworld biome encounter started: " + boss.Id + " native ZDO " + _living.m_uid);
    }
}
