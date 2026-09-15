using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers additive Magenheim-owned Deep Fracture hostile creature chassis.</summary>
internal sealed class DeepFractureCreatureRegistrar : IDisposable
{
    internal const string AnnoyanceWispPrefix = "Magenheim_AnnoyanceWisp_";
    internal const string GeodeCrawlerPrefix = "Magenheim_GeodeCrawler_";
    internal const string ShardlingPrefix = "Magenheim_Shardling_";
    internal const string CrystalParasitePrefix = "Magenheim_CrystalParasite_";

    private readonly ManualLogSource _log; private bool _subscribed; private bool _registered;
    internal DeepFractureCreatureRegistrar(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));
    internal void Register() { if (_subscribed || _registered) return; PrefabManager.OnVanillaPrefabsAvailable += RegisterContent; _subscribed = true; }

    private void RegisterContent()
    {
        if (_registered) return;
        try
        {
            var wispSource = RequireCreatureSource("Wisp"); var crawlerSource = RequireCreatureSource("Tick");
            var shardlingSource = RequireCreatureSource("Greydwarf"); var parasiteSource = RequireCreatureSource("Leech");
            var registered = new List<string>();
            foreach (ElementalAlignment alignment in Enum.GetValues(typeof(ElementalAlignment)))
            {
                registered.Add(RegisterAnnoyanceWisp(wispSource, alignment)); registered.Add(RegisterGeodeCrawler(crawlerSource, alignment));
                registered.Add(RegisterShardling(shardlingSource, alignment)); registered.Add(RegisterCrystalParasite(parasiteSource, alignment));
            }
            _registered = true; _log.LogInfo($"Registered {registered.Count} additive Deep Fracture creature variants: {string.Join(", ", registered)}.");
        }
        catch (Exception exception) { _log.LogError($"Deep Fracture creature registration failed: {exception}"); throw; }
        finally { Dispose(); }
    }

    private static GameObject RequireCreatureSource(string name)
    {
        var source = PrefabManager.Instance.GetPrefab(name) ?? throw new InvalidOperationException($"Vanilla creature source '{name}' is unavailable; refusing to create an unverified chassis.");
        if (!source.GetComponent<Character>()) throw new InvalidOperationException($"Vanilla creature source '{name}' exposes no Character behavior.");
        if (!source.GetComponent<BaseAI>()) throw new InvalidOperationException($"Vanilla creature source '{name}' exposes no BaseAI behavior.");
        if (!source.GetComponent<ZNetView>()) throw new InvalidOperationException($"Vanilla creature source '{name}' exposes no ZNetView network identity."); return source;
    }
    private static GameObject CloneVerified(string prefabName, GameObject source)
    {
        if (PrefabManager.Instance.GetPrefab(prefabName)) throw new InvalidOperationException($"Cannot replace occupied Deep Fracture creature identity '{prefabName}'.");
        var prefab = PrefabManager.Instance.CreateClonedPrefab(prefabName, source) ?? throw new InvalidOperationException($"Unable to clone Deep Fracture creature host '{prefabName}'."); prefab.name = prefabName;
        if (!prefab.GetComponent<Character>() || !prefab.GetComponent<BaseAI>() || !prefab.GetComponent<ZNetView>()) throw new InvalidOperationException($"Cloned Deep Fracture creature '{prefabName}' lost required Character/BaseAI/ZNetView behavior."); return prefab;
    }
    private static string RegisterAnnoyanceWisp(GameObject source, ElementalAlignment alignment) => Register(source, alignment, AnnoyanceWispPrefix, "Annoyance Wisp", 34f, 5.5f, 2.5f, DeepFractureCreatureVisuals.ApplyAnnoyanceWisp);
    private static string RegisterGeodeCrawler(GameObject source, ElementalAlignment alignment) => Register(source, alignment, GeodeCrawlerPrefix, "Geode Crawler", 72f, 4f, 1.8f, DeepFractureCreatureVisuals.ApplyGeodeCrawler);
    private static string RegisterShardling(GameObject source, ElementalAlignment alignment) => Register(source, alignment, ShardlingPrefix, "Shardling", 48f, 6.2f, 2.8f, DeepFractureCreatureVisuals.ApplyShardling);
    private static string RegisterCrystalParasite(GameObject source, ElementalAlignment alignment) => Register(source, alignment, CrystalParasitePrefix, "Crystal Parasite", 58f, 3.8f, 1.6f, DeepFractureCreatureVisuals.ApplyCrystalParasite);

    private static string Register(GameObject source, ElementalAlignment alignment, string prefix, string displayName, float health, float runSpeed, float walkSpeed, Action<GameObject, ElementalAlignment> visuals)
    {
        var prefabName = prefix + alignment; var prefab = CloneVerified(prefabName, source); var character = prefab.GetComponent<Character>();
        character.m_name = $"{alignment} {displayName}"; character.m_health = health; character.m_runSpeed = Mathf.Max(character.m_runSpeed, runSpeed); character.m_walkSpeed = Mathf.Max(character.m_walkSpeed, walkSpeed);
        visuals(prefab, alignment); PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, true)); return prefabName;
    }
    public void Dispose() { if (!_subscribed) return; PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent; _subscribed = false; }
}
