using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class CrystalBedVisuals
{

    internal static GameObject Apply(GameObject prefab, ElementalAlignment element) => ModelAssets.Load(prefab, "crystal-bed-" + element.ToString().ToLowerInvariant());
}
