using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core;
using Magenheim.Core.Transactions;
using UnityEngine;

namespace Magenheim.Runtime.Networking;

/// <summary>
/// Remote-client transport for Geologist's Workstation geode opening and refinement.
/// The client chooses an operation and owns its local Valheim inventory mutation; the server
/// owns definition admission, station/upgrade validation, random rolls, and the resulting
/// mutation plan. A client never supplies a refinement/geode outcome or random roll.
/// </summary>
internal static class WorkshopOperationRpc
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
            "WorkshopOperation",
            ReceiveServerMessage,
            ReceiveClientResponse);
    }

    internal static bool TrySubmitClient(
        InventoryGui gui,
        Player player,
        WorkshopOperationDefinition operation,
        out string diagnostic)
    {
        if (_rpc is null || _services is null || _authority is null)
        {
            diagnostic = "Magenheim workshop networking is not initialized.";
            return false;
        }
        if (gui is null || player is null || operation is null)
        {
            diagnostic = "Workshop request is missing required local state.";
            return false;
        }
        if (ZNet.instance is null || ZNet.instance.IsServer())
        {
            diagnostic = "Remote workshop transport is only used by non-host clients.";
            return false;
        }
        if (!_authority.IsClientMutationAuthorized)
        {
            diagnostic = $"Server definition authority has not admitted Magenheim mutation. {_authority.ClientAuthorityResult.Diagnostic}";
            return false;
        }
        if (!TryGetCurrentMagenheimStation(player, out _))
        {
            diagnostic = "Use a Geologist's Workstation for this operation.";
            return false;
        }

        var inventory = player.GetInventory();
        if (FindSource(inventory, operation.SourcePrefab) is null)
        {
            diagnostic = "The required source item is no longer in your inventory.";
            return false;
        }

        TrimClientOperations();
        if (ClientOperations.Count >= MaximumTrackedClientOperations)
        {
            diagnostic = "Too many unacknowledged Magenheim workstation operations are pending; reconnect before submitting more.";
            return false;
        }

        var operationId = Guid.NewGuid().ToString("N");
        var skill = Mathf.Clamp(
            Mathf.FloorToInt(player.GetSkillLevel(EarthContentRegistrar.CrystalShapingSkill)),
            0,
            100);
        ClientOperations.Add(operationId, new ClientOperationRecord(operation.RecipeName));
        ClientOperationOrder.Enqueue(operationId);

        var package = new ZPackage();
        package.Write(RequestMessage);
        package.Write(operationId);
        package.Write(operation.RecipeName);
        package.Write(skill);
        _rpc.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);

        diagnostic = "Sent workstation operation to the server for authoritative resolution.";
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
                    _log?.LogWarning($"Ignoring unknown Magenheim workshop RPC message type {messageType} from peer {sender}.");
                    break;
            }
        }
        catch (Exception exception)
        {
            _log?.LogError($"Failed to process Magenheim workshop RPC from peer {sender}: {exception}");
        }

        yield break;
    }

    private static void HandleServerRequest(long sender, ZPackage package)
    {
        if (_rpc is null || _services is null || _authority is null)
            throw new InvalidOperationException("Workshop RPC server state is not configured.");

        var operationId = package.ReadString();
        var recipeName = package.ReadString();
        var clientSkill = package.ReadInt();

        if (string.IsNullOrWhiteSpace(operationId) || operationId.Length > 128)
        {
            SendRejection(sender, operationId, recipeName, "Invalid workstation operation id.");
            return;
        }
        if (!WorkshopOperationCatalog.TryGet(recipeName, out var operation))
        {
            SendRejection(sender, operationId, recipeName, "Unknown Magenheim workstation operation.");
            return;
        }
        if (clientSkill < 0 || clientSkill > 100)
        {
            SendRejection(sender, operationId, recipeName, "Crystal Shaping skill snapshot is outside the valid 0-100 range.");
            return;
        }

        var generation = _authority.GetPeerSessionGeneration(sender);
        if (generation <= 0)
        {
            SendRejection(sender, operationId, recipeName, "Peer has no active Magenheim authority session.");
            return;
        }
        if (!_authority.IsPeerMutationAuthorized(sender))
        {
            SendRejection(sender, operationId, recipeName,
                $"Peer is not admitted to Magenheim gameplay mutation. {_authority.GetPeerResult(sender).Diagnostic}");
            return;
        }

        var remoteKey = new RemoteOperationKey(sender, generation, operationId);
        if (ServerOperations.TryGetValue(remoteKey, out var existing))
        {
            if (!string.Equals(existing.RecipeName, recipeName, StringComparison.Ordinal))
            {
                SendRejection(sender, operationId, recipeName,
                    "Operation id was replayed for a different workstation operation; mutation is denied.");
                return;
            }

            SendResponse(sender, operationId, existing);
            return;
        }

        if (!TryResolvePeerPlayer(sender, out var player, out var playerError))
        {
            SendRejection(sender, operationId, recipeName, playerError);
            return;
        }
        if (!TryResolveNearbyMagenheimStation(player, out var station, out var stationError))
        {
            SendRejection(sender, operationId, recipeName, stationError);
            return;
        }
        if (!HasRequiredStationIdentity(station, operation.RequiredStationIdentity))
        {
            SendRejection(sender, operationId, recipeName,
                $"This refinement requires {RequiredStationDisplay(operation.RequiredStationIdentity)}.");
            return;
        }

        ServerOperationRecord record;
        if (operation.Kind == WorkshopOperationKind.OpenGeode)
            record = PrepareServerGeodeOpening(sender, generation, operationId, operation);
        else
            record = PrepareServerRefinement(sender, generation, operationId, operation, clientSkill);

        if (!record.Authorized)
        {
            SendResponse(sender, operationId, record);
            return;
        }

        TrimServerOperations();
        if (ServerOperations.Count >= MaximumTrackedServerOperations)
        {
            AbortPrepared(record);
            SendRejection(sender, operationId, recipeName,
                "Server has too many unacknowledged Magenheim workstation operations; retry after pending operations clear.");
            return;
        }

        ServerOperations.Add(remoteKey, record);
        ServerOperationOrder.Enqueue(remoteKey);
        SendResponse(sender, operationId, record);
    }

    private static ServerOperationRecord PrepareServerGeodeOpening(
        long peerId,
        long generation,
        string operationId,
        WorkshopOperationDefinition operation)
    {
        var services = _services!;
        var authority = _authority!;
        try
        {
            var geode = services.Definitions.Geodes.Single(entry =>
                string.Equals(entry.Id, operation.GeodeId, StringComparison.Ordinal));
            var crackingRequest = new GeodeCrackingRequest(
                geode,
                ServerRandom.NextUnit(),
                ServerRandom.NextUnit(),
                new[] { ServerRandom.NextUnit(), ServerRandom.NextUnit(), ServerRandom.NextUnit() });
            var preview = GeodeCrackingService.Crack(crackingRequest);
            if (!preview.IsSuccess)
                return ServerOperationRecord.Rejected(operation.RecipeName, preview.Reason);

            var request = new GeodeOpeningTransactionRequest(
                authority.GetPeerResult(peerId),
                1,
                preview.Crystals.Count,
                crackingRequest);
            var guardKey = new GeodeOpeningOperationKey(peerId, generation, operationId);
            var decision = services.GeodeOpeningOperations.Begin(guardKey, request);
            if (!decision.MutationAuthorized)
                return ServerOperationRecord.Rejected(operation.RecipeName, decision.Diagnostic);

            var grants = decision.Plan.GrantCrystals
                .GroupBy(CrystalPrefab)
                .Select(group => new RemoteGrant(group.Key, group.Count()))
                .ToArray();
            return ServerOperationRecord.PreparedGeode(
                operation.RecipeName,
                guardKey,
                decision.Plan.ConsumeGeodeCount,
                grants,
                CrystalShapingExperience.CrackGeode,
                decision.Plan.Diagnostic);
        }
        catch (Exception exception)
        {
            return ServerOperationRecord.Rejected(
                operation.RecipeName,
                $"Server could not prepare geode opening: {exception.Message}");
        }
    }

    private static ServerOperationRecord PrepareServerRefinement(
        long peerId,
        long generation,
        string operationId,
        WorkshopOperationDefinition operation,
        int clientSkill)
    {
        var services = _services!;
        var authority = _authority!;
        try
        {
            if (operation.SourceTier is null)
                return ServerOperationRecord.Rejected(operation.RecipeName, "Refinement operation has no source tier.");

            var refinement = new RefinementRequest(
                new Crystal(ElementalAlignment.Earth, operation.SourceTier.Value),
                clientSkill,
                operation.RequiredStationIdentity,
                ServerRandom.NextUnit());
            var request = new RefinementTransactionRequest(
                authority.GetPeerResult(peerId),
                1,
                1,
                refinement);
            var guardKey = new RefinementOperationKey(peerId, generation, operationId);
            var decision = services.RefinementOperations.Begin(guardKey, request);
            if (!decision.MutationAuthorized)
                return ServerOperationRecord.Rejected(operation.RecipeName, decision.Diagnostic);

            RemoteGrant[] grants;
            if (decision.Plan.GrantCrystal is { } crystal)
            {
                grants = new[] { new RemoteGrant(CrystalPrefab(crystal), 1) };
            }
            else if (decision.Plan.GrantShardCount > 0 && decision.Plan.ShardElement is { } shardElement)
            {
                grants = new[] { new RemoteGrant(ShardPrefab(shardElement), decision.Plan.GrantShardCount) };
            }
            else
            {
                grants = Array.Empty<RemoteGrant>();
            }

            var xp = decision.Plan.AwardExperience
                ? CrystalShapingExperience.ForRefinementAttempt(operation.SourceTier.Value)
                : 0f;
            return ServerOperationRecord.PreparedRefinement(
                operation.RecipeName,
                guardKey,
                decision.Plan.ConsumeSourceCount,
                grants,
                xp,
                decision.Plan.Diagnostic);
        }
        catch (Exception exception)
        {
            return ServerOperationRecord.Rejected(
                operation.RecipeName,
                $"Server could not prepare refinement: {exception.Message}");
        }
    }

    private static IEnumerator ReceiveClientResponse(long sender, ZPackage package)
    {
        try
        {
            var messageType = package.ReadInt();
            if (messageType != ResponseMessage)
                throw new InvalidOperationException($"Unexpected workshop response message type {messageType}.");

            var operationId = package.ReadString();
            var recipeName = package.ReadString();
            var authorized = package.ReadBool();
            var diagnostic = package.ReadString();
            var consumeSourceCount = package.ReadInt();
            var grantCount = package.ReadInt();
            if (grantCount < 0 || grantCount > 16)
                throw new InvalidOperationException($"Invalid workshop grant count {grantCount}.");

            var grants = new InventoryGrant[grantCount];
            for (var index = 0; index < grantCount; index++)
            {
                var prefabName = package.ReadString();
                var amount = package.ReadInt();
                if (string.IsNullOrWhiteSpace(prefabName) || amount <= 0 || amount > 64)
                    throw new InvalidOperationException("Workshop response contains an invalid inventory grant.");
                grants[index] = new InventoryGrant(prefabName, amount);
            }
            var experience = package.ReadSingle();

            if (!ClientOperations.TryGetValue(operationId, out var pending))
            {
                _log?.LogWarning($"Ignoring unrecognized Magenheim workshop response '{operationId}'.");
                yield break;
            }
            if (!string.Equals(pending.RecipeName, recipeName, StringComparison.Ordinal))
            {
                SendAcknowledgement(operationId, recipeName, false);
                ClientOperations.Remove(operationId);
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                    "Magenheim rejected a mismatched workstation response without mutating inventory.");
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
                SendAcknowledgement(operationId, recipeName, true);
                yield break;
            }
            if (!WorkshopOperationCatalog.TryGet(recipeName, out var operation))
            {
                SendAcknowledgement(operationId, recipeName, false);
                ClientOperations.Remove(operationId);
                yield break;
            }

            var player = Player.m_localPlayer;
            if (player is null)
            {
                SendAcknowledgement(operationId, recipeName, false);
                ClientOperations.Remove(operationId);
                yield break;
            }

            var inventory = player.GetInventory();
            var source = FindSource(inventory, operation.SourcePrefab);
            if (source is null)
            {
                SendAcknowledgement(operationId, recipeName, false);
                ClientOperations.Remove(operationId);
                player.Message(MessageHud.MessageType.Center,
                    "Source item changed before the server response; operation was cancelled without mutation.");
                yield break;
            }

            if (!WorkshopInventoryTransactions.TryApply(
                    inventory,
                    source,
                    consumeSourceCount,
                    grants,
                    out var mutationError))
            {
                SendAcknowledgement(operationId, recipeName, false);
                ClientOperations.Remove(operationId);
                player.Message(MessageHud.MessageType.Center, mutationError);
                yield break;
            }

            pending.Applied = true;
            if (experience > 0f)
                player.RaiseSkill(EarthContentRegistrar.CrystalShapingSkill, experience);
            player.Message(MessageHud.MessageType.Center, diagnostic);
            InventoryGui.instance?.UpdateCraftingPanel();
            SendAcknowledgement(operationId, recipeName, true);
        }
        catch (Exception exception)
        {
            _log?.LogError($"Failed to process Magenheim workshop response: {exception}");
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center,
                "Magenheim workstation response failed safely; verify inventory before retrying.");
        }

        yield break;
    }

    private static void HandleServerAcknowledgement(long sender, ZPackage package)
    {
        if (_services is null || _authority is null) return;

        var operationId = package.ReadString();
        var recipeName = package.ReadString();
        var applied = package.ReadBool();
        var generation = _authority.GetPeerSessionGeneration(sender);
        if (generation <= 0) return;

        var key = new RemoteOperationKey(sender, generation, operationId);
        if (!ServerOperations.TryGetValue(key, out var record)) return;
        if (!string.Equals(record.RecipeName, recipeName, StringComparison.Ordinal))
        {
            _log?.LogWarning($"Ignoring mismatched workstation acknowledgement '{operationId}' from peer {sender}.");
            return;
        }

        if (applied)
        {
            if (record.Applied) return;
            var committed = record.Kind switch
            {
                ServerGuardKind.Geode => _services.GeodeOpeningOperations.MarkApplied(record.GeodeKey),
                ServerGuardKind.Refinement => _services.RefinementOperations.MarkApplied(record.RefinementKey),
                _ => false,
            };
            if (!committed)
            {
                _log?.LogError($"Could not commit prepared remote workstation operation '{operationId}' for peer {sender}.");
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
        package.Write(record.RecipeName ?? string.Empty);
        package.Write(record.Authorized);
        package.Write(record.Diagnostic ?? string.Empty);
        package.Write(record.ConsumeSourceCount);
        package.Write(record.Grants.Count);
        foreach (var grant in record.Grants)
        {
            package.Write(grant.PrefabName);
            package.Write(grant.Amount);
        }
        package.Write(record.Experience);
        _rpc.SendPackage(peerId, package);
    }

    private static void SendRejection(long peerId, string operationId, string recipeName, string diagnostic) =>
        SendResponse(peerId, operationId,
            ServerOperationRecord.Rejected(recipeName ?? string.Empty, diagnostic));

    private static void SendAcknowledgement(string operationId, string recipeName, bool applied)
    {
        if (_rpc is null || ZRoutedRpc.instance is null) return;
        var package = new ZPackage();
        package.Write(AcknowledgementMessage);
        package.Write(operationId);
        package.Write(recipeName);
        package.Write(applied);
        _rpc.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
    }

    private static bool TryResolvePeerPlayer(long peerId, out Player player, out string diagnostic)
    {
        player = null!;
        if (ZNet.instance is null || ZNetScene.instance is null)
        {
            diagnostic = "Server world state is unavailable for workstation validation.";
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
            diagnostic = "Requesting player's world character is unavailable for workstation validation.";
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

    private static bool HasRequiredStationIdentity(CraftingStation station, string requiredIdentity)
    {
        if (string.Equals(requiredIdentity, WorkshopRegistrar.StationPrefab, StringComparison.Ordinal))
            return true;

        var extensions = new List<StationExtension>();
        StationExtension.FindExtensions(station, station.transform.position, extensions);
        return extensions.Any(extension => string.Equals(
            NormalizeCloneName(extension.gameObject.name),
            requiredIdentity,
            StringComparison.Ordinal));
    }

    private static string RequiredStationDisplay(string identity)
    {
        if (string.Equals(identity, WorkshopRegistrar.FracturingPrefab, StringComparison.Ordinal)) return "Fracturing Block";
        if (string.Equals(identity, WorkshopRegistrar.FacetingPrefab, StringComparison.Ordinal)) return "Faceting Wheel";
        if (string.Equals(identity, WorkshopRegistrar.ResonancePrefab, StringComparison.Ordinal)) return "Resonance Frame";
        return "Geologist's Workstation";
    }

    private static ItemDrop.ItemData? FindSource(Inventory inventory, string prefabName) =>
        inventory.GetAllItems().FirstOrDefault(item =>
            item.m_dropPrefab && string.Equals(item.m_dropPrefab.name, prefabName, StringComparison.Ordinal));

    private static string NormalizeCloneName(string name) =>
        name.EndsWith("(Clone)", StringComparison.Ordinal)
            ? name.Substring(0, name.Length - "(Clone)".Length)
            : name;

    private static string CrystalPrefab(Crystal crystal) =>
        $"Magenheim_Crystal_{crystal.Element}_{crystal.Tier}";

    private static string ShardPrefab(ElementalAlignment element) =>
        $"Magenheim_Shard_{element}";

    private static void AbortPrepared(ServerOperationRecord record)
    {
        if (_services is null || !record.Authorized || record.Applied) return;
        switch (record.Kind)
        {
            case ServerGuardKind.Geode:
                _services.GeodeOpeningOperations.AbortPrepared(record.GeodeKey);
                break;
            case ServerGuardKind.Refinement:
                _services.RefinementOperations.AbortPrepared(record.RefinementKey);
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

    private readonly record struct RemoteOperationKey(long PeerId, long SessionGeneration, string OperationId);

    private readonly struct RemoteGrant
    {
        internal RemoteGrant(string prefabName, int amount)
        {
            PrefabName = prefabName;
            Amount = amount;
        }
        internal string PrefabName { get; }
        internal int Amount { get; }
    }

    private enum ServerGuardKind
    {
        None = 0,
        Geode = 1,
        Refinement = 2,
    }

    private sealed class ClientOperationRecord
    {
        internal ClientOperationRecord(string recipeName) => RecipeName = recipeName;
        internal string RecipeName { get; }
        internal bool Applied { get; set; }
    }

    private sealed class ServerOperationRecord
    {
        private ServerOperationRecord(
            string recipeName,
            bool authorized,
            ServerGuardKind kind,
            GeodeOpeningOperationKey geodeKey,
            RefinementOperationKey refinementKey,
            int consumeSourceCount,
            IReadOnlyList<RemoteGrant> grants,
            float experience,
            string diagnostic)
        {
            RecipeName = recipeName;
            Authorized = authorized;
            Kind = kind;
            GeodeKey = geodeKey;
            RefinementKey = refinementKey;
            ConsumeSourceCount = consumeSourceCount;
            Grants = grants;
            Experience = experience;
            Diagnostic = diagnostic;
        }

        internal string RecipeName { get; }
        internal bool Authorized { get; }
        internal ServerGuardKind Kind { get; }
        internal GeodeOpeningOperationKey GeodeKey { get; }
        internal RefinementOperationKey RefinementKey { get; }
        internal int ConsumeSourceCount { get; }
        internal IReadOnlyList<RemoteGrant> Grants { get; }
        internal float Experience { get; }
        internal string Diagnostic { get; }
        internal bool Applied { get; set; }

        internal static ServerOperationRecord Rejected(string recipeName, string diagnostic) =>
            new(recipeName, false, ServerGuardKind.None, default, default, 0,
                Array.Empty<RemoteGrant>(), 0f, diagnostic);

        internal static ServerOperationRecord PreparedGeode(
            string recipeName,
            GeodeOpeningOperationKey key,
            int consumeSourceCount,
            IReadOnlyList<RemoteGrant> grants,
            float experience,
            string diagnostic) =>
            new(recipeName, true, ServerGuardKind.Geode, key, default, consumeSourceCount,
                grants, experience, diagnostic);

        internal static ServerOperationRecord PreparedRefinement(
            string recipeName,
            RefinementOperationKey key,
            int consumeSourceCount,
            IReadOnlyList<RemoteGrant> grants,
            float experience,
            string diagnostic) =>
            new(recipeName, true, ServerGuardKind.Refinement, default, key, consumeSourceCount,
                grants, experience, diagnostic);
    }
}
