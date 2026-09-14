using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using Magenheim.Core.Socketing;
using UnityEngine;

namespace Magenheim.Runtime.Networking;

/// <summary>
/// Server-authoritative transport for remote Geologist's Workstation socket operations.
/// The server validates gameplay authority, station proximity/upgrades, canonical equipment
/// prefab classification, socket policy, operation replay identity, and extraction randomness.
/// The remote client retains ownership of its concrete ItemData and applies the approved plan
/// only after revalidating that exact item instance and original socket state.
/// </summary>
internal static class SocketOperationRpc
{
    private const int RequestMessage = 1;
    private const int ResponseMessage = 2;
    private const int AcknowledgementMessage = 3;
    private const int MaximumTrackedClientOperations = 256;
    private const int MaximumTrackedServerOperations = 512;

    private static readonly Dictionary<string, ClientOperationRecord> ClientOperations =
        new(StringComparer.Ordinal);
    private static readonly Queue<string> ClientOperationOrder = new();
    private static readonly Dictionary<RemoteOperationKey, ServerOperationRecord> ServerOperations = new();
    private static readonly Queue<RemoteOperationKey> ServerOperationOrder = new();

    private static RuntimeServices? _services;
    private static DefinitionAuthoritySynchronizer? _authority;
    private static ManualLogSource? _log;
    private static CustomRPC? _rpc;

    internal static void Register(
        RuntimeServices services,
        DefinitionAuthoritySynchronizer authority,
        ManualLogSource log)
    {
        if (_rpc is not null) return;

        _services = services ?? throw new ArgumentNullException(nameof(services));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _rpc = NetworkManager.Instance.AddRPC(
            "SocketOperation",
            ReceiveServerMessage,
            ReceiveClientResponse);
    }

    internal static bool TrySubmitAddSlot(
        Player player,
        ItemDrop.ItemData equipment,
        out string diagnostic) =>
        TrySubmitClient(player, equipment, RemoteSocketKind.AddSlot, null, null, -1, out diagnostic);

    internal static bool TrySubmitInstall(
        Player player,
        ItemDrop.ItemData equipment,
        ItemDrop.ItemData crystalItem,
        Crystal crystal,
        out string diagnostic) =>
        TrySubmitClient(player, equipment, RemoteSocketKind.InstallCrystal, crystalItem, crystal, -1, out diagnostic);

    internal static bool TrySubmitExtraction(
        Player player,
        ItemDrop.ItemData equipment,
        int crystalIndex,
        out string diagnostic) =>
        TrySubmitClient(player, equipment, RemoteSocketKind.ExtractCrystal, null, null, crystalIndex, out diagnostic);

    private static bool TrySubmitClient(
        Player player,
        ItemDrop.ItemData equipment,
        RemoteSocketKind kind,
        ItemDrop.ItemData? crystalItem,
        Crystal? crystal,
        int crystalIndex,
        out string diagnostic)
    {
        if (_rpc is null || _services is null || _authority is null)
        {
            diagnostic = "Magenheim socket networking is not initialized.";
            return false;
        }
        if (player is null || equipment is null)
        {
            diagnostic = "Socket request is missing required local state.";
            return false;
        }
        if (ZNet.instance is null || ZNet.instance.IsServer())
        {
            diagnostic = "Remote socket transport is only used by non-host clients.";
            return false;
        }
        if (!_authority.IsClientMutationAuthorized)
        {
            diagnostic = $"Server definition authority has not admitted Magenheim mutation. {_authority.ClientAuthorityResult.Diagnostic}";
            return false;
        }
        if (!TryGetCurrentMagenheimStation(player, out _))
        {
            diagnostic = "Use a Geologist's Workstation for socket operations.";
            return false;
        }

        var inventory = player.GetInventory();
        if (!inventory.ContainsItem(equipment))
        {
            diagnostic = "Selected equipment is no longer in your inventory.";
            return false;
        }
        if (!ItemSocketAdapter.TryRead(equipment, out var state, out var metadataError))
        {
            diagnostic = metadataError;
            return false;
        }

        if (kind == RemoteSocketKind.InstallCrystal)
        {
            if (crystalItem is null || crystal is null || !inventory.ContainsItem(crystalItem) || crystalItem.m_stack < 1)
            {
                diagnostic = "Selected crystal is no longer available.";
                return false;
            }
            if (!TryParseCrystal(crystalItem, out var parsed) || parsed != crystal.Value)
            {
                diagnostic = "Selected crystal identity changed before the request was sent.";
                return false;
            }
        }
        else if (kind == RemoteSocketKind.ExtractCrystal)
        {
            if (crystalIndex < 0 || crystalIndex >= state.InstalledCrystals.Count)
            {
                diagnostic = "Installed crystal index is outside the selected item's current socket state.";
                return false;
            }
        }

        TrimClientOperations();
        if (ClientOperations.Count >= MaximumTrackedClientOperations)
        {
            diagnostic = "Too many unacknowledged Magenheim socket operations are pending; reconnect before submitting more.";
            return false;
        }

        var descriptor = ItemSocketAdapter.Describe(equipment);
        var operationId = Guid.NewGuid().ToString("N");
        var skill = Mathf.Clamp(
            Mathf.FloorToInt(player.GetSkillLevel(EarthContentRegistrar.CrystalShapingSkill)),
            0,
            100);
        var record = new ClientOperationRecord(kind, equipment, crystalItem, state, crystal, crystalIndex);
        ClientOperations.Add(operationId, record);
        ClientOperationOrder.Enqueue(operationId);

        var package = new ZPackage();
        package.Write(RequestMessage);
        package.Write(operationId);
        package.Write((int)kind);
        WriteDescriptor(package, descriptor);
        WriteState(package, state);
        package.Write(crystal is not null);
        if (crystal is { } requestedCrystal)
            WriteCrystal(package, requestedCrystal);
        package.Write(crystalItem?.m_stack ?? 0);
        package.Write(crystalIndex);
        package.Write(skill);
        _rpc.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);

