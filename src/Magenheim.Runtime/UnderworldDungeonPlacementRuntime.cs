using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Magenheim.Core.Underworld;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Verifies the positions Valheim actually pregenerated for runtime-ready Underworld dungeons.
/// Existing Underworld saves can predate newly admitted dungeon rows, so the authoritative server
/// gets one native GenerateLocationsTimeSliced recovery pass for any missing family. After that,
/// count, owning biome and same-family spacing are hard requirements rather than assumptions.
/// </summary>
internal sealed class UnderworldDungeonPlacementRuntime : MonoBehaviour
{
    internal enum PlacementState
    {
        WaitingForNativeGeneration,
        Reconciling,
        Healthy,
        Failed,
    }

    internal sealed record FamilyReport(
        string PrefabName,
        UnderworldTerrainBiome Biome,
        int Expected,
        int Found,
        double MinimumObservedSpacingMeters);

    private static readonly FieldInfo LocationsGeneratedField =
        AccessTools.Field(typeof(ZoneSystem), "m_locationsGenerated")
        ?? throw new MissingFieldException(typeof(ZoneSystem).FullName, "m_locationsGenerated");

    private static readonly MethodInfo GenerateLocationsMethod = ResolveGenerateLocationsMethod();

    private static ManualLogSource? _log;
    private static PlacementState _state = PlacementState.WaitingForNativeGeneration;
    private static string _diagnostic = "Native Underworld location generation has not completed.";
    private static IReadOnlyList<FamilyReport> _lastReport = Array.Empty<FamilyReport>();

    private ZoneSystem _zoneSystem = null!;
    private bool _running;
    private bool _finished;

    internal static PlacementState State => _state;
    internal static string Diagnostic => _diagnostic;
    internal static IReadOnlyList<FamilyReport> LastReport => _lastReport;

    internal static void Configure(ManualLogSource log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        ResetState();
    }

    internal static void ResetState()
    {
        _state = PlacementState.WaitingForNativeGeneration;
        _diagnostic = "Native Underworld location generation has not completed.";
        _lastReport = Array.Empty<FamilyReport>();
    }

    private void Awake()
    {
        _zoneSystem = GetComponent<ZoneSystem>()
            ?? throw new InvalidOperationException(
                "Underworld dungeon placement runtime requires the native Underworld ZoneSystem.");
    }

    private void Update()
    {
        if (_finished || _running || !_zoneSystem) return;
        if (!LocationsGenerated()) return;

        var audit = Analyze(_zoneSystem);
        if (audit.Invalid.Count != 0)
        {
            Fail("Underworld dungeon placement audit failed: " + string.Join("; ", audit.Invalid));
            return;
        }

        if (audit.Missing.Count == 0)
        {
            Pass(audit.Reports);
            return;
        }

        if (ZNet.instance is null || !ZNet.instance.IsServer())
        {
            _state = PlacementState.WaitingForNativeGeneration;
            _diagnostic =
                "Runtime-ready Underworld dungeon positions are missing locally; waiting for the authoritative server.";
            return;
        }

        _running = true;
        _state = PlacementState.Reconciling;
        _diagnostic =
            "Reconciling missing runtime-ready Underworld dungeon positions through native Valheim placement.";
        StartCoroutine(Reconcile(audit.Missing));
    }

    private IEnumerator Reconcile(IReadOnlyList<MissingFamily> missing)
    {
        try
        {
            foreach (var family in missing)
            {
                _log?.LogWarning(
                    $"Underworld dungeon '{family.Definition.DisplayName}' has {family.Found}/" +
                    $"{family.Definition.Quantity} pregenerated positions. Running one native " +
                    "GenerateLocationsTimeSliced recovery pass for the detached Underworld instance.");

                var generated = GenerateLocationsMethod.Invoke(
                    _zoneSystem,
                    new object[] { family.Location, Stopwatch.StartNew(), new ZPackage() }) as IEnumerator
                    ?? throw new InvalidOperationException(
                        "Valheim GenerateLocationsTimeSliced no longer returns IEnumerator.");

                while (generated.MoveNext())
                    yield return generated.Current;
            }

            var after = Analyze(_zoneSystem);
            if (after.Invalid.Count != 0)
            {
                Fail(
                    "Underworld dungeon placement reconciliation produced invalid positions: " +
                    string.Join("; ", after.Invalid));
                yield break;
            }

            if (after.Missing.Count != 0)
            {
                Fail(
                    "Underworld dungeon placement reconciliation could not reach requested quantities: " +
                    string.Join(
                        "; ",
                        after.Missing.Select(x =>
                            x.Definition.DisplayName + "=" + x.Found + "/" + x.Definition.Quantity)));
                yield break;
            }

            Pass(after.Reports);
        }
        catch (Exception exception)
        {
            Fail("Underworld dungeon placement reconciliation failed: " + exception);
        }
        finally
        {
            _running = false;
        }
    }

