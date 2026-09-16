using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core;
using Magenheim.Core.DeepFractures;
using UnityEngine;
namespace Magenheim.Runtime;
internal static class DeepFractureRoomVisuals {
    internal const string PassagePrefabName="Magenheim_DF_Passage",TraversalNodePrefabName="Magenheim_DF_TraversalNode";
    internal const float NominalDistrictSize=96f,PassageLength=16f;
internal static string RoomPrefabName(string pieceFamilyId)
    {
        if (string.IsNullOrWhiteSpace(pieceFamilyId) || !pieceFamilyId.StartsWith("DF-", StringComparison.Ordinal))
            throw new ArgumentException("A canonical Deep Fracture piece family ID is required.", nameof(pieceFamilyId));
        return "Magenheim_DF_" + pieceFamilyId.Replace('-', '_');
    }
    internal static GameObject CreateDistrictPrefab(DeepFracturePieceFamily family) {
        if(family==null)throw new ArgumentNullException(nameof(family));family.Validate();
        var root=CreateRoomRoot(RoomPrefabName(family.Id),new Vector3Int(96,32,96));ModelAssets.Load(root,family.Id,hideOriginal:false);return root;
    }
    internal static GameObject CreatePassagePrefab(){var root=CreateRoomRoot(PassagePrefabName,new Vector3Int(10,10,16));ModelAssets.Load(root,"deep-fracture-passage",hideOriginal:false);return root;}
    internal static GameObject CreateTraversalNodePrefab(){var root=CreateRoomRoot(TraversalNodePrefabName,new Vector3Int(12,10,12));ModelAssets.Load(root,"deep-fracture-traversal",hideOriginal:false);root.AddComponent<DeepFractureTraversalPortal>();return root;}
private static GameObject CreateRoomRoot(string name, Vector3Int size)
    {
        var root = new GameObject(name);
        root.SetActive(false);
        var room = root.AddComponent<Room>();
        room.m_enabled = true;
        room.m_size = size;
        return root;
    }
internal static void ApplyElementalState(Room placedRoom, IReadOnlyList<ElementalAlignment> elementalStates)
    {
        if (placedRoom is null)
            throw new ArgumentNullException(nameof(placedRoom));
        if (elementalStates is null || elementalStates.Count == 0)
            return;

        var red = 0f;
        var green = 0f;
        var blue = 0f;
        foreach (var element in elementalStates)
        {
            var tint = ElementVisualPalette.Tint(element);
            red += tint.r;
            green += tint.g;
            blue += tint.b;
        }

        var blend = new Color(red / elementalStates.Count, green / elementalStates.Count, blue / elementalStates.Count, 1f);
        foreach (var renderer in placedRoom.GetComponentsInChildren<Renderer>(true)
                     .Where(candidate => candidate.gameObject.name.IndexOf("Crystal", StringComparison.OrdinalIgnoreCase) >= 0
                         || candidate.gameObject.name.IndexOf("Seam", StringComparison.OrdinalIgnoreCase) >= 0
                         || candidate.gameObject.name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            if (renderer.sharedMaterial is null)
                continue;
            var material = new Material(renderer.sharedMaterial) { name = renderer.sharedMaterial.name + "_Placed" };
            TintMaterial(material, blend, blend * .35f);
            renderer.sharedMaterial = material;
        }
    }
private static void TintMaterial(Material material, Color color, Color? emission)
    {
        if (material.HasProperty("_Color"))
            material.color = color;
        if (emission.HasValue && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value);
        }
    }
}
