using System;
using System.Collections;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Runtime.Networking;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Session-bound transport for Deepstone activation. Clients identify only the stone they touched;
/// the server resolves the peer character, canonical trophy, inventory count, progression state,
/// consumption and durable activation.
/// </summary>
internal static class UnderworldDeepstoneInteractionRpc
{
    private const int RequestMessage = 1;
    private const int ResponseMessage = 2;
    private static DefinitionAuthoritySynchronizer? _authority;
    private static ManualLogSource? _log;
    private static CustomRPC? _rpc;

    internal static void Register(DefinitionAuthoritySynchronizer authority, ManualLogSource log)
    {
        if (_rpc is not null) return;
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _rpc = NetworkManager.Instance.AddRPC("UnderworldDeepstoneInteraction", ReceiveServerMessage, ReceiveClientMessage);
    }

    internal static bool TryActivate(UnderworldDeepstoneRuntime stone, Humanoid user, out string diagnostic)
    {
        if (stone is null || user is null || !stone.IsBound) { diagnostic = "Deepstone interaction is unavailable."; return false; }
        if (Vector3.Distance(user.transform.position, stone.transform.position) > 5f) { diagnostic = "Move closer to the Deepstone."; return false; }
        if (ZNet.instance is null) { diagnostic = "World network authority is unavailable."; return false; }
        if (ZNet.instance.IsServer()) return TryActivateServer(stone, user, out diagnostic);
        if (_rpc is null || _authority is null || !_authority.IsClientMutationAuthorized) { diagnostic = "Server gameplay authority has not admitted Deepstone mutation."; return false; }
        var generation = _authority.ClientSessionGeneration;
        if (generation <= 0L) { diagnostic = "No active Magenheim authority session exists."; return false; }
        var package = new ZPackage(); package.Write(RequestMessage); package.Write(generation); package.Write(stone.DeepstoneId);
        _rpc.SendPackage(RuntimeGameApi.ServerPeerId, package);
        diagnostic = "The Deepstone offering was submitted for server resolution.";
        return true;
    }

    private static IEnumerator ReceiveServerMessage(long sender, ZPackage package)
    {
        try
        {
            if (package.ReadInt() != RequestMessage) yield break;
            var suppliedGeneration = package.ReadLong(); var deepstoneId = package.ReadString();
            if (_authority is null || _rpc is null) yield break;
            var generation = _authority.GetPeerSessionGeneration(sender);
            if (generation <= 0L || generation != suppliedGeneration || !_authority.IsPeerMutationAuthorized(sender)) { SendResponse(sender, generation, false, "Deepstone request belongs to an unauthorized or stale Magenheim session."); yield break; }
            if (!TryResolvePeerPlayer(sender, out var player, out var diagnostic) || !TryResolveNearbyStone(player, deepstoneId, out var stone, out diagnostic)) { SendResponse(sender, generation, false, diagnostic); yield break; }
            var applied = TryActivateServer(stone, player, out diagnostic); SendResponse(sender, generation, applied, diagnostic);
        }
        catch (Exception exception) { _log?.LogError($"Failed to process Deepstone interaction RPC from peer {sender}: {exception}"); }
        yield break;
    }

    private static IEnumerator ReceiveClientMessage(long sender, ZPackage package)
    {
        try
        {
            if (package.ReadInt() != ResponseMessage) yield break;
            var generation = package.ReadLong(); var applied = package.ReadBool(); var diagnostic = package.ReadString();
            if (_authority is null || sender != RuntimeGameApi.ServerPeerId || generation <= 0L || generation != _authority.ClientSessionGeneration) yield break;
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, diagnostic);
            if (applied) _log?.LogDebug("Server accepted Deepstone trophy activation.");
        }
        catch (Exception exception) { _log?.LogError($"Failed to process Deepstone interaction response: {exception}"); }
        yield break;
    }

    private static bool TryActivateServer(UnderworldDeepstoneRuntime stone, Humanoid user, out string diagnostic)
    {
        if (ZNet.instance is null || !ZNet.instance.IsServer()) { diagnostic = "Deepstone mutation requires server authority."; return false; }
        var boss = UnderworldDeepstoneRuntimeAuthority.BossForStone(stone.DeepstoneId);
        var inventory = user.GetInventory();
        var trophyItems = inventory.GetAllItems().Where(item => item.m_dropPrefab && string.Equals(item.m_dropPrefab.name, boss.TrophyPrefabName, StringComparison.Ordinal)).ToArray();
        var trophyCount = trophyItems.Sum(item => item.m_stack);
        var plan = UnderworldDeepstoneRuntimeAuthority.PlanMount(stone.DeepstoneId, boss.TrophyPrefabName, trophyCount);
        if (!plan.Ready || plan.ResultingState is null) { diagnostic = plan.Diagnostic; return false; }
        var remaining = plan.TrophyConsumeCount;
        foreach (var item in trophyItems)
        {
            if (remaining <= 0) break;
            var consume = Math.Min(item.m_stack, remaining);
            if (!inventory.RemoveItem(item, consume)) { diagnostic = "The required trophy changed before the server could consume it."; return false; }
            remaining -= consume;
        }
        if (remaining != 0) { diagnostic = "The server could not complete the authorized trophy consumption."; return false; }
        if (!stone.PersistAuthorizedActivation(plan.ResultingState.TrophyMounted, plan.ResultingState.BoonUnlocked))
        {
            diagnostic = "The trophy was consumed but the Deepstone state could not be persisted; server recovery is required.";
            _log?.LogError($"Deepstone '{stone.DeepstoneId}' consumed an authorized trophy but persistence failed."); return false;
        }
        diagnostic = plan.Diagnostic; return true;
    }

    private static bool TryResolvePeerPlayer(long peerId, out Player player, out string diagnostic)
    {
        player = null!;
        if (ZNet.instance is null || ZNetScene.instance is null) { diagnostic = "Server world state is unavailable."; return false; }
        var peer = ZNet.instance.GetPeer(peerId); var instance = peer is null ? null : ZNetScene.instance.FindInstance(peer.m_characterID); var resolved = instance ? instance.GetComponent<Player>() : null;
        if (!resolved) { diagnostic = "Requesting player's server character is unavailable."; return false; }
        player = resolved; diagnostic = string.Empty; return true;
    }

    private static bool TryResolveNearbyStone(Player player, string deepstoneId, out UnderworldDeepstoneRuntime stone, out string diagnostic)
    {
        stone = UnityEngine.Object.FindObjectsOfType<UnderworldDeepstoneRuntime>().FirstOrDefault(value => value.IsBound && string.Equals(value.DeepstoneId, deepstoneId, StringComparison.Ordinal) && Vector3.Distance(player.transform.position, value.transform.position) <= 5f)!;
        if (!stone) { diagnostic = "The requested canonical Deepstone is not within interaction range."; return false; }
        diagnostic = string.Empty; return true;
    }

    private static void SendResponse(long peerId, long generation, bool applied, string diagnostic)
    {
        if (_rpc is null || generation <= 0L) return;
        var package = new ZPackage(); package.Write(ResponseMessage); package.Write(generation); package.Write(applied); package.Write(diagnostic ?? string.Empty); _rpc.SendPackage(peerId, package);
    }
}
