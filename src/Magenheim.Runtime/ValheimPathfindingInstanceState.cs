using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Magenheim.Runtime;

/// <summary>
/// Virtualizes Valheim's single Pathfinding component per world instance without moving either
/// world's coordinates. Instance 1 owns a detached tile/build state plus duplicate NavMesh agent
/// settings; Unity's global NavMesh can therefore hold Surface and Underworld data at identical
/// coordinates because every query/build is separated by agentTypeID.
/// </summary>
internal sealed class ValheimPathfindingInstanceState : IDisposable
{
    private static readonly string[] StatefulFields =
    {
        "tempPath", "optPath", "tempStitchPoints", "tempHitArray",
        "m_tiles", "m_updatePathfindingTimer", "m_queuedAreas",
        "m_linkRemoveQueue", "m_tileRemoveQueue",
        "m_cachedTileID", "m_cachedTile", "m_agentSettings",
        "m_buildOperation", "m_buildTile", "m_edgeBuildQueue", "m_path",
    };

    private static readonly MethodInfo UpdateMethod =
        AccessTools.Method(typeof(Pathfinding), "Update", Type.EmptyTypes)
        ?? throw new MissingMethodException(typeof(Pathfinding).FullName, "Update()");
    private static readonly MethodInfo DestroyMethod =
        AccessTools.Method(typeof(Pathfinding), "OnDestroy", Type.EmptyTypes)
        ?? throw new MissingMethodException(typeof(Pathfinding).FullName, "OnDestroy()");

    private readonly Dictionary<FieldInfo, object?> _values = new();
    private readonly List<int> _ownedAgentTypeIds = new();
    private bool _disposed;

    private ValheimPathfindingInstanceState() { }

    internal static ValheimPathfindingInstanceState CreateUnderworld(Pathfinding pathfinding, ManualLogSource log)
    {
        if (!pathfinding) throw new ArgumentNullException(nameof(pathfinding));
        if (log is null) throw new ArgumentNullException(nameof(log));

        var state = new ValheimPathfindingInstanceState();
        var fields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        foreach (var name in StatefulFields)
        {
            var field = AccessTools.Field(typeof(Pathfinding), name)
                ?? throw new MissingFieldException(typeof(Pathfinding).FullName, name);
            fields.Add(name, field);
        }

        foreach (var pair in fields)
        {
            var name = pair.Key;
            var field = pair.Value;
            var source = field.GetValue(pathfinding);

            if (name == "m_agentSettings")
                continue;
            if (name == "m_updatePathfindingTimer")
            {
                state._values[field] = Convert.ChangeType(0f, field.FieldType);
                continue;
            }
            if (name is "m_cachedTile" or "m_buildOperation" or "m_buildTile")
            {
                state._values[field] = null;
                continue;
            }
            if (name == "m_cachedTileID")
            {
                state._values[field] = source;
                continue;
            }
            if (name == "m_path")
            {
                state._values[field] = new NavMeshPath();
                continue;
            }
            state._values[field] = CreateEmptyRuntimeState(field, source);
        }

        var agentField = fields["m_agentSettings"];
        state._values[agentField] = state.CreateDetachedAgentSettings(pathfinding, agentField, log);
        state.ValidateDetached(pathfinding);
        return state;
    }

