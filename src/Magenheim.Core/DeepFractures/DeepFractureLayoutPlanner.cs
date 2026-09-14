using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.DeepFractures;

public sealed record DeepFractureGenerationPolicy(
    double UpperFractureFraction,
    double MiddleWorksFraction,
    double DeepDomainsFraction,
    double UpperElementChance,
    double MiddleElementChance,
    double MixedDeepElementChance,
    int HeartElementCount,
    IReadOnlyList<ElementalAlignment> DomainAlignments)
{
    public static DeepFractureGenerationPolicy Default => new(
        0.20d,
        0.30d,
        0.50d,
        0.20d,
        0.55d,
        0.10d,
        3,
        new[]
        {
            ElementalAlignment.Earth,
            ElementalAlignment.Fire,
            ElementalAlignment.Frost,
            ElementalAlignment.Storm,
            ElementalAlignment.Venom,
            ElementalAlignment.Radiance,
            ElementalAlignment.Spirit,
        });

    public void Validate()
    {
        ValidateFraction(UpperFractureFraction, nameof(UpperFractureFraction));
        ValidateFraction(MiddleWorksFraction, nameof(MiddleWorksFraction));
        ValidateFraction(DeepDomainsFraction, nameof(DeepDomainsFraction));
        ValidateFraction(UpperElementChance, nameof(UpperElementChance));
        ValidateFraction(MiddleElementChance, nameof(MiddleElementChance));
        ValidateFraction(MixedDeepElementChance, nameof(MixedDeepElementChance));

        var bandTotal = UpperFractureFraction + MiddleWorksFraction + DeepDomainsFraction;
        if (Math.Abs(bandTotal - 1d) > 0.0000001d)
            throw new InvalidOperationException("Deep Fracture depth-band fractions must sum to exactly 1.0.");

        if (DomainAlignments is null || DomainAlignments.Count < 2)
            throw new InvalidOperationException("Deep Fracture generation requires at least two available domain alignments.");

        if (DomainAlignments.Distinct().Count() != DomainAlignments.Count)
            throw new InvalidOperationException("Deep Fracture domain alignments must be unique.");

        if (HeartElementCount < 2 || HeartElementCount > DomainAlignments.Count)
            throw new InvalidOperationException("Confluence Heart elemental count must be at least two and cannot exceed available domain alignments.");
    }

    private static void ValidateFraction(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1d)
            throw new InvalidOperationException($"{name} must be finite and between 0 and 1 inclusive.");
    }
}

public sealed record DeepFractureGenerationRequest(
    DeepFractureScale Scale,
    int Seed,
    DeepFractureGenerationPolicy? Policy = null,
    DeepFractureConnectionPolicy? ConnectionPolicy = null);

public static class DeepFractureLayoutPlanner
{
    private static readonly OccupationProfile[] UpperOccupations =
    {
        OccupationProfile.Empty,
        OccupationProfile.WispRoaming,
        OccupationProfile.CrawlerColony,
        OccupationProfile.ShardlingFeeding,
    };

    private static readonly OccupationProfile[] MiddleOccupations =
    {
        OccupationProfile.Empty,
        OccupationProfile.ShardlingFeeding,
        OccupationProfile.RevenantHold,
        OccupationProfile.SentryDefense,
        OccupationProfile.GuardianObjective,
    };

    private static readonly OccupationProfile[] DeepOccupations =
    {
        OccupationProfile.Empty,
        OccupationProfile.CrawlerColony,
        OccupationProfile.ShardlingFeeding,
        OccupationProfile.RevenantHold,
        OccupationProfile.SentryDefense,
        OccupationProfile.GuardianObjective,
        OccupationProfile.GolemLair,
        OccupationProfile.Mixed,
    };

    private static readonly ResourceState[] UpperResources =
    {
        ResourceState.Sparse,
        ResourceState.Normal,
        ResourceState.Normal,
        ResourceState.Rich,
    };

    private static readonly ResourceState[] MiddleResources =
    {
        ResourceState.Normal,
        ResourceState.Rich,
        ResourceState.AncientInfrastructure,
    };

    private static readonly ResourceState[] DeepResources =
    {
        ResourceState.Normal,
        ResourceState.Rich,
        ResourceState.Rich,
        ResourceState.GreaterGeode,
        ResourceState.AncientInfrastructure,
    };

