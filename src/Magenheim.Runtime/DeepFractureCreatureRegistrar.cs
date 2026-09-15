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

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal DeepFractureCreatureRegistrar(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));

    internal void Register()
    {
        if (_subscribed || _registered) return;
        PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;
        _subscribed = true;
    }

    private void RegisterContent()
    {
        if (_registered) return;
        try
        {
            var wispSource = RequireCreatureSource("Wisp");
            var crawlerSource = RequireCreatureSource("Tick");
            var registered = new List<string>();
            foreach (ElementalAlignment alignment in Enum.GetValues(typeof(ElementalAlignment)))
            {
                registered.Add(RegisterAnnoyanceWisp(wispSource, alignment));
                registered.Add(RegisterGeodeCrawler(crawlerSource, alignment));
            }
            _registered = true;
            _log.LogInfo($"Registered {registered.Count} additive Deep Fracture creature variants: {string.Join(", ", registered)}.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Deep Fracture creature registration failed: {exception}");
            throw;
        }
        finally { Dispose(); }
    }

    private static GameObject RequireCreatureSource(string name)
    {
        var source = PrefabManager.Instance.GetPrefab(name)
            ?? throw new InvalidOperationException($"Vanilla creature source '{name}' is unavailable; refusing to create an unverified chassis.");
        if (!source.GetComponent<Character>()) throw new InvalidOperationException($"Vanilla creature source '{name}' exposes no Character behavior.");
        if (!source.GetComponent<BaseAI>()) throw new InvalidOperationException($"Vanilla creature source '{name}' exposes no BaseAI behavior.");
        if (!source.GetComponent<ZNetView>()) throw new InvalidOperationException($"Vanilla creature source '{name}' exposes no ZNetView network identity.");
        return source;
    }

    private static GameObject CloneVerified(string prefabName, GameObject source)
    {
        if (PrefabManager.Instance.GetPrefab(prefabName)) throw new InvalidOperationException($"Cannot replace occupied Deep Fracture creature identity '{prefabName}'.");
        var prefab = PrefabManager.Instance.CreateClonedPrefab(prefabName, source)
            ?? throw new InvalidOperationException($"Unable to clone Deep Fracture creature host '{prefabName}'.");
        prefab.name = prefabName;
        if (!prefab.GetComponent<Character>() || !prefab.GetComponent<BaseAI>() || !prefab.GetComponent<ZNetView>())
            throw new InvalidOperationException($"Cloned Deep Fracture creature '{prefabName}' lost required Character/BaseAI/ZNetView behavior.");
        return prefab;
    }

    private static string RegisterAnnoyanceWisp(GameObject source, ElementalAlignment alignment)
    {
        var prefabName = AnnoyanceWispPrefix + alignment;
        var prefab = CloneVerified(prefabName, source);
        var character = prefab.GetComponent<Character>();
        character.m_name = $"{alignment} Annoyance Wisp";
        character.m_health = 34f;
        character.m_runSpeed = Mathf.Max(character.m_runSpeed, 5.5f);
        character.m_walkSpeed = Mathf.Max(character.m_walkSpeed, 2.5f);
        DeepFractureCreatureVisuals.ApplyAnnoyanceWisp(prefab, alignment);
        PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, true));
        return prefabName;
    }

    private static string RegisterGeodeCrawler(GameObject source, ElementalAlignment alignment)
    {
        var prefabName = GeodeCrawlerPrefix + alignment;
        var prefab = CloneVerified(prefabName, source);
        var character = prefab.GetComponent<Character>();
        character.m_name = $"{alignment} Geode Crawler";
        character.m_health = 72f;
        character.m_runSpeed = Mathf.Max(character.m_runSpeed, 4.0f);
        character.m_walkSpeed = Mathf.Max(character.m_walkSpeed, 1.8f);
        DeepFractureCreatureVisuals.ApplyGeodeCrawler(prefab, alignment);
        PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, true));
        return prefabName;
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }
}
