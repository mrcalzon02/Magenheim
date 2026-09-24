using System;
using System.Collections;
using System.Globalization;
using System.Threading;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Underworld;
using Magenheim.Runtime.Networking;

namespace Magenheim.Runtime;

/// <summary>
/// Session-bound transport for per-player Deep Boon selection. Clients may request only a boon id
/// or a clear operation. The server resolves peer/player/world identity, reconstructs authoritative
/// Conclave unlocks, revalidates persisted selection, executes Core selection authority, and only
/// then persists an admitted changed state.
/// </summary>
internal static class UnderworldDeepBoonSelectionRpc
{
    private const int SelectMessage = 1;
    private const int ClearMessage = 2;
    private const int ResponseMessage = 3;
    private static readonly UnderworldDeepstoneRequestLedger ReplayLedger = new();
    private static long _nextRequestId;
    private static UnderworldRuntimeServices? _services;
    private static DefinitionAuthoritySynchronizer? _authority;
    private static ManualLogSource? _log;
    private static CustomRPC? _rpc;

    internal static void Register(UnderworldRuntimeServices services, DefinitionAuthoritySynchronizer authority, ManualLogSource log)
    {
        if (_rpc is not null) return;
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _rpc = NetworkManager.Instance.AddRPC("UnderworldDeepBoonSelection", ReceiveServerMessage, ReceiveClientMessage);
    }

    internal static bool TrySelect(string deepBoonId, out string diagnostic)
    {
        if (string.IsNullOrWhiteSpace(deepBoonId)) { diagnostic = "Deep Boon selection requires a canonical boon identity."; return false; }
        return TrySubmit(SelectMessage, deepBoonId.Trim(), out diagnostic);
    }

    internal static bool TryClear(out string diagnostic) => TrySubmit(ClearMessage, string.Empty, out diagnostic);

    private static bool TrySubmit(int operation, string deepBoonId, out string diagnostic)
    {
        if (ZNet.instance is null) { diagnostic = "World network authority is unavailable."; return false; }
        if (_services is null || _authority is null) { diagnostic = "Deep Boon selection authority is unavailable."; return false; }
        if (ZNet.instance.IsServer())
        {
            var player = Player.m_localPlayer;
            if (player is null) { diagnostic = "Local player identity is unavailable."; return false; }
            return TryApplyServer(player, operation, deepBoonId, out diagnostic);
        }
        if (_rpc is null || !_authority.IsClientMutationAuthorized) { diagnostic = "Server gameplay authority has not admitted Deep Boon mutation."; return false; }
        var generation = _authority.ClientSessionGeneration;
        if (generation <= 0L) { diagnostic = "No active Magenheim authority session exists."; return false; }
        var requestId = Interlocked.Increment(ref _nextRequestId);
        var package = new ZPackage(); package.Write(operation); package.Write(generation); package.Write(requestId); package.Write(deepBoonId);
        _rpc.SendPackage(RuntimeGameApi.ServerPeerId, package);
        diagnostic = operation == ClearMessage ? "Deep Boon clear request submitted for server resolution." : "Deep Boon selection submitted for server resolution.";
        return true;
    }

    private static IEnumerator ReceiveServerMessage(long sender, ZPackage package)
    {
        try
        {
            var operation = package.ReadInt();
            if (operation != SelectMessage && operation != ClearMessage) yield break;
            var suppliedGeneration = package.ReadLong(); var requestId = package.ReadLong(); var deepBoonId = package.ReadString();
            if (_authority is null || _rpc is null) yield break;
            var generation = _authority.GetPeerSessionGeneration(sender);
            if (generation <= 0L || generation != suppliedGeneration || !_authority.IsPeerMutationAuthorized(sender)) { SendResponse(sender, generation, requestId, false, "Deep Boon request belongs to an unauthorized or stale Magenheim session."); yield break; }
            if (!ReplayLedger.TryAdmit(sender, generation, requestId)) { SendResponse(sender, generation, requestId, false, "Duplicate or invalid Deep Boon selection request rejected."); yield break; }
            if (!TryResolvePeerPlayer(sender, out var player, out var diagnostic)) { SendResponse(sender, generation, requestId, false, diagnostic); yield break; }
            var applied = TryApplyServer(player, operation, deepBoonId, out diagnostic);
            SendResponse(sender, generation, requestId, applied, diagnostic);
        }
        catch (Exception exception) { _log?.LogError($"Failed to process Deep Boon selection RPC from peer {sender}: {exception}"); }
        yield break;
    }

