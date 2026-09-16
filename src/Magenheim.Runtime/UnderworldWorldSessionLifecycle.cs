using System;
using BepInEx.Logging;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class UnderworldWorldSessionLifecycle : MonoBehaviour
{
    private UnderworldRuntimeServices? _services;private UnderworldDeepGateRegistrar? _deepGateRegistrar;private UnderworldDeepGateLocationRegistrar? _deepGateLocationRegistrar;private ManualLogSource? _log;private long? _observedWorldUid;
    internal void Configure(UnderworldRuntimeServices services,ManualLogSource log)
    {
        if(_services is not null)throw new InvalidOperationException("Underworld world-session lifecycle is already configured.");_services=services??throw new ArgumentNullException(nameof(services));_log=log??throw new ArgumentNullException(nameof(log));
        _deepGateRegistrar=new UnderworldDeepGateRegistrar(log);_deepGateRegistrar.Register();_deepGateLocationRegistrar=new UnderworldDeepGateLocationRegistrar(log);_deepGateLocationRegistrar.Register();
        // UnderworldWorldCenterRegistrar is intentionally NOT registered in the parent world.
        // The separate-world loader admits it only after the derived Underworld world identity is active.
    }
    private void Update(){if(_services is null)return;var znet=ZNet.instance;if(znet is null){if(_observedWorldUid.HasValue){_services.ResetForWorldUnload();_log?.LogDebug($"Underworld logical context reset after Valheim world {_observedWorldUid.Value} unloaded.");_observedWorldUid=null;}return;}var currentWorldUid=znet.GetWorldUID();if(!_observedWorldUid.HasValue){_observedWorldUid=currentWorldUid;return;}if(_observedWorldUid.Value==currentWorldUid)return;var previous=_observedWorldUid.Value;_services.ResetForWorldUnload();_observedWorldUid=currentWorldUid;_log?.LogInfo($"Underworld logical context reset for Valheim world change {previous} -> {currentWorldUid}.");}
    private void OnDestroy(){_deepGateLocationRegistrar?.Dispose();_deepGateLocationRegistrar=null;_deepGateRegistrar?.Dispose();_deepGateRegistrar=null;_services?.ResetForWorldUnload();_observedWorldUid=null;}
}
