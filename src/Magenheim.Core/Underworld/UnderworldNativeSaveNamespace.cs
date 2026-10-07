using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Magenheim.Core.Underworld;

/// <summary>Native cloud names stay relative; disk saves stay rooted. Both end in a directory slash.</summary>
public static class UnderworldNativeSaveNamespace
{
    public static int ReadChunkHeader(BinaryReader reader, ushort expectedCodec)
    {
        var codec = reader.ReadUInt16();
        var count = reader.ReadInt32();
        var stream = reader.BaseStream;
        if (!IsValidChunkHeader(codec, count, stream.Length - stream.Position, expectedCodec))
            throw new IOException("Invalid native Underworld chunk header.");
        return count;
    }
    public static bool IsValidChunkHeader(ushort codec, int count, long remainingBytes, ushort expectedCodec) =>
        codec == expectedCodec && count >= 0 && count <= 10000000 && remainingBytes >= 0 &&
        (count == 0 || remainingBytes >= count);
    public static bool TryParseChunkFileName(string name, out ushort index, out byte size, out uint version)
    {
        index = 0; size = 0; version = 0;
        var match = Regex.Match(name, @"^([0-9a-fA-F]{2})_([0-9a-fA-F]{2})__([0-3])_([0-9]+)\.chunk$");
        if (!match.Success || !uint.TryParse(match.Groups[4].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out version) || version == 0) return false;
        index = (ushort)((byte.Parse(match.Groups[1].Value, NumberStyles.HexNumber) << 8) |
                        byte.Parse(match.Groups[2].Value, NumberStyles.HexNumber));
        size = byte.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        return true;
    }
    public static string NormalizeParent(string parentPath, bool cloud)
    {
        if (string.IsNullOrWhiteSpace(parentPath)) throw new ArgumentException("Parent save path is empty.", nameof(parentPath));
        if (!cloud) return Path.GetFullPath(parentPath).Replace('\\', '/').TrimEnd('/');
        var relative = parentPath.Replace('\\', '/').Trim('/');
        if (relative.Length == 0 || relative.Contains(':') ||
            relative.Split('/').Any(segment => segment.Length == 0 || segment == "." || segment == ".."))
            throw new ArgumentException("Cloud save name must remain inside its native relative namespace.", nameof(parentPath));
        return relative;
    }

    public static string ChildDirectory(string parentPath, bool cloud) =>
        NormalizeParent(parentPath, cloud) + "/magenheim_instances/1/";
}
