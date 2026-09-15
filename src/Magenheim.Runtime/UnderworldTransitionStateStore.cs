using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Server-owned durable storage for the canonical Core Underworld transition payload.
/// Records are isolated by paired-world identity and player identity, written through a
/// same-directory temporary file, and atomically promoted only after read-back validation.
/// </summary>
internal sealed class UnderworldTransitionStateStore
{
    private readonly string _rootDirectory;
    private readonly ManualLogSource _log;

    internal UnderworldTransitionStateStore(string rootDirectory, ManualLogSource log)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
            throw new ArgumentException("Underworld transition-state root directory is required.", nameof(rootDirectory));
        _rootDirectory = Path.GetFullPath(rootDirectory);
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal void Save(UnderworldPlayerLayerState state, UnderworldWorldIdentity identity)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (identity is null) throw new ArgumentNullException(nameof(identity));

        UnderworldTransitionRules.ValidatePersistedState(state, identity);
        var payload = UnderworldTransitionStateCodec.Encode(state, identity);
        var path = RecordPath(state.PlayerId, identity);
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Unable to resolve Underworld transition-state directory.");
        Directory.CreateDirectory(directory);

        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            WriteDurable(temporary, payload);

            // Do not promote bytes that the canonical decoder cannot read back against this world pair.
            var verified = UnderworldTransitionStateCodec.Decode(File.ReadAllText(temporary, Encoding.UTF8), identity);
            if (!string.Equals(verified.PlayerId, state.PlayerId, StringComparison.Ordinal))
                throw new InvalidOperationException("Underworld transition-state read-back changed player identity.");

            Promote(temporary, path);
            temporary = string.Empty;
        }
        finally
        {
            if (temporary.Length != 0 && File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    internal bool TryLoad(
        string playerId,
        UnderworldWorldIdentity identity,
        out UnderworldPlayerLayerState? state,
        out string diagnostic)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (string.IsNullOrWhiteSpace(playerId))
            throw new ArgumentException("Underworld transition-state player identity is required.", nameof(playerId));

        var path = RecordPath(playerId, identity);
        if (!File.Exists(path))
        {
            state = null;
            diagnostic = "No persisted Underworld transition state exists for this player/world pair.";
            return false;
        }

        try
        {
            var decoded = UnderworldTransitionStateCodec.Decode(File.ReadAllText(path, Encoding.UTF8), identity);
            if (!string.Equals(decoded.PlayerId, playerId, StringComparison.Ordinal))
                throw new InvalidOperationException("Persisted Underworld transition state belongs to a different player.");
            state = decoded;
            diagnostic = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            state = null;
            diagnostic = "Persisted Underworld transition state is invalid and was not admitted: " + exception.Message;
            _log.LogError(diagnostic);
            return false;
        }
    }

    private string RecordPath(string playerId, UnderworldWorldIdentity identity)
    {
        var pairKey = Hash(identity.ParentWorldId + "\n" + identity.DerivedWorldId + "\n" + identity.DerivedSeed);
        var playerKey = Hash(playerId.Trim());
        return Path.Combine(_rootDirectory, pairKey, playerKey + ".uwstate");
    }

    private static void WriteDurable(string path, string payload)
    {
        var bytes = new UTF8Encoding(false).GetBytes(payload);
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush(true);
    }

    private static void Promote(string temporary, string destination)
    {
        if (!File.Exists(destination))
        {
            File.Move(temporary, destination);
            return;
        }

        var backup = destination + ".bak";
        try
        {
            if (File.Exists(backup)) File.Delete(backup);
            File.Replace(temporary, destination, backup);
            if (File.Exists(backup)) File.Delete(backup);
        }
        catch (PlatformNotSupportedException)
        {
            ReplaceByRename(temporary, destination, backup);
        }
    }

    private static void ReplaceByRename(string temporary, string destination, string backup)
    {
        if (File.Exists(backup)) File.Delete(backup);
        File.Move(destination, backup);
        try
        {
            File.Move(temporary, destination);
            File.Delete(backup);
        }
        catch
        {
            if (!File.Exists(destination) && File.Exists(backup))
                File.Move(backup, destination);
            throw;
        }
    }

    private static string Hash(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var item in bytes)
            builder.Append(item.ToString("x2"));
        return builder.ToString();
    }
}
