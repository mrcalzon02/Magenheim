using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers the first Deep Fracture hostile creature chassis as additive Magenheim-owned prefabs.</summary>
internal sealed class DeepFractureCreatureRegistrar : IDisposable
{
    internal const string AnnoyanceWispPrefix = "Magenheim_AnnoyanceWisp_";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal DeepFractureCreatureRegistrar(ManualLogSource log)
        => _log = log ?? throw new ArgumentNullException(nameof(log));

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
            var source = PrefabManager.Instance.GetPrefab("Wisp")
                ?? throw new InvalidOperationException("Vanilla Wisp source prefab is unavailable; refusing to create an unverified creature chassis.");
            if (!source.GetComponent<Character>())
                throw new InvalidOperationException("Vanilla Wisp source no longer exposes Character behavior.");
            if (!source.GetComponent<BaseAI>())
                throw new InvalidOperationException("Vanilla Wisp source no longer exposes BaseAI behavior.");

            var registered = new List<string>();
            foreach (ElementalAlignment alignment in Enum.GetValues(typeof(ElementalAlignment)))
                registered.Add(RegisterAnnoyanceWisp(source, alignment));

            _registered = true;
            _log.LogInfo($"Registered {registered.Count} additive Annoyance Wisp creature variants: {string.Join(", ", registered)}.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Deep Fracture creature registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static string RegisterAnnoyanceWisp(GameObject source, ElementalAlignment alignment)
    {
        var prefabName = AnnoyanceWispPrefix + alignment;
        if (PrefabManager.Instance.GetPrefab(prefabName))
            throw new InvalidOperationException($"Cannot replace occupied Deep Fracture creature identity '{prefabName}'.");

        var prefab = PrefabManager.Instance.CreateClonedPrefab(prefabName, source)
            ?? throw new InvalidOperationException($"Unable to clone Annoyance Wisp host for {alignment}.");
        prefab.name = prefabName;

        var character = prefab.GetComponent<Character>()
            ?? throw new InvalidOperationException($"Cloned Annoyance Wisp '{prefabName}' lost Character behavior.");
        if (!prefab.GetComponent<BaseAI>())
            throw new InvalidOperationException($"Cloned Annoyance Wisp '{prefabName}' lost BaseAI behavior.");
        if (!prefab.GetComponent<ZNetView>())
            throw new InvalidOperationException($"Cloned Annoyance Wisp '{prefabName}' lost ZNetView network identity.");

        character.m_name = $"{alignment} Annoyance Wisp";
        character.m_health = 34f;
        character.m_runSpeed = Mathf.Max(character.m_runSpeed, 5.5f);
        character.m_walkSpeed = Mathf.Max(character.m_walkSpeed, 2.5f);

        DeepFractureCreatureVisuals.ApplyAnnoyanceWisp(prefab, alignment);
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
