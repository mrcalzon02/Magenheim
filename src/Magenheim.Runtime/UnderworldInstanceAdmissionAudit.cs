using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine.SceneManagement;

namespace Magenheim.Runtime;

/// <summary>
/// Final fail-closed admission gate for the concurrent multi-world architecture. Construction-time
/// checks prove each component can be detached; this audit proves the assembled post-load graph is
/// still two native world instances inside one parent save before instance 1 may become Active.
/// </summary>
internal static class UnderworldInstanceAdmissionAudit
{
    private static readonly FieldInfo WorldUidField =
        AccessTools.Field(typeof(World), "m_uid")
        ?? throw new MissingFieldException(typeof(World).FullName, "m_uid");

    private static readonly FieldInfo ZdoIdentityField =
        AccessTools.Field(typeof(ZDOMan), "m_sessionID") ??
        AccessTools.Field(typeof(ZDOMan), "m_myid") ??
        throw new MissingFieldException(typeof(ZDOMan).FullName, "m_sessionID/m_myid");

    private static readonly FieldInfo ZdoPeersField =
        AccessTools.Field(typeof(ZDOMan), "m_peers")
        ?? throw new MissingFieldException(typeof(ZDOMan).FullName, "m_peers");

    private static readonly Type ZdoPeerType =
        AccessTools.Inner(typeof(ZDOMan), "ZDOPeer")
        ?? throw new MissingMemberException(typeof(ZDOMan).FullName, "ZDOPeer");

    private static readonly FieldInfo NetworkPeerField =
        AccessTools.Field(ZdoPeerType, "m_peer")
        ?? throw new MissingFieldException(ZdoPeerType.FullName, "m_peer");

    internal static string Validate(UnderworldRuntimeServices services, UnderworldWorldIdentity identity)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (identity is null) throw new ArgumentNullException(nameof(identity));

