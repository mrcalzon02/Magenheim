using System;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Registers the unique boss chassis without mutating the vanilla source prefab.</summary>
internal sealed class NowhereKingRegistrar : IDisposable
{
    internal const string PrefabName = "Magenheim_NowhereKing";

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal NowhereKingRegistrar(ManualLogSource log) => _log = log ?? throw new ArgumentNullException(nameof(log));

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
            if (PrefabManager.Instance.GetPrefab(PrefabName) != null)
            {
                _registered = true;
                _log.LogWarning($"Skipped Nowhere King registration because prefab identity '{PrefabName}' is occupied; existing content was left untouched.");
                return;
            }

            var source = PrefabManager.Instance.GetPrefab("DvergerMage")
                ?? throw new InvalidOperationException("Nowhere King requires verified vanilla humanoid source 'DvergerMage'.");
            if (source.GetComponent<Character>() == null || source.GetComponent<BaseAI>() == null || source.GetComponent<ZNetView>() == null)
                throw new InvalidOperationException("Nowhere King source lacks required Character/BaseAI/ZNetView behavior.");

            var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, source)
                ?? throw new InvalidOperationException("Unable to clone Nowhere King host prefab.");
            prefab.name = PrefabName;

            var character = prefab.GetComponent<Character>();
            character.m_name = "The Nowhere King";
            character.m_health = 7200f;
            character.m_walkSpeed = Mathf.Max(character.m_walkSpeed, 2.4f);
            character.m_runSpeed = Mathf.Max(character.m_runSpeed, 5.4f);
            prefab.transform.localScale = Vector3.one * 1.82f;

            // The Dverger is only a networked humanoid/animation chassis. Its autonomous spell AI
            // must never run beside authored King combat.
            foreach (var ai in prefab.GetComponents<BaseAI>()) ai.enabled = false;

            NowhereKingVisuals.Apply(prefab);
            prefab.AddComponent<NowhereKingArenaLeash>();
            PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, true));
            _registered = true;
            _log.LogInfo($"Registered unique boss prefab '{PrefabName}' with inherited Dverger AI disabled.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Nowhere King registration failed: {exception}");
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
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterContent;
        _subscribed = false;
    }
}

internal static class NowhereKingVisuals
{
    internal static void Apply(GameObject prefab)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));

        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            var source = renderer.sharedMaterial;
            if (!source) continue;
            var material = new Material(source) { name = "magenheim.nowhere-king.armor." + renderer.name };
            if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(.018f, .02f, .027f, 1f));
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .82f);
            renderer.sharedMaterial = material;
        }

        AttachVanillaCrown(prefab);
    }

    private static void AttachVanillaCrown(GameObject prefab)
    {
        var crownPrefab = PrefabManager.Instance.GetPrefab(NowhereKingRewardRegistrar.VanillaCrownPrefabName)
            ?? throw new InvalidOperationException(
                $"Nowhere King requires vanilla Crown of Valheim prefab '{NowhereKingRewardRegistrar.VanillaCrownPrefabName}'.");

        // Vanilla equipment exposes its display geometry below an attach/attach_skin root. Clone
        // that visual subtree only so the boss receives Iron Gate's authored crown without nesting
        // ItemDrop/ZNetView/Rigidbody gameplay components inside the networked boss prefab.
        var visualSource = FindVisualAttach(crownPrefab.transform)
            ?? throw new InvalidOperationException(
                $"Vanilla Crown of Valheim '{NowhereKingRewardRegistrar.VanillaCrownPrefabName}' exposes neither an attach nor attach_skin visual root.");

        var crown = UnityEngine.Object.Instantiate(visualSource.gameObject, prefab.transform, false);
        crown.name = "Magenheim_NowhereKing_CrownOfValheim";
        crown.SetActive(true);
        crown.transform.localPosition = new Vector3(0f, 2.05f, 0f);
        crown.transform.localRotation = Quaternion.identity;
        crown.transform.localScale = Vector3.one * .92f;

        foreach (var renderer in crown.GetComponentsInChildren<Renderer>(true))
        {
            var originals = renderer.sharedMaterials;
            if (originals is null || originals.Length == 0) continue;

            var replacements = new Material[originals.Length];
            for (var i = 0; i < originals.Length; i++)
            {
                var source = originals[i];
                if (!source) continue;
                // Keep the new vanilla crown's authored textures and normal maps; only corrupt the
                // metal palette for the Nowhere King instead of replacing the asset with generated geometry.
                var material = new Material(source) { name = $"magenheim.nowhere-king.crown.{renderer.name}.{i}" };
                if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(.035f, .016f, .041f, 1f));
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .88f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .32f);
                replacements[i] = material;
            }
            renderer.sharedMaterials = replacements;
        }

        var lightHolder = new GameObject("Magenheim_NowhereKing_CrownGlow");
        lightHolder.transform.SetParent(crown.transform, false);
        lightHolder.transform.localPosition = new Vector3(0f, .10f, 0f);
        var light = lightHolder.AddComponent<Light>();
        light.color = new Color(.32f, 0f, .02f);
        light.range = 3.5f;
        light.intensity = 1.15f;
    }

    private static Transform? FindVisualAttach(Transform root)
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(transform.name, "attach", StringComparison.OrdinalIgnoreCase))
                return transform;

        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(transform.name, "attach_skin", StringComparison.OrdinalIgnoreCase))
                return transform;

        return null;
    }
}
