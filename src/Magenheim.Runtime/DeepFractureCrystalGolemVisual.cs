using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads the editable Crystal Golem model and binds its attack presentation runtime.</summary>
internal static class DeepFractureCrystalGolemVisual
{
    internal static void Apply(GameObject prefab,ElementalAlignment alignment)
    {
        ModelAssets.Load(prefab,"deep-fracture-crystal-golem-visual-"+alignment.ToString().ToLowerInvariant());
        var impact=prefab.GetComponent<DeepFractureHeavyImpactRuntime>()??prefab.AddComponent<DeepFractureHeavyImpactRuntime>();
        impact.ImpactProfile=DeepFractureHeavyImpactRuntime.Profile.CrystalGolem;
        impact.Alignment=alignment;
    }
}
