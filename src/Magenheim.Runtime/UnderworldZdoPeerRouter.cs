using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Moves the existing player character ZDO and its ZNet peer between native instance ZDO managers.
/// The same ZDO object is re-indexed in the target manager; character state is not serialized into a
/// Magenheim format and is never duplicated.
/// </summary>
internal static class UnderworldZdoPeerRouter
{
    private static readonly AsyncLocal<int> Reentry = new();
    private static readonly FieldInfo ObjectsById = AccessTools.Field(typeof(ZDOMan), "m_objectsByID")
        ?? throw new MissingFieldException(typeof(ZDOMan).FullName, "m_objectsByID");
    private static readonly MethodInfo GetSector = AccessTools.Method(typeof(ZDO), "GetSector", Type.EmptyTypes)
        ?? throw new MissingMethodException(typeof(ZDO).FullName, "GetSector()");
    private static readonly MethodInfo AddPeerMethod = AccessTools.Method(typeof(ZDOMan), "AddPeer", new[] { typeof(ZNetPeer) })
        ?? throw new MissingMethodException(typeof(ZDOMan).FullName, "AddPeer(ZNetPeer)");
    private static readonly MethodInfo RemovePeerMethod = AccessTools.Method(typeof(ZDOMan), "RemovePeer", new[] { typeof(ZNetPeer) })
        ?? throw new MissingMethodException(typeof(ZDOMan).FullName, "RemovePeer(ZNetPeer)");
    private static readonly MethodInfo AddToSector = ResolveSectorMethod("AddToSector");
    private static readonly MethodInfo RemoveFromSector = ResolveSectorMethod("RemoveFromSector");
    private static readonly FieldInfo? ZNetZdoMan = AccessTools.Field(typeof(ZNet), "m_zdoMan");

    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;

    internal static void Configure(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal static bool TryMovePlayer(
        Player player,
        ValheimWorldInstanceContext source,
        ValheimWorldInstanceContext target,
        out string diagnostic)
    {
        diagnostic = string.Empty;
        if (player is null) { diagnostic = "Player is unavailable for ZDO instance routing."; return false; }
        if (ReferenceEquals(source.ZdoMan, target.ZdoMan)) return true;

        var view = player.GetComponent<ZNetView>();
        if (view is null || !view.IsValid())
        {
            diagnostic = "Player has no valid native ZNetView/ZDO.";
            return false;
        }

        var zdo = view.GetZDO();
        if (zdo is null)
        {
            diagnostic = "Player has no native character ZDO.";
            return false;
        }

        var peer = ResolvePeer(player);
        var zdoMoved = false;
        var peerMoved = false;
        try
        {
            Reentry.Value++;
            if (peer is not null)
            {
                RemovePeerMethod.Invoke(source.ZdoMan, new object[] { peer });
                AddPeerMethod.Invoke(target.ZdoMan, new object[] { peer });
                peerMoved = true;
            }

            MoveZdo(zdo, source.ZdoMan, target.ZdoMan);
            zdoMoved = true;

            // A remote client has one physical world at a time. Its normal ZNet update loop must
            // therefore point at the same manager as the local player. Dedicated/listen servers
            // retain Surface as their global manager and tick Underworld separately.
            if (ZNet.instance is not null && !ZNet.instance.IsServer() && ZNetZdoMan is not null)
                ZNetZdoMan.SetValue(ZNet.instance, target.ZdoMan);

            _log?.LogDebug(
                $"Moved player {player.GetPlayerID()} character ZDO {zdo.m_uid} from instance {source.InstanceId} to {target.InstanceId}.");
            return true;
        }
        catch (Exception exception)
        {
            try
            {
                if (zdoMoved) MoveZdo(zdo, target.ZdoMan, source.ZdoMan);
                if (peerMoved && peer is not null)
                {
                    RemovePeerMethod.Invoke(target.ZdoMan, new object[] { peer });
                    AddPeerMethod.Invoke(source.ZdoMan, new object[] { peer });
                }
                if (ZNet.instance is not null && !ZNet.instance.IsServer() && ZNetZdoMan is not null)
                    ZNetZdoMan.SetValue(ZNet.instance, source.ZdoMan);
            }
            catch (Exception rollback)
            {
                _log?.LogError($"Player ZDO instance rollback also failed: {rollback}");
            }

            diagnostic = "Native player ZDO/peer instance routing failed: " +
                         (exception is TargetInvocationException tie && tie.InnerException is not null
                             ? tie.InnerException.Message
                             : exception.Message);
            return false;
        }
        finally
        {
            Reentry.Value--;
        }
    }

    internal static void TickUnderworld(float deltaTime)
    {
        var services = _services;
        if (services is null || ZNet.instance is null ||
            !services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Underworld, out var context) ||
            context is null)
            return;

        using (ValheimWorldInstanceExecution.Enter(context))
            context.ZdoMan.Update(deltaTime);
    }

