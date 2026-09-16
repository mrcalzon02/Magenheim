using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class CapstoneArtifactVisuals
{
    internal const string EikthyrStormheart = "boss-eikthyr-stormheart";
    internal const string ElderrootHeart = "boss-elder-rootheart";
    internal const string BonemassRotheart = "boss-bonemass-rotheart";
    internal const string ModerRimeheart = "boss-moder-rimeheart";
    internal const string YagluthSunheart = "boss-yagluth-sunheart";
    internal const string QueenVeilheart = "boss-queen-veilheart";
    internal const string FaderAshheart = "boss-fader-ashheart";
    internal const string KallWinterheart = "boss-kall-winterheart";
    internal const string FateShard = "fate-shard";
    internal const string FateCrystal = "fate-crystal";
    internal const string NornSpindle = "norn-spindle";
    internal static GameObject Apply(GameObject prefab, string modelId, bool itemModel = true, float scale = 1f) => ModelAssets.Load(prefab, modelId, item: itemModel, scale: scale);
}
