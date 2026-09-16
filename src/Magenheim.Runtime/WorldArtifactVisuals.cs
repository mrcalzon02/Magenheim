using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class WorldArtifactVisuals
{
    internal const string CrystalWardstone = "crystal-wardstone";
    internal const string PassageStone = "passage-stone";
    internal const string RunedTotem = "runed-totem";
    internal const string RunicKeelstone = "runic-keelstone";
    internal const string CrystalLantern = "crystal-lantern";
    internal const string CrystalBrazier = "crystal-brazier";
    internal const string RuneEngraver = "rune-engraver";
    internal const string SeidrRitualFocus = "seidr-ritual-focus";
    internal const string SpiritFetishWolf = "spirit-fetish-wolf";
    internal const string SpiritFetishRaven = "spirit-fetish-raven";
    internal const string SpiritFetishWarrior = "spirit-fetish-warrior";
    internal static GameObject Apply(GameObject prefab, string modelId, bool itemModel = false, float scale = 1f, Color? accentOverride = null) {
        var root = ModelAssets.Load(prefab, modelId, item: itemModel, scale: scale);
        if (accentOverride.HasValue) foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)) {
            var source=renderer.sharedMaterial; if(source.name.IndexOf(".crystal",StringComparison.Ordinal)<0)continue;
            var bright=source.name.EndsWith(".crystal-bright",StringComparison.Ordinal);var tint=bright?Color.Lerp(accentOverride.Value,Color.white,.28f):accentOverride.Value;
            var material=new Material(source);material.color=tint;if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",tint*(bright?.62f:.42f));renderer.sharedMaterial=material;
        }
        return root;
    }
}
