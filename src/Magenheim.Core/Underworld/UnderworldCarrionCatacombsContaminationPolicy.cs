using System;

namespace Magenheim.Core.Underworld;

public readonly record struct UnderworldCarrionCatacombsExposureState(
    UnderworldAtmosphereEvent Event,
    double EventIntensity01,
    double HazardFloor01,
    bool RoomWide)
{
    public void Validate()
    {
        if (!UnderworldAtmosphere.EventApplies(UnderworldTerrainBiome.GreatDecay, Event))
            throw new InvalidOperationException(
                "Carrion Catacombs exposure must use a Great Decay-compatible atmosphere event.");
        Unit(EventIntensity01, nameof(EventIntensity01));
        Unit(HazardFloor01, nameof(HazardFloor01));
    }

    private static void Unit(double value,string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1d)
            throw new InvalidOperationException(name + " must be finite in 0..1.");
    }
}

/// <summary>
/// Converts authored Carrion route semantics into the existing Great Decay atmosphere. Resistance
/// and local Censer suppression are still resolved by the normal runtime, so this policy cannot
/// invent immunity or a parallel contamination meter.
/// </summary>
public static class UnderworldCarrionCatacombsContaminationPolicy
{
    public static UnderworldCarrionCatacombsExposureState Resolve(
        UnderworldCarrionCatacombsRoomDefinition definition)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        definition.Validate();

        return definition.Route switch
        {
            UnderworldCarrionCatacombsRoute.Sanctuary =>
                Validated(new(UnderworldAtmosphereEvent.None,0d,
                    Math.Min(.10d,definition.Contamination01),true)),
            UnderworldCarrionCatacombsRoute.PreservedRuin =>
                Validated(new(UnderworldAtmosphereEvent.None,0d,
                    Math.Max(.12d,definition.Contamination01*.60d),true)),
            UnderworldCarrionCatacombsRoute.TaintedRuin =>
                Validated(new(UnderworldAtmosphereEvent.None,0d,
                    definition.Contamination01,false)),
            UnderworldCarrionCatacombsRoute.RootIngress =>
                Validated(new(UnderworldAtmosphereEvent.BlackBloom,
                    Math.Max(.20d,definition.BlackBloomIntensity01),
                    definition.Contamination01,false)),
            UnderworldCarrionCatacombsRoute.BlackBloom =>
                Validated(new(UnderworldAtmosphereEvent.BlackBloom,
                    definition.BlackBloomIntensity01,
                    definition.Contamination01,false)),
            _ => throw new InvalidOperationException(
                "Unknown Carrion Catacombs route " + definition.Route),
        };
    }

    public static UnderworldCarrionCatacombsExposureState ResolvePassage(
        UnderworldCarrionCatacombsRoomDefinition from,
        UnderworldCarrionCatacombsRoomDefinition to)
    {
        if (from is null) throw new ArgumentNullException(nameof(from));
        if (to is null) throw new ArgumentNullException(nameof(to));
        from.Validate();
        to.Validate();

        var contamination=Math.Max(from.Contamination01,to.Contamination01);
        var bloom=Math.Max(from.BlackBloomIntensity01,to.BlackBloomIntensity01);

        if (from.Route==UnderworldCarrionCatacombsRoute.Sanctuary &&
            to.Route==UnderworldCarrionCatacombsRoute.Sanctuary)
            return Validated(new(UnderworldAtmosphereEvent.None,0d,.08d,true));

        if (from.Route==UnderworldCarrionCatacombsRoute.BlackBloom ||
            to.Route==UnderworldCarrionCatacombsRoute.BlackBloom)
            return Validated(new(
                UnderworldAtmosphereEvent.BlackBloom,
                Math.Max(.58d,bloom),
                Math.Max(.52d,contamination),
                false));

        if (from.Route==UnderworldCarrionCatacombsRoute.RootIngress ||
            to.Route==UnderworldCarrionCatacombsRoute.RootIngress)
            return Validated(new(
                UnderworldAtmosphereEvent.BlackBloom,
                Math.Max(.20d,bloom),
                Math.Max(.42d,contamination),
                false));

        if (from.Route==UnderworldCarrionCatacombsRoute.TaintedRuin ||
            to.Route==UnderworldCarrionCatacombsRoute.TaintedRuin)
            return Validated(new(
                UnderworldAtmosphereEvent.None,0d,
                Math.Max(.28d,contamination),
                false));

        return Validated(new(
            UnderworldAtmosphereEvent.None,0d,
            Math.Max(.12d,contamination*.60d),
            true));
    }

    private static UnderworldCarrionCatacombsExposureState Validated(
        UnderworldCarrionCatacombsExposureState state)
    {
        state.Validate();
        return state;
    }
}
