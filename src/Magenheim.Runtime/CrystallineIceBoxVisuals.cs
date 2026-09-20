using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class CrystallineIceBoxVisuals
{

    /// <summary>The model id, which is also the icon's asset name.</summary>
    internal const string ModelId = "crystalline-ice-box";

    internal static GameObject Apply(GameObject prefab) => ModelAssets.Load(prefab, ModelId);
}
