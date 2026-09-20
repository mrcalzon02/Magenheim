using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>Durable server-owned storage for per-player Deep Boon selection, isolated and authenticated by paired-world identity.</summary>
internal sealed class UnderworldDeepBoonSelectionStore
{
    private const string EnvelopeHeader = "MGEN-INSTANCE-DEEP-BOON";
    private const int EnvelopeVersion = 1;
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
        var selectionPayload = UnderworldDeepBoonSelectionCodec.Encode(state ?? throw new ArgumentNullException(nameof(state)));
        var payload = EncodeEnvelope(identity, selectionPayload);
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
            var verified = DecodeEnvelope(identity, File.ReadAllText(temporary, Encoding.UTF8));
            if (UnderworldDeepBoonSelectionCodec.Encode(verified) != selectionPayload) throw new InvalidOperationException("Deep Boon selection read-back was not canonical.");
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
        if (TryRead(path, identity, out state, out diagnostic, out var legacy))
        {
            if (legacy) { Save(playerId, identity, state); diagnostic = "Migrated legacy Deep Boon selection into authenticated instance envelope."; _log.LogInfo(diagnostic); }
            return true;
        }
        var primary = diagnostic;
        if (TryRead(path + ".bak", identity, out state, out diagnostic, out legacy))
        {
            var recovery = "Recovered Deep Boon selection from backup after primary failure: " + primary;
            if (legacy) { Save(playerId, identity, state); recovery += " Legacy payload migrated into authenticated instance envelope."; }
            diagnostic = recovery;
            _log.LogWarning(diagnostic);
            return true;
        }
        state = UnderworldDeepBoonSelection.CreateInitialState();
        diagnostic = "No admissible persisted Deep Boon selection. Primary: " + primary + " Backup: " + diagnostic;
        return false;
    }

    private static bool TryRead(string path, UnderworldWorldIdentity identity, out UnderworldDeepBoonSelectionState state, out string diagnostic, out bool legacy)
    {
        legacy = false;
        if (!File.Exists(path)) { state = UnderworldDeepBoonSelection.CreateInitialState(); diagnostic = "candidate missing"; return false; }
        try
        {
            var text = File.ReadAllText(path, Encoding.UTF8);
            if (text.StartsWith(EnvelopeHeader + "\n", StringComparison.Ordinal)) state = DecodeEnvelope(identity, text);
            else { state = UnderworldDeepBoonSelectionCodec.Decode(text); legacy = true; }
            diagnostic = string.Empty;
            return true;
        }
        catch (Exception exception) { state = UnderworldDeepBoonSelection.CreateInitialState(); diagnostic = exception.Message; return false; }
    }

    private static string EncodeEnvelope(UnderworldWorldIdentity identity, string selectionPayload) =>
        EnvelopeHeader + "\n" +
        "version=" + EnvelopeVersion + "\n" +
        "instance=" + InstanceFingerprint(identity) + "\n" +
        "payload=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(selectionPayload)) + "\n";

    private static UnderworldDeepBoonSelectionState DecodeEnvelope(UnderworldWorldIdentity identity, string envelope)
    {
        var lines = envelope.Replace("\r\n", "\n").Split('\n');
        if (lines.Length != 5 || lines[0] != EnvelopeHeader || lines[1] != "version=" + EnvelopeVersion || !lines[2].StartsWith("instance=", StringComparison.Ordinal) || !lines[3].StartsWith("payload=", StringComparison.Ordinal) || lines[4].Length != 0)
            throw new InvalidDataException("Deep Boon persistence envelope is malformed or unsupported.");
        var expected = InstanceFingerprint(identity);
        var actual = lines[2].Substring("instance=".Length);
        if (!FixedEquals(expected, actual)) throw new InvalidDataException("Deep Boon persistence belongs to a different Underworld instance.");
        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(lines[3].Substring("payload=".Length)));
        return UnderworldDeepBoonSelectionCodec.Decode(payload);
    }

    private static string InstanceFingerprint(UnderworldWorldIdentity identity) => Hash(identity.ParentWorldId + "\n" + identity.DerivedWorldId + "\n" + identity.DerivedSeedFingerprint);

    private static bool FixedEquals(string expected, string actual)
    {
        if (expected.Length != actual.Length) return false;
        var difference = 0;
        for (var index = 0; index < expected.Length; index++) difference |= expected[index] ^ actual[index];
        return difference == 0;
    }

    private string RecordPath(string playerId, UnderworldWorldIdentity identity) => Path.Combine(_rootDirectory, InstanceFingerprint(identity), Hash(playerId.Trim()) + ".uwboon");

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
