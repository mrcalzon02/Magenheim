using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Magenheim-owned durable registry for generated objects in one derived Underworld instance.
/// This store is deliberately independent of Valheim ZoneSystem/ZDO persistence: the parent world
/// must not become persistence authority for native Underworld structures. Repeatable structure kinds
/// are keyed by deterministic instance-local placement identity, not merely by structure kind.
/// </summary>
internal sealed class UnderworldGeneratedObjectStateStore
{
    private const string Magic = "MGEN-INSTANCE-GENERATED-OBJECT";
    private const string Version = "1";
    private readonly string _rootDirectory;
    private readonly ManualLogSource _log;

    internal UnderworldGeneratedObjectStateStore(string rootDirectory, ManualLogSource log)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory)) throw new ArgumentException("Generated-object state root is required.", nameof(rootDirectory));
        _rootDirectory = Path.GetFullPath(rootDirectory);
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal bool IsRecorded(UnderworldWorldIdentity identity, string objectKind) => IsRecorded(identity, objectKind, string.Empty);

    internal bool IsRecorded(UnderworldWorldIdentity identity, string objectKind, string placementKey)
    {
        Validate(identity, objectKind);
        return TryRead(identity, objectKind, placementKey, out _);
    }

    internal void RecordGenerated(UnderworldWorldIdentity identity, string objectKind) => RecordGenerated(identity, objectKind, string.Empty);

    internal void RecordGenerated(UnderworldWorldIdentity identity, string objectKind, string placementKey)
    {
        Validate(identity, objectKind);
        var stableId = UnderworldGeneratedObjectIdentity.BuildStableObjectId(identity, objectKind, placementKey);
        var path = RecordPath(identity, stableId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var payload = Encode(identity, objectKind, stableId);
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = new UTF8Encoding(false).GetBytes(payload);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (!TryDecode(File.ReadAllText(temporary, Encoding.UTF8), identity, objectKind, stableId))
                throw new InvalidOperationException("Generated-object state failed authenticated read-back.");
            Promote(temporary, path);
            temporary = string.Empty;
        }
        finally { if (temporary.Length != 0 && File.Exists(temporary)) File.Delete(temporary); }
    }

    private bool TryRead(UnderworldWorldIdentity identity, string objectKind, string placementKey, out string stableId)
    {
        stableId = UnderworldGeneratedObjectIdentity.BuildStableObjectId(identity, objectKind, placementKey);
        var path = RecordPath(identity, stableId);
        foreach (var candidate in new[] { path, path + ".bak" })
        {
            if (!File.Exists(candidate)) continue;
            try
            {
                if (TryDecode(File.ReadAllText(candidate, Encoding.UTF8), identity, objectKind, stableId)) return true;
                _log.LogWarning("Rejected generated Underworld object record with mismatched instance ownership: " + candidate);
            }
            catch (Exception ex) { _log.LogWarning("Failed reading generated Underworld object record " + candidate + ": " + ex.Message); }
        }
        return false;
    }

    private string RecordPath(UnderworldWorldIdentity identity, string stableId) =>
        Path.Combine(_rootDirectory, IdentityFingerprint(identity), Hash(stableId) + ".uwobject");

    private static string Encode(UnderworldWorldIdentity identity, string objectKind, string stableId) =>
        Magic + "\n" + Version + "\n" + IdentityFingerprint(identity) + "\n" + Convert.ToBase64String(Encoding.UTF8.GetBytes(objectKind.Trim())) + "\n" + Convert.ToBase64String(Encoding.UTF8.GetBytes(stableId)) + "\n";

    private static bool TryDecode(string payload, UnderworldWorldIdentity identity, string objectKind, string stableId)
    {
        var lines = payload.Replace("\r", string.Empty).Split('\n');
        if (lines.Length < 5 || lines[0] != Magic || lines[1] != Version) return false;
        if (!string.Equals(lines[2], IdentityFingerprint(identity), StringComparison.Ordinal)) return false;
        string decodedKind;
        string decodedId;
        try
        {
            decodedKind = Encoding.UTF8.GetString(Convert.FromBase64String(lines[3]));
            decodedId = Encoding.UTF8.GetString(Convert.FromBase64String(lines[4]));
        }
        catch (FormatException) { return false; }
        return string.Equals(decodedKind, objectKind.Trim(), StringComparison.Ordinal)
            && string.Equals(decodedId, stableId, StringComparison.Ordinal);
    }

    private static string IdentityFingerprint(UnderworldWorldIdentity identity) =>
        Hash(identity.ParentWorldId + "\n" + identity.DerivedWorldId + "\n" + identity.DerivedSeedFingerprint);

    private static string Hash(string value)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) builder.Append(b.ToString("x2"));
        return builder.ToString();
    }

    private static void Validate(UnderworldWorldIdentity identity, string objectKind)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        if (string.IsNullOrWhiteSpace(identity.ParentWorldId) || string.IsNullOrWhiteSpace(identity.DerivedWorldId) || string.IsNullOrWhiteSpace(identity.DerivedSeedFingerprint))
            throw new ArgumentException("Complete Underworld instance identity is required.", nameof(identity));
        if (string.IsNullOrWhiteSpace(objectKind)) throw new ArgumentException("Generated Underworld object kind is required.", nameof(objectKind));
    }

    private static void Promote(string temporary, string path)
    {
        var backup = path + ".bak";
        if (File.Exists(path))
        {
            if (File.Exists(backup)) File.Delete(backup);
            File.Replace(temporary, path, backup);
        }
        else File.Move(temporary, path);
    }
}
