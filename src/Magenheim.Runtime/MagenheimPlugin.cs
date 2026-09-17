using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using Magenheim.Core.Socketing;
using Magenheim.Runtime.Compatibility;
using Magenheim.Runtime.Definitions;
using Magenheim.Runtime.Networking;

namespace Magenheim.Runtime;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
internal sealed partial class MagenheimPlugin
{
    private void BindUnderworldTransitionAuthority(MagenheimDefinitionSet effectiveDefinitions,SocketCompatibilityPolicy socketPolicy)
    {
        if(_services is null)throw new InvalidOperationException("Runtime services must exist before Underworld transition authority is bound.");
        _underworldRuntimeServices=UnderworldRuntimeServices.Create(BepInEx.Paths.ConfigPath,Logger);
        _authoritySynchronizer=new DefinitionAuthoritySynchronizer(effectiveDefinitions,socketPolicy,_services.GeodeOpeningOperations,_services.RefinementOperations,_services.SocketOperations,_services.SocketExtractionOperations,Logger);
        _underworldWorldSessionLifecycle=gameObject.AddComponent<UnderworldWorldSessionLifecycle>();
        _underworldWorldSessionLifecycle.Configure(_underworldRuntimeServices,Logger);
        _underworldWorldSessionLifecycle.SetGameplayAuthorityFingerprint(_authoritySynchronizer.GameplayFingerprint);
    }
}
