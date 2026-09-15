using System;

namespace Magenheim.Core.DeepFractures;

public sealed record DeepFractureExpeditionScalePolicy(
    int SmallWeight,
    int FullWeight,
    int GrandWeight)
{
    public static DeepFractureExpeditionScalePolicy Default => new(
        SmallWeight: 55,
        FullWeight: 35,
        GrandWeight: 10);

    public void Validate()
    {
        if (SmallWeight < 0 || FullWeight < 0 || GrandWeight < 0)
            throw new InvalidOperationException("Deep Fracture scale weights cannot be negative.");
        if (SmallWeight + FullWeight + GrandWeight <= 0)
            throw new InvalidOperationException("Deep Fracture scale selection requires at least one positive weight.");
    }
}

public sealed record DeepFractureExpeditionPlan(
    int Seed,
    DeepFractureScale Scale,
    DeepFractureDungeonPlan Dungeon,
    DeepFractureEncounterPlan Encounters,
    DeepFractureInteriorBlueprint Interior);

/// <summary>
/// Single deterministic runtime entrypoint for a Deep Fracture expedition. Runtime code supplies
/// only the Valheim dungeon seed; this planner owns scale selection, topology, encounters and the
/// interior projection so no Unity/Jotunn layer can independently reroll authoritative state.
/// </summary>
public static class DeepFractureExpeditionPlanner
{
    private const uint ScaleStreamSalt = 0xD33F5CA1u;

    public static DeepFractureExpeditionPlan Build(
        int seed,
        DeepFractureExpeditionScalePolicy? scalePolicyOverride = null,
        DeepFractureGenerationPolicy? generationPolicyOverride = null,
        DeepFractureConnectionPolicy? connectionPolicyOverride = null,
        DeepFractureEncounterPolicy? encounterPolicyOverride = null,
        DeepFractureInteriorProjectionPolicy? projectionPolicyOverride = null)
    {
        var scalePolicy = scalePolicyOverride ?? DeepFractureExpeditionScalePolicy.Default;
        scalePolicy.Validate();

        var scale = SelectScale(seed, scalePolicy);
        return BuildForScale(
            seed,
            scale,
            generationPolicyOverride,
            connectionPolicyOverride,
            encounterPolicyOverride,
            projectionPolicyOverride);
    }

    public static DeepFractureExpeditionPlan BuildForScale(
        int seed,
        DeepFractureScale scale,
        DeepFractureGenerationPolicy? generationPolicyOverride = null,
        DeepFractureConnectionPolicy? connectionPolicyOverride = null,
        DeepFractureEncounterPolicy? encounterPolicyOverride = null,
        DeepFractureInteriorProjectionPolicy? projectionPolicyOverride = null)
    {
        _ = DeepFractureCatalog.GetScaleRule(scale);

        var dungeon = DeepFractureLayoutPlanner.Build(new DeepFractureGenerationRequest(
            scale,
            seed,
            generationPolicyOverride,
            connectionPolicyOverride));
        var encounters = DeepFractureEncounterPlanner.Build(dungeon, encounterPolicyOverride);
        var interior = DeepFractureInteriorBlueprintCompiler.Build(dungeon, encounters, projectionPolicyOverride);

        return new DeepFractureExpeditionPlan(seed, scale, dungeon, encounters, interior);
    }

    public static DeepFractureScale SelectScale(
        int seed,
        DeepFractureExpeditionScalePolicy? policyOverride = null)
    {
        var policy = policyOverride ?? DeepFractureExpeditionScalePolicy.Default;
        policy.Validate();

        var total = checked(policy.SmallWeight + policy.FullWeight + policy.GrandWeight);
        var random = new DeterministicSequence(seed, ScaleStreamSalt);
        var roll = random.NextInt(total);

        if (roll < policy.SmallWeight)
            return DeepFractureScale.Small;
        roll -= policy.SmallWeight;
        if (roll < policy.FullWeight)
            return DeepFractureScale.Full;
        return DeepFractureScale.Grand;
    }

    /// <summary>
    /// Conservative horizontal radius for any supported expedition under the supplied projection
    /// policy. This is useful to size runtime interior-environment volumes without generating a
    /// second speculative dungeon plan.
    /// </summary>
    public static double MaximumSupportedInteriorRadius(
        DeepFractureInteriorProjectionPolicy? projectionPolicyOverride = null)
    {
        var policy = projectionPolicyOverride ?? DeepFractureInteriorProjectionPolicy.Default;
        policy.Validate();

        var maximum = 0d;
        foreach (DeepFractureScale scale in Enum.GetValues(typeof(DeepFractureScale)))
        {
            var rule = DeepFractureCatalog.GetScaleRule(scale);
            var columns = policy.GetColumns(scale);
            var rows = (rule.MaximumMajorModules + columns - 1) / columns;
            var maxX = (columns - 1) * policy.GridSpacing;
            var maxZ = Math.Max(0, rows - 1) * policy.GridSpacing;
            var radius = Math.Sqrt((maxX * maxX) + (maxZ * maxZ))
                + (policy.ModuleFootprint * 0.5d)
                + policy.BoundaryPadding;
            maximum = Math.Max(maximum, radius);
        }

        return maximum;
    }
}
