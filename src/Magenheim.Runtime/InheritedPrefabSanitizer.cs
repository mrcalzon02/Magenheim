using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Removes presentation behavior that belongs to a vanilla clone carrier but not to the owned
/// Magenheim piece. Gameplay components intentionally retained by the registrar are left intact.
/// </summary>
internal static class InheritedPrefabSanitizer
{
    internal static int DisableInheritedAudio(GameObject prefab)
    {
        if (prefab is null) throw new ArgumentNullException(nameof(prefab));

        var disabled = 0;
        foreach (var source in prefab.GetComponentsInChildren<AudioSource>(true))
        {
            source.Stop();
            source.playOnAwake = false;
            source.loop = false;
            source.clip = null;
            source.enabled = false;
            disabled++;
        }

        return disabled;
    }
}
