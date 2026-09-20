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
    private const string EnvelopeMagic = "MGEN-INSTANCE-EXPLORATION";
    private const string EnvelopeVersion = "1";
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
        var payload = Wrap(identity, UnderworldExplorationStateCodec.Encode(state));
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
            var verified = ReadAuthenticated(File.ReadAllText(temporary, Encoding.UTF8), identity);
            if (Wrap(identity, UnderworldExplorationStateCodec.Encode(verified)) != payload) throw new InvalidOperationException("Exploration state read-back was not canonical.");
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
        if (TryRead(path, identity, target, false, out diagnostic)) return true;
        var primary = diagnostic;
        if (TryRead(path + ".bak", identity, target, false, out diagnostic))
        {
            diagnostic = "Recovered Underworld exploration state from backup after primary failure: " + primary;
            _log.LogWarning(diagnostic);
            return true;
        }

        // Version-1 exploration records predate payload-level instance authentication. They are
        // admitted only from the exact canonical/legacy path derived for this identity, then are
        // immediately rewritten as authenticated envelopes. A copied authenticated record from a
        // different derived world therefore fails closed even when placed under this directory.
        if (TryRead(path, identity, target, true, out diagnostic) || TryRead(path + ".bak", identity, target, true, out diagnostic))
        {
            _log.LogWarning("Migrating unauthenticated Underworld exploration payload into the instance-authenticated envelope.");
            Save(playerId, identity, target);
            diagnostic = "Migrated legacy exploration payload into the instance-authenticated envelope.";
            return true;
        }

        var legacyPath = LegacyRecordPath(playerId, identity);
        if (!string.Equals(path, legacyPath, StringComparison.Ordinal)
            && (TryRead(legacyPath, identity, target, true, out diagnostic) || TryRead(legacyPath + ".bak", identity, target, true, out diagnostic)))
        {
            _log.LogWarning("Migrating Underworld exploration state from the legacy partial-identity namespace.");
            Save(playerId, identity, target);
            diagnostic = "Migrated legacy Underworld exploration state into the canonical authenticated instance namespace.";
            return true;
        }

        diagnostic = "No admissible persisted Underworld exploration state. Primary: " + primary + " Last candidate: " + diagnostic;
        return false;
    }

    private static bool TryRead(string path, UnderworldWorldIdentity identity, UnderworldExplorationState target, bool allowLegacy, out string diagnostic)
    {
        if (!File.Exists(path)) { diagnostic = "candidate missing"; return false; }
        try
        {
            var payload = File.ReadAllText(path, Encoding.UTF8);
            UnderworldExplorationState decoded;
            if (payload.StartsWith(EnvelopeMagic + "|", StringComparison.Ordinal)) decoded = ReadAuthenticated(payload, identity);
            else if (allowLegacy) decoded = UnderworldExplorationStateCodec.Decode(payload);
            else throw new InvalidOperationException("Exploration payload lacks instance authentication.");
            if (decoded.Layer != target.Layer || decoded.Width != target.Width || decoded.Height != target.Height) throw new InvalidOperationException("Exploration payload authority does not match the target logical map layer.");
            target.Restore(decoded.Pack());
            diagnostic = string.Empty;
            return true;
        }
        catch (Exception exception) { diagnostic = exception.Message; return false; }
    }

    private static UnderworldExplorationState ReadAuthenticated(string payload, UnderworldWorldIdentity identity)
    {
        var fields = payload.Split('|');
        if (fields.Length != 4 || fields[0] != EnvelopeMagic || fields[1] != EnvelopeVersion) throw new InvalidOperationException("Exploration instance envelope is invalid.");
        if (!FixedEquals(fields[2], IdentityFingerprint(identity))) throw new InvalidOperationException("Exploration payload belongs to a different Underworld instance.");
        string inner;
        try { inner = Encoding.UTF8.GetString(Convert.FromBase64String(fields[3])); }
        catch (FormatException exception) { throw new InvalidOperationException("Exploration instance envelope payload is invalid.", exception); }
        return UnderworldExplorationStateCodec.Decode(inner);
    }

    private static string Wrap(UnderworldWorldIdentity identity, string payload) =>
        EnvelopeMagic + "|" + EnvelopeVersion + "|" + IdentityFingerprint(identity) + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));

    private string RecordPath(string playerId, UnderworldWorldIdentity identity) =>
        Path.Combine(_rootDirectory, IdentityFingerprint(identity), Hash(playerId.Trim()) + ".uwmap");

    private string LegacyRecordPath(string playerId, UnderworldWorldIdentity identity) =>
        Path.Combine(_rootDirectory, Hash(identity.ParentWorldId + "\n" + identity.DerivedSeedFingerprint), Hash(playerId.Trim()) + ".uwmap");

    private static string IdentityFingerprint(UnderworldWorldIdentity identity) =>
        Hash(identity.ParentWorldId + "\n" + identity.DerivedWorldId + "\n" + identity.DerivedSeedFingerprint);

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

    private static bool FixedEquals(string left, string right)
    {
        if (left.Length != right.Length) return false;
        var difference = 0;
        for (var i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
        return difference == 0;
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
