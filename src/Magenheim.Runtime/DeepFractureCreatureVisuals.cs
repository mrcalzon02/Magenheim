using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class DeepFractureCreatureVisuals
{

    internal static void ApplyAnnoyanceWisp(GameObject p,ElementalAlignment a) => ModelAssets.Load(p,"deep-fracture-creature-AnnoyanceWisp-"+a.ToString().ToLowerInvariant());
    internal static void ApplyGeodeCrawler(GameObject p,ElementalAlignment a) => ModelAssets.Load(p,"deep-fracture-creature-GeodeCrawler-"+a.ToString().ToLowerInvariant());
    internal static void ApplyShardling(GameObject p,ElementalAlignment a) => ModelAssets.Load(p,"deep-fracture-creature-Shardling-"+a.ToString().ToLowerInvariant());
    internal static void ApplyCrystalParasite(GameObject p,ElementalAlignment a) => ModelAssets.Load(p,"deep-fracture-creature-CrystalParasite-"+a.ToString().ToLowerInvariant());
    internal static void ApplyCrystalRevenant(GameObject p,ElementalAlignment a) => ModelAssets.Load(p,"deep-fracture-creature-CrystalRevenant-"+a.ToString().ToLowerInvariant());
    internal static void ApplyFacetSentry(GameObject p,ElementalAlignment a) => ModelAssets.Load(p,"deep-fracture-creature-FacetSentry-"+a.ToString().ToLowerInvariant());
    internal static void ApplyStoneSentinel(GameObject p,ElementalAlignment a) => ModelAssets.Load(p,"deep-fracture-creature-StoneSentinel-"+a.ToString().ToLowerInvariant());
    internal static void ApplyCrystalHound(GameObject p,ElementalAlignment a) => ModelAssets.Load(p,"deep-fracture-creature-CrystalHound-"+a.ToString().ToLowerInvariant());
    internal static void ApplyBurrower(GameObject p,ElementalAlignment a) => ModelAssets.Load(p,"deep-fracture-creature-Burrower-"+a.ToString().ToLowerInvariant());
    internal static void ApplyStoneGuardian(GameObject p,ElementalAlignment a) => ModelAssets.Load(p,"deep-fracture-creature-StoneGuardian-"+a.ToString().ToLowerInvariant());
}