        diagnostic = "Sent socket operation to the server for authoritative resolution.";
        return true;
    }

    private static IEnumerator ReceiveServerMessage(long sender, ZPackage package)
    {
        try
        {
            var messageType = package.ReadInt();
            switch (messageType)
            {
                case RequestMessage:
                    HandleServerRequest(sender, package);
                    break;
                case AcknowledgementMessage:
                    HandleServerAcknowledgement(sender, package);
                    break;
                default:
                    _log?.LogWarning($"Ignoring unknown Magenheim socket RPC message type {messageType} from peer {sender}.");
                    break;
            }
        }
        catch (Exception exception)
        {
            _log?.LogError($"Failed to process Magenheim socket RPC from peer {sender}: {exception}");
        }

        yield break;
    }

    private static void HandleServerRequest(long sender, ZPackage package)
    {
        if (_rpc is null || _services is null || _authority is null)
            throw new InvalidOperationException("Socket RPC server state is not configured.");

        var operationId = package.ReadString();
        var rawKind = package.ReadInt();
        var suppliedDescriptor = ReadDescriptor(package);
        var state = ReadState(package);
        var hasCrystal = package.ReadBool();
        var crystal = hasCrystal ? ReadCrystal(package) : (Crystal?)null;
        var sourceCrystalCount = package.ReadInt();
        var crystalIndex = package.ReadInt();
        var clientSkill = package.ReadInt();

        if (string.IsNullOrWhiteSpace(operationId) || operationId.Length > SocketOperationGuard.MaximumOperationIdLength)
        {
            SendRejection(sender, operationId, RemoteSocketKind.AddSlot, "Invalid socket operation id.");
            return;
        }
        if (!Enum.IsDefined(typeof(RemoteSocketKind), rawKind))
        {
            SendRejection(sender, operationId, RemoteSocketKind.AddSlot, "Unknown Magenheim socket operation kind.");
            return;
        }
        var kind = (RemoteSocketKind)rawKind;
        if (clientSkill < 0 || clientSkill > 100)
        {
            SendRejection(sender, operationId, kind, "Crystal Shaping skill snapshot is outside the valid 0-100 range.");
            return;
        }

        var generation = _authority.GetPeerSessionGeneration(sender);
        if (generation <= 0)
        {
            SendRejection(sender, operationId, kind, "Peer has no active Magenheim authority session.");
            return;
        }
        if (!_authority.IsPeerMutationAuthorized(sender))
        {
            SendRejection(sender, operationId, kind,
                $"Peer is not admitted to Magenheim gameplay mutation. {_authority.GetPeerResult(sender).Diagnostic}");
            return;
        }

        var remoteKey = new RemoteOperationKey(sender, generation, operationId);
        if (ServerOperations.TryGetValue(remoteKey, out var existing))
        {
            if (existing.Kind != kind)
            {
                SendRejection(sender, operationId, kind,
                    "Operation id was replayed for a different socket operation; mutation is denied.");
                return;
            }
            SendResponse(sender, operationId, existing);
            return;
        }

        if (!TryResolvePeerPlayer(sender, out var player, out var playerError))
        {
            SendRejection(sender, operationId, kind, playerError);
            return;
        }
        if (!TryResolveNearbyMagenheimStation(player, out var station, out var stationError))
        {
            SendRejection(sender, operationId, kind, stationError);
            return;
        }
        if (kind == RemoteSocketKind.ExtractCrystal && !HasFacetingWheel(station))
        {
            SendRejection(sender, operationId, kind,
                "Crystal extraction requires the Faceting Wheel at the Geologist's Workstation.");
            return;
        }

        if (!TryResolveCanonicalDescriptor(suppliedDescriptor, out var descriptor, out var descriptorError))
        {
            SendRejection(sender, operationId, kind, descriptorError);
            return;
        }

        ServerOperationRecord record;
        switch (kind)
        {
            case RemoteSocketKind.AddSlot:
                if (hasCrystal || sourceCrystalCount != 0 || crystalIndex != -1)
                {
                    SendRejection(sender, operationId, kind, "Add-slot request contains conflicting crystal intent.");
                    return;
                }
                record = PrepareServerSocketMutation(
                    sender, generation, operationId, kind, descriptor, state,
                    SocketTransactionKind.AddSlot, null, 0);
                break;

            case RemoteSocketKind.InstallCrystal:
                if (!hasCrystal || crystal is null || sourceCrystalCount < 1 || crystalIndex != -1)
                {
                    SendRejection(sender, operationId, kind, "Crystal-install request is incomplete or conflicting.");
                    return;
                }
                if (crystal.Value.Tier == CrystalTier.Rough ||
                    PrefabManager.Instance.GetPrefab(CrystalPrefab(crystal.Value)) is null)
                {
                    SendRejection(sender, operationId, kind, "Requested socket crystal is not a valid registered Simple-or-better Magenheim crystal.");
                    return;
                }
                record = PrepareServerSocketMutation(
                    sender, generation, operationId, kind, descriptor, state,
                    SocketTransactionKind.InstallCrystal, crystal, sourceCrystalCount);
                break;

            case RemoteSocketKind.ExtractCrystal:
                if (hasCrystal || sourceCrystalCount != 0)
                {
                    SendRejection(sender, operationId, kind, "Extraction request contains conflicting source-crystal intent.");
                    return;
                }
                record = PrepareServerExtraction(
                    sender, generation, operationId, kind, state, crystalIndex, clientSkill);
                break;

            default:
                SendRejection(sender, operationId, kind, "Unsupported socket operation.");
                return;
        }

        if (!record.Authorized)
        {
            SendResponse(sender, operationId, record);
            return;
        }

        TrimServerOperations();
        if (ServerOperations.Count >= MaximumTrackedServerOperations)
        {
            AbortPrepared(record);
            SendRejection(sender, operationId, kind,
                "Server has too many unacknowledged Magenheim socket operations; retry after pending operations clear.");
            return;
        }

        ServerOperations.Add(remoteKey, record);
        ServerOperationOrder.Enqueue(remoteKey);
        SendResponse(sender, operationId, record);
    }

    private static ServerOperationRecord PrepareServerSocketMutation(
        long peerId,
        long generation,
        string operationId,
        RemoteSocketKind remoteKind,
        EquipmentDescriptor descriptor,
        SocketState state,
        SocketTransactionKind kind,
        Crystal? crystal,
        int sourceCrystalCount)
    {
        var services = _services!;
        var authority = _authority!;
        try
        {
            var request = new SocketTransactionRequest(
                authority.GetPeerResult(peerId),
                descriptor,
                services.SocketPolicy,
                state,
                kind,
                crystal,
                sourceCrystalCount);
            var guardKey = new SocketOperationKey(peerId, generation, operationId);
            var decision = services.SocketOperations.Begin(guardKey, request);
            if (!decision.MutationAuthorized)
                return ServerOperationRecord.Rejected(remoteKind, decision.Diagnostic);

            return ServerOperationRecord.PreparedSocket(
                remoteKind,
                guardKey,
                decision.Plan.OriginalState,
                decision.Plan.ResultState,
                decision.Plan.ConsumeCrystalCount,
                string.Empty,
                0,
                (float)decision.Plan.CrystalShapingExperience,
                decision.Plan.Diagnostic);
        }
        catch (Exception exception)
        {
            return ServerOperationRecord.Rejected(remoteKind,
                $"Server could not prepare socket mutation: {exception.Message}");
        }
    }

    private static ServerOperationRecord PrepareServerExtraction(
        long peerId,
        long generation,
        string operationId,
        RemoteSocketKind kind,
        SocketState state,
        int crystalIndex,
        int clientSkill)
    {
        var services = _services!;
        var authority = _authority!;
        try
        {
            var request = new SocketExtractionRequest(
                authority.GetPeerResult(peerId),
                state,
                crystalIndex,
                clientSkill,
                SocketExtractionService.RequiredStationId,
                ServerRandom.NextUnit());
            var guardKey = new SocketExtractionOperationKey(peerId, generation, operationId);
            var decision = services.SocketExtractionOperations.Begin(guardKey, request);
            if (!decision.MutationAuthorized)
                return ServerOperationRecord.Rejected(kind, decision.Diagnostic);

            var outputPrefab = decision.Plan.ReturnsCrystal
                ? CrystalPrefab(decision.Plan.ReturnedCrystal!.Value)
                : decision.Plan.ShardElement is { } element
                    ? ShardPrefab(element)
                    : string.Empty;
            var outputAmount = decision.Plan.ReturnsCrystal ? 1 : decision.Plan.ShardReturnCount;
            if (string.IsNullOrWhiteSpace(outputPrefab) || outputAmount <= 0 ||
                PrefabManager.Instance.GetPrefab(outputPrefab) is null)
            {
                services.SocketExtractionOperations.AbortPrepared(guardKey);
                return ServerOperationRecord.Rejected(kind,
                    $"Server extraction output '{outputPrefab}' is unavailable; mutation is denied.");
            }

            return ServerOperationRecord.PreparedExtraction(
                kind,
                guardKey,
                decision.Plan.OriginalState,
                decision.Plan.ResultState,
                outputPrefab,
                outputAmount,
                (float)decision.Plan.CrystalShapingExperience,
                decision.Plan.Diagnostic);
        }
        catch (Exception exception)
        {
            return ServerOperationRecord.Rejected(kind,
                $"Server could not prepare crystal extraction: {exception.Message}");
        }
    }

    private static IEnumerator ReceiveClientResponse(long sender, ZPackage package)
    {
        try
        {
            var messageType = package.ReadInt();
            if (messageType != ResponseMessage)
                throw new InvalidOperationException($"Unexpected socket response message type {messageType}.");

            var operationId = package.ReadString();
            var rawKind = package.ReadInt();
            var authorized = package.ReadBool();
            var diagnostic = package.ReadString();
            var originalState = ReadState(package);
            var resultState = ReadState(package);
            var consumeCrystalCount = package.ReadInt();
            var outputPrefab = package.ReadString();
            var outputAmount = package.ReadInt();
            var experience = package.ReadSingle();

            if (!Enum.IsDefined(typeof(RemoteSocketKind), rawKind))
                throw new InvalidOperationException($"Invalid socket response kind {rawKind}.");
            var kind = (RemoteSocketKind)rawKind;

            if (!ClientOperations.TryGetValue(operationId, out var pending))
            {
                _log?.LogWarning($"Ignoring unrecognized Magenheim socket response '{operationId}'.");
                yield break;
            }
            if (pending.Kind != kind)
            {
                SendAcknowledgement(operationId, kind, false);
                ClientOperations.Remove(operationId);
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                    "Magenheim rejected a mismatched socket response without mutating inventory.");
                yield break;
            }
            if (!authorized)
            {
                ClientOperations.Remove(operationId);
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, diagnostic);
                yield break;
            }
            if (pending.Applied)
            {
                SendAcknowledgement(operationId, kind, true);
                yield break;
            }

            var player = Player.m_localPlayer;
            if (player is null)
            {
                SendAcknowledgement(operationId, kind, false);
                ClientOperations.Remove(operationId);
                yield break;
            }
            var inventory = player.GetInventory();
            if (!inventory.ContainsItem(pending.Equipment))
            {
                SendAcknowledgement(operationId, kind, false);
                ClientOperations.Remove(operationId);
                player.Message(MessageHud.MessageType.Center,
                    "Selected equipment changed before the server response; operation was cancelled without mutation.");
                yield break;
            }
            if (!StatesMatch(pending.OriginalState, originalState) ||
                !SocketStatesStillMatch(pending.Equipment, originalState, out var staleReason))
            {
                SendAcknowledgement(operationId, kind, false);
                ClientOperations.Remove(operationId);
                player.Message(MessageHud.MessageType.Center,
                    string.IsNullOrWhiteSpace(staleReason)
                        ? "Server socket plan did not match the submitted item state; mutation was cancelled."
                        : staleReason);
                yield break;
            }

            GameObject? output = null;
            if (kind == RemoteSocketKind.ExtractCrystal)
            {
                output = PrefabManager.Instance.GetPrefab(outputPrefab);
                if (!output || outputAmount <= 0 || !inventory.CanAddItem(output, outputAmount))
                {
                    SendAcknowledgement(operationId, kind, false);
                    ClientOperations.Remove(operationId);
                    player.Message(MessageHud.MessageType.Center,
                        output ? "Not enough inventory capacity for extraction output; socket state was not changed."
                               : $"Extraction output '{outputPrefab}' is unavailable; socket state was not changed.");
                    yield break;
                }
            }
            else if (!string.IsNullOrEmpty(outputPrefab) || outputAmount != 0)
            {
                SendAcknowledgement(operationId, kind, false);
                ClientOperations.Remove(operationId);
                yield break;
            }

            if (kind == RemoteSocketKind.InstallCrystal)
            {
                if (pending.CrystalItem is null || pending.Crystal is null ||
                    !inventory.ContainsItem(pending.CrystalItem) ||
                    pending.CrystalItem.m_stack < consumeCrystalCount ||
                    consumeCrystalCount != 1 ||
                    !TryParseCrystal(pending.CrystalItem, out var currentCrystal) ||
                    currentCrystal != pending.Crystal.Value)
                {
                    SendAcknowledgement(operationId, kind, false);
                    ClientOperations.Remove(operationId);
                    player.Message(MessageHud.MessageType.Center,
                        "Selected crystal changed before the server response; installation was cancelled without mutation.");
                    yield break;
                }
            }
            else if (consumeCrystalCount != 0)
            {
                SendAcknowledgement(operationId, kind, false);
                ClientOperations.Remove(operationId);
                yield break;
            }

            var snapshot = InventorySnapshot.Capture(inventory);
            var oldMetadata = CaptureMagenheimMetadata(pending.Equipment);
            try
            {
                if (kind == RemoteSocketKind.InstallCrystal &&
                    !inventory.RemoveItem(pending.CrystalItem!, consumeCrystalCount))
                    throw new InvalidOperationException("Inventory refused the server-approved crystal consumption.");

                ItemSocketAdapter.Write(pending.Equipment, resultState);
                if (kind == RemoteSocketKind.ExtractCrystal &&
                    (output is null || !inventory.AddItem(output, outputAmount)))
                    throw new InvalidOperationException("Inventory refused the preflighted extraction output.");

                NotifyInventoryChanged(inventory);
                pending.Applied = true;
                if (experience > 0f)
                    player.RaiseSkill(EarthContentRegistrar.CrystalShapingSkill, experience);
                player.Message(MessageHud.MessageType.Center, diagnostic);
                SendAcknowledgement(operationId, kind, true);
            }
            catch (Exception exception)
            {
                snapshot.Restore(inventory);
                RestoreMagenheimMetadata(pending.Equipment, oldMetadata);
                NotifyInventoryChanged(inventory);
                SendAcknowledgement(operationId, kind, false);
                ClientOperations.Remove(operationId);
                player.Message(MessageHud.MessageType.Center,
                    $"Socket operation rolled back: {exception.Message}");
            }
        }
        catch (Exception exception)
        {
            _log?.LogError($"Failed to process Magenheim socket response: {exception}");
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                "Magenheim socket response failed safely; verify inventory before retrying.");
        }

        yield break;
    }

    private static void HandleServerAcknowledgement(long sender, ZPackage package)
    {
        if (_services is null || _authority is null) return;

        var operationId = package.ReadString();
        var rawKind = package.ReadInt();
        var applied = package.ReadBool();
        if (!Enum.IsDefined(typeof(RemoteSocketKind), rawKind)) return;
        var kind = (RemoteSocketKind)rawKind;
        var generation = _authority.GetPeerSessionGeneration(sender);
        if (generation <= 0) return;

        var key = new RemoteOperationKey(sender, generation, operationId);
        if (!ServerOperations.TryGetValue(key, out var record) || record.Kind != kind) return;

        if (applied)
        {
            if (record.Applied) return;
            var committed = record.GuardKind switch
            {
                ServerGuardKind.Socket => _services.SocketOperations.MarkApplied(record.SocketKey),
                ServerGuardKind.Extraction => _services.SocketExtractionOperations.MarkApplied(record.ExtractionKey),
                _ => false,
            };
            if (!committed)
            {
                _log?.LogError($"Could not commit prepared remote socket operation '{operationId}' for peer {sender}.");
                return;
            }
            record.Applied = true;
            return;
        }

        if (!record.Applied)
        {
            AbortPrepared(record);
            ServerOperations.Remove(key);
        }
    }

    private static void SendResponse(long peerId, string operationId, ServerOperationRecord record)
    {
        if (_rpc is null) return;
        var package = new ZPackage();
        package.Write(ResponseMessage);
        package.Write(operationId ?? string.Empty);
        package.Write((int)record.Kind);
        package.Write(record.Authorized);
        package.Write(record.Diagnostic ?? string.Empty);
        WriteState(package, record.OriginalState);
        WriteState(package, record.ResultState);
        package.Write(record.ConsumeCrystalCount);
        package.Write(record.OutputPrefab ?? string.Empty);
        package.Write(record.OutputAmount);
        package.Write(record.Experience);
        _rpc.SendPackage(peerId, package);
    }

    private static void SendRejection(
        long peerId,
        string operationId,
        RemoteSocketKind kind,
        string diagnostic) =>
        SendResponse(peerId, operationId,
            ServerOperationRecord.Rejected(kind, diagnostic));

    private static void SendAcknowledgement(string operationId, RemoteSocketKind kind, bool applied)
    {
        if (_rpc is null || ZRoutedRpc.instance is null) return;
        var package = new ZPackage();
        package.Write(AcknowledgementMessage);
        package.Write(operationId);
        package.Write((int)kind);
        package.Write(applied);
        _rpc.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
    }

    private static bool TryResolveCanonicalDescriptor(
        EquipmentDescriptor supplied,
        out EquipmentDescriptor canonical,
        out string diagnostic)
    {
        canonical = null!;
        try
        {
            supplied.Validate();
        }
        catch (Exception exception)
        {
            diagnostic = $"Client equipment descriptor is invalid: {exception.Message}";
            return false;
        }

        var prefab = PrefabManager.Instance.GetPrefab(supplied.PrefabName);
        var itemDrop = prefab ? prefab.GetComponent<ItemDrop>() : null;
        if (!itemDrop)
        {
            diagnostic = $"Equipment prefab '{supplied.PrefabName}' is not registered on the server.";
            return false;
        }

        canonical = ItemSocketAdapter.Describe(itemDrop.m_itemData);
        if (!string.Equals(canonical.PrefabName, supplied.PrefabName, StringComparison.Ordinal) ||
            !string.Equals(canonical.ModOrigin, supplied.ModOrigin, StringComparison.Ordinal) ||
            canonical.Category != supplied.Category ||
            !string.Equals(canonical.ItemName, supplied.ItemName, StringComparison.Ordinal))
        {
            diagnostic = "Client equipment descriptor does not match the server's registered prefab identity; mutation is denied.";
            return false;
        }

        diagnostic = string.Empty;
        return true;
    }

    private static bool TryResolvePeerPlayer(long peerId, out Player player, out string diagnostic)
    {
        player = null!;
        if (ZNet.instance is null || ZNetScene.instance is null)
        {
            diagnostic = "Server world state is unavailable for socket validation.";
            return false;
        }

        var peer = ZNet.instance.GetPeer(peerId);
        if (peer is null)
        {
            diagnostic = "Requesting peer is no longer connected.";
            return false;
        }

        var instance = ZNetScene.instance.FindInstance(peer.m_characterID);
        var resolved = instance ? instance.GetComponent<Player>() : null;
        if (!resolved)
        {
            diagnostic = "Requesting player's world character is unavailable for socket validation.";
            return false;
        }

        player = resolved;
        diagnostic = string.Empty;
        return true;
    }

    private static bool TryResolveNearbyMagenheimStation(
        Player player,
        out CraftingStation station,
        out string diagnostic)
    {
        station = CraftingStation.FindClosestStationInRange(
            "Geologist's Workstation",
            player.transform.position,
            4f);
        if (!station ||
            !string.Equals(NormalizeCloneName(station.gameObject.name), WorkshopRegistrar.StationPrefab, StringComparison.Ordinal) ||
            !station.InUseDistance(player))
        {
            diagnostic = "No in-range Geologist's Workstation could be validated for the requesting player.";
            return false;
        }

        diagnostic = string.Empty;
        return true;
    }

    private static bool TryGetCurrentMagenheimStation(Player player, out CraftingStation station)
    {
        station = player.GetCurrentCraftingStation();
        return station && string.Equals(
            NormalizeCloneName(station.gameObject.name),
            WorkshopRegistrar.StationPrefab,
            StringComparison.Ordinal);
    }

    private static bool HasFacetingWheel(CraftingStation station)
    {
        var extensions = new List<StationExtension>();
        StationExtension.FindExtensions(station, station.transform.position, extensions);
        return extensions.Any(extension => string.Equals(
            NormalizeCloneName(extension.gameObject.name),
            WorkshopRegistrar.FacetingPrefab,
            StringComparison.Ordinal));
    }

    private static void WriteDescriptor(ZPackage package, EquipmentDescriptor descriptor)
    {
        package.Write(descriptor.PrefabName ?? string.Empty);
        package.Write(descriptor.ModOrigin ?? string.Empty);
        package.Write((int)descriptor.Category);
        package.Write(descriptor.ItemName ?? string.Empty);
    }

    private static EquipmentDescriptor ReadDescriptor(ZPackage package)
    {
        var prefabName = package.ReadString();
        var modOrigin = package.ReadString();
        var rawCategory = package.ReadInt();
        var itemName = package.ReadString();
        var category = Enum.IsDefined(typeof(EquipmentCategory), rawCategory)
            ? (EquipmentCategory)rawCategory
            : EquipmentCategory.Unknown;
        return new EquipmentDescriptor(prefabName, modOrigin, category) { ItemName = itemName };
    }

    private static void WriteState(ZPackage package, SocketState state)
    {
        package.Write(state.UnlockedSlots);
        package.Write(state.InstalledCrystals.Count);
        foreach (var crystal in state.InstalledCrystals)
            WriteCrystal(package, crystal);
    }

    private static SocketState ReadState(ZPackage package)
    {
        var unlockedSlots = package.ReadInt();
        var count = package.ReadInt();
        if (unlockedSlots < 0 || unlockedSlots > SocketState.MaximumSupportedSlots ||
            count < 0 || count > unlockedSlots || count > SocketState.MaximumSupportedSlots)
            throw new InvalidOperationException("Socket RPC state is outside supported slot bounds.");

        var crystals = new Crystal[count];
        for (var index = 0; index < count; index++)
            crystals[index] = ReadCrystal(package);
        return new SocketState(unlockedSlots, crystals);
    }

    private static void WriteCrystal(ZPackage package, Crystal crystal)
    {
        package.Write((int)crystal.Element);
        package.Write((int)crystal.Tier);
    }

    private static Crystal ReadCrystal(ZPackage package)
    {
        var rawElement = package.ReadInt();
        var rawTier = package.ReadInt();
        if (!Enum.IsDefined(typeof(ElementalAlignment), rawElement) ||
            !Enum.IsDefined(typeof(CrystalTier), rawTier))
            throw new InvalidOperationException("Socket RPC contains an unknown crystal identity.");
        return new Crystal((ElementalAlignment)rawElement, (CrystalTier)rawTier);
    }

    private static bool TryParseCrystal(ItemDrop.ItemData item, out Crystal crystal)
    {
        crystal = default;
        if (item is null || !item.m_dropPrefab) return false;
        const string prefix = "Magenheim_Crystal_";
        var prefabName = item.m_dropPrefab.name;
        if (!prefabName.StartsWith(prefix, StringComparison.Ordinal)) return false;

        var remainder = prefabName.Substring(prefix.Length);
        var separator = remainder.LastIndexOf('_');
        if (separator <= 0 || separator >= remainder.Length - 1) return false;
        var elementText = remainder.Substring(0, separator);
        var tierText = remainder.Substring(separator + 1);
        if (!Enum.TryParse(elementText, false, out ElementalAlignment element) ||
            !Enum.IsDefined(typeof(ElementalAlignment), element) ||
            !Enum.TryParse(tierText, false, out CrystalTier tier) ||
            !Enum.IsDefined(typeof(CrystalTier), tier))
            return false;

        crystal = new Crystal(element, tier);
        return true;
    }

    private static bool SocketStatesStillMatch(
        ItemDrop.ItemData equipment,
        SocketState expected,
        out string reason)
    {
        if (!ItemSocketAdapter.TryRead(equipment, out var current, out var diagnostic))
        {
            reason = $"Socket metadata changed or became invalid before mutation: {diagnostic}";
            return false;
        }
        if (!StatesMatch(current, expected))
        {
            reason = "Socket metadata changed after operation planning; mutation was cancelled without overwriting the newer item state.";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    private static bool StatesMatch(SocketState left, SocketState right)
    {
        if (left.UnlockedSlots != right.UnlockedSlots ||
            left.InstalledCrystals.Count != right.InstalledCrystals.Count)
            return false;
        for (var index = 0; index < left.InstalledCrystals.Count; index++)
            if (left.InstalledCrystals[index] != right.InstalledCrystals[index]) return false;
        return true;
    }

    private static MetadataSnapshot CaptureMagenheimMetadata(ItemDrop.ItemData item)
    {
        if (item.m_customData is not null &&
            item.m_customData.TryGetValue(SocketMetadataCodec.CustomDataKey, out var value))
            return new MetadataSnapshot(true, value);
        return new MetadataSnapshot(false, string.Empty);
    }

    private static void RestoreMagenheimMetadata(ItemDrop.ItemData item, MetadataSnapshot snapshot)
    {
        item.m_customData ??= new Dictionary<string, string>();
        if (snapshot.HadValue)
            item.m_customData[SocketMetadataCodec.CustomDataKey] = snapshot.Value;
        else
            item.m_customData.Remove(SocketMetadataCodec.CustomDataKey);
    }

    private static void NotifyInventoryChanged(Inventory inventory)
    {
        var changed = HarmonyLib.AccessTools.Method(typeof(Inventory), "Changed");
        changed?.Invoke(inventory, Array.Empty<object>());
        InventoryGui.instance?.m_playerGrid?.UpdateInventory(inventory, Player.m_localPlayer, null);
    }

    private static string NormalizeCloneName(string name) =>
        name.EndsWith("(Clone)", StringComparison.Ordinal)
            ? name.Substring(0, name.Length - "(Clone)".Length)
            : name;

    private static string CrystalPrefab(Crystal crystal) =>
        $"Magenheim_Crystal_{crystal.Element}_{crystal.Tier}";

    private static string ShardPrefab(ElementalAlignment element) =>
        $"Magenheim_Shard_{element}";

    private static string DisplayName(ItemDrop.ItemData item)
    {
        var localized = Localization.instance?.Localize(item.m_shared.m_name);
        if (!string.IsNullOrWhiteSpace(localized) && localized != item.m_shared.m_name)
            return localized;
        return item.m_dropPrefab ? item.m_dropPrefab.name : item.m_shared.m_name;
    }

    private static void AbortPrepared(ServerOperationRecord record)
    {
        if (_services is null || !record.Authorized || record.Applied) return;
        switch (record.GuardKind)
        {
            case ServerGuardKind.Socket:
                _services.SocketOperations.AbortPrepared(record.SocketKey);
                break;
            case ServerGuardKind.Extraction:
                _services.SocketExtractionOperations.AbortPrepared(record.ExtractionKey);
                break;
        }
    }

    private static void TrimClientOperations()
    {
        while (ClientOperations.Count >= MaximumTrackedClientOperations && ClientOperationOrder.Count > 0)
        {
            var key = ClientOperationOrder.Dequeue();
            if (ClientOperations.TryGetValue(key, out var record) && record.Applied)
                ClientOperations.Remove(key);
        }
    }

    private static void TrimServerOperations()
    {
        while (ServerOperations.Count >= MaximumTrackedServerOperations && ServerOperationOrder.Count > 0)
        {
            var key = ServerOperationOrder.Dequeue();
            if (ServerOperations.TryGetValue(key, out var record) && record.Applied)
                ServerOperations.Remove(key);
        }
    }

    private enum RemoteSocketKind
    {
        AddSlot = 1,
        InstallCrystal = 2,
        ExtractCrystal = 3,
    }

    private enum ServerGuardKind
    {
        None = 0,
        Socket = 1,
        Extraction = 2,
    }

    private readonly record struct RemoteOperationKey(long PeerId, long SessionGeneration, string OperationId);

    private readonly struct MetadataSnapshot
    {
        internal MetadataSnapshot(bool hadValue, string value)
        {
            HadValue = hadValue;
            Value = value;
        }
        internal bool HadValue { get; }
        internal string Value { get; }
    }

    private sealed class ClientOperationRecord
    {
        internal ClientOperationRecord(
            RemoteSocketKind kind,
            ItemDrop.ItemData equipment,
            ItemDrop.ItemData? crystalItem,
            SocketState originalState,
            Crystal? crystal,
            int crystalIndex)
        {
            Kind = kind;
            Equipment = equipment;
            CrystalItem = crystalItem;
            OriginalState = originalState;
            Crystal = crystal;
            CrystalIndex = crystalIndex;
        }

        internal RemoteSocketKind Kind { get; }
        internal ItemDrop.ItemData Equipment { get; }
        internal ItemDrop.ItemData? CrystalItem { get; }
        internal SocketState OriginalState { get; }
        internal Crystal? Crystal { get; }
        internal int CrystalIndex { get; }
        internal bool Applied { get; set; }
    }

    private sealed class ServerOperationRecord
    {
        private ServerOperationRecord(
            RemoteSocketKind kind,
            bool authorized,
            ServerGuardKind guardKind,
            SocketOperationKey socketKey,
            SocketExtractionOperationKey extractionKey,
            SocketState originalState,
            SocketState resultState,
            int consumeCrystalCount,
            string outputPrefab,
            int outputAmount,
            float experience,
            string diagnostic)
        {
            Kind = kind;
            Authorized = authorized;
            GuardKind = guardKind;
            SocketKey = socketKey;
            ExtractionKey = extractionKey;
            OriginalState = originalState;
            ResultState = resultState;
            ConsumeCrystalCount = consumeCrystalCount;
            OutputPrefab = outputPrefab;
            OutputAmount = outputAmount;
            Experience = experience;
            Diagnostic = diagnostic;
        }

        internal RemoteSocketKind Kind { get; }
        internal bool Authorized { get; }
        internal ServerGuardKind GuardKind { get; }
        internal SocketOperationKey SocketKey { get; }
        internal SocketExtractionOperationKey ExtractionKey { get; }
        internal SocketState OriginalState { get; }
        internal SocketState ResultState { get; }
        internal int ConsumeCrystalCount { get; }
        internal string OutputPrefab { get; }
        internal int OutputAmount { get; }
        internal float Experience { get; }
        internal string Diagnostic { get; }
        internal bool Applied { get; set; }

        internal static ServerOperationRecord Rejected(RemoteSocketKind kind, string diagnostic) =>
            new(kind, false, ServerGuardKind.None, default, default,
                SocketState.Empty, SocketState.Empty, 0, string.Empty, 0, 0f, diagnostic);

        internal static ServerOperationRecord PreparedSocket(
            RemoteSocketKind kind,
            SocketOperationKey key,
            SocketState originalState,
            SocketState resultState,
            int consumeCrystalCount,
            string outputPrefab,
            int outputAmount,
            float experience,
            string diagnostic) =>
            new(kind, true, ServerGuardKind.Socket, key, default,
                originalState, resultState, consumeCrystalCount, outputPrefab, outputAmount, experience, diagnostic);

        internal static ServerOperationRecord PreparedExtraction(
            RemoteSocketKind kind,
            SocketExtractionOperationKey key,
            SocketState originalState,
            SocketState resultState,
            string outputPrefab,
            int outputAmount,
            float experience,
            string diagnostic) =>
            new(kind, true, ServerGuardKind.Extraction, default, key,
                originalState, resultState, 0, outputPrefab, outputAmount, experience, diagnostic);
    }

    private sealed class InventorySnapshot
    {
        private readonly ItemDrop.ItemData[] _items;
        private readonly Dictionary<ItemDrop.ItemData, int> _stacks;

        private InventorySnapshot(ItemDrop.ItemData[] items)
        {
            _items = items;
            _stacks = items.ToDictionary(item => item, item => item.m_stack);
        }

        internal static InventorySnapshot Capture(Inventory inventory) =>
            new(inventory.GetAllItems().ToArray());

        internal void Restore(Inventory inventory)
        {
            var originalSet = new HashSet<ItemDrop.ItemData>(_items);
            foreach (var current in inventory.GetAllItems().ToArray())
            {
                if (!originalSet.Contains(current))
                    inventory.RemoveItem(current);
            }

            foreach (var original in _items)
            {
                if (inventory.ContainsItem(original))
                {
                    original.m_stack = _stacks[original];
                    continue;
                }

                original.m_stack = _stacks[original];
                if (!inventory.AddItem(original))
                    throw new InvalidOperationException(
                        $"Socket transaction rollback could not restore '{DisplayName(original)}'.");
            }
        }
    }
}
