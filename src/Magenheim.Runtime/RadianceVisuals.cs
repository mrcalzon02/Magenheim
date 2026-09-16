using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class RadianceVisuals
{

    internal static GameObject Apply(GameObject prefab, string assetName) => ModelAssets.Load(prefab, assetName, item: true);
}
