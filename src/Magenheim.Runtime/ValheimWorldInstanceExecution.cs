using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Magenheim.Core.Underworld;

namespace Magenheim.Runtime;

/// <summary>
/// Temporarily presents one world-instance's native services through Valheim's singleton seams.
/// Scopes are synchronous and nestable; Surface globals are restored on dispose.
/// </summary>
internal static class ValheimWorldInstanceExecution
{
    private static readonly AsyncLocal<ValheimWorldInstanceContext?> Current = new();
    private static ValheimWorldInstanceRegistry? _registry;

    private static readonly FieldInfo? ZoneInstance = AccessTools.Field(typeof(ZoneSystem), "instance");
    private static readonly FieldInfo? GeneratorInstance = AccessTools.Field(typeof(WorldGenerator), "instance");
    private static readonly FieldInfo? ZNetWorld = AccessTools.Field(typeof(ZNet), "m_world");
    private static readonly FieldInfo? ZNetZdoMan = AccessTools.Field(typeof(ZNet), "m_zdoMan");

    internal static void BindRegistry(ValheimWorldInstanceRegistry registry) =>
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    internal static ValheimWorldInstanceContext? Active => Current.Value;

    internal static IDisposable Enter(ValheimWorldInstanceContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        return new Scope(context);
    }

    internal static bool TryEnterScene(int sceneHandle, out IDisposable? scope)
    {
        scope = null;
        var registry = _registry;
        if (registry is null || !registry.TryGetContextForScene(sceneHandle, out var context) || context is null)
            return false;
        scope = Enter(context);
        return true;
    }

    private sealed class Scope : IDisposable
    {
        private readonly ValheimWorldInstanceContext? _previousContext;
        private readonly object? _previousZone;
        private readonly object? _previousGenerator;
        private readonly object? _previousWorld;
        private readonly object? _previousZdoMan;
        private bool _disposed;

        internal Scope(ValheimWorldInstanceContext context)
        {
            _previousContext = Current.Value;
            _previousZone = ZoneInstance?.GetValue(null);
            _previousGenerator = GeneratorInstance?.GetValue(null);
            _previousWorld = ZNetWorld?.GetValue(null);
            _previousZdoMan = ZNet.instance is null ? null : ZNetZdoMan?.GetValue(ZNet.instance);

            Current.Value = context;
            ZoneInstance?.SetValue(null, context.ZoneSystem);
            GeneratorInstance?.SetValue(null, context.WorldGenerator);
            ZNetWorld?.SetValue(null, context.World);
            if (ZNet.instance is not null) ZNetZdoMan?.SetValue(ZNet.instance, context.ZdoMan);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (ZNet.instance is not null && ZNetZdoMan is not null) ZNetZdoMan.SetValue(ZNet.instance, _previousZdoMan);
            ZNetWorld?.SetValue(null, _previousWorld);
            GeneratorInstance?.SetValue(null, _previousGenerator);
            ZoneInstance?.SetValue(null, _previousZone);
            Current.Value = _previousContext;
        }
    }
}

/// <summary>
/// Ensures lifecycle callbacks on the Underworld ZoneSystem execute with its instance-native
/// World/WorldGenerator/ZDOMan bindings. Other ZoneSystem instances remain vanilla.
/// </summary>
[HarmonyPatch]
internal static class UnderworldZoneSystemInstanceScopePatch
{
    private static readonly string[] Names = { "Awake", "Start", "Update", "FixedUpdate", "LateUpdate" };

    internal static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var name in Names)
        {
            var method = AccessTools.Method(typeof(ZoneSystem), name);
            if (method is not null) yield return method;
        }
    }

    internal static void Prefix(ZoneSystem __instance, out IDisposable? __state)
    {
        __state = null;
        if (!__instance) return;
        ValheimWorldInstanceExecution.TryEnterScene(__instance.gameObject.scene.handle, out __state);
    }

    internal static void Finalizer(IDisposable? __state) => __state?.Dispose();
}
