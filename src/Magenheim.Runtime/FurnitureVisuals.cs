using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class FurnitureVisuals
{
    internal const string GeodeTable = "furniture-geode-table";
    internal const string GeodeChair = "furniture-geode-chair";
    internal const string CrystalBench = "furniture-crystal-bench";
    internal const string CrystalBed = "furniture-crystal-bed";
    internal const string MineralShelf = "furniture-mineral-shelf";
    internal const string LapidaryCabinet = "furniture-lapidary-cabinet";
    internal const string GeoDesk = "furniture-geo-desk";
    internal const string GeodePedestal = "furniture-geode-pedestal";
    internal const string CrystalDivider = "furniture-crystal-divider";
    internal const string CrystalThrone = "furniture-crystal-throne";
    internal static GameObject Apply(GameObject prefab, string modelId) => ModelAssets.Load(prefab, modelId, item: false);
}