    private static IEnumerator ReceiveClientMessage(long sender, ZPackage package)
    {
        try
        {
            if (package.ReadInt() != ResponseMessage) yield break;
            var generation = package.ReadLong(); var requestId = package.ReadLong(); var applied = package.ReadBool(); var diagnostic = package.ReadString();
            if (_authority is null || sender != RuntimeGameApi.ServerPeerId || generation <= 0L || generation != _authority.ClientSessionGeneration || requestId <= 0L) yield break;
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, diagnostic);
            if (applied) _log?.LogDebug($"Server accepted Deep Boon selection request {requestId}.");
        }
        catch (Exception exception) { _log?.LogError($"Failed to process Deep Boon selection response: {exception}"); }
        yield break;
    }

    private static bool TryApplyServer(Player player, int operation, string deepBoonId, out string diagnostic)
    {
        if (_services is null || ZNet.instance is null || !ZNet.instance.IsServer()) { diagnostic = "Deep Boon mutation requires server authority."; return false; }
        if (ZNet.World is null) { diagnostic = "Deep Boon mutation requires a loaded world."; return false; }
        // Server mutation is authorized against the requesting Player's physical instance placement.
        // Never consult Player.m_localPlayer here: on a dedicated server it is absent, and on a listen
        // server it represents the host rather than an arbitrary remote requester.
        if (!_services.TryResolvePlayerSession(player, out var identity, out var layer, out diagnostic) || identity is null) return false;
        if (layer != UnderworldLayer.Underworld) { diagnostic = "Deep Boons may only be selected within the paired Underworld."; return false; }
        var playerId = player.GetPlayerID().ToString(CultureInfo.InvariantCulture);
        var persistedId = default(string);
        if (_services.DeepBoonSelectionStore.TryLoad(playerId, identity, out var persisted, out var loadDiagnostic)) persistedId = persisted.SelectedDeepBoonId;
        else if (loadDiagnostic.IndexOf("candidate missing", StringComparison.Ordinal) < 0) _log?.LogWarning($"Deep Boon selection for player {playerId} could not be recovered: {loadDiagnostic}");

        UnderworldDeepBoonSelectionState current;
        try { current = UnderworldDeepstoneRuntimeAuthority.ReconstructDeepBoonSelection(persistedId); }
        catch (Exception exception) { diagnostic = "Persisted Deep Boon selection is incompatible with authoritative Conclave progression: " + exception.Message; return false; }

        UnderworldDeepBoonSelectionResult result;
        if (operation == ClearMessage) result = UnderworldDeepstoneRuntimeAuthority.ClearDeepBoon(current);
        else result = UnderworldDeepstoneRuntimeAuthority.SelectDeepBoon(current, deepBoonId);

        if (!result.Changed) { diagnostic = result.Diagnostic; return result.Status == UnderworldDeepBoonSelectionStatus.AlreadySelected; }
        try { _services.DeepBoonSelectionStore.Save(playerId, identity, result.State); }
        catch (Exception exception) { diagnostic = "Deep Boon selection was admitted but durable persistence failed; no runtime effect was applied. " + exception.Message; return false; }
        diagnostic = result.Diagnostic;
        return true;
    }

    private static bool TryResolvePeerPlayer(long peerId, out Player player, out string diagnostic)
    {
        player = null!;
        if (ZNet.instance is null || ZNetScene.instance is null) { diagnostic = "Server world state is unavailable."; return false; }
        var peer = ZNet.instance.GetPeer(peerId); var instance = peer is null ? null : ZNetScene.instance.FindInstance(peer.m_characterID); var resolved = instance ? instance.GetComponent<Player>() : null;
        if (!resolved) { diagnostic = "Requesting player's server character is unavailable."; return false; }
        player = resolved; diagnostic = string.Empty; return true;
    }

    private static void SendResponse(long peerId, long generation, long requestId, bool applied, string diagnostic)
    {
        if (_rpc is null || generation <= 0L) return;
        var package = new ZPackage(); package.Write(ResponseMessage); package.Write(generation); package.Write(requestId); package.Write(applied); package.Write(diagnostic ?? string.Empty); _rpc.SendPackage(peerId, package);
    }
}
