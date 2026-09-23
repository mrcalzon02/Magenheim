using System;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Marks a child network prefab that should inherit the deterministic identity of its admitted
/// native Underworld structure. Persistence remains owned by the child's ordinary ZNetView/ZDO.
/// </summary>
internal sealed class UnderworldPersistentObjectBinding : MonoBehaviour
{
    internal void Bind(UnderworldWorldIdentity worldIdentity, UnderworldGeneratedObjectIdentity structureIdentity)
    {
        if (worldIdentity is null) throw new ArgumentNullException(nameof(worldIdentity));
        if (structureIdentity is null) throw new ArgumentNullException(nameof(structureIdentity));

        var adapter = GetComponent<UnderworldZdoStateAdapter>()
            ?? throw new InvalidOperationException("Persistent Underworld object is missing its ZDO state adapter.");
        adapter.Bind(worldIdentity, structureIdentity);
    }
}
