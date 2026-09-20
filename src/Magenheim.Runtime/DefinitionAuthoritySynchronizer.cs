using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Definitions;
using Magenheim.Core.Networking;
using Magenheim.Core.Socketing;
using Magenheim.Core.Transactions;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime.Networking;

internal sealed class DefinitionAuthoritySynchronizer
{
    private const int ServerAuthorityMessage = 1;
    private const int ClientAcknowledgementMessage = 2;

    private readonly DefinitionAuthorityDescriptor _localAuthority;
    private readonly ManualLogSource _logger;
    private readonly GeodeOpeningOperationGuard _geodeOpeningOperations;
    private readonly RefinementOperationGuard _refinementOperations;
    private readonly SocketOperationGuard _socketOperations;
    private readonly SocketExtractionOperationGuard _socketExtractionOperations;
    private readonly Dictionary<long, DefinitionAuthorityResult> _peerResults = new();
    private readonly Dictionary<long, long> _peerSessionGenerations = new();
    private readonly CustomRPC _rpc;
    private long _lastSessionGeneration;

    internal DefinitionAuthoritySynchronizer(
        MagenheimDefinitionSet definitions,
        SocketEligibilityPolicy socketPolicy,
        GeodeOpeningOperationGuard geodeOpeningOperations,
        RefinementOperationGuard refinementOperations,
        SocketOperationGuard socketOperations,
        SocketExtractionOperationGuard socketExtractionOperations,
        ManualLogSource logger)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
        if (socketPolicy is null) throw new ArgumentNullException(nameof(socketPolicy));
        _geodeOpeningOperations = geodeOpeningOperations ?? throw new ArgumentNullException(nameof(geodeOpeningOperations));
        _refinementOperations = refinementOperations ?? throw new ArgumentNullException(nameof(refinementOperations));
        _socketOperations = socketOperations ?? throw new ArgumentNullException(nameof(socketOperations));
        _socketExtractionOperations = socketExtractionOperations ?? throw new ArgumentNullException(nameof(socketExtractionOperations));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Native instance dimensions are code-authoritative until their policy moves into a later
        // static definition schema. They are gameplay-significant, so peers must agree before any
        // persistent Magenheim mutation is admitted. Surface host placement is not authority.
        var underworldTerrainFingerprint = definitions.Underworld is null
            ? null
            : UnderworldInstanceTerrainDomain.CreateDefault().Fingerprint;
        var gameplayFingerprint = GameplayAuthorityFingerprint.Compute(
            definitions.Fingerprint,
            socketPolicy,
            underworldTerrainFingerprint);
        _localAuthority = new DefinitionAuthorityDescriptor(definitions.SchemaVersion, gameplayFingerprint);
        if (!DefinitionAuthorityHandshake.IsValid(_localAuthority, out var validationError))
            throw new InvalidOperationException($"Cannot register gameplay synchronization with invalid local authority: {validationError}");

        _rpc = NetworkManager.Instance.AddRPC(
            "DefinitionAuthority",
            ReceiveClientAcknowledgement,
            ReceiveServerAuthority);

