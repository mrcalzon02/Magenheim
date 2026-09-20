using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class CrystalBedVisuals
{

    /// <summary>The model id, which is also the icon's asset name.</summary>
    internal static string ModelId(ElementalAlignment element) => "crystal-bed-" + element.ToString().ToLowerInvariant();

    internal static GameObject Apply(GameObject prefab, ElementalAlignment element) => ModelAssets.Load(prefab, ModelId(element));
}
