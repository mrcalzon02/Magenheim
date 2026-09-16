using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class CrystalBannerVisuals
{

    internal enum BannerStyle { Standard, Swallowtail, Pennant }
    internal static GameObject Apply(GameObject prefab, BannerStyle style, ElementalAlignment element) => ModelAssets.Load(prefab, "crystal-banner-" + style.ToString().ToLowerInvariant() + "-" + element.ToString().ToLowerInvariant());
}
