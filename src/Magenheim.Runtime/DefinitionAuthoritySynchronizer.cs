using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Definitions;
using Magenheim.Core.Networking;
using Magenheim.Core.Transactions;

namespace Magenheim.Runtime.Networking;

internal sealed class DefinitionAuthoritySynchronizer
{
    private const int ServerAuthorityMessage = 1;
    private const int ClientAcknowledgementMessage = 2;

    private readonly DefinitionAuthorityDescriptor _localAuthority;
    private readonly ManualLogSource _logger;
    private readonly GeodeOpeningOperationGuard _geodeOpeningOperations;
    private readonly RefinementOperationGuard _refinementOperations;
    private readonly Dictionary<long, DefinitionAuthorityResult> _peerResults = new();
    private readonly Dictionary<long, long> _peerSessionGenerations = new();
    private readonly CustomRPC _rpc;

    internal DefinitionAuthoritySynchronizer(
        MagenheimDefinitionSet definitions,
        GeodeOpeningOperationGuard geodeOpeningOperations,
        RefinementOperationGuard refinementOperations,
        ManualLogSource logger)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        _geodeOpeningOperations = geodeOpeningOperations ?? throw new ArgumentNullException(nameof(geodeOpeningOperations));
        _refinementOperations = refinementOperations ?? throw new ArgumentNullException(nameof(refinementOperations));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _localAuthority = new DefinitionAuthorityDescriptor(definitions.SchemaVersion, definitions.Fingerprint);
        if (!DefinitionAuthorityHandshake.IsValid(_localAuthority, out var validationError))
            throw new InvalidOperationException($"Cannot register definition synchronization with invalid local authority: {validationError}");

        _rpc = NetworkManager.Instance.AddRPC(
            "DefinitionAuthority",
            ReceiveClientAcknowledgement,
            ReceiveServerAuthority);

        SynchronizationManager.Instance.AddInitialSynchronization(_rpc, BuildServerAuthorityPackage);
    }

    internal DefinitionAuthorityResult LocalAuthorityResult =>
        DefinitionAuthorityHandshake.Compare(_localAuthority, _localAuthority);

    internal DefinitionAuthorityResult ClientAuthorityResult { get; private set; } = DefinitionAuthorityResult.Pending;

    internal bool IsClientMutationAuthorized => ClientAuthorityResult.MutationAuthorized;

    internal bool IsPeerMutationAuthorized(long peerId) =>
        _peerResults.TryGetValue(peerId, out var result) && result.MutationAuthorized;

    internal DefinitionAuthorityResult GetPeerResult(long peerId) =>
        _peerResults.TryGetValue(peerId, out var result) ? result : DefinitionAuthorityResult.Pending;

    internal long GetPeerSessionGeneration(long peerId) =>
        _peerSessionGenerations.TryGetValue(peerId, out var generation) ? generation : 0L;

    private ZPackage BuildServerAuthorityPackage(ZNetPeer peer)
    {
        if (peer is null) throw new ArgumentNullException(nameof(peer));

        // Peer ids are transport/session identities, not durable authorization identities.
        // Every initial synchronization creates a fresh server-local generation so replay
        // keys from an earlier connection cannot authorize a mutation after reconnect/id reuse.
        _peerResults.Remove(peer.m_uid);

        var nextGeneration = NextSessionGeneration(peer.m_uid);
        _peerSessionGenerations[peer.m_uid] = nextGeneration;
        _geodeOpeningOperations.RetirePeerSessions(peer.m_uid, nextGeneration);
        _refinementOperations.RetirePeerSessions(peer.m_uid, nextGeneration);

        var package = new ZPackage();
        package.Write(ServerAuthorityMessage);
        WriteDescriptor(package, _localAuthority);
        return package;
    }

    private long NextSessionGeneration(long peerId)
    {
        if (!_peerSessionGenerations.TryGetValue(peerId, out var current))
            return 1L;

        if (current == long.MaxValue)
            throw new InvalidOperationException($"Peer {peerId} exhausted Magenheim session generations; refusing to recycle replay identity.");

        return current + 1L;
    }

    private IEnumerator ReceiveServerAuthority(long sender, ZPackage package)
    {
        DefinitionAuthorityDescriptor? serverAuthority = null;

        try
        {
            var messageType = package.ReadInt();
            if (messageType != ServerAuthorityMessage)
                throw new InvalidOperationException($"Unexpected definition authority message type {messageType} on client.");

            serverAuthority = ReadDescriptor(package);
            ClientAuthorityResult = DefinitionAuthorityHandshake.Compare(_localAuthority, serverAuthority);
        }
        catch (Exception exception)
        {
            ClientAuthorityResult = new DefinitionAuthorityResult(
                DefinitionAuthorityStatus.InvalidDescriptor,
                false,
                $"Failed to read server definition authority: {exception.Message}");
        }

        if (ClientAuthorityResult.MutationAuthorized)
            _logger.LogInfo(ClientAuthorityResult.Diagnostic);
        else
            _logger.LogError($"Magenheim gameplay mutation disabled for this connection. {ClientAuthorityResult.Diagnostic}");

        if (serverAuthority is not null)
        {
            var acknowledgement = new ZPackage();
            acknowledgement.Write(ClientAcknowledgementMessage);
            WriteDescriptor(acknowledgement, serverAuthority);
            WriteDescriptor(acknowledgement, _localAuthority);
            _rpc.SendPackage(sender, acknowledgement);
        }

        yield break;
    }

    private IEnumerator ReceiveClientAcknowledgement(long sender, ZPackage package)
    {
        DefinitionAuthorityResult result;
        try
        {
            var messageType = package.ReadInt();
            if (messageType != ClientAcknowledgementMessage)
                throw new InvalidOperationException($"Unexpected definition authority message type {messageType} on server.");

            var echoedServerAuthority = ReadDescriptor(package);
            var clientAuthority = ReadDescriptor(package);

            var echoResult = DefinitionAuthorityHandshake.Compare(_localAuthority, echoedServerAuthority);
            if (!echoResult.MutationAuthorized)
            {
                result = new DefinitionAuthorityResult(
                    echoResult.Status,
                    false,
                    $"Client did not acknowledge this server's definition authority. {echoResult.Diagnostic}");
            }
            else
            {
                result = DefinitionAuthorityHandshake.Compare(_localAuthority, clientAuthority);
            }
        }
        catch (Exception exception)
        {
            result = new DefinitionAuthorityResult(
                DefinitionAuthorityStatus.InvalidDescriptor,
                false,
                $"Failed to read client definition authority acknowledgement: {exception.Message}");
        }

        _peerResults[sender] = result;

        if (result.MutationAuthorized)
            _logger.LogInfo($"Peer {sender} admitted to Magenheim gameplay authority for session {GetPeerSessionGeneration(sender)}. {result.Diagnostic}");
        else
            _logger.LogError($"Peer {sender} is not admitted to Magenheim gameplay mutation. {result.Diagnostic}");

        yield break;
    }

    private static void WriteDescriptor(ZPackage package, DefinitionAuthorityDescriptor descriptor)
    {
        package.Write(descriptor.SchemaVersion);
        package.Write(descriptor.Fingerprint);
    }

    private static DefinitionAuthorityDescriptor ReadDescriptor(ZPackage package) =>
        new(package.ReadInt(), package.ReadString());
}
