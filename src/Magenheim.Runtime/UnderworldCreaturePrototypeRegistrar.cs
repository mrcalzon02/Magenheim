using System;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Spawnable review content. Valheim retains rig, clips, attacks, AI and persistence.</summary>
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
                clone.GetComponent<Character>().m_name = entry.Name + " (prototype)";
                // Uniform scale keeps the donor's bone hierarchy and attack sockets together.
                clone.transform.localScale *= entry.Scale;
                Tint(clone, entry.Color);
                PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, true));
                registered++;
                _log.LogDebug($"Underworld prototype {entry.Prefab}: intact {entry.Donor} skeleton/controller; {entry.Limit}");
            }
            catch (Exception exception)
            {
                _log.LogWarning($"Underworld prototype {entry.Name} unavailable: {exception.Message}");
            }
        }
        _log.LogInfo($"Registered {registered}/{UnderworldCreaturePrototypes.All.Length} Underworld creature review prototypes. Console spawn only; donor combat and loot are unchanged.");
        RegisterInfrastructure();
        Dispose();
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
