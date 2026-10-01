using System;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Loaded native effect materials; Unity built-in particle shaders are not shipped by Valheim.</summary>
internal static class NativeEffectMaterials
{
    private static Material? _shared;
    internal static Material SharedParticleMaterial
    {
        get
        {
            if (_shared) return _shared;
            var fire = PrefabManager.Instance.GetPrefab("fire_pit")
                ?? throw new InvalidOperationException("Native particle donor fire_pit is unavailable.");
            var smoke = fire.GetComponentsInChildren<ParticleSystemRenderer>(true)
                .OrderByDescending(r => r.name.IndexOf("smoke", StringComparison.OrdinalIgnoreCase) >= 0)
                .FirstOrDefault(r => r.sharedMaterial && r.sharedMaterial.shader && r.sharedMaterial.mainTexture);
            if (!smoke) throw new InvalidOperationException("Native textured particle material is unavailable.");
            return _shared = new Material(smoke.sharedMaterial) { name = "magenheim.native-particle-template" };
        }
    }
    internal static Material CreateParticleMaterial() => new(SharedParticleMaterial);
}
