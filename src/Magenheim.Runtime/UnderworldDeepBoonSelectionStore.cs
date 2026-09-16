using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>Durable server-owned storage for per-player Deep Boon selection, isolated by paired-world identity.</summary>
internal sealed class UnderworldDeepBoonSelectionStore
{
    private readonly string _rootDirectory;
    private readonly ManualLogSource _log;

    internal UnderworldDeepBoonSelectionStore(string rootDirectory, ManualLogSource log)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory)) throw new ArgumentException("Deep Boon selection root is required.", nameof(rootDirectory));
        _rootDirectory = Path.GetFullPath(rootDirectory);
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal void Save(string playerId, UnderworldWorldIdentity identity, UnderworldDeepBoonSelectionState state)
    {
        if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("Player identity is required.", nameof(playerId));
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        var payload = UnderworldDeepBoonSelectionCodec.Encode(state ?? throw new ArgumentNullException(nameof(state)));
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
            var verified = UnderworldDeepBoonSelectionCodec.Decode(File.ReadAllText(temporary, Encoding.UTF8));
            if (UnderworldDeepBoonSelectionCodec.Encode(verified) != payload) throw new InvalidOperationException("Deep Boon selection read-back was not canonical.");
            Promote(temporary, path);
            temporary = string.Empty;
        }
        finally { if (temporary.Length != 0 && File.Exists(temporary)) File.Delete(temporary); }
    }

    internal bool TryLoad(string playerId, UnderworldWorldIdentity identity, out UnderworldDeepBoonSelectionState state, out string diagnostic)
    {
        if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("Player identity is required.", nameof(playerId));
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        var path = RecordPath(playerId, identity);
        if (TryRead(path, out state, out diagnostic)) return true;
        var primary = diagnostic;
        if (TryRead(path + ".bak", out state, out diagnostic)) { diagnostic = "Recovered Deep Boon selection from backup after primary failure: " + primary; _log.LogWarning(diagnostic); return true; }
        state = UnderworldDeepBoonSelection.CreateInitialState();
        diagnostic = "No admissible persisted Deep Boon selection. Primary: " + primary + " Backup: " + diagnostic;
        return false;
    }

    private static bool TryRead(string path, out UnderworldDeepBoonSelectionState state, out string diagnostic)
    {
        if (!File.Exists(path)) { state = UnderworldDeepBoonSelection.CreateInitialState(); diagnostic = "candidate missing"; return false; }
        try { state = UnderworldDeepBoonSelectionCodec.Decode(File.ReadAllText(path, Encoding.UTF8)); diagnostic = string.Empty; return true; }
        catch (Exception exception) { state = UnderworldDeepBoonSelection.CreateInitialState(); diagnostic = exception.Message; return false; }
    }

    private string RecordPath(string playerId, UnderworldWorldIdentity identity) => Path.Combine(_rootDirectory, Hash(identity.ParentWorldId + "\n" + identity.DerivedWorldId + "\n" + identity.DerivedSeedFingerprint), Hash(playerId.Trim()) + ".uwboon");

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