    public static DeepFractureDungeonPlan Build(DeepFractureGenerationRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        DeepFractureCatalog.ValidateDefaults();
        var policy = request.Policy ?? DeepFractureGenerationPolicy.Default;
        policy.Validate();

        var random = new DeterministicSequence(request.Seed);
        var scaleRule = DeepFractureCatalog.GetScaleRule(request.Scale);
        var moduleCount = scaleRule.MinimumMajorModules + random.NextInt(scaleRule.MaximumMajorModules - scaleRule.MinimumMajorModules + 1);
        var bandCounts = AllocateBands(moduleCount - 2, policy);
        var pieceIndex = DeepFractureCatalog.CreatePieceIndex();
        var reuseCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var modules = new List<DungeonModuleState>(moduleCount);

        AddModule(modules, pieceIndex["DF-01"], DungeonDepthBand.UpperFracture, request.Seed, random, policy, reuseCounts);
        AddBandModules(modules, DungeonDepthBand.UpperFracture, bandCounts.Upper, request.Seed, random, policy, reuseCounts);
        AddBandModules(modules, DungeonDepthBand.MiddleWorks, bandCounts.Middle, request.Seed, random, policy, reuseCounts);
        AddBandModules(modules, DungeonDepthBand.DeepDomains, bandCounts.Deep, request.Seed, random, policy, reuseCounts);
        AddModule(modules, pieceIndex["DF-20"], DungeonDepthBand.Heart, request.Seed, random, policy, reuseCounts);

        var structuralPlan = new DeepFractureDungeonPlan(
            request.Scale,
            request.Seed,
            modules.ToArray(),
            Array.Empty<DungeonConnection>());

        var plan = DeepFractureConnectionPlanner.Attach(structuralPlan, request.ConnectionPolicy);
        var validation = DeepFracturePlanValidator.Validate(plan);
        if (!validation.IsValid)
            throw new InvalidOperationException("Generated Deep Fracture plan failed validation: " + string.Join(" | ", validation.Errors));

        return plan;
    }

    private static void AddBandModules(
        List<DungeonModuleState> modules,
        DungeonDepthBand band,
        int count,
        int seed,
        DeterministicSequence random,
        DeepFractureGenerationPolicy policy,
        Dictionary<string, int> reuseCounts)
    {
        for (var index = 0; index < count; index++)
        {
            var previousPieceId = modules.Count == 0 ? null : modules[modules.Count - 1].PieceFamilyId;
            var piece = SelectPiece(band, previousPieceId, random, reuseCounts);
            AddModule(modules, piece, band, seed, random, policy, reuseCounts);
        }
    }

    private static DeepFracturePieceFamily SelectPiece(
        DungeonDepthBand band,
        string? previousPieceId,
        DeterministicSequence random,
        IReadOnlyDictionary<string, int> reuseCounts)
    {
        var candidates = DeepFractureCatalog.PieceFamilies
            .Where(piece => piece.Id != "DF-01" && piece.Id != "DF-20")
            .Where(piece => piece.AllowedBands.Contains(band))
            .Where(piece => !reuseCounts.TryGetValue(piece.Id, out var used) || used < piece.MaximumRecommendedReuse)
            .OrderBy(piece => piece.Id, StringComparer.Ordinal)
            .ToArray();

        if (candidates.Length == 0)
            throw new InvalidOperationException($"No Deep Fracture piece remains available for depth band {band} without exceeding reuse limits.");

        if (candidates.Length > 1 && previousPieceId is not null)
        {
            var nonRepeating = candidates.Where(piece => !string.Equals(piece.Id, previousPieceId, StringComparison.Ordinal)).ToArray();
            if (nonRepeating.Length > 0)
                candidates = nonRepeating;
        }

        return candidates[random.NextInt(candidates.Length)];
    }

    private static void AddModule(
        List<DungeonModuleState> modules,
        DeepFracturePieceFamily piece,
        DungeonDepthBand band,
        int seed,
        DeterministicSequence random,
        DeepFractureGenerationPolicy policy,
        Dictionary<string, int> reuseCounts)
    {
        reuseCounts.TryGetValue(piece.Id, out var reuseCount);
        reuseCounts[piece.Id] = reuseCount + 1;

        var moduleIndex = modules.Count;
        var instanceId = $"magenheim.fracture.{unchecked((uint)seed):x8}.{moduleIndex + 1:00}";
        var elements = SelectElements(piece, band, random, policy);
        var structuralDamage = SelectStructuralDamage(piece, band, random);
        var occupation = SelectOccupation(piece, band, random);
        var resources = SelectResources(piece, band, random);

        modules.Add(new DungeonModuleState(
            instanceId,
            piece.Id,
            band,
            elements,
            structuralDamage,
            occupation,
            resources));
    }

    private static IReadOnlyList<ElementalAlignment> SelectElements(
        DeepFracturePieceFamily piece,
        DungeonDepthBand band,
        DeterministicSequence random,
        DeepFractureGenerationPolicy policy)
    {
        if (!piece.SupportsElementalMutation || piece.Id == "DF-01")
            return Array.Empty<ElementalAlignment>();

        if (piece.Id == "DF-20")
            return PickDistinctAlignments(policy.DomainAlignments, policy.HeartElementCount, random);

        var elementCount = 0;
        switch (band)
        {
            case DungeonDepthBand.UpperFracture:
                elementCount = random.NextDouble() < policy.UpperElementChance ? 1 : 0;
                break;
            case DungeonDepthBand.MiddleWorks:
                elementCount = random.NextDouble() < policy.MiddleElementChance ? 1 : 0;
                break;
            case DungeonDepthBand.DeepDomains:
                elementCount = 1;
                if (policy.DomainAlignments.Count > 1 && random.NextDouble() < policy.MixedDeepElementChance)
                    elementCount = 2;
                break;
        }

        return elementCount == 0
            ? Array.Empty<ElementalAlignment>()
            : PickDistinctAlignments(policy.DomainAlignments, elementCount, random);
    }

