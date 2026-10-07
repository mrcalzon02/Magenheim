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
        Check(UnderworldNativeSaveNamespace.TryParseChunkFileName("20_20__1_1.chunk", out var index, out var size, out var version)
            && index == 0x2020 && size == 1 && version == 1);
        Check(UnderworldNativeSaveNamespace.IsValidChunkHeader(41, 1, 67, 41));
        Check(UnderworldNativeSaveNamespace.IsValidChunkHeader(41, 0, 0, 41));
        Check(!UnderworldNativeSaveNamespace.IsValidChunkHeader(40, 1, 67, 41));
        Check(!UnderworldNativeSaveNamespace.IsValidChunkHeader(41, -1, 67, 41));
        Check(!UnderworldNativeSaveNamespace.IsValidChunkHeader(41, 10000001, 67, 41));
        Check(!UnderworldNativeSaveNamespace.IsValidChunkHeader(41, 1, 0, 41));
        foreach (var fixture in new (ushort Codec, int Count, int Bytes, bool Valid)[] { (41, 1, 67, true),
            ((ushort)41, 0, 0, true), ((ushort)40, 1, 67, false),
            ((ushort)41, 1, 0, false), ((ushort)41, -1, 0, false) })
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write(fixture.Codec);
                writer.Write(fixture.Count);
                writer.Write(new byte[fixture.Bytes]);
            }
            stream.Position = 0;
            using var reader = new BinaryReader(new BufferedStream(stream));
            bool accepted = false;
            try { accepted = UnderworldNativeSaveNamespace.ReadChunkHeader(reader, 41) == fixture.Count; }
            catch (IOException) { }
            Check(accepted == fixture.Valid);
        }
        using (var truncated = new BinaryReader(new MemoryStream(new byte[] { 41, 0, 1 })))
        {
            bool rejected = false;
            try { UnderworldNativeSaveNamespace.ReadChunkHeader(truncated, 41); }
            catch (EndOfStreamException) { rejected = true; }
            Check(rejected);
        }
        foreach (var invalid in new[] { "../20_20__1_1.chunk", "20_20__4_1.chunk", "20_20__1_0.chunk", "20_20__1_4294967296.chunk", "_main.3.chunks", "20_20__1_1.chunk.bak" })
            Check(!UnderworldNativeSaveNamespace.TryParseChunkFileName(invalid, out _, out _, out _));
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
