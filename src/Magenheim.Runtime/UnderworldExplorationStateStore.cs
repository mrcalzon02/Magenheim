using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>Crash-safe storage for Magenheim-owned logical-map fog. Vanilla surface fog remains vanilla-owned.</summary>
internal sealed class UnderworldExplorationStateStore
{
    private readonly string _rootDirectory;
    private readonly ManualLogSource _log;

    internal UnderworldExplorationStateStore(string rootDirectory, ManualLogSource log)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory)) throw new ArgumentException("Exploration state root is required.", nameof(rootDirectory));
        _rootDirectory = Path.GetFullPath(rootDirectory);
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal void Save(string playerId, UnderworldWorldIdentity identity, UnderworldExplorationState state)
    {
        ValidateIdentity(playerId, identity);
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (state.Layer != MagenheimMapLayer.Underworld) throw new InvalidOperationException("Magenheim exploration storage may only own the Underworld logical layer.");
        var payload = UnderworldExplorationStateCodec.Encode(state);
        var path = RecordPath(playerId, identity);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = new UTF8Encoding(false).GetBytes(payload);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            var verified = UnderworldExplorationStateCodec.Decode(File.ReadAllText(temporary, Encoding.UTF8));
            if (UnderworldExplorationStateCodec.Encode(verified) != payload) throw new InvalidOperationException("Exploration state read-back was not canonical.");
            Promote(temporary, path);
            temporary = string.Empty;
        }
        finally { if (temporary.Length != 0 && File.Exists(temporary)) File.Delete(temporary); }
    }

    internal bool TryRestore(string playerId, UnderworldWorldIdentity identity, UnderworldExplorationState target, out string diagnostic)
    {
        ValidateIdentity(playerId, identity);
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (target.Layer != MagenheimMapLayer.Underworld) throw new InvalidOperationException("Magenheim exploration storage may only restore the Underworld logical layer.");

        var path = RecordPath(playerId, identity);
        if (TryRead(path, target, out diagnostic)) return true;
        var primary = diagnostic;
        if (TryRead(path + ".bak", target, out diagnostic))
        {
            diagnostic = "Recovered Underworld exploration state from backup after primary failure: " + primary;
            _log.LogWarning(diagnostic);
            return true;
        }

        // Pre-native-instance builds omitted DerivedWorldId from the exploration namespace. Admit
        // that location only as a migration source; every subsequent Save writes the canonical
        // full-instance namespace so two derived worlds can never share exploration ownership.
        var legacyPath = LegacyRecordPath(playerId, identity);
        if (!string.Equals(path, legacyPath, StringComparison.Ordinal))
        {
            if (TryRead(legacyPath, target, out diagnostic) || TryRead(legacyPath + ".bak", target, out diagnostic))
            {
                _log.LogWarning("Migrating Underworld exploration state from the legacy partial-identity namespace.");
                Save(playerId, identity, target);
                diagnostic = "Migrated legacy Underworld exploration state into the canonical instance namespace.";
                return true;
            }
        }

        diagnostic = "No admissible persisted Underworld exploration state. Primary: " + primary + " Backup: " + diagnostic;
        return false;
    }

    private static bool TryRead(string path, UnderworldExplorationState target, out string diagnostic)
    {
        if (!File.Exists(path)) { diagnostic = "candidate missing"; return false; }
        try { UnderworldExplorationStateCodec.RestoreInto(target, File.ReadAllText(path, Encoding.UTF8)); diagnostic = string.Empty; return true; }
        catch (Exception exception) { diagnostic = exception.Message; return false; }
    }

    private string RecordPath(string playerId, UnderworldWorldIdentity identity) =>
        Path.Combine(_rootDirectory, Hash(identity.ParentWorldId + "\n" + identity.DerivedWorldId + "\n" + identity.DerivedSeedFingerprint), Hash(playerId.Trim()) + ".uwmap");

    private string LegacyRecordPath(string playerId, UnderworldWorldIdentity identity) =>
        Path.Combine(_rootDirectory, Hash(identity.ParentWorldId + "\n" + identity.DerivedSeedFingerprint), Hash(playerId.Trim()) + ".uwmap");

    private static void ValidateIdentity(string playerId, UnderworldWorldIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("Player identity is required.", nameof(playerId));
        if (identity is null) throw new ArgumentNullException(nameof(identity));
    }

    private static void Promote(string temporary, string destination)
    {
        if (!File.Exists(destination)) { File.Move(temporary, destination); return; }
        var backup = destination + ".bak";
        if (File.Exists(backup)) File.Delete(backup);
        try { File.Replace(temporary, destination, backup); }
        catch (PlatformNotSupportedException)
        {
            File.Move(destination, backup);
            try { File.Move(temporary, destination); }
            catch { if (!File.Exists(destination) && File.Exists(backup)) File.Move(backup, destination); throw; }
        }
    }

    private static string Hash(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var item in bytes) builder.Append(item.ToString("x2"));
        return builder.ToString();
    }
}
