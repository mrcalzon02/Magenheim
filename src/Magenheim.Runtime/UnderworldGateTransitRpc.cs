using System;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Connection-authenticated Deep Gate instance transit. This deliberately uses Valheim's direct
/// per-peer ZRpc instead of ZRoutedRpc/Jotunn CustomRPC: a routed message carries a sender id supplied
/// by the packet, while the ZRpc passed to this handler is the actual network connection.
/// </summary>
internal static class UnderworldGateTransitRpc
{
    private const string RpcName = "Magenheim_DeepGateTransit_v2";
    private const int RequestMessage = 1;
    private const int ResponseMessage = 2;

    private static UnderworldRuntimeServices? _services;
    private static ManualLogSource? _log;

    internal static void Register(UnderworldRuntimeServices services, ManualLogSource log)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal static void RegisterPeer(ZNetPeer peer)
    {
        if (peer?.m_rpc is null) return;

        // ZRpc.Register replaces an existing handler of the same name, so repeated connection
        // lifecycle hooks are idempotent without a parallel peer-registration store.
        peer.m_rpc.Register<ZPackage>(RpcName, ReceivePeerMessage);
    }

    internal static bool TryTransit(UnderworldGateRole role, Player player, out string diagnostic)
    {
        diagnostic = string.Empty;
        var net = ZNet.instance;
        if (player is null || net is null)
        {
            diagnostic = "Deep Gate network authority is unavailable.";
            return false;
        }

        if (net.IsServer())
            return UnderworldGateTransitRuntime.TryTransit(role, player, out diagnostic);

        var serverPeer = net.GetServerPeer();
        if (serverPeer?.m_rpc is null || !serverPeer.IsReady())
        {
            diagnostic = "The server peer is unavailable for Deep Gate transit.";
            return false;
        }

        var package = new ZPackage();
        package.Write(RequestMessage);
        package.Write((int)role);
        serverPeer.m_rpc.Invoke(RpcName, package);
        diagnostic = "Deep Gate transit submitted to the server.";
        return true;
    }

    private static void ReceivePeerMessage(ZRpc rpc, ZPackage package)
    {
        try
        {
            var net = ZNet.instance;
            if (net is null || rpc is null || package is null) return;

            var message = package.ReadInt();
            if (net.IsServer())
            {
                if (message != RequestMessage) return;
                ReceiveServerRequest(net, rpc, package);
                return;
            }

            if (message != ResponseMessage) return;
            ReceiveClientResponse(net, rpc, package);
        }
        catch (Exception exception)
        {
            _log?.LogError($"Failed to process connection-authenticated Deep Gate RPC: {exception}");
        }
    }

    private static void ReceiveServerRequest(ZNet net, ZRpc rpc, ZPackage package)
    {
        if (!TryResolveConnectionPeer(net, rpc, out var peer) || peer is null)
        {
            _log?.LogWarning("Rejected Deep Gate request from an unrecognized ZRpc connection.");
            return;
        }

        var roleValue = package.ReadInt();
        if (!Enum.IsDefined(typeof(UnderworldGateRole), roleValue))
        {
            SendResponse(rpc, false, "Invalid Deep Gate role.", UnderworldWorldInstanceId.Surface, Vector3.zero, Quaternion.identity);
            return;
        }

        if (!TryResolvePeerPlayer(peer, out var player, out var diagnostic))
        {
            SendResponse(rpc, false, diagnostic, UnderworldWorldInstanceId.Surface, Vector3.zero, Quaternion.identity);
            return;
        }

        var role = (UnderworldGateRole)roleValue;
        var applied = UnderworldGateTransitRuntime.TryTransit(role, player, out diagnostic);
        var instanceId = UnderworldWorldInstanceId.Surface;
        if (_services is not null)
            _services.WorldInstances.TryGetPlayerInstance(player.GetPlayerID(), out instanceId);

        SendResponse(
            rpc,
            applied,
            diagnostic,
            instanceId,
            player.transform.position,
            player.transform.rotation);
    }

    private static void ReceiveClientResponse(ZNet net, ZRpc rpc, ZPackage package)
    {
        var serverPeer = net.GetServerPeer();

        // This is the direct connection identity, not a message field. Never accept an instance
        // transition response from any other peer.
        if (serverPeer?.m_rpc is null || !ReferenceEquals(serverPeer.m_rpc, rpc))
        {
            _log?.LogWarning("Rejected Deep Gate response from a non-server ZRpc connection.");
            return;
        }

        var applied = package.ReadBool();
        var diagnostic = package.ReadString();
        var instanceId = new UnderworldWorldInstanceId(package.ReadInt());
        var position = new Vector3(package.ReadSingle(), package.ReadSingle(), package.ReadSingle());
        var rotation = new Quaternion(
            package.ReadSingle(),
            package.ReadSingle(),
            package.ReadSingle(),
            package.ReadSingle());

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

    private static bool TryResolveConnectionPeer(ZNet net, ZRpc rpc, out ZNetPeer? peer)
    {
        foreach (var candidate in net.GetPeers())
        {
            if (candidate?.m_rpc is null || !ReferenceEquals(candidate.m_rpc, rpc)) continue;
            peer = candidate;
            return true;
        }

        peer = null;
        return false;
    }

    private static bool TryResolvePeerPlayer(ZNetPeer peer, out Player player, out string diagnostic)
    {
        player = null!;
        if (ZNetScene.instance is null)
        {
            diagnostic = "Server world state is unavailable.";
            return false;
        }

        var instance = ZNetScene.instance.FindInstance(peer.m_characterID);
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
        ZRpc rpc,
        bool applied,
        string diagnostic,
        UnderworldWorldInstanceId instanceId,
        Vector3 position,
        Quaternion rotation)
    {
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
        rpc.Invoke(RpcName, package);
    }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection), new[] { typeof(ZNetPeer) })]
internal static class UnderworldGatePeerRpcRegistrationPatch
{
    private static void Postfix(ZNetPeer peer) =>
        UnderworldGateTransitRpc.RegisterPeer(peer);
}