    internal IDisposable Enter(Pathfinding pathfinding)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ValheimPathfindingInstanceState));
        return new Scope(this, pathfinding);
    }

    internal void TickActive(Pathfinding pathfinding)
    {
        if (_disposed) return;
        try
        {
            UpdateMethod.Invoke(pathfinding, Array.Empty<object>());
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw new InvalidOperationException("Native Underworld Pathfinding.Update failed.", exception.InnerException);
        }
    }

    private object CreateDetachedAgentSettings(Pathfinding pathfinding, FieldInfo agentField, ManualLogSource log)
    {
        if (agentField.GetValue(pathfinding) is not IList surface)
            throw new InvalidOperationException("Valheim Pathfinding.m_agentSettings is no longer an IList.");

        var fresh = Activator.CreateInstance(surface.GetType()) as IList
            ?? throw new InvalidOperationException("Unable to create detached Pathfinding agent-settings list.");

        var previous = agentField.GetValue(pathfinding);
        agentField.SetValue(pathfinding, fresh);
        try
        {
            var addAgent = typeof(Pathfinding).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo? add = null;
            foreach (var candidate in addAgent)
            {
                if (candidate.Name != "AddAgent") continue;
                var parameters = candidate.GetParameters();
                if (parameters.Length != 2) continue;
                add = candidate;
                break;
            }
            if (add is null)
                throw new MissingMethodException(typeof(Pathfinding).FullName, "AddAgent(agentType,copy)");

            foreach (var sourceSetting in surface)
            {
                if (sourceSetting is null) continue;
                var settingType = sourceSetting.GetType();
                var agentTypeField = AccessTools.Field(settingType, "m_agentType")
                    ?? throw new MissingFieldException(settingType.FullName, "m_agentType");
                var agentType = agentTypeField.GetValue(sourceSetting);
                var created = add.Invoke(pathfinding, new[] { agentType, sourceSetting })
                    ?? throw new InvalidOperationException("Pathfinding.AddAgent returned null.");
                CopyAgentRuntimeFlags(sourceSetting, created);
                var id = ReadAgentTypeId(created);
                if (id == 0)
                    throw new InvalidOperationException("Underworld Pathfinding received Unity's default agent type ID 0 instead of a detached runtime agent.");
                _ownedAgentTypeIds.Add(id);
            }
        }
        finally
        {
            agentField.SetValue(pathfinding, previous);
        }

        log.LogInfo($"Allocated {_ownedAgentTypeIds.Count} detached Unity NavMesh agent types for Underworld pathfinding.");
        return fresh;
    }

    private static void CopyAgentRuntimeFlags(object source, object target)
    {
        var type = source.GetType();
        foreach (var name in new[] { "m_canWalk", "m_avoidWater", "m_canSwim", "m_swimDepth", "m_areaMask" })
        {
            var field = AccessTools.Field(type, name);
            if (field is not null) field.SetValue(target, field.GetValue(source));
        }

        var buildField = AccessTools.Field(type, "m_build")
            ?? throw new MissingFieldException(type.FullName, "m_build");
        var sourceBuild = buildField.GetValue(source)
            ?? throw new InvalidOperationException("Pathfinding AgentSettings.m_build is null.");
        var targetBuild = buildField.GetValue(target)
            ?? throw new InvalidOperationException("Detached Pathfinding AgentSettings.m_build is null.");
        var buildType = sourceBuild.GetType();

        foreach (var property in buildType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.Name == "agentTypeID" || !property.CanRead || !property.CanWrite) continue;
            try { property.SetValue(targetBuild, property.GetValue(sourceBuild, null), null); }
            catch { }
        }
        buildField.SetValue(target, targetBuild);
    }

    private static int ReadAgentTypeId(object setting)
    {
        var buildField = AccessTools.Field(setting.GetType(), "m_build")
            ?? throw new MissingFieldException(setting.GetType().FullName, "m_build");
        var build = buildField.GetValue(setting)
            ?? throw new InvalidOperationException("Pathfinding AgentSettings.m_build is null.");
        var property = build.GetType().GetProperty("agentTypeID", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new MissingMemberException(build.GetType().FullName, "agentTypeID");
        return Convert.ToInt32(property.GetValue(build, null));
    }

    private static object? CreateEmptyRuntimeState(FieldInfo field, object? source)
    {
        if (source is null) return null;
        if (field.FieldType.IsValueType || source is string) return source;

        if (source is Array array)
            return Array.CreateInstance(array.GetType().GetElementType()!, array.Length);

        if (source is IList || source is IDictionary ||
            source.GetType().Name.StartsWith("Queue", StringComparison.Ordinal) ||
            source.GetType().Name.StartsWith("Dictionary", StringComparison.Ordinal))
        {
            try { return Activator.CreateInstance(source.GetType()); }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Unable to detach Pathfinding field '{field.Name}'.", exception);
            }
        }

        throw new InvalidOperationException(
            $"Unsupported mutable Pathfinding field '{field.Name}' ({field.FieldType.FullName}); Underworld pathfinding will not share it.");
    }

    private void ValidateDetached(Pathfinding surface)
    {
        foreach (var pair in _values)
        {
            if (pair.Value is null) continue;
            var surfaceValue = pair.Key.GetValue(surface);
            if (surfaceValue is not null && !pair.Key.FieldType.IsValueType && ReferenceEquals(surfaceValue, pair.Value))
                throw new InvalidOperationException($"Underworld Pathfinding still shares mutable field '{pair.Key.Name}' with Surface.");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        // NavMeshData/links live in Unity's process-global NavMesh backend, not the local Unity
        // scene. Run Valheim's own Pathfinding teardown while the detached Underworld state is
        // swapped into the singleton, so only instance-1 tiles and links are removed.
        var pathfinding = Pathfinding.instance;
        if (pathfinding)
        {
            try
            {
                using (Enter(pathfinding))
                    DestroyMethod.Invoke(pathfinding, Array.Empty<object>());
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                throw new InvalidOperationException(
                    "Native Underworld Pathfinding teardown failed.",
                    exception.InnerException);
            }
        }

        _disposed = true;
        foreach (var id in _ownedAgentTypeIds)
        {
            try { NavMesh.RemoveSettings(id); }
            catch { }
        }
        _ownedAgentTypeIds.Clear();
        _values.Clear();
    }

    private sealed class Scope : IDisposable
    {
        private readonly ValheimPathfindingInstanceState _owner;
        private readonly Pathfinding _pathfinding;
        private readonly Dictionary<FieldInfo, object?> _previous = new();
        private bool _disposed;

        internal Scope(ValheimPathfindingInstanceState owner, Pathfinding pathfinding)
        {
            _owner = owner;
            _pathfinding = pathfinding;
            foreach (var pair in owner._values)
            {
                _previous[pair.Key] = pair.Key.GetValue(pathfinding);
                pair.Key.SetValue(pathfinding, pair.Value);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var field in new List<FieldInfo>(_owner._values.Keys))
                _owner._values[field] = field.GetValue(_pathfinding);
            foreach (var pair in _previous)
                pair.Key.SetValue(_pathfinding, pair.Value);
        }
    }
}