        SynchronizationManager.Instance.AddInitialSynchronization(_rpc, BuildServerAuthorityPackage);
    }

    internal string GameplayFingerprint => _localAuthority.Fingerprint;

    internal DefinitionAuthorityResult LocalAuthorityResult =>
        DefinitionAuthorityHandshake.Compare(_localAuthority, _localAuthority);

    internal DefinitionAuthorityResult ClientAuthorityResult { get; private set; } = DefinitionAuthorityResult.Pending;

    internal long ClientSessionGeneration { get; private set; }

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

        // Keep only live transport identities. Session generations are globally monotonic, so
        // pruning a disconnected peer cannot recycle a replay identity if that uid later returns.
        RetireDisconnectedPeerState(peer.m_uid);
        _peerResults.Remove(peer.m_uid);

        var nextGeneration = NextSessionGeneration();
        _peerSessionGenerations[peer.m_uid] = nextGeneration;
        _geodeOpeningOperations.RetirePeerSessions(peer.m_uid, nextGeneration);
        _refinementOperations.RetirePeerSessions(peer.m_uid, nextGeneration);
        _socketOperations.RetirePeerSessions(peer.m_uid, nextGeneration);
        _socketExtractionOperations.RetirePeerSessions(peer.m_uid, nextGeneration);

        var package = new ZPackage();
        package.Write(ServerAuthorityMessage);
        package.Write(nextGeneration);
        WriteDescriptor(package, _localAuthority);
        return package;
    }

    private long NextSessionGeneration()
    {
        if (_lastSessionGeneration == long.MaxValue)
            throw new InvalidOperationException("Magenheim exhausted server session generations; refusing to recycle replay identity.");
        return ++_lastSessionGeneration;
    }

    private void RetireDisconnectedPeerState(long synchronizingPeerId)
    {
        if (ZNet.instance is null || _peerSessionGenerations.Count == 0) return;
        var stale = _peerSessionGenerations.Keys
            .Where(peerId => peerId != synchronizingPeerId && ZNet.instance.GetPeer(peerId) is null)
            .ToArray();
        foreach (var peerId in stale)
        {
            _peerResults.Remove(peerId);
            _peerSessionGenerations.Remove(peerId);
        }
        if (stale.Length > 0)
            _logger.LogDebug($"Retired {stale.Length} disconnected Magenheim authority peer record(s); replay generations remain globally non-recyclable.");
    }

    private IEnumerator ReceiveServerAuthority(long sender, ZPackage package)
    {
        DefinitionAuthorityDescriptor? serverAuthority = null;
        var serverSessionGeneration = 0L;

        try
        {
            var messageType = package.ReadInt();
            if (messageType != ServerAuthorityMessage)
                throw new InvalidOperationException($"Unexpected gameplay authority message type {messageType} on client.");

            serverSessionGeneration = package.ReadLong();
            if (serverSessionGeneration <= 0L)
                throw new InvalidOperationException($"Server supplied invalid gameplay session generation {serverSessionGeneration}.");

            serverAuthority = ReadDescriptor(package);
            ClientAuthorityResult = DefinitionAuthorityHandshake.Compare(_localAuthority, serverAuthority);
            ClientSessionGeneration = ClientAuthorityResult.MutationAuthorized ? serverSessionGeneration : 0L;
        }
        catch (Exception exception)
        {
            ClientSessionGeneration = 0L;
            ClientAuthorityResult = new DefinitionAuthorityResult(
                DefinitionAuthorityStatus.InvalidDescriptor,
                false,
                $"Failed to read server gameplay authority: {exception.Message}");
        }

        if (ClientAuthorityResult.MutationAuthorized)
            _logger.LogInfo($"{ClientAuthorityResult.Diagnostic} Session generation {ClientSessionGeneration}.");
        else
            _logger.LogError($"Magenheim gameplay mutation disabled for this connection. {ClientAuthorityResult.Diagnostic}");

        if (serverAuthority is not null && serverSessionGeneration > 0L)
        {
            var acknowledgement = new ZPackage();
            acknowledgement.Write(ClientAcknowledgementMessage);
            acknowledgement.Write(serverSessionGeneration);
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
                throw new InvalidOperationException($"Unexpected gameplay authority message type {messageType} on server.");

            var acknowledgedGeneration = package.ReadLong();
            if (!_peerSessionGenerations.TryGetValue(sender, out var currentGeneration))
            {
                result = new DefinitionAuthorityResult(
                    DefinitionAuthorityStatus.InvalidDescriptor,
                    false,
                    $"Peer {sender} acknowledged gameplay authority without an active server session generation.");
            }
            else if (acknowledgedGeneration != currentGeneration)
            {
                result = new DefinitionAuthorityResult(
                    DefinitionAuthorityStatus.InvalidDescriptor,
                    false,
                    $"Peer {sender} acknowledged stale gameplay session {acknowledgedGeneration}; current session is {currentGeneration}.");
            }
            else
            {
                var echoedServerAuthority = ReadDescriptor(package);
                var clientAuthority = ReadDescriptor(package);

                var echoResult = DefinitionAuthorityHandshake.Compare(_localAuthority, echoedServerAuthority);
                if (!echoResult.MutationAuthorized)
                {
                    result = new DefinitionAuthorityResult(
                        echoResult.Status,
                        false,
                        $"Client did not acknowledge this server's gameplay authority. {echoResult.Diagnostic}");
                }
                else
                {
                    result = DefinitionAuthorityHandshake.Compare(_localAuthority, clientAuthority);
                }
            }
        }
        catch (Exception exception)
        {
            result = new DefinitionAuthorityResult(
                DefinitionAuthorityStatus.InvalidDescriptor,
                false,
                $"Failed to read client gameplay authority acknowledgement: {exception.Message}");
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
