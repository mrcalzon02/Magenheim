using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Durable authority that lets either physical world instance recover the same parent/derived pair.
/// Unlike player transition state, this record is world-scoped and therefore survives a process
/// restart before any player has been admitted to the derived world.
/// </summary>
internal sealed class UnderworldWorldPairManifestStore
{
    private const string Version = "magenheim-underworld-pair-v1";
    private readonly string _root;
    private readonly ManualLogSource _log;

    internal UnderworldWorldPairManifestStore(string root, ManualLogSource log)
    {
        _root = Path.GetFullPath(root ?? throw new ArgumentNullException(nameof(root)));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    internal string GetDerivedSaveName(UnderworldWorldIdentity identity)
    {
        Validate(identity);
        return "Magenheim_Underworld_" + identity.DerivedSeedFingerprint.Substring(0, 20);
    }

    internal void EnsureManifest(UnderworldWorldIdentity identity)
    {
        Validate(identity);
        Directory.CreateDirectory(_root);
        var saveName = GetDerivedSaveName(identity);
        var path = Path.Combine(_root, saveName + ".worldpair");
        var payload = Encode(identity, saveName);
        if (File.Exists(path))
        {
            var existing = Decode(File.ReadAllText(path, Encoding.UTF8));
            if (!SamePair(existing, identity))
                throw new InvalidOperationException("Derived Underworld save identity collides with a different parent world pair.");
            return;
        }

        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = new UTF8Encoding(false).GetBytes(payload);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            var verified = Decode(File.ReadAllText(temporary, Encoding.UTF8));
            if (!SamePair(verified, identity)) throw new InvalidOperationException("Underworld world-pair manifest failed read-back identity verification.");
            File.Move(temporary, path);
            temporary = string.Empty;
            _log.LogInfo($"Persisted Underworld world-pair manifest '{saveName}'.");
        }
        finally
        {
            if (temporary.Length != 0 && File.Exists(temporary)) File.Delete(temporary);
        }
    }

    internal bool TryResolveByDerivedSaveName(string saveName, out UnderworldWorldIdentity? identity)
    {
        identity = null;
        if (string.IsNullOrWhiteSpace(saveName)) return false;
        var path = Path.Combine(_root, Path.GetFileName(saveName.Trim()) + ".worldpair");
        if (!File.Exists(path)) return false;
        try
        {
            identity = Decode(File.ReadAllText(path, Encoding.UTF8));
            return string.Equals(GetDerivedSaveName(identity), saveName.Trim(), StringComparison.Ordinal);
        }
        catch (Exception exception)
        {
            _log.LogError($"Rejected Underworld world-pair manifest '{saveName}': {exception.Message}");
            identity = null;
            return false;
        }
    }

    private static string Encode(UnderworldWorldIdentity identity, string saveName) => string.Join("\n", new[]
    {
        Version,
        "save=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(saveName)),
        "parent=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(identity.ParentWorldId)),
        "seed=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(identity.ParentSeed)),
        "derived=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(identity.DerivedWorldId)),
        "fingerprint=" + identity.DerivedSeedFingerprint,
        "seed32=" + identity.DerivedSeed32,
        string.Empty
    });

    private static UnderworldWorldIdentity Decode(string payload)
    {
        var lines = payload.Replace("\r", string.Empty).Split('\n');
        if (lines.Length < 7 || lines[0] != Version) throw new InvalidDataException("Unsupported Underworld world-pair manifest version.");
        string Read(string prefix, int index)
        {
            if (!lines[index].StartsWith(prefix, StringComparison.Ordinal)) throw new InvalidDataException("Malformed Underworld world-pair manifest.");
            return lines[index].Substring(prefix.Length);
        }
        var parent = Encoding.UTF8.GetString(Convert.FromBase64String(Read("parent=", 2)));
        var seed = Encoding.UTF8.GetString(Convert.FromBase64String(Read("seed=", 3)));
        var expected = UnderworldWorldIdentityFactory.Derive(parent, seed);
        var derived = Encoding.UTF8.GetString(Convert.FromBase64String(Read("derived=", 4)));
        var fingerprint = Read("fingerprint=", 5);
        if (!int.TryParse(Read("seed32=", 6), out var seed32)) throw new InvalidDataException("Invalid Underworld derived seed.");
        var decoded = new UnderworldWorldIdentity(parent, seed, derived, fingerprint, seed32);
        if (!SamePair(decoded, expected) || decoded.DerivedSeed32 != expected.DerivedSeed32) throw new InvalidDataException("Underworld world-pair manifest failed deterministic identity validation.");
        return decoded;
    }

    private static void Validate(UnderworldWorldIdentity identity)
    {
        if (identity is null) throw new ArgumentNullException(nameof(identity));
        var expected = UnderworldWorldIdentityFactory.Derive(identity.ParentWorldId, identity.ParentSeed);
        if (!SamePair(identity, expected) || identity.DerivedSeed32 != expected.DerivedSeed32)
            throw new InvalidOperationException("Underworld world-pair identity is not the deterministic derivative of its parent.");
    }

    private static bool SamePair(UnderworldWorldIdentity left, UnderworldWorldIdentity right) =>
        string.Equals(left.ParentWorldId, right.ParentWorldId, StringComparison.Ordinal) &&
        string.Equals(left.DerivedWorldId, right.DerivedWorldId, StringComparison.Ordinal) &&
        string.Equals(left.DerivedSeedFingerprint, right.DerivedSeedFingerprint, StringComparison.Ordinal);
}