    private static AuditResult Analyze(ZoneSystem zoneSystem)
    {
        var reports = new List<FamilyReport>();
        var missing = new List<MissingFamily>();
        var invalid = new List<string>();

        foreach (var dungeon in UnderworldDungeonCatalog.All)
        {
            var locations = zoneSystem.m_locations
                .Where(x => x is not null &&
                            string.Equals(PrefabName(x), dungeon.PrefabName, StringComparison.Ordinal))
                .ToArray();
            var instances = zoneSystem.m_locationInstances.Values
                .Where(x => x.m_location is not null &&
                            string.Equals(PrefabName(x.m_location), dungeon.PrefabName, StringComparison.Ordinal))
                .ToArray();

            if (dungeon.Status != UnderworldDungeonStatus.RuntimeReady)
            {
                if (locations.Length != 0 || instances.Length != 0)
                    invalid.Add(
                        $"{dungeon.DisplayName} is Planned but detached worldgen contains " +
                        $"{locations.Length} catalog row(s) and {instances.Length} placement(s)");
                continue;
            }

            if (locations.Length != 1)
            {
                invalid.Add(
                    $"{dungeon.DisplayName} requires exactly one detached catalog row; found {locations.Length}");
                continue;
            }

            var expectedBiome = UnderworldTerrainRuntime.ToNativeBiome(dungeon.Biome);
            if (locations[0].m_biome != expectedBiome)
            {
                invalid.Add(
                    $"{dungeon.DisplayName} catalog biome mask {(int)locations[0].m_biome} " +
                    $"does not equal {(int)expectedBiome} ({dungeon.Biome})");
                continue;
            }

            foreach (var instance in instances)
            {
                var terrain = UnderworldTerrainRuntime.SampleForDiagnostics(
                    instance.m_position.x,
                    instance.m_position.z);
                if (!terrain.Admitted)
                {
                    invalid.Add(
                        $"{dungeon.DisplayName} placement at " +
                        $"({instance.m_position.x:0},{instance.m_position.z:0}) is outside the playable Underworld domain");
                    continue;
                }

                if (terrain.Biome != dungeon.Biome)
                    invalid.Add(
                        $"{dungeon.DisplayName} placement at " +
                        $"({instance.m_position.x:0},{instance.m_position.z:0}) samples {terrain.Biome}, " +
                        $"expected {dungeon.Biome}");
            }

            var minimumSpacing = MinimumSpacing(instances);
            if (instances.Length > 1 &&
                minimumSpacing + 1d < dungeon.MinDistanceFromSimilarMeters)
                invalid.Add(
                    $"{dungeon.DisplayName} minimum generated spacing is {minimumSpacing:0}m, " +
                    $"below required {dungeon.MinDistanceFromSimilarMeters:0}m");

            reports.Add(new FamilyReport(
                dungeon.PrefabName,
                dungeon.Biome,
                dungeon.Quantity,
                instances.Length,
                minimumSpacing));

            if (instances.Length != dungeon.Quantity)
                missing.Add(new MissingFamily(dungeon, locations[0], instances.Length));
        }

        return new AuditResult(
            reports.AsReadOnly(),
            missing.AsReadOnly(),
            invalid.AsReadOnly());
    }

    private bool LocationsGenerated()
    {
        var value = LocationsGeneratedField.GetValue(_zoneSystem);
        if (value is not bool generated)
            throw new InvalidOperationException(
                "ZoneSystem.m_locationsGenerated is no longer a bool.");
        return generated;
    }

    private static double MinimumSpacing(IReadOnlyList<LocationInstance> instances)
    {
        if (instances.Count < 2) return double.PositiveInfinity;
        var minimum = double.PositiveInfinity;
        for (var i = 0; i < instances.Count; i++)
        for (var j = i + 1; j < instances.Count; j++)
        {
            var dx = instances[i].m_position.x - instances[j].m_position.x;
            var dz = instances[i].m_position.z - instances[j].m_position.z;
            minimum = Math.Min(minimum, Math.Sqrt(dx * dx + dz * dz));
        }
        return minimum;
    }

    private static string PrefabName(ZoneSystem.ZoneLocation location)
    {
        if (location is null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(location.m_prefabName))
            return location.m_prefabName;
        return location.m_prefab.Name ?? string.Empty;
    }

    private static MethodInfo ResolveGenerateLocationsMethod()
    {
        var matches = typeof(ZoneSystem)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method =>
            {
                if (!string.Equals(
                        method.Name,
                        "GenerateLocationsTimeSliced",
                        StringComparison.Ordinal) ||
                    !typeof(IEnumerator).IsAssignableFrom(method.ReturnType))
                    return false;
                var parameters = method.GetParameters();
                return parameters.Length == 3 &&
                       parameters[0].ParameterType == typeof(ZoneSystem.ZoneLocation) &&
                       parameters[1].ParameterType == typeof(Stopwatch) &&
                       parameters[2].ParameterType == typeof(ZPackage);
            })
            .ToArray();

        if (matches.Length != 1)
            throw new MissingMethodException(
                typeof(ZoneSystem).FullName,
                "GenerateLocationsTimeSliced(ZoneLocation, Stopwatch, ZPackage)");

        return matches[0];
    }

    private void Pass(IReadOnlyList<FamilyReport> report)
    {
        _lastReport = report;
        _state = PlacementState.Healthy;
        _diagnostic = string.Join(
            "; ",
            report.Select(x =>
                $"{x.PrefabName}={x.Found}/{x.Expected}@{x.Biome}"));
        _finished = true;
        _log?.LogInfo(
            "Underworld dungeon placement audit PASS: " + _diagnostic);
    }

    private void Fail(string message)
    {
        _state = PlacementState.Failed;
        _diagnostic = message;
        _finished = true;
        _log?.LogError(message);
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(_zoneSystem, null)) return;
        ResetState();
    }

    private sealed record MissingFamily(
        UnderworldDungeonDefinition Definition,
        ZoneSystem.ZoneLocation Location,
        int Found);

    private sealed record AuditResult(
        IReadOnlyList<FamilyReport> Reports,
        IReadOnlyList<MissingFamily> Missing,
        IReadOnlyList<string> Invalid);
}
