using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class CrystalBannerVisuals
{

    internal enum BannerStyle { Standard, Swallowtail, Pennant }

    /// <summary>The model id, which is also the icon's asset name.</summary>
    internal static string ModelId(BannerStyle style, ElementalAlignment element) =>
        "crystal-banner-" + style.ToString().ToLowerInvariant() + "-" + element.ToString().ToLowerInvariant();

    internal static GameObject Apply(GameObject prefab, BannerStyle style, ElementalAlignment element) => ModelAssets.Load(prefab, ModelId(style, element));
}
