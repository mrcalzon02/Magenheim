using HarmonyLib;
using UnityEngine;

namespace Magenheim.Runtime;

// Native Minimap has no palette entries for our reserved biome flags.
[HarmonyPatch(typeof(Minimap), "GetPixelColor", new[] { typeof(Heightmap.Biome) })]
internal static class UnderworldMinimapPalette
{
    private static bool Prefix(Heightmap.Biome __0, ref Color __result)
    {
        switch ((int)__0)
        {
            case 1024: __result = new Color(.20f,.43f,.29f); return false;
            case 2048: __result = new Color(.10f,.27f,.34f); return false;
            case 4096: __result = new Color(.49f,.33f,.15f); return false;
            case 8192: __result = new Color(.58f,.73f,.80f); return false;
            case 16384: __result = new Color(.43f,.33f,.57f); return false;
            case 32768: __result = new Color(.38f,.27f,.23f); return false;
            default: return true;
        }
    }
}
