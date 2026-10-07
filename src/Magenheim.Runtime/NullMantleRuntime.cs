using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Reversible presentation and weaker adaptive protection inherited from the Nowhere King.</summary>
internal sealed class NullMantleRuntime : MonoBehaviour
{
    private const float WindowSeconds = 10f;
    private const float DominanceThreshold = .45f;
    private const float Resistance = .22f;
    private const float ActiveSeconds = 6f;

    private readonly List<Sample> _samples = new();
    private readonly Dictionary<Renderer, MaterialPropertyBlock[]> _originalBlocks = new();
    private readonly List<Material> _effectMaterials = new();
    private Transform? _eyes;
    private Transform? _eyeAnchor;

    private GameObject? _presentation;
    private DamageType _active = DamageType.None;
    private float _activeUntil;

    private struct Sample
    {
        internal float Time;
        internal DamageType Type;
        internal float Amount;
    }

    private enum DamageType
    {
        None,
        Fire,
        Frost,
        Lightning,
        Poison,
        Spirit,
    }

    internal void SetEquipped(bool equipped)
    {
        if (equipped && NullMantleEquipmentState.IsEquipped(GetComponent<Player>()))
        {
            if (_presentation == null)
            {
                try { ApplyPresentation(); }
                catch { RemovePresentation(); throw; }
            }
        }
        else if (_presentation != null || _originalBlocks.Count > 0)
        {
            RemovePresentation();
        }
    }

    internal void ObserveAndMitigate(HitData hit)
    {
        if (hit == null || _presentation == null ||
            !NullMantleEquipmentState.IsEquipped(GetComponent<Player>())) return;
        var now = Time.time;
        Mitigate(hit, now);
        Add(now, DamageType.Fire, hit.m_damage.m_fire);
        Add(now, DamageType.Frost, hit.m_damage.m_frost);
        Add(now, DamageType.Lightning, hit.m_damage.m_lightning);
        Add(now, DamageType.Poison, hit.m_damage.m_poison);
        Add(now, DamageType.Spirit, hit.m_damage.m_spirit);

        for (var i = _samples.Count - 1; i >= 0; i--)
            if (now - _samples[i].Time > WindowSeconds)
                _samples.RemoveAt(i);

        var totals = new float[6];
        var all = 0f;
        foreach (var sample in _samples)
        {
            totals[(int)sample.Type] += sample.Amount;
            all += sample.Amount;
        }
        if (all <= 0f) return;

        var best = DamageType.None;
        var value = 0f;
        for (var i = 1; i < totals.Length; i++)
        {
            if (totals[i] <= value) continue;
            value = totals[i];
            best = (DamageType)i;
        }

        if (value / all >= DominanceThreshold)
        {
            _active = best;
            _activeUntil = now + ActiveSeconds;
        }
    }

    private void Mitigate(HitData hit, float now)
    {
        if (_active == DamageType.None || now > _activeUntil) return;
        var multiplier = 1f - Resistance;
        switch (_active)
        {
            case DamageType.Fire:
                hit.m_damage.m_fire *= multiplier;
                break;
            case DamageType.Frost:
                hit.m_damage.m_frost *= multiplier;
                break;
            case DamageType.Lightning:
                hit.m_damage.m_lightning *= multiplier;
                break;
            case DamageType.Poison:
                hit.m_damage.m_poison *= multiplier;
                break;
            case DamageType.Spirit:
                hit.m_damage.m_spirit *= multiplier;
                break;
        }
    }

    private void Add(float time, DamageType type, float amount)
    {
        if (amount > 0f && !float.IsNaN(amount) && !float.IsInfinity(amount))
            _samples.Add(new Sample { Time = time, Type = type, Amount = amount });
    }

