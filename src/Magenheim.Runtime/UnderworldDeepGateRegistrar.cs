using System;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Managers;
using SoftReferenceableAssets;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Loads the actual Aesir boss gate through Valheim's native soft-asset loader.</summary>
internal sealed class UnderworldDeepGateRegistrar : IDisposable
{
    internal const string PrefabName = "Magenheim_DeepGate";
    // Verified in the installed SoftRef manifest: Assets/world/Props/DeepNorth/LastBossGate/LastBossGate.prefab.
    private const string GateAssetId = "22a0a0b6f09802a0f861b9789a1bde32";
    private readonly ManualLogSource _log;
    private SoftReference<GameObject> _source;
    private bool _loaded;
    private bool _subscribed;

    internal UnderworldDeepGateRegistrar(ManualLogSource log) => _log = log;
    internal void Register()
    {
        if (_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
        _subscribed = true;
    }
    public void Dispose()
    {
        if (_subscribed) PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        _subscribed = false;
        if (_loaded) _source.Release();
        _loaded = false;
    }
    private void OnVanillaPrefabsAvailable()
    {
        if (PrefabManager.Instance.GetPrefab(PrefabName)) return;
        if (!AssetID.TryParse(GateAssetId, out var id)) throw new InvalidOperationException("Invalid Aesir gate asset ID.");
        _source = new SoftReference<GameObject>(id);
        _source.Load();
        _loaded = true; // Retain the source assets for the lifetime of the registered copies.
        var donor = _source.Asset;
        if (!donor) throw new InvalidOperationException("Valheim could not load the LastBossGate asset.");
        var gate = PrefabManager.Instance.CreateClonedPrefab(PrefabName, donor);
        if (!gate) throw new InvalidOperationException("Could not clone the Aesir boss gate.");
        // Copy the native geometry/materials unchanged. Remove boss/network/encounter authority
        // from this local gate copy, before it can spawn. Never tint the native materials purple.
        foreach (var component in gate.GetComponentsInChildren<Component>(true).Reverse())
        {
            if (!component || component is Transform || component is MeshFilter || component is Renderer ||
                component is LODGroup || component is Collider || component is Light || component is ParticleSystem)
                continue;
            UnityEngine.Object.DestroyImmediate(component);
        }
        var renderers = gate.GetComponentsInChildren<Renderer>(true).Where(r => r is not ParticleSystemRenderer).ToArray();
        if (renderers.Length == 0) throw new InvalidOperationException("Aesir gate contains no renderers.");
        // Imported gate geometry is offset inside its mesh. Normalize its visible foot, not just
        // the prefab transform, so it cannot float above the standing stones.
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        var offset = gate.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
        foreach (Transform child in gate.transform) child.localPosition -= offset;
        foreach (var lod in gate.GetComponentsInChildren<LODGroup>(true)) lod.RecalculateBounds();
        var identity = gate.AddComponent<UnderworldDeepGateIdentity>();
        identity.DonorPrefabName = "LastBossGate";
        PrefabManager.Instance.AddPrefab(gate);
        _log.LogInfo($"Registered Aesir Deep Gate directly from LastBossGate: {renderers.Length} renderers; grounded native geometry and materials.");
        PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        _subscribed = false;
    }
}

internal sealed class UnderworldDeepGateIdentity : MonoBehaviour
{
    internal string DonorPrefabName { get; set; } = string.Empty;
}
