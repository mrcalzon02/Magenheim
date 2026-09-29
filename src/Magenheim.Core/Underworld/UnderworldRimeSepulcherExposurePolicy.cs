using System;

namespace Magenheim.Core.Underworld;

public readonly record struct UnderworldRimeSepulcherExposureState(
    UnderworldAtmosphereEvent Event,
    double EventIntensity01,
    double HazardFloor01,
    bool RoomWide)
{
    public void Validate()
    {
        if (!UnderworldAtmosphere.EventApplies(
                UnderworldTerrainBiome.FrozenCaverns,
                Event))
            throw new InvalidOperationException(
                "Rime Sepulcher exposure must use a Frozen-compatible atmosphere event.");
        Unit(EventIntensity01, nameof(EventIntensity01));
        Unit(HazardFloor01, nameof(HazardFloor01));
    }

    private static void Unit(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) ||
            value < 0d || value > 1d)
            throw new InvalidOperationException(name + " must be finite in 0..1.");
    }
}

/// <summary>
/// Maps authored Rime Sepulcher route semantics onto the existing Frozen atmosphere authority.
/// Clear/shelter routes deliberately suppress global Whiteout locally; pressure routes reuse
/// Deep Fog or Whiteout and raise the hazard floor consumed by the normal resistance pipeline.
/// </summary>
public static class UnderworldRimeSepulcherExposurePolicy
{
    public static UnderworldRimeSepulcherExposureState Resolve(
        UnderworldRimeSepulcherRoomDefinition definition)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        definition.Validate();

        UnderworldRimeSepulcherExposureState state;
        switch (definition.Route)
        {
            case UnderworldRimeSepulcherRoute.Shelter:
                state = new(
                    UnderworldAtmosphereEvent.None,
                    0d,
                    Math.Min(.06d, definition.ColdExposure01),
                    RoomWide: true);
                break;

            case UnderworldRimeSepulcherRoute.ClearGallery:
                state = new(
                    UnderworldAtmosphereEvent.None,
                    0d,
                    Math.Max(.08d, definition.ColdExposure01 * .55d),
                    RoomWide: true);
                break;

            case UnderworldRimeSepulcherRoute.FrostField:
                state = new(
                    UnderworldAtmosphereEvent.DeepFog,
                    Math.Max(.10d, definition.WhiteoutIntensity01 * .55d),
                    definition.ColdExposure01,
                    RoomWide: false);
                break;

            case UnderworldRimeSepulcherRoute.WhiteoutChoke:
                state = new(
                    UnderworldAtmosphereEvent.Whiteout,
                    definition.WhiteoutIntensity01,
                    definition.ColdExposure01,
                    RoomWide: false);
                break;

            case UnderworldRimeSepulcherRoute.IceShear:
                state = new(
                    UnderworldAtmosphereEvent.DeepFog,
                    Math.Max(.18d, definition.WhiteoutIntensity01),
                    definition.ColdExposure01,
                    RoomWide: false);
                break;

            default:
                throw new InvalidOperationException(
                    "Unknown Rime Sepulcher route " + definition.Route);
        }

        state.Validate();
        return state;
    }

    public static UnderworldRimeSepulcherExposureState ResolvePassage(
        UnderworldRimeSepulcherRoomDefinition from,
        UnderworldRimeSepulcherRoomDefinition to)
    {
        if (from is null) throw new ArgumentNullException(nameof(from));
        if (to is null) throw new ArgumentNullException(nameof(to));
        from.Validate(); to.Validate();

        var maxCold = Math.Max(from.ColdExposure01, to.ColdExposure01);
        var maxWhiteout = Math.Max(from.WhiteoutIntensity01, to.WhiteoutIntensity01);

        if (from.Route == UnderworldRimeSepulcherRoute.Shelter &&
            to.Route == UnderworldRimeSepulcherRoute.Shelter)
            return new UnderworldRimeSepulcherExposureState(
                UnderworldAtmosphereEvent.None, 0d, .05d, true);

        if (from.Route == UnderworldRimeSepulcherRoute.WhiteoutChoke ||
            to.Route == UnderworldRimeSepulcherRoute.WhiteoutChoke)
            return Validated(new(
                UnderworldAtmosphereEvent.Whiteout,
                Math.Max(.55d, maxWhiteout),
                Math.Max(.42d, maxCold),
                false));

        if (from.Route == UnderworldRimeSepulcherRoute.IceShear ||
            to.Route == UnderworldRimeSepulcherRoute.IceShear)
            return Validated(new(
                UnderworldAtmosphereEvent.DeepFog,
                Math.Max(.20d, maxWhiteout),
                Math.Max(.48d, maxCold),
                false));

        if (from.Route == UnderworldRimeSepulcherRoute.FrostField ||
            to.Route == UnderworldRimeSepulcherRoute.FrostField)
            return Validated(new(
                UnderworldAtmosphereEvent.DeepFog,
                Math.Max(.10d, maxWhiteout * .55d),
                Math.Max(.36d, maxCold),
                false));

        return Validated(new(
            UnderworldAtmosphereEvent.None,
            0d,
            Math.Max(.08d, maxCold * .55d),
            true));
    }

    private static UnderworldRimeSepulcherExposureState Validated(
        UnderworldRimeSepulcherExposureState state)
    {
        state.Validate();
        return state;
    }
}
