using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class GeologyDecorVisuals
{
    internal const string GeodeBowl = "decor-geode-bowl";
    internal const string CutGeodePlaque = "decor-cut-geode-plaque";
    internal const string CrystalEndTable = "decor-crystal-end-table";
    internal const string GeologistStool = "decor-geologist-stool";
    internal const string MineralDisplayCase = "decor-mineral-display-case";
    internal const string CrystalWallSconce = "decor-crystal-wall-sconce";
    internal const string StrataMapTable = "decor-strata-map-table";
    internal const string SpecimenSideboard = "decor-specimen-sideboard";
    internal const string CrystalCoatRack = "decor-crystal-coat-rack";
    internal const string GeodeHearthMantel = "decor-geode-hearth-mantel";
    internal static GameObject Apply(GameObject prefab, string modelId) => ModelAssets.Load(prefab, modelId, item: false);
}
