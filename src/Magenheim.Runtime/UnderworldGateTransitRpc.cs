using System;
using System.Collections;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Server-authoritative Deep Gate instance transit. Clients request a role transition; the server
/// resolves the requesting character, validates the gate/progression path through the canonical
/// runtime, moves only that player, then returns the accepted instance/pose for the client mirror.
/// </summary>
internal static class UnderworldGateTransitRpc
{
    private const int RequestMessage = 1;
    private const int ResponseMessage = 2;
    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;
    private static CustomRPC? _rpc;

    internal static void Register(UnderworldRuntimeServices services, ManualLogSource log)
    {
        if (_rpc is not null) return;
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _rpc = NetworkManager.Instance.AddRPC(
            "UnderworldGateTransit",
            ReceiveServerMessage,
            ReceiveClientMessage);
    }

    internal static bool TryTransit(UnderworldGateRole role, Player player, out string diagnostic)
    {
        diagnostic = string.Empty;
        if (player is null || ZNet.instance is null)
        {
            diagnostic = "Deep Gate network authority is unavailable.";
            return false;
        }

        if (ZNet.instance.IsServer())
            return UnderworldGateTransitRuntime.TryTransit(role, player, out diagnostic);

        if (_rpc is null)
        {
            diagnostic = "Deep Gate RPC is unavailable.";
            return false;
        }

        var package = new ZPackage();
        package.Write(RequestMessage);
        package.Write((int)role);
        _rpc.SendPackage(RuntimeGameApi.ServerPeerId, package);
        diagnostic = "Deep Gate transit submitted to the server.";
        return true;
    }

    private static IEnumerator ReceiveServerMessage(long sender, ZPackage package)
    {
        try
        {
            if (package.ReadInt() != RequestMessage) yield break;
            var roleValue = package.ReadInt();
            if (!Enum.IsDefined(typeof(UnderworldGateRole), roleValue))
            {
                SendResponse(sender, false, "Invalid Deep Gate role.", UnderworldWorldInstanceId.Surface, Vector3.zero, Quaternion.identity);
                yield break;
            }

            if (!TryResolvePeerPlayer(sender, out var player, out var diagnostic))
            {
                SendResponse(sender, false, diagnostic, UnderworldWorldInstanceId.Surface, Vector3.zero, Quaternion.identity);
                yield break;
            }

            var role = (UnderworldGateRole)roleValue;
            var applied = UnderworldGateTransitRuntime.TryTransit(role, player, out diagnostic);
            var instanceId = UnderworldWorldInstanceId.Surface;
            if (_services is not null)
                _services.WorldInstances.TryGetPlayerInstance(player.GetPlayerID(), out instanceId);

            SendResponse(
                sender,
                applied,
                diagnostic,
                instanceId,
                player.transform.position,
                player.transform.rotation);
        }
        catch (Exception exception)
        {
            _log?.LogError($"Failed to process Deep Gate transit RPC from peer {sender}: {exception}");
        }
        yield break;
    }

    private static IEnumerator ReceiveClientMessage(long sender, ZPackage package)
    {
        try
        {
            if (package.ReadInt() != ResponseMessage) yield break;
            if (sender != RuntimeGameApi.ServerPeerId) yield break;

            var applied = package.ReadBool();
            var diagnostic = package.ReadString();
            var instanceId = new UnderworldWorldInstanceId(package.ReadInt());
            var position = new Vector3(package.ReadSingle(), package.ReadSingle(), package.ReadSingle());
            var rotation = new Quaternion(package.ReadSingle(), package.ReadSingle(), package.ReadSingle(), package.ReadSingle());

            var player = Player.m_localPlayer;
            if (applied && player)
            {
                if (!UnderworldGateTransitRuntime.ApplyAcceptedTransit(
                        player,
                        instanceId,
                        position,
                        rotation,
                        out var mirrorDiagnostic))
                {
                    applied = false;
                    diagnostic = mirrorDiagnostic;
                }
            }

            if (player && !string.IsNullOrWhiteSpace(diagnostic))
                player.Message(MessageHud.MessageType.Center, diagnostic);

            if (applied)
                _log?.LogInfo($"Mirrored server-authorized Deep Gate transit into world instance {instanceId}.");
        }
        catch (Exception exception)
        {
            _log?.LogError($"Failed to process Deep Gate transit response: {exception}");
        }
        yield break;
    }

    private static bool TryResolvePeerPlayer(long peerId, out Player player, out string diagnostic)
    {
        player = null!;
        if (ZNet.instance is null || ZNetScene.instance is null)
        {
            diagnostic = "Server world state is unavailable.";
            return false;
        }

        var peer = ZNet.instance.GetPeer(peerId);
        var instance = peer is null ? null : ZNetScene.instance.FindInstance(peer.m_characterID);
        var resolved = instance ? instance.GetComponent<Player>() : null;
        if (!resolved)
        {
            diagnostic = "Requesting player's server character is unavailable.";
            return false;
        }

        player = resolved;
        diagnostic = string.Empty;
        return true;
    }

    private static void SendResponse(
        long peerId,
        bool applied,
        string diagnostic,
        UnderworldWorldInstanceId instanceId,
        Vector3 position,
        Quaternion rotation)
    {
        if (_rpc is null) return;
        var package = new ZPackage();
        package.Write(ResponseMessage);
        package.Write(applied);
        package.Write(diagnostic ?? string.Empty);
        package.Write(instanceId.Value);
        package.Write(position.x);
        package.Write(position.y);
        package.Write(position.z);
        package.Write(rotation.x);
        package.Write(rotation.y);
        package.Write(rotation.z);
        package.Write(rotation.w);
        _rpc.SendPackage(peerId, package);
    }
}
