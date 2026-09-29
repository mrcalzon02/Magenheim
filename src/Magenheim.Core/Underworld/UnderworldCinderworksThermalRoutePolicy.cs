using System;

namespace Magenheim.Core.Underworld;

public sealed record UnderworldCinderworksThermalRouteState(
    bool HasHazard,
    string? HazardId,
    float Intensity,
    double ActiveSeconds,
    double PeriodSeconds)
{
    public bool Cycles => HasHazard && PeriodSeconds > 0d && ActiveSeconds < PeriodSeconds;

    public void Validate()
    {
        if (!HasHazard)
        {
            if (HazardId is not null || Intensity != 0f || ActiveSeconds != 0d || PeriodSeconds != 0d)
                throw new InvalidOperationException("Hazard-free Cinderworks routes must be completely neutral.");
            return;
        }

        if (!UnderworldGeothermalHazard.IsCanonical(HazardId))
            throw new InvalidOperationException("Cinderworks route must use a canonical geothermal hazard.");
        if (float.IsNaN(Intensity) || float.IsInfinity(Intensity) || Intensity <= 0f ||
            Intensity > UnderworldGeothermalHazard.MaximumIntensity)
            throw new InvalidOperationException("Cinderworks hazard intensity is outside the canonical range.");
        if (double.IsNaN(ActiveSeconds) || double.IsInfinity(ActiveSeconds) ||
            double.IsNaN(PeriodSeconds) || double.IsInfinity(PeriodSeconds) ||
            ActiveSeconds <= 0d || PeriodSeconds <= 0d || ActiveSeconds > PeriodSeconds)
            throw new InvalidOperationException("Cinderworks hazard timing is invalid.");
    }
}

/// <summary>
/// Converts authored Cinderworks route pressure into the existing geothermal gameplay vocabulary.
/// No dungeon-only heat state exists: runtime adapters feed these canonical samples into
/// UnderworldGeothermalHazardVolume, which already owns network state, Furnace Blood mitigation,
/// Thermal Surge amplification and passive recovery.
/// </summary>
public static class UnderworldCinderworksThermalRoutePolicy
{
    public const double ContinuousPeriodSeconds = 1d;

    public static UnderworldCinderworksThermalRouteState Resolve(
        UnderworldCinderworksRoomDefinition definition)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        definition.Validate();

        var pressure = (float)definition.ThermalPressure01;
        UnderworldCinderworksThermalRouteState state;
        switch (definition.HeatRoute)
        {
            case UnderworldCinderworksHeatRoute.Safe:
                state = new(false, null, 0f, 0d, 0d);
                break;

            case UnderworldCinderworksHeatRoute.Warm:
                state = Continuous(
                    UnderworldGeothermalHazard.VentField,
                    0.28f + pressure * 0.65f);
                break;

            case UnderworldCinderworksHeatRoute.Hot:
                state = Continuous(
                    UnderworldGeothermalHazard.VentField,
                    0.48f + pressure * 0.95f);
                break;

            case UnderworldCinderworksHeatRoute.VentCycle:
                state = new(
                    true,
                    UnderworldGeothermalHazard.VentField,
                    Clamp(0.75f + pressure * 1.05f, 0.1f, 1.8f),
                    ActiveSeconds: 3.5d + pressure * 2.5d,
                    PeriodSeconds: 9d);
                break;

            case UnderworldCinderworksHeatRoute.SlagChannel:
                state = Continuous(
                    UnderworldGeothermalHazard.LavaChannel,
                    0.55f + pressure * 0.85f);
                break;

            default:
                throw new InvalidOperationException(
                    "Unknown Cinderworks heat route " + definition.HeatRoute);
        }

        state.Validate();
        return state;
    }

    public static bool IsActive(
        UnderworldCinderworksThermalRouteState state,
        double worldSeconds,
        int phaseSeed)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        state.Validate();
        if (!state.HasHazard) return false;
        if (!state.Cycles) return true;
        if (double.IsNaN(worldSeconds) || double.IsInfinity(worldSeconds))
            return false;

        var period = state.PeriodSeconds;
        var phase = PositiveModulo(
            unchecked((uint)phaseSeed) * 0.6180339887498949d,
            period);
        var time = PositiveModulo(worldSeconds + phase, period);
        return time < state.ActiveSeconds;
    }

    private static UnderworldCinderworksThermalRouteState Continuous(
        string hazard,
        float intensity) =>
        new(
            true,
            hazard,
            Clamp(intensity, 0.1f, 1.8f),
            ContinuousPeriodSeconds,
            ContinuousPeriodSeconds);

    private static double PositiveModulo(double value, double modulus)
    {
        var result = value % modulus;
        return result < 0d ? result + modulus : result;
    }

    private static float Clamp(float value, float minimum, float maximum) =>
        value < minimum ? minimum : value > maximum ? maximum : value;
}