    internal static void MirrorDisconnectFromSurface(ZDOMan source, ZNetPeer peer)
    {
        if (Reentry.Value != 0 || _services is null || peer is null) return;
        if (!_services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Surface, out var surface) ||
            surface is null || !ReferenceEquals(source, surface.ZdoMan))
            return;
        if (!_services.WorldInstances.TryGetContext(UnderworldWorldInstanceId.Underworld, out var underworld) ||
            underworld is null)
            return;

        try
        {
            Reentry.Value++;
            RemovePeerMethod.Invoke(underworld.ZdoMan, new object[] { peer });
        }
        catch
        {
            // RemovePeer is intentionally idempotent at disconnect; a Surface-resident peer was
            // never added to the Underworld manager.
        }
        finally
        {
            Reentry.Value--;
        }
    }

    private static void MoveZdo(ZDO zdo, ZDOMan source, ZDOMan target)
    {
        var sourceObjects = ObjectsById.GetValue(source) as IDictionary
            ?? throw new InvalidOperationException("Source ZDOMan object index is unavailable.");
        var targetObjects = ObjectsById.GetValue(target) as IDictionary
            ?? throw new InvalidOperationException("Target ZDOMan object index is unavailable.");

        var sector = GetSector.Invoke(zdo, Array.Empty<object>())
            ?? throw new InvalidOperationException("Character ZDO returned no sector.");

        RemoveFromSector.Invoke(source, new[] { (object)zdo, sector });
        sourceObjects.Remove(zdo.m_uid);

        if (targetObjects.Contains(zdo.m_uid))
            throw new InvalidOperationException($"Target instance already owns character ZDO {zdo.m_uid}.");

        targetObjects.Add(zdo.m_uid, zdo);
        try
        {
            AddToSector.Invoke(target, new[] { (object)zdo, sector });
        }
        catch
        {
            targetObjects.Remove(zdo.m_uid);
            sourceObjects.Add(zdo.m_uid, zdo);
            AddToSector.Invoke(source, new[] { (object)zdo, sector });
            throw;
        }
    }

    private static ZNetPeer? ResolvePeer(Player player)
    {
        var znet = ZNet.instance;
        if (znet is null) return null;
        var id = player.GetZDOID();
        return znet.GetPeers().FirstOrDefault(value => value.m_characterID == id);
    }

    private static MethodInfo ResolveSectorMethod(string name)
    {
        var candidates = typeof(ZDOMan).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.Name == name)
            .Where(method =>
            {
                var p = method.GetParameters();
                return p.Length == 2 && p[0].ParameterType == typeof(ZDO);
            })
            .ToArray();
        if (candidates.Length != 1)
            throw new MissingMethodException(typeof(ZDOMan).FullName, name + "(ZDO, SectorIndex)");
        return candidates[0];
    }
}

[HarmonyPatch(typeof(ZDOMan), "RemovePeer", new[] { typeof(ZNetPeer) })]
internal static class UnderworldZdoPeerDisconnectPatch
{
    private static void Postfix(ZDOMan __instance, ZNetPeer __0) =>
        UnderworldZdoPeerRouter.MirrorDisconnectFromSurface(__instance, __0);
}

/// <summary>
/// Local scene streaming must execute against the local player's world-instance manager. This is
/// essential for listen-server hosts, where the server's global manager remains Surface while the
/// host character may physically occupy the Underworld.
/// </summary>
[HarmonyPatch]
internal static class UnderworldZNetSceneInstanceScopePatch
{
    private static readonly string[] Names = { "Update", "CreateDestroyObjects", "IsAreaReady" };

    internal static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var name in Names)
        {
            foreach (var method in typeof(ZNetScene).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .Where(value => value.Name == name))
                yield return method;
        }
    }

    private static void Prefix(out IDisposable? __state)
    {
        __state = null;
        var player = Player.m_localPlayer;
        if (!player) return;
        ValheimWorldInstanceExecution.TryEnterScene(player.gameObject.scene.handle, out __state);
    }

    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
}
