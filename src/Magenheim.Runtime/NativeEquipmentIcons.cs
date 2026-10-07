using System;
using Jotunn.Managers;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Use Jotunn's native visual clone renderer for owned donor equipment.</summary>
internal static class NativeEquipmentIcons
{
    internal static void Apply(GameObject ownedPrefab)
    {
        var shared = ownedPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
        shared.m_icons = new[] { Render(ownedPrefab) };
    }

    internal static Sprite Render(GameObject ownedPrefab)
    {
        var request = new RenderManager.RenderRequest(ownedPrefab)
        {
            Width = 256,
            Height = 256,
            UseCache = false,
        };
        var sprite = RenderManager.Instance.Render(request);
        if (!sprite)
            throw new InvalidOperationException("Jotunn could not render owned equipment icon: " + ownedPrefab.name);
        sprite.name = "magenheim.icon." + ownedPrefab.name;
        return sprite;
    }
}