        if (!services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Surface, out var surface) ||
            surface is null)
            throw new InvalidOperationException("Surface native world context is not bound.");
        if (!services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Underworld, out var underworld) ||
            underworld is null)
            throw new InvalidOperationException("Underworld native world context is not bound.");

        RequireDistinct(surface.World, underworld.World, "World");
        RequireDistinct(surface.WorldGenerator, underworld.WorldGenerator, "WorldGenerator");
        RequireDistinct(surface.ZoneSystem, underworld.ZoneSystem, "ZoneSystem");
        RequireDistinct(surface.ZdoMan, underworld.ZdoMan, "ZDOMan");

        if (!surface.Scene.IsValid() || !surface.Scene.isLoaded)
            throw new InvalidOperationException("Surface Unity scene is not valid and loaded.");
        if (!underworld.Scene.IsValid() || !underworld.Scene.isLoaded)
            throw new InvalidOperationException("Underworld Unity scene is not valid and loaded.");
        if (surface.Scene.handle == underworld.Scene.handle)
            throw new InvalidOperationException("Surface and Underworld share the same Unity scene.");

        if (!surface.PhysicsScene.IsValid() || !underworld.PhysicsScene.IsValid())
            throw new InvalidOperationException("Surface/Underworld PhysicsScene is invalid.");
        if (surface.PhysicsScene.Equals(underworld.PhysicsScene))
            throw new InvalidOperationException("Surface and Underworld share the same PhysicsScene.");

        if (underworld.ZoneSystem.gameObject.scene.handle != underworld.Scene.handle)
            throw new InvalidOperationException("Underworld ZoneSystem is not resident in the Underworld Unity scene.");
        if (surface.ZoneSystem.gameObject.scene.handle != surface.Scene.handle)
            throw new InvalidOperationException("Surface ZoneSystem is not resident in the Surface Unity scene.");

        if (underworld.PathfindingState is null)
            throw new InvalidOperationException("Underworld native Pathfinding state is not bound.");

        var surfaceWorldUid = Convert.ToInt64(WorldUidField.GetValue(surface.World));
        var underworldWorldUid = Convert.ToInt64(WorldUidField.GetValue(underworld.World));
        if (surfaceWorldUid == underworldWorldUid)
            throw new InvalidOperationException(
                $"Surface and Underworld share Valheim World UID {surfaceWorldUid}.");

        var surfaceZdoId = Convert.ToInt64(ZdoIdentityField.GetValue(surface.ZdoMan));
        var underworldZdoId = Convert.ToInt64(ZdoIdentityField.GetValue(underworld.ZdoMan));
        if (surfaceZdoId == 0L || underworldZdoId == 0L || surfaceZdoId == underworldZdoId)
            throw new InvalidOperationException(
                $"Invalid native ZDO namespaces (Surface={surfaceZdoId}, Underworld={underworldZdoId}).");

        ValidateZoneState(surface.ZoneSystem, underworld.ZoneSystem);
        ValidatePeerSets(surface.ZdoMan, underworld.ZdoMan);
        ValidateAmbientSurfaceAuthority(surface);

        var expectedSeed = identity.DerivedSeed32;
        var seedField = AccessTools.Field(typeof(World), "m_seed")
            ?? throw new MissingFieldException(typeof(World).FullName, "m_seed");
        var actualSeed = Convert.ToInt32(seedField.GetValue(underworld.World));
        if (actualSeed != expectedSeed)
            throw new InvalidOperationException(
                $"Underworld native World seed {actualSeed} does not match admitted derived seed {expectedSeed}.");

        var persistence = UnderworldInstancePersistence.ValidateBoundNamespace();
        return $"SurfaceWorld={surfaceWorldUid}; UnderworldWorld={underworldWorldUid}; " +
               $"SurfaceZDO={surfaceZdoId}; UnderworldZDO={underworldZdoId}; " +
               $"Scene={underworld.Scene.name}; SaveNamespace={persistence}";
    }

    private static void ValidateZoneState(ZoneSystem surface, ZoneSystem underworld)
    {
        foreach (var name in new[] { "m_vegetation", "m_locations", "m_locationsByHash" })
        {
            var field = AccessTools.Field(typeof(ZoneSystem), name)
                ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, name);
            var surfaceValue = field.GetValue(surface);
            var underworldValue = field.GetValue(underworld);
            if (surfaceValue is null || underworldValue is null)
                throw new InvalidOperationException($"ZoneSystem.{name} is unavailable.");
            if (ReferenceEquals(surfaceValue, underworldValue))
                throw new InvalidOperationException($"ZoneSystem.{name} still aliases Surface.");
        }

        foreach (var name in new[] { "m_globalKeys", "m_globalKeysEnums", "m_globalKeysValues" })
        {
            var field = AccessTools.Field(typeof(ZoneSystem), name)
                ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, name);
            var surfaceValue = field.GetValue(surface);
            var underworldValue = field.GetValue(underworld);
            if (surfaceValue is null || underworldValue is null)
                throw new InvalidOperationException($"ZoneSystem.{name} is unavailable.");
            if (!ReferenceEquals(surfaceValue, underworldValue))
                throw new InvalidOperationException(
                    $"Campaign-global ZoneSystem field {name} is not shared across instances.");
        }
    }

    private static void ValidatePeerSets(ZDOMan surface, ZDOMan underworld)
    {
        var surfacePeers = ReadPeers(surface);
        var underworldPeers = ReadPeers(underworld);
        foreach (var peer in underworldPeers)
            if (surfacePeers.Contains(peer))
                throw new InvalidOperationException(
                    $"Native peer {peer.m_uid} is attached to both Surface and Underworld ZDO managers.");
    }

    private static HashSet<ZNetPeer> ReadPeers(ZDOMan manager)
    {
        if (ZdoPeersField.GetValue(manager) is not IEnumerable entries)
            throw new InvalidOperationException("ZDOMan.m_peers is not enumerable.");

        var peers = new HashSet<ZNetPeer>();
        foreach (var entry in entries)
        {
            if (entry is null) continue;
            if (NetworkPeerField.GetValue(entry) is not ZNetPeer peer || peer is null)
                throw new InvalidOperationException("ZDOMan peer entry does not contain a ZNetPeer.");
            peers.Add(peer);
        }
        return peers;
    }

    private static void ValidateAmbientSurfaceAuthority(ValheimWorldInstanceContext surface)
    {
        if (!ReferenceEquals(ZDOMan.instance, surface.ZdoMan))
            throw new InvalidOperationException("Ambient ZDOMan.instance is not Surface after Underworld admission.");
        if (!ReferenceEquals(ZoneSystem.instance, surface.ZoneSystem))
            throw new InvalidOperationException("Ambient ZoneSystem.instance is not Surface after Underworld admission.");
        if (!ReferenceEquals(WorldGenerator.instance, surface.WorldGenerator))
            throw new InvalidOperationException("Ambient WorldGenerator.instance is not Surface after Underworld admission.");
        if (!ReferenceEquals(ZNet.World, surface.World))
            throw new InvalidOperationException("Ambient ZNet.World is not Surface after Underworld admission.");
        if (SceneManager.GetActiveScene().handle != surface.Scene.handle)
            throw new InvalidOperationException("Ambient Unity active scene is not Surface after Underworld admission.");
    }

    private static void RequireDistinct(object surface, object underworld, string label)
    {
        if (ReferenceEquals(surface, underworld))
            throw new InvalidOperationException($"Surface and Underworld share the same native {label} instance.");
    }
}
