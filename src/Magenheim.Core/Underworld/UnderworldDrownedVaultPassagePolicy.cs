using System;

namespace Magenheim.Core.Underworld;

public sealed record UnderworldDrownedVaultPassageState(
    UnderworldDrownedVaultRouteMode RouteMode,
    double WaterDepthMeters,
    bool HasDryLedge)
{
    public bool HasWater => WaterDepthMeters > 0d;

    public void Validate()
    {
        if (RouteMode is not (
                UnderworldDrownedVaultRouteMode.Dry or
                UnderworldDrownedVaultRouteMode.Mixed or
                UnderworldDrownedVaultRouteMode.Flooded))
            throw new InvalidOperationException(
                "Drowned Vault passage mode must resolve to Dry, Mixed or Flooded.");
        if (double.IsNaN(WaterDepthMeters) ||
            double.IsInfinity(WaterDepthMeters) ||
            WaterDepthMeters < 0d ||
            WaterDepthMeters > UnderworldDrownedVaultPassagePolicy.MaximumPassageWaterDepthMeters)
            throw new InvalidOperationException("Drowned Vault passage water depth is invalid.");
        if (RouteMode == UnderworldDrownedVaultRouteMode.Dry && WaterDepthMeters != 0d)
            throw new InvalidOperationException("Dry Drowned Vault passages cannot contain water.");
        if (RouteMode != UnderworldDrownedVaultRouteMode.Dry && WaterDepthMeters <= 0d)
            throw new InvalidOperationException("Wet Drowned Vault passages require positive water depth.");
        if (RouteMode == UnderworldDrownedVaultRouteMode.Flooded && HasDryLedge)
            throw new InvalidOperationException("Fully flooded Drowned Vault passages cannot advertise a dry ledge.");
        if (RouteMode == UnderworldDrownedVaultRouteMode.Mixed && !HasDryLedge)
            throw new InvalidOperationException("Mixed Drowned Vault passages require the authored side ledge.");
    }
}

/// <summary>
/// Deterministic transition authority for the single adaptive Drowned Vault passage model.
/// The model owns a 3m channel below the Z=0 waterline plus raised side ledges; this policy decides
/// whether a connection leaves the channel dry, fills it shallowly, or floods the whole route.
/// </summary>
public static class UnderworldDrownedVaultPassagePolicy
{
    public const double MixedWaterDepthMeters = 1.6d;
    public const double FloodedWaterDepthMeters = 3.0d;
    public const double MaximumPassageWaterDepthMeters = 3.0d;

    public static UnderworldDrownedVaultPassageState Resolve(
        UnderworldDrownedVaultRoomDefinition from,
        UnderworldDrownedVaultRoomDefinition to)
    {
        if (from is null) throw new ArgumentNullException(nameof(from));
        if (to is null) throw new ArgumentNullException(nameof(to));
        from.Validate();
        to.Validate();

        if (from.RouteMode == UnderworldDrownedVaultRouteMode.Dry &&
            to.RouteMode == UnderworldDrownedVaultRouteMode.Dry)
            return Valid(new(
                UnderworldDrownedVaultRouteMode.Dry,
                0d,
                HasDryLedge: true));

        if (RequiresSubmergedPassage(from.RouteMode) &&
            RequiresSubmergedPassage(to.RouteMode))
            return Valid(new(
                UnderworldDrownedVaultRouteMode.Flooded,
                FloodedWaterDepthMeters,
                HasDryLedge: false));

        return Valid(new(
            UnderworldDrownedVaultRouteMode.Mixed,
            MixedWaterDepthMeters,
            HasDryLedge: true));
    }

    private static bool RequiresSubmergedPassage(UnderworldDrownedVaultRouteMode mode) =>
        mode is UnderworldDrownedVaultRouteMode.Flooded or
            UnderworldDrownedVaultRouteMode.VerticalWater;

    private static UnderworldDrownedVaultPassageState Valid(
        UnderworldDrownedVaultPassageState state)
    {
        state.Validate();
        return state;
    }
}
