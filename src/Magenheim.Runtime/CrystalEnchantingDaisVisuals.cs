using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class CrystalEnchantingDaisVisuals
{

    /// <summary>
    /// The dais clones a building-piece donor whose piece shader projects its surface from world
    /// position and ignores mesh UVs, which is what made it read as a smeared lattice in the field.
    /// Cloning the same static-rock donor the geode shell and the slabs/pillars use fixes it.
    /// </summary>
    internal static GameObject Apply(GameObject prefab) =>
        ModelAssets.Load(prefab, "crystal-enchanting-dais", materialSource: CrystalArchitectureVisuals.SurfaceDonorMaterial());
}
