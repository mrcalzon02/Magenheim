using System;
using System.Linq;
using BepInEx.Logging;
using Jotunn.Entities;
using Jotunn.Managers;
using Magenheim.Core.DeepFractures;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Additive Jotunn location boundary for Deep Fracture entrances. The core owns location intent
/// and collision planning. This registrar translates approved additions only and requires a real
/// interior binder before it will add a surface location, preventing dead cave mouths from being
/// injected into worlds.
/// </summary>
internal sealed class DeepFractureLocationRegistrar : IDisposable
{
    private readonly DeepFractureLocationDefinition _definition;
    private readonly IDeepFractureInteriorBinder _interiorBinder;
    private readonly ManualLogSource _log;
    private bool _subscribed;
    private bool _registered;

    internal DeepFractureLocationRegistrar(
        IDeepFractureInteriorBinder interiorBinder,
        ManualLogSource log,
        DeepFractureLocationDefinition? definition = null)
    {
        _interiorBinder = interiorBinder ?? throw new ArgumentNullException(nameof(interiorBinder));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _definition = definition ?? DeepFractureLocationCatalog.SurfaceFractureEntrance;
        _definition.Validate();
    }

    internal void Register()
    {
        if (_subscribed || _registered)
            return;

        ZoneManager.OnVanillaLocationsAvailable += OnVanillaLocationsAvailable;
        _subscribed = true;
    }

    public void Dispose()
    {
        if (!_subscribed)
            return;

        ZoneManager.OnVanillaLocationsAvailable -= OnVanillaLocationsAvailable;
        _subscribed = false;
    }

    private void OnVanillaLocationsAvailable()
    {
        if (_registered)
            return;

        try
        {
            var desired = new[] { _definition };
            var observed = JotunnDeepFractureLocationAdapter.ObserveDesiredHostIdentities(desired);
            var plan = DeepFractureLocationRegistrationPlanner.Build(desired, observed);

            if (plan.HasErrors)
            {
                var diagnostics = plan.Entries
                    .Where(entry => entry.Action == DeepFractureLocationRegistrationAction.Error)
                    .Select(entry => $"{entry.Desired.RegistrationKey}: {entry.Diagnostic}");
                throw new InvalidOperationException(
                    "Deep Fracture location compatibility planning rejected registration: " + string.Join("; ", diagnostics));
            }

            var skipped = plan.Entries.SingleOrDefault(entry => entry.Action == DeepFractureLocationRegistrationAction.Skip);
            if (skipped is not null)
            {
                _registered = true;
                _log.LogWarning(
                    $"Skipped Deep Fracture location '{skipped.Desired.RegistrationKey}' / '{skipped.Desired.PrefabName}': {skipped.Diagnostic}");
                return;
            }

            var addition = plan.Additions.SingleOrDefault()
                ?? throw new InvalidOperationException("Deep Fracture location plan produced neither an addition nor a skip result.");

            PreflightIdentity(addition.Desired);
            RegisterLocation(addition.Desired);
            _registered = true;

            _log.LogInfo(
                $"Registered additive Deep Fracture entrance location '{addition.Desired.PrefabName}' across " +
                $"{addition.Desired.Biomes.Count} land biomes with quantity {addition.Desired.Quantity} and " +
                $"minimum similar-location spacing {addition.Desired.MinDistanceFromSimilar:0.#}m.");
        }
        catch (Exception exception)
        {
            _log.LogError($"Deep Fracture location registration failed: {exception}");
            throw;
        }
        finally
        {
            Dispose();
        }
    }

    private static void PreflightIdentity(DeepFractureLocationDefinition definition)
    {
        if (ZoneManager.Instance.GetZoneLocation(definition.PrefabName) is not null
            || CustomLocation.IsCustomLocation(definition.PrefabName))
        {
            throw new InvalidOperationException(
                $"Deep Fracture location prefab identity '{definition.PrefabName}' became occupied after compatibility planning. " +
                "Magenheim will not replace or modify the existing host location.");
        }
    }

    private void RegisterLocation(DeepFractureLocationDefinition definition)
    {
        var locationContainer = ZoneManager.Instance.CreateLocationContainer(definition.PrefabName)
            ?? throw new InvalidOperationException($"Jotunn could not create location container '{definition.PrefabName}'.");

        DeepFractureEntranceVisuals.Build(locationContainer);

        var anchor = FindInteriorAnchor(locationContainer)
            ?? throw new InvalidOperationException(
                $"Generated Deep Fracture entrance '{definition.PrefabName}' is missing required interior anchor '{DeepFractureEntranceVisuals.InteriorAnchorName}'.");

        var interior = _interiorBinder.AttachInterior(locationContainer);
        interior.Validate();

        if (FindInteriorAnchor(locationContainer) != anchor)
        {
            throw new InvalidOperationException(
                "Deep Fracture interior binding replaced the authoritative entrance anchor. " +
                "Interior binders must attach to the existing anchor rather than replacing the surface-location authority.");
        }

        var config = JotunnDeepFractureLocationAdapter.BuildLocationConfig(definition, interior);
        var customLocation = new CustomLocation(locationContainer, fixReference: false, config);

        if (!ZoneManager.Instance.AddCustomLocation(customLocation))
        {
            throw new InvalidOperationException(
                $"Jotunn refused additive Deep Fracture location registration for '{definition.PrefabName}'. Existing host locations were not modified.");
        }
    }

    private static Transform? FindInteriorAnchor(GameObject locationContainer)
    {
        foreach (var transform in locationContainer.GetComponentsInChildren<Transform>(includeInactive: true))
        {
            if (string.Equals(transform.name, DeepFractureEntranceVisuals.InteriorAnchorName, StringComparison.Ordinal))
                return transform;
        }

        return null;
    }
}
