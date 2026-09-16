using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads the editable Colossus model and binds its attack presentation runtime.</summary>
internal static class DeepFractureDeepColossusVisual
{
    internal static void Apply(GameObject prefab,ElementalAlignment alignment)
    {
        ModelAssets.Load(prefab,"deep-fracture-deep-colossus-visual-"+alignment.ToString().ToLowerInvariant());
        var impact=prefab.GetComponent<DeepFractureHeavyImpactRuntime>()??prefab.AddComponent<DeepFractureHeavyImpactRuntime>();
        impact.ImpactProfile=DeepFractureHeavyImpactRuntime.Profile.DeepColossus;
    }
}
