using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.Definitions;
using Magenheim.Core.Networking;

namespace Magenheim.Runtime.Networking;

internal sealed class DefinitionAuthoritySynchronizer
{
    private const int ServerAuthorityMessage = 1;
    private const int ClientAcknowledgementMessage = 2;

    private readonly DefinitionAuthorityDescriptor _localAuthority;
    private readonly ManualLogSource _logger;
    private readonly Dictionary<long, DefinitionAuthorityResult> _peerResults = new();
    private readonly CustomRPC _rpc;

    internal DefinitionAuthoritySynchronizer(MagenheimDefinitionSet definitions, ManualLogSource logger)
    {
        if (definitions is null) throw new ArgumentNullException(nameof(definitions));
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

    internal DefinitionAuthorityResult ClientAuthorityResult { get; private set; } = DefinitionAuthorityResult.Pending;

    internal bool IsClientMutationAuthorized => ClientAuthorityResult.MutationAuthorized;

    internal bool IsPeerMutationAuthorized(long peerId) =>
        _peerResults.TryGetValue(peerId, out var result) && result.MutationAuthorized;

    internal DefinitionAuthorityResult GetPeerResult(long peerId) =>
        _peerResults.TryGetValue(peerId, out var result) ? result : DefinitionAuthorityResult.Pending;

    private ZPackage BuildServerAuthorityPackage()
    {
        var package = new ZPackage();
        package.Write(ServerAuthorityMessage);
        WriteDescriptor(package, _localAuthority);
        return package;
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

        // Fail closed: only acknowledge after the server descriptor was successfully parsed.
        // The acknowledgement echoes that descriptor so the server can prove the client
        // actually received its authority, rather than authorizing solely from the client's
        // self-reported descriptor.
        if (serverAuthority is not null)
        {
            var acknowledgement = new ZPackage();
            acknowledgement.Write(ClientAcknowledgementMessage);
            WriteDescriptor(acknowledgement, serverAuthority);
            WriteDescriptor(acknowledgement, _localAuthority);
            _rpc.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), acknowledgement);
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
            _logger.LogInfo($"Peer {sender} admitted to Magenheim gameplay authority. {result.Diagnostic}");
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