    private void ApplyPresentation()
    {
        _presentation = new GameObject("Magenheim_NullMantle_Form");
        _presentation.transform.SetParent(transform, false);

        SyncDarkening();

        // EyePos is root-relative in the installed Player prefab. Helmet_attach belongs to
        // the animated Head bone and therefore follows crouching, attacks and head turns.
        _eyeAnchor = GetComponentInChildren<VisEquipment>(true)?.m_helmet;
        if (!_eyeAnchor) throw new InvalidOperationException("Null Mantle requires the native animated helmet attachment.");
        _eyes = new GameObject("RedEyes").transform;
        _eyes.SetParent(_presentation.transform, false);
        var eyeMaterial = ModelAssets.CreateSurfaceMaterial();
        eyeMaterial.color = new Color(.9f, .002f, .001f);
        eyeMaterial.EnableKeyword("_EMISSION");
        eyeMaterial.SetColor("_EmissionColor", new Color(4f, .008f, .004f));
        _effectMaterials.Add(eyeMaterial);
        foreach (var side in new[] { -1f, 1f })
        {
            var eye = EffectModelAssets.Create("effect-orb");
            eye.name = side < 0 ? "LeftEye" : "RightEye";
            eye.transform.SetParent(_eyes, false);
            eye.transform.localPosition = new Vector3(side * .033f, 0f, .012f);
            eye.transform.localScale = new Vector3(.022f, .012f, .012f);
            foreach (var collider in eye.GetComponentsInChildren<Collider>(true)) Destroy(collider);
            foreach (var renderer in eye.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = eyeMaterial;
        }
        var light = _eyes.gameObject.AddComponent<Light>();
        light.color = new Color(.9f, .015f, .01f);
        light.range = .35f;
        light.intensity = .65f;
        light.shadows = LightShadows.None;

        var fog = new GameObject("NowhereFog");
        fog.transform.SetParent(_presentation.transform, false);
        fog.transform.localPosition = Vector3.up;
        var particles = fog.AddComponent<ParticleSystem>();
        fog.GetComponent<ParticleSystemRenderer>().sharedMaterial = NativeEffectMaterials.SharedParticleMaterial;
        var main = particles.main;
        main.startLifetime = 2.4f;
        main.startSpeed = .18f;
        main.startSize = .75f;
        main.startColor = new Color(.01f, .005f, .018f, .34f);
        main.maxParticles = 32;
        var emission = particles.emission;
        emission.rateOverTime = 8f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.15f;
    }

    private void LateUpdate()
    {
        if (!NullMantleEquipmentState.IsEquipped(GetComponent<Player>()))
        {
            SetEquipped(false);
            return;
        }
        if (!_presentation) return;
        SyncDarkening();
        if (_eyes && _eyeAnchor)
        {
            // Measured in the installed prefab: native eye height (1.808 m), advanced
            // 0.11 m to the face, transformed into Helmet_attach's rest coordinate space.
            _eyes.position = _eyeAnchor.TransformPoint(new Vector3(.00030136f, .03380953f, .05945418f));
            _eyes.rotation = _eyeAnchor.rotation;
        }
    }

    private void SyncDarkening()
    {
        if (_presentation == null) return;
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.transform.IsChildOf(_presentation.transform)) continue;
            var materials = renderer.sharedMaterials;
            if (!_originalBlocks.TryGetValue(renderer, out var originals))
            {
                originals = new MaterialPropertyBlock[materials.Length];
                for (var i = 0; i < originals.Length; i++)
                {
                    originals[i] = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(originals[i], i);
                }
                _originalBlocks[renderer] = originals;
            }
            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (!material) continue;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, i);
                var black = new Color(.003f, .002f, .006f, 1f);
                if (material.HasProperty("_Color")) block.SetColor("_Color", black);
                // Native body tint is separate from the ordinary gear tint.
                if (material.HasProperty("_SkinColor")) block.SetColor("_SkinColor", black);
                if (material.HasProperty("_EmissionColor")) block.SetColor("_EmissionColor", Color.black);
                if (material.HasProperty("_Metallic")) block.SetFloat("_Metallic", 0f);
                if (material.HasProperty("_Glossiness")) block.SetFloat("_Glossiness", .06f);
                renderer.SetPropertyBlock(block, i);
            }
        }
    }

    private void RemovePresentation()
    {
        foreach (var pair in _originalBlocks)
            if (pair.Key)
                for (var i = 0; i < Math.Min(pair.Value.Length, pair.Key.sharedMaterials.Length); i++)
                    pair.Key.SetPropertyBlock(pair.Value[i].isEmpty ? null : pair.Value[i], i);

        foreach (var material in _effectMaterials) if (material) Destroy(material);
        _effectMaterials.Clear();
        _originalBlocks.Clear();
        Destroy(_presentation);
        _presentation = null;
        _eyes = null;
        _eyeAnchor = null;
        _samples.Clear();
        _active = DamageType.None;
        _activeUntil = 0f;
    }

    private void OnDestroy()
    {
        RemovePresentation();
    }

    private void OnDisable() => RemovePresentation();
}

[HarmonyPatch(typeof(Player), "Update", new Type[0])]
internal static class NullMantleEquipmentPatch
{
    private static void Postfix(Player __instance)
    {
        if (__instance == null) return;
        var equipped = NullMantleEquipmentState.IsEquipped(__instance);
        var runtime = __instance.GetComponent<NullMantleRuntime>();
        if (equipped)
        {
            runtime ??= __instance.gameObject.AddComponent<NullMantleRuntime>();
            runtime.SetEquipped(true);
        }
        else if (runtime != null)
        {
            runtime.SetEquipped(false);
        }
    }
}

[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
internal static class NullMantleDamagePatch
{
    private static void Prefix(Character __instance, HitData hit)
    {
        if (__instance is Player player)
        {
            var runtime = player.GetComponent<NullMantleRuntime>();
            runtime?.ObserveAndMitigate(hit);
        }
    }
}
