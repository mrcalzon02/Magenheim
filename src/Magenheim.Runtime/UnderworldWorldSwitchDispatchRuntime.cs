using System;
using BepInEx.Logging;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Live-test dispatcher for a prepared physical handoff. The actual Valheim save loader remains a
/// deliberately narrow engine boundary; this component makes missing loader integration observable
/// immediately instead of silently leaving a durable transition waiting forever.
/// </summary>
internal sealed class UnderworldWorldSwitchDispatchRuntime : MonoBehaviour
{
    private UnderworldRuntimeServices? _services;
    private ManualLogSource? _log;
    private IUnderworldPhysicalWorldLoader? _loader;
    private float _nextAt;
    private string _lastDiagnostic=string.Empty;

    internal void Configure(UnderworldRuntimeServices services,ManualLogSource log,IUnderworldPhysicalWorldLoader? loader=null)
    { _services=services??throw new ArgumentNullException(nameof(services));_log=log??throw new ArgumentNullException(nameof(log));_loader=loader; }

    internal void SetLoader(IUnderworldPhysicalWorldLoader loader)
    { _loader=loader??throw new ArgumentNullException(nameof(loader));_lastDiagnostic=string.Empty; }

    private void Update()
    {
        if(Time.unscaledTime<_nextAt)return;_nextAt=Time.unscaledTime+0.5f;
        if(_services is null||ZNet.instance is null||!ZNet.instance.IsServer())return;
        if(!_services.WorldSwitchDriver.HasPendingPhysicalSwitch)return;
        if(_loader is null)
        {
            ReportOnce($"Underworld physical handoff to '{_services.WorldSwitchDriver.PendingTargetSaveName}' is durably pending but no Valheim save loader is installed.");
            return;
        }
        if(_services.WorldSwitchDriver.TryDispatchPending(_loader,out var diagnostic))ReportOnce(diagnostic);
        else if(diagnostic.IndexOf("No prepared",StringComparison.Ordinal)<0)ReportOnce(diagnostic);
    }

    private void ReportOnce(string diagnostic)
    {
        if(string.Equals(_lastDiagnostic,diagnostic,StringComparison.Ordinal))return;
        _lastDiagnostic=diagnostic;_log?.LogWarning(diagnostic);
    }
}
