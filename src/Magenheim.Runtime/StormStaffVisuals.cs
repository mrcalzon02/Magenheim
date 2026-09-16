using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class StormStaffVisuals
{

    internal static GameObject Apply(GameObject prefab, string prefabName) => ModelAssets.Load(prefab, prefabName, item: true);
}
