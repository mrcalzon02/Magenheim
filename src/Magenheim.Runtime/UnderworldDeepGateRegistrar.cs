using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Creates Magenheim's Deep Gate by cloning the shipped Morkhalla jotun gate geometry, which is
/// the Deep North passage Valheim surfaces to players as the "Aesir Passage" map pin.
/// The vanilla object is never mutated. Boss/offering behaviour is removed from the clone so the
/// Magenheim transition authority can own interaction and persistence independently.
/// </summary>
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

    internal UnderworldDeepGateRegistrar(ManualLogSource log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal void Register()
    {
        if (_registered || _subscribed) return;
        PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
        _subscribed = true;
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