/// <summary>
/// NavMeshBuilder.CollectSources is process-global even when physics is scene-local. Filter every
/// collected physics source to the active world-instance Unity scene before Valheim builds a tile.
/// Surface and Underworld then build separate data sets with separate agent IDs at the same coords.
/// </summary>
[HarmonyPatch]
internal static class UnderworldNavMeshSourceIsolationPatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var method in typeof(NavMeshBuilder).GetMethods(BindingFlags.Static | BindingFlags.Public))
        {
            if (method.Name != "CollectSources") continue;
            var parameters = method.GetParameters();
            if (parameters.Length == 0) continue;
            if (parameters[parameters.Length - 1].ParameterType != typeof(List<NavMeshBuildSource>)) continue;
            yield return method;
        }
    }

    private static void Postfix(object[] __args)
    {
        if (!ValheimWorldInstanceExecution.TryGetQueryScene(out var scene)) return;

        List<NavMeshBuildSource>? sources = null;
        foreach (var arg in __args)
            if (arg is List<NavMeshBuildSource> list) sources = list;
        if (sources is null) return;

        for (var i = sources.Count - 1; i >= 0; i--)
        {
            var component = sources[i].component as Component;
            if (!component || component.gameObject.scene.handle != scene.handle)
                sources.RemoveAt(i);
        }
    }
}
