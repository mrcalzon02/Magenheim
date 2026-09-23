using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Creates Magenheim's Deep Gate. Its identity is registered at startup from the Morkhalla jotun gate
/// so worlds can resolve it before anything spawns; its visible body is then replaced by the Aesir
/// gate once Valheim's locations load.
/// </summary>
/// <remarks>
/// "Aesir Passage" is the display name of the <c>hud_pin_dnboss</c> map pin: the Deep North boss site,
/// whose gate is the <c>LastBossGate</c> assembly (pillars, chains, rotator, floorstone, internal gate,
/// rune tiles). The Morkhalla jotun gate this registrar originally cloned is the interior archway, a
/// different object; the user identified the Aesir gate as the intended donor on 2026-09-22. That
/// assembly is not a standalone prefab -- it exists only inside the boss location -- so it can only be
/// read when <see cref="ZoneManager.OnVanillaLocationsAvailable"/> fires, which is at world load, not
/// at the main menu. If it cannot be found the Morkhalla body stays and the log says why.
/// The vanilla objects are never mutated; boss and offering behaviour is stripped from the copies.
/// </remarks>
internal sealed class UnderworldDeepGateRegistrar : IDisposable
{
    internal const string PrefabName = "Magenheim_DeepGate";

    // "Aesir Passage" is not a prefab name in Valheim 1.0.12 -- it is the localized display string
    // for the hud_pin_dnboss map pin, and no asset with "aesir" in its name exists in the build at
    // all, so the original name list could never resolve. The Deep North boss location it names is
    // internally Morkhalla, and its jotun gate is the geometry this pin points at.
    private static readonly string[] PreferredDonorNames =
    {
        "Morkhalla_jotun_gate",
        "Morkborg_gate",
    };

    private static readonly HashSet<string> ForbiddenGameplayComponents = new(StringComparer.Ordinal)
    {
        "OfferingBowl",
        "ItemStand",
        "Vegvisir",
        "SpawnArea",
        "CreatureSpawner",
        "RandomSpawn",
        "BossStone",
        "TeleportWorld",
        "DungeonGenerator",
    };

    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;
    private bool _aesirSubscribed;
    private bool _aesirApplied;

