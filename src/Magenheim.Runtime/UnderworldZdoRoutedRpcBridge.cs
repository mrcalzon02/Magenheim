using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Bridges Valheim's two process-global routed ZDO handlers across the two native instance managers.
/// Valheim registers DestroyZDO/RequestZDO once when the Surface ZDOMan is constructed. Instance 1
/// deliberately does not run a second constructor, so replay the same native handler against both
/// managers under their own singleton/world scopes instead of registering duplicate RPC names.
/// </summary>
internal static class UnderworldZdoRoutedRpcBridge
{
    private static readonly AsyncLocal<int> Reentry = new();
    private static readonly MethodInfo DestroyRpc =
        AccessTools.Method(typeof(ZDOMan), "RPC_DestroyZDO", new[] { typeof(long), typeof(ZPackage) })
        ?? throw new MissingMethodException(typeof(ZDOMan).FullName, "RPC_DestroyZDO");
    private static readonly MethodInfo RequestRpc =
        AccessTools.Method(typeof(ZDOMan), "RPC_RequestZDO", new[] { typeof(long), typeof(ZDOID) })
        ?? throw new MissingMethodException(typeof(ZDOMan).FullName, "RPC_RequestZDO");

    internal static bool ReplayDestroy(long sender, ZPackage package)
    {
        if (Reentry.Value != 0 || !HasBothContexts()) return false;

        var bytes = package.GetArray();
        var position = package.GetPos();
        Replay(DestroyRpc, sender, () =>
        {
            var copy = new ZPackage(bytes);
            copy.SetPos(position);
            return copy;
        });
        return true;
    }

    internal static bool ReplayRequest(long sender, ZDOID id)
    {
        if (Reentry.Value != 0 || !HasBothContexts()) return false;
        Replay(RequestRpc, sender, () => id);
        return true;
    }

    internal static bool TryEnterTarget(ZDOID target, out IDisposable? scope)
    {
        scope = null;
        if (target.IsNone()) return false;

        if (ValheimWorldInstanceExecution.TryGetContext(UnderworldWorldInstanceId.Underworld, out var underworld) &&
            underworld is not null && underworld.ZdoMan.GetZDO(target) is not null)
        {
            scope = ValheimWorldInstanceExecution.Enter(underworld);
            return true;
        }

        var active = ValheimWorldInstanceExecution.Active;
        if (active is not null && active.InstanceId.IsUnderworld &&
            ValheimWorldInstanceExecution.TryGetContext(UnderworldWorldInstanceId.Surface, out var surface) &&
            surface is not null && surface.ZdoMan.GetZDO(target) is not null)
        {
            scope = ValheimWorldInstanceExecution.Enter(surface);
            return true;
        }

        return false;
    }

    private static bool HasBothContexts() =>
        ValheimWorldInstanceExecution.TryGetContext(UnderworldWorldInstanceId.Surface, out var surface) &&
        surface is not null &&
        ValheimWorldInstanceExecution.TryGetContext(UnderworldWorldInstanceId.Underworld, out var underworld) &&
        underworld is not null;

    private static void Replay(MethodInfo method, long sender, Func<object> payloadFactory)
    {
        Reentry.Value++;
        try
        {
            ReplayOne(UnderworldWorldInstanceId.Surface, method, sender, payloadFactory());
            ReplayOne(UnderworldWorldInstanceId.Underworld, method, sender, payloadFactory());
        }
        finally
        {
            Reentry.Value--;
        }
    }

    private static void ReplayOne(
        UnderworldWorldInstanceId instanceId,
        MethodInfo method,
        long sender,
        object payload)
    {
        if (!ValheimWorldInstanceExecution.TryGetContext(instanceId, out var context) || context is null)
            return;

        try
        {
            using (ValheimWorldInstanceExecution.Enter(context))
                method.Invoke(context.ZdoMan, new[] { (object)sender, payload });
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"Native Valheim {method.Name} replay failed for world instance {instanceId}.",
                exception.InnerException);
        }
    }
}

[HarmonyPatch(typeof(ZDOMan), "RPC_DestroyZDO", new[] { typeof(long), typeof(ZPackage) })]
internal static class UnderworldZdoDestroyRoutedRpcPatch
{
    private static bool Prefix(long __0, ZPackage __1) =>
        !UnderworldZdoRoutedRpcBridge.ReplayDestroy(__0, __1);
}

[HarmonyPatch(typeof(ZDOMan), "RPC_RequestZDO", new[] { typeof(long), typeof(ZDOID) })]
internal static class UnderworldZdoRequestRoutedRpcPatch
{
    private static bool Prefix(long __0, ZDOID __1) =>
        !UnderworldZdoRoutedRpcBridge.ReplayRequest(__0, __1);
}

[HarmonyPatch(typeof(ZNetScene), "CreateObject", new[] { typeof(ZDO) })]
internal static class UnderworldZNetSceneCreateObjectInstancePatch
{
    private static void Prefix(ZDO __0, out IDisposable? __state)
    {
        __state = null;
        if (__0 is null) return;
        UnderworldZdoRoutedRpcBridge.TryEnterTarget(__0.m_uid, out __state);
    }

    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
}

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Destroy), new[] { typeof(UnityEngine.GameObject) })]
internal static class UnderworldZNetSceneDestroyInstancePatch
{
    private static void Prefix(UnityEngine.GameObject __0, out IDisposable? __state)
    {
        __state = null;
        if (!__0) return;
        if (ValheimWorldInstanceExecution.TryEnterScene(__0.scene.handle, out __state)) return;

        var view = __0.GetComponent<ZNetView>();
        var zdo = view && view.IsValid() ? view.GetZDO() : null;
        if (zdo is not null)
            UnderworldZdoRoutedRpcBridge.TryEnterTarget(zdo.m_uid, out __state);
    }

    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
}

[HarmonyPatch(typeof(ZRoutedRpc), "HandleRoutedRPC", new[] { typeof(ZRoutedRpc.RoutedRPCData) })]
internal static class UnderworldObjectRoutedRpcInstancePatch
{
    private static void Prefix(ZRoutedRpc.RoutedRPCData __0, out IDisposable? __state)
    {
        __state = null;
        if (__0 is null) return;
        UnderworldZdoRoutedRpcBridge.TryEnterTarget(__0.m_targetZDO, out __state);
    }

    private static void Finalizer(IDisposable? __state) => __state?.Dispose();
}
