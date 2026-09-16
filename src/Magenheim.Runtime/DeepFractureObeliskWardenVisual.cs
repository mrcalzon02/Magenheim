using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class DeepFractureObeliskWardenVisual
{
    internal static void Apply(GameObject prefab,ElementalAlignment alignment)
    {
        ModelAssets.Load(prefab,"deep-fracture-obelisk-warden-visual-"+alignment.ToString().ToLowerInvariant());
        var pulse=prefab.GetComponent<DeepFractureWardenPulseRuntime>() ?? prefab.AddComponent<DeepFractureWardenPulseRuntime>();
        pulse.Alignment=alignment;
    }
}