    internal UnderworldDeepGateRegistrar(ManualLogSource log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal void Register()
    {
        if (_registered || _subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
        _subscribed = true;
        // Subscribed here, before UnderworldDeepGateLocationRegistrar, so the gate's body is swapped
        // before that registrar copies the prefab into the surface gate locations.
        if (!_aesirSubscribed)
        {
            ZoneManager.OnVanillaLocationsAvailable += OnVanillaLocationsAvailable;
            _aesirSubscribed = true;
        }
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        _subscribed = false;
    }

    private void OnVanillaPrefabsAvailable()
    {
        if (_registered) return;
        try
        {
            if (PrefabManager.Instance.GetPrefab(PrefabName))
            {
                _registered = true;
                _log.LogWarning($"Deep Gate identity '{PrefabName}' is already occupied; existing content was left untouched.");
                return;
            }

            var donor = ResolveDeepNorthGateDonor()
                ?? throw new InvalidOperationException("Morkhalla jotun gate geometry (the Aesir Passage donor) was not found. Magenheim will not substitute unrelated geometry.");

            var gate = PrefabManager.Instance.CreateClonedPrefab(PrefabName, donor);
            if (!gate) throw new InvalidOperationException($"Jotunn failed to clone Deep North gate donor '{donor.name}'.");

            RemoveVanillaEncounterAuthority(gate);
            ApplyUnderworldMaterials(gate);

            var identity = gate.GetComponent<UnderworldDeepGateIdentity>() ?? gate.AddComponent<UnderworldDeepGateIdentity>();
            identity.DonorPrefabName = donor.name;

            // Jotunn's AddPrefab(GameObject) overload returns void, so confirm the registration
            // took by reading the prefab back rather than testing a non-existent result.
            PrefabManager.Instance.AddPrefab(gate);
            if (PrefabManager.Instance.GetPrefab(PrefabName) == null)
                throw new InvalidOperationException($"Jotunn refused Deep Gate prefab registration for '{PrefabName}'.");

            _registered = true;
            _log.LogInfo($"Registered '{PrefabName}' from Deep North gate donor '{donor.name}' with independent Magenheim materials and encounter authority removed.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Underworld Deep Gate registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private void OnVanillaLocationsAvailable()
    {
        if (_aesirApplied) return;
        try
        {
            var gate = PrefabManager.Instance.GetPrefab(PrefabName)
                ?? throw new InvalidOperationException($"'{PrefabName}' is not registered, so there is no gate to re-body.");
            var body = ExtractAesirGate(out var source);
            // Replace the Morkhalla body: every child of the gate is donor visual or collider; the
            // interaction identity lives on the root and is untouched.
            foreach (var child in gate.transform.Cast<Transform>().ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            body.transform.SetParent(gate.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.SetActive(true);
            RemoveVanillaEncounterAuthority(gate);
            ApplyUnderworldMaterials(gate);
            var identity = gate.GetComponent<UnderworldDeepGateIdentity>();
            if (identity) identity.DonorPrefabName = source;
            _aesirApplied = true;
            var size = Measure(gate);
            _log.LogInfo($"Deep Gate re-bodied from the Aesir gate '{source}': {gate.GetComponentsInChildren<Renderer>(true).Length} renderers, " +
                         $"{gate.GetComponentsInChildren<Collider>(true).Length} colliders, size {size.x:0.0}x{size.y:0.0}x{size.z:0.0}m.");
        }
        catch (Exception exception)
        {
            _aesirApplied = true;
            _log.LogWarning($"Deep Gate keeps its Morkhalla body; the Aesir gate could not be used: {exception.Message}");
        }
    }

    /// <summary>
    /// Finds the LastBossGate assembly inside the Deep North boss location and returns a stripped,
    /// visual-and-collider-only copy of it.
    /// </summary>
    private static GameObject ExtractAesirGate(out string source)
    {
        var zone = ZoneSystem.instance ?? throw new InvalidOperationException("ZoneSystem is not available.");
        var considered = new List<string>();
        foreach (var location in zone.m_locations.Where(l => l != null && !string.IsNullOrEmpty(l.m_prefabName))
                     .Where(l => l.m_biome == Heightmap.Biome.DeepNorth || l.m_prefabName.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            considered.Add(location.m_prefabName);
            GameObject? container = null;
            try
            {
                container = ZoneManager.Instance.CreateLocationContainer(location.m_prefabName);
                if (!container) continue;
                var parts = container.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.StartsWith("LastBossGate", StringComparison.Ordinal)).ToArray();
                if (parts.Length == 0) continue;
                var root = parts.FirstOrDefault(t => t.name == "LastBossGate") ?? CommonAncestor(parts);
                var copy = UnityEngine.Object.Instantiate(root.gameObject);
                copy.name = "aesir-gate";
                StripToVisuals(copy);
                if (copy.GetComponentsInChildren<Renderer>(true).Length < 2)
                {
                    UnityEngine.Object.DestroyImmediate(copy);
                    continue;
                }
                source = location.m_prefabName + "/" + root.name;
                return copy;
            }
            finally
            {
                if (container) UnityEngine.Object.Destroy(container);
            }
        }
        throw new InvalidOperationException("no LastBossGate assembly in any Deep North or boss location (checked: " +
                                            (considered.Count == 0 ? "none" : string.Join(", ", considered)) + ").");
    }

    private static Transform CommonAncestor(Transform[] parts)
    {
        var candidate = parts[0].parent ?? parts[0];
        while (candidate.parent != null && !parts.All(p => p.IsChildOf(candidate))) candidate = candidate.parent;
        return candidate;
    }

    /// <summary>Keeps geometry, LODs, colliders, lights and particles; removes every behaviour.</summary>
    private static void StripToVisuals(GameObject root)
    {
        foreach (var body in root.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(body);
        foreach (var component in root.GetComponentsInChildren<Component>(true).Reverse())
        {
            if (!component || component is Transform || component is MeshFilter || component is Renderer ||
                component is LODGroup || component is Collider || component is Light || component is ParticleSystem)
                continue;
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    private static Vector3 Measure(GameObject gate)
    {
        var renderers = gate.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
        if (renderers.Length == 0) return Vector3.zero;
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds.size;
    }

    private static GameObject? ResolveDeepNorthGateDonor()
    {
        foreach (var name in PreferredDonorNames)
        {
            var prefab = PrefabManager.Instance.GetPrefab(name);
            if (prefab && HasGateGeometry(prefab)) return prefab;
        }

        // Internal child names may move between patches. Restrict fallback discovery to assets
        // explicitly carrying the Morkhalla identity that the Aesir Passage pin names; never
        // silently clone an unrelated gate such as the final-boss or dvergr town gates.
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(candidate => candidate &&
                candidate.name.IndexOf("morkhalla", StringComparison.OrdinalIgnoreCase) >= 0 &&
                candidate.name.IndexOf("gate", StringComparison.OrdinalIgnoreCase) >= 0)
            .Where(HasGateGeometry)
            .OrderByDescending(candidate => candidate.GetComponentsInChildren<Renderer>(true).Length)
            .FirstOrDefault();
    }

    private static bool HasGateGeometry(GameObject candidate) =>
        candidate.GetComponentsInChildren<Renderer>(true).Length >= 2;

    private static void RemoveVanillaEncounterAuthority(GameObject gate)
    {
        foreach (var component in gate.GetComponentsInChildren<Component>(true))
        {
            if (!component) continue;
            if (!ForbiddenGameplayComponents.Contains(component.GetType().Name)) continue;
            UnityEngine.Object.DestroyImmediate(component);
        }
    }

    private static void ApplyUnderworldMaterials(GameObject gate)
    {
        var materialMap = new Dictionary<Material, Material>();
        foreach (var renderer in gate.GetComponentsInChildren<Renderer>(true))
        {
            var sourceMaterials = renderer.sharedMaterials;
            var replacements = new Material[sourceMaterials.Length];
            for (var i = 0; i < sourceMaterials.Length; i++)
            {
                var source = sourceMaterials[i];
                if (!source)
                {
                    replacements[i] = source!;
                    continue;
                }

                if (!materialMap.TryGetValue(source, out var replacement))
                {
                    replacement = new Material(source)
                    {
                        name = $"Magenheim_DeepGate_{source.name}",
                    };
                    RethemeMaterial(replacement);
                    materialMap.Add(source, replacement);
                }
                replacements[i] = replacement;
            }
            renderer.sharedMaterials = replacements;
        }
    }

    private static void RethemeMaterial(Material material)
    {
        // Preserve the donor's normal/metallic/UV work while replacing its readable surface language.
        // These are independent material instances, so no vanilla Morkhalla material is mutated.
        var stone = new Color(0.105f, 0.115f, 0.135f, 1f);
        var mineral = new Color(0.20f, 0.075f, 0.31f, 1f);
        if (material.HasProperty("_Color")) material.SetColor("_Color", stone);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", stone);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", mineral * 2.4f);
        }
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", Mathf.Max(material.GetFloat("_Metallic"), 0.18f));
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", Mathf.Min(material.GetFloat("_Glossiness"), 0.42f));
    }
}

/// <summary>Marker only. Transition interaction is deliberately owned by the server-authoritative Underworld gate runtime.</summary>
internal sealed class UnderworldDeepGateIdentity : MonoBehaviour
{
    internal string DonorPrefabName { get; set; } = string.Empty;
}
