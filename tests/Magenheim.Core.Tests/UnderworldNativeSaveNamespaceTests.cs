using System;
using System.IO;
using Magenheim.Core.Underworld;

internal static class UnderworldNativeSaveNamespaceTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool value) { checks++; if (!value) throw new Exception("Native child save namespace regression."); }
        Check(UnderworldNativeSaveNamespace.ChildDirectory("worlds/Rawheim/", true) == "worlds/Rawheim/magenheim_instances/1/");
        Check(UnderworldNativeSaveNamespace.ChildDirectory("/worlds/Rawheim", true) == "worlds/Rawheim/magenheim_instances/1/");
        Check(UnderworldNativeSaveNamespace.ChildDirectory(@"worlds\Rawheim\", true) == "worlds/Rawheim/magenheim_instances/1/");
        var parent = Path.Combine(Path.GetTempPath(), "magenheim-save-test", "world");
        var disk = UnderworldNativeSaveNamespace.ChildDirectory(parent, false);
        Check(Path.IsPathRooted(disk));
        Check(disk.StartsWith(Path.GetFullPath(parent).Replace('\\', '/') + "/", StringComparison.Ordinal));
        Check(disk + "aa_bb__0_1.chunk" == disk.TrimEnd('/') + "/aa_bb__0_1.chunk");
        foreach (var bad in new[] { "", "/", "worlds/../other", "C:/worlds/Rawheim", "worlds//Rawheim", "worlds/./Rawheim" })
        {
            bool rejected = false;
            try { UnderworldNativeSaveNamespace.ChildDirectory(bad, true); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected);
        }
        return checks;
    }
}
