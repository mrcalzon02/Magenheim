using System;
using System.Linq;
using Magenheim.Core;
using UnityEngine;
namespace Magenheim.Runtime;
/// <summary>Loads editable assets from assets/models/source; geometry is never generated in game.</summary>
internal static class CrystalWeaponVisuals
{
    internal const string Sword = "crystal-weapon-sword";
    internal const string Greatsword = "crystal-weapon-greatsword";
    internal const string Axe = "crystal-weapon-axe";
    internal const string Battleaxe = "crystal-weapon-battleaxe";
    internal const string Mace = "crystal-weapon-mace";
    internal const string Spear = "crystal-weapon-spear";
    internal const string Knife = "crystal-weapon-knife";
    internal const string Atgeir = "crystal-weapon-atgeir";
    internal const string Bow = "crystal-weapon-bow";
    internal const string Crossbow = "crystal-weapon-crossbow";
    internal static GameObject Apply(GameObject prefab, string modelId) => ModelAssets.Load(prefab, modelId, item: true);
}
