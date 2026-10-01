using System;
using System.IO;
using System.Linq;

namespace Magenheim.Core.Underworld;

/// <summary>Native cloud names stay relative; disk saves stay rooted. Both end in a directory slash.</summary>
public static class UnderworldNativeSaveNamespace
{
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
