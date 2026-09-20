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

        // The dedicated Underworld's native terrain dimensions are code-authoritative until they
        // move into static definitions. They are gameplay-significant, so peers must agree before
        // any persistent Magenheim mutation is admitted. Surface host placement is not authority.
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

        RetireDisconnectedPeerState(peer.m_uid);
        _peerResults.Remove(peer.m_uid);

        var nextGeneration = NextSessionGeneration();
        _peerSessionGenerations[peer.m_uid] = nextGeneration;
        return DefinitionAuthorityPackageCodec.Encode(
            ServerAuthorityMessage,
            nextGeneration,
            _localAuthority);
    }

    private IEnumerator ReceiveServerAuthority(long sender, ZPackage package)
    {
        if (!DefinitionAuthorityPackageCodec.TryDecode(package, out var messageType, out var generation, out var remoteAuthority) ||
            messageType != ServerAuthorityMessage)
        {
            ClientAuthorityResult = DefinitionAuthorityResult.Rejected("Malformed server definition-authority package.");
            yield break;
        }

        ClientSessionGeneration = generation;
        ClientAuthorityResult = DefinitionAuthorityHandshake.Compare(_localAuthority, remoteAuthority);
        var response = DefinitionAuthorityPackageCodec.Encode(
            ClientAcknowledgementMessage,
            generation,
            _localAuthority);
        _rpc.SendPackage(sender, response);
        yield break;
    }

    private IEnumerator ReceiveClientAcknowledgement(long sender, ZPackage package)
    {
        if (!DefinitionAuthorityPackageCodec.TryDecode(package, out var messageType, out var generation, out var remoteAuthority) ||
            messageType != ClientAcknowledgementMessage)
        {
            _peerResults[sender] = DefinitionAuthorityResult.Rejected("Malformed client definition-authority package.");
            yield break;
        }

        if (!_peerSessionGenerations.TryGetValue(sender, out var expectedGeneration) || generation != expectedGeneration)
        {
            _peerResults[sender] = DefinitionAuthorityResult.Rejected("Stale definition-authority acknowledgement.");
            yield break;
        }

        _peerResults[sender] = DefinitionAuthorityHandshake.Compare(_localAuthority, remoteAuthority);
        yield break;
    }

    private long NextSessionGeneration()
    {
        _lastSessionGeneration++;
        if (_lastSessionGeneration <= 0L)
            _lastSessionGeneration = 1L;
        return _lastSessionGeneration;
    }

    private void RetireDisconnectedPeerState(long currentPeerId)
    {
        var livePeerIds = ZNet.instance?.GetPeers()?.Select(peer => peer.m_uid).ToHashSet()
            ?? new HashSet<long>();
        livePeerIds.Add(currentPeerId);

        foreach (var peerId in _peerResults.Keys.Where(peerId => !livePeerIds.Contains(peerId)).ToArray())
            _peerResults.Remove(peerId);
        foreach (var peerId in _peerSessionGenerations.Keys.Where(peerId => !livePeerIds.Contains(peerId)).ToArray())
            _peerSessionGenerations.Remove(peerId);
    }
}
