using System;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Definitions;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Recoverable bulk geodes use native pile construction/drop authority.</summary>
internal static class GeodePileRegistrar
{
    internal static void Register(GeodeDefinition definition, GameObject itemPrefab)
    {
        var name = definition.PrefabName + "_Pile";
        if (PrefabManager.Instance.GetPrefab(name))
            throw new InvalidOperationException("Occupied geode pile identity: " + name);
        var shared = itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
        var piece = new CustomPiece(name, "stone_pile", new PieceConfig
        {
            Name = shared.m_name + " Pile",
            Description = "Fifty geodes in a compact stack. Dismantle with the hammer to recover them.",
            PieceTable = "Hammer",
            Category = "Stacks",
            Requirements = new[] { new RequirementConfig(definition.PrefabName, 50, 0, true) },
            Icon = shared.m_icons[0],
        });
        var prefab = piece.PiecePrefab;
        var layer = prefab.GetComponentsInChildren<Collider>(true).First(c => !c.isTrigger).gameObject.layer;
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
        foreach (var collider in prefab.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        var source = itemPrefab.GetComponentsInChildren<Transform>(true)
            .Single(t => t.name == "magenheim.geode-sample.visual");
        var sourceCollider = source.GetComponent<BoxCollider>();
        var sourceBounds = sourceCollider.size;
        var scale = .30f / Mathf.Max(sourceBounds.x, Mathf.Max(sourceBounds.y, sourceBounds.z));
        var visual = new GameObject("magenheim.geode-pile.visual") { layer = prefab.layer };
        visual.transform.SetParent(prefab.transform, false);
        int index = 0;
        // Square close-packed layers: 9 + 4 + 1, with each layer supported below.
        for (int level = 0; level < 3; level++)
        {
            int width = 3 - level;
            for (int x = 0; x < width; x++)
            for (int z = 0; z < width; z++)
            {
                var ball = UnityEngine.Object.Instantiate(source.gameObject, visual.transform);
                ball.name = "geode-" + index++;
                ball.transform.localScale = Vector3.one * scale;
                ball.transform.localRotation = Quaternion.Euler(0, index * 37f, 0);
                ball.transform.localPosition = new Vector3((x - (width - 1) * .5f) * .30f,
                    .15f + level * .245f, (z - (width - 1) * .5f) * .30f)
                    - ball.transform.localRotation * (sourceCollider.center * scale);
                foreach (var collider in ball.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                ball.SetActive(true);
            }
        }
        var collision = new GameObject("geode-pile-collision") { layer = layer };
        collision.transform.SetParent(prefab.transform, false);
        var box = collision.AddComponent<BoxCollider>();
        box.center = new Vector3(0, .4f, 0); box.size = new Vector3(.92f, .8f, .92f);
        var wear = prefab.GetComponent<WearNTear>();
        if (wear)
        {
            wear.m_new = visual;
            wear.m_worn = UnityEngine.Object.Instantiate(visual, prefab.transform);
            wear.m_broken = UnityEngine.Object.Instantiate(visual, prefab.transform);
            wear.m_worn.name = "geode-pile-worn"; wear.m_broken.name = "geode-pile-weathered";
            wear.m_worn.SetActive(false); wear.m_broken.SetActive(false);
            wear.m_fragmentRoots = new[] { visual, wear.m_worn, wear.m_broken };
        }
        piece.Piece.m_icon = NativeEquipmentIcons.Render(visual);
        if (!PieceManager.Instance.AddPiece(piece))
            throw new InvalidOperationException("Jotunn refused geode pile: " + name);
    }
}