    private static IReadOnlyList<ElementalAlignment> PickDistinctAlignments(
        IReadOnlyList<ElementalAlignment> available,
        int count,
        DeterministicSequence random)
    {
        var pool = available.ToList();
        var result = new List<ElementalAlignment>(count);
        for (var index = 0; index < count; index++)
        {
            var selectedIndex = random.NextInt(pool.Count);
            result.Add(pool[selectedIndex]);
            pool.RemoveAt(selectedIndex);
        }
        return result.ToArray();
    }

    private static StructuralDamageState SelectStructuralDamage(
        DeepFracturePieceFamily piece,
        DungeonDepthBand band,
        DeterministicSequence random)
    {
        if (piece.Id == "DF-01")
            return StructuralDamageState.Fractured;

        var roll = random.NextInt(100);
        if (band == DungeonDepthBand.Heart)
            return roll < 55 ? StructuralDamageState.Intact : StructuralDamageState.Fractured;
        if (roll < 20)
            return StructuralDamageState.Intact;
        if (roll < 55)
            return StructuralDamageState.Weathered;
        if (roll < 90)
            return StructuralDamageState.Fractured;
        return StructuralDamageState.Collapsed;
    }

    private static OccupationProfile SelectOccupation(
        DeepFracturePieceFamily piece,
        DungeonDepthBand band,
        DeterministicSequence random)
    {
        if (!piece.SupportsOccupationMutation)
            return OccupationProfile.Empty;
        if (piece.Id == "DF-01")
            return OccupationProfile.WispRoaming;
        if (piece.Id == "DF-20")
            return OccupationProfile.GuardianObjective;

        return band switch
        {
            DungeonDepthBand.UpperFracture => UpperOccupations[random.NextInt(UpperOccupations.Length)],
            DungeonDepthBand.MiddleWorks => MiddleOccupations[random.NextInt(MiddleOccupations.Length)],
            DungeonDepthBand.DeepDomains => DeepOccupations[random.NextInt(DeepOccupations.Length)],
            _ => OccupationProfile.Empty,
        };
    }

    private static ResourceState SelectResources(
        DeepFracturePieceFamily piece,
        DungeonDepthBand band,
        DeterministicSequence random)
    {
        if (piece.Id == "DF-20")
            return ResourceState.HeartReward;

        return band switch
        {
            DungeonDepthBand.UpperFracture => UpperResources[random.NextInt(UpperResources.Length)],
            DungeonDepthBand.MiddleWorks => MiddleResources[random.NextInt(MiddleResources.Length)],
            DungeonDepthBand.DeepDomains => DeepResources[random.NextInt(DeepResources.Length)],
            _ => ResourceState.Normal,
        };
    }

    private static (int Upper, int Middle, int Deep) AllocateBands(
        int modulesBetweenEntranceAndHeart,
        DeepFractureGenerationPolicy policy)
    {
        if (modulesBetweenEntranceAndHeart < 3)
            throw new InvalidOperationException("Deep Fracture generation requires room for Upper, Middle, and Deep districts between entrance and Heart.");

        var upper = Math.Max(1, (int)Math.Floor(modulesBetweenEntranceAndHeart * policy.UpperFractureFraction));
        var middle = Math.Max(1, (int)Math.Floor(modulesBetweenEntranceAndHeart * policy.MiddleWorksFraction));
        var deep = modulesBetweenEntranceAndHeart - upper - middle;

        if (deep < 1)
        {
            var needed = 1 - deep;
            while (needed > 0 && middle > 1)
            {
                middle--;
                needed--;
            }
            while (needed > 0 && upper > 1)
            {
                upper--;
                needed--;
            }
            deep = modulesBetweenEntranceAndHeart - upper - middle;
        }

        var upperCapacity = DeepFractureCatalog.PieceFamilies
            .Where(piece => piece.Id != "DF-01" && piece.AllowedBands.Contains(DungeonDepthBand.UpperFracture))
            .Sum(piece => piece.MaximumRecommendedReuse);

        if (upper > upperCapacity)
        {
            deep += upper - upperCapacity;
            upper = upperCapacity;
        }

        if (upper < 1 || middle < 1 || deep < 1)
            throw new InvalidOperationException("Depth-band policy cannot produce at least one Upper, Middle, and Deep district.");

        return (upper, middle, deep);
    }
}
