using System;
using System.IO;
using System.Text;
using BepInEx.Logging;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

internal sealed class UnderworldWorldPairManifestStore
{
    private const string Version="magenheim-underworld-pair-v2";private const string LegacyVersion="magenheim-underworld-pair-v1";
    private readonly string _root;private readonly ManualLogSource _log;
    internal UnderworldWorldPairManifestStore(string root,ManualLogSource log){_root=Path.GetFullPath(root??throw new ArgumentNullException(nameof(root)));_log=log??throw new ArgumentNullException(nameof(log));}
    internal string GetDerivedSaveName(UnderworldWorldIdentity identity){Validate(identity);return "Magenheim_Underworld_"+identity.DerivedSeedFingerprint.Substring(0,20);}
    internal void EnsureManifest(UnderworldWorldIdentity identity,string parentSaveName)
    {
        Validate(identity);parentSaveName=NormalizeSaveName(parentSaveName);if(string.IsNullOrWhiteSpace(parentSaveName))throw new InvalidOperationException("Parent Surface save name is required for reversible physical-world handoff.");Directory.CreateDirectory(_root);
        var saveName=GetDerivedSaveName(identity);var path=Path.Combine(_root,saveName+".worldpair");
        if(File.Exists(path))
        {
            var existing=Decode(File.ReadAllText(path,Encoding.UTF8));if(!SamePair(existing.Identity,identity))throw new InvalidOperationException("Derived Underworld save identity collides with a different parent world pair.");
            if(!string.IsNullOrWhiteSpace(existing.ParentSaveName)&&!string.Equals(existing.ParentSaveName,parentSaveName,StringComparison.Ordinal))throw new InvalidOperationException("Underworld world-pair manifest resolves to a different parent Surface save name.");
            if(string.Equals(existing.ParentSaveName,parentSaveName,StringComparison.Ordinal)&&!existing.IsLegacy)return;
        }
        WriteVerified(path,Encode(identity,saveName,parentSaveName));_log.LogInfo($"Persisted reversible Underworld world-pair manifest '{saveName}' -> '{parentSaveName}'.");
    }
    internal bool TryResolveByDerivedSaveName(string saveName,out UnderworldWorldIdentity? identity){identity=null;return TryResolvePair(saveName,out identity,out _);}
    internal bool TryResolvePair(string derivedSaveName,out UnderworldWorldIdentity? identity,out string parentSaveName)
    {
        identity=null;parentSaveName=string.Empty;if(string.IsNullOrWhiteSpace(derivedSaveName))return false;var normalized=NormalizeSaveName(derivedSaveName);var path=Path.Combine(_root,normalized+".worldpair");if(!File.Exists(path))return false;
        try{var decoded=Decode(File.ReadAllText(path,Encoding.UTF8));if(!string.Equals(GetDerivedSaveName(decoded.Identity),normalized,StringComparison.Ordinal))return false;identity=decoded.Identity;parentSaveName=decoded.ParentSaveName;return true;}
        catch(Exception exception){_log.LogError($"Rejected Underworld world-pair manifest '{derivedSaveName}': {exception.Message}");identity=null;parentSaveName=string.Empty;return false;}
    }
    internal string GetTargetSaveName(UnderworldWorldIdentity identity,UnderworldLayer targetLayer)
    {
        Validate(identity);var derived=GetDerivedSaveName(identity);if(targetLayer==UnderworldLayer.Underworld)return derived;
        if(!TryResolvePair(derived,out var persisted,out var parentSaveName)||persisted is null||!SamePair(persisted,identity)||string.IsNullOrWhiteSpace(parentSaveName))throw new InvalidOperationException("Cannot return to Surface because the world-pair manifest predates reversible parent-save identity. Load the Surface world once to migrate it.");return parentSaveName;
    }
    private void WriteVerified(string path,string payload)
    {
        var temporary=path+".tmp-"+Guid.NewGuid().ToString("N");try{using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){var bytes=new UTF8Encoding(false).GetBytes(payload);stream.Write(bytes,0,bytes.Length);stream.Flush(true);}Decode(File.ReadAllText(temporary,Encoding.UTF8));if(File.Exists(path))File.Replace(temporary,path,path+".bak",true);else File.Move(temporary,path);temporary=string.Empty;}finally{if(temporary.Length!=0&&File.Exists(temporary))File.Delete(temporary);}
    }
    private static string Encode(UnderworldWorldIdentity identity,string saveName,string parentSaveName)=>string.Join("\n",new[]{Version,"save="+B64(saveName),"parentsave="+B64(parentSaveName),"parent="+B64(identity.ParentWorldId),"seed="+B64(identity.ParentSeed),"derived="+B64(identity.DerivedWorldId),"fingerprint="+identity.DerivedSeedFingerprint,"seed32="+identity.DerivedSeed32,string.Empty});
    private static DecodedPair Decode(string payload)
    {
        var lines=payload.Replace("\r",string.Empty).Split('\n');if(lines.Length<7)throw new InvalidDataException("Malformed Underworld world-pair manifest.");var legacy=lines[0]==LegacyVersion;if(!legacy&&lines[0]!=Version)throw new InvalidDataException("Unsupported Underworld world-pair manifest version.");
        string Read(string prefix,int index){if(index>=lines.Length||!lines[index].StartsWith(prefix,StringComparison.Ordinal))throw new InvalidDataException("Malformed Underworld world-pair manifest.");return lines[index].Substring(prefix.Length);}
        var offset=legacy?0:1;var parentSave=legacy?string.Empty:FromB64(Read("parentsave=",2));var parent=FromB64(Read("parent=",2+offset));var seed=FromB64(Read("seed=",3+offset));var expected=UnderworldWorldIdentityFactory.Derive(parent,seed);var derived=FromB64(Read("derived=",4+offset));var fingerprint=Read("fingerprint=",5+offset);if(!int.TryParse(Read("seed32=",6+offset),out var seed32))throw new InvalidDataException("Invalid Underworld derived seed.");var decoded=new UnderworldWorldIdentity(parent,seed,derived,fingerprint,seed32);if(!SamePair(decoded,expected)||decoded.DerivedSeed32!=expected.DerivedSeed32)throw new InvalidDataException("Underworld world-pair manifest failed deterministic identity validation.");return new DecodedPair(decoded,parentSave,legacy);
    }
    private static string B64(string value)=>Convert.ToBase64String(Encoding.UTF8.GetBytes(value));private static string FromB64(string value)=>Encoding.UTF8.GetString(Convert.FromBase64String(value));private static string NormalizeSaveName(string value)=>string.IsNullOrWhiteSpace(value)?string.Empty:Path.GetFileName(value.Trim());
    private static void Validate(UnderworldWorldIdentity identity){if(identity is null)throw new ArgumentNullException(nameof(identity));var expected=UnderworldWorldIdentityFactory.Derive(identity.ParentWorldId,identity.ParentSeed);if(!SamePair(identity,expected)||identity.DerivedSeed32!=expected.DerivedSeed32)throw new InvalidOperationException("Underworld world-pair identity is not the deterministic derivative of its parent.");}
    private static bool SamePair(UnderworldWorldIdentity left,UnderworldWorldIdentity right)=>string.Equals(left.ParentWorldId,right.ParentWorldId,StringComparison.Ordinal)&&string.Equals(left.DerivedWorldId,right.DerivedWorldId,StringComparison.Ordinal)&&string.Equals(left.DerivedSeedFingerprint,right.DerivedSeedFingerprint,StringComparison.Ordinal);
    private sealed class DecodedPair{internal DecodedPair(UnderworldWorldIdentity identity,string parentSaveName,bool isLegacy){Identity=identity;ParentSaveName=parentSaveName;IsLegacy=isLegacy;}internal UnderworldWorldIdentity Identity{get;}internal string ParentSaveName{get;}internal bool IsLegacy{get;}}
}
