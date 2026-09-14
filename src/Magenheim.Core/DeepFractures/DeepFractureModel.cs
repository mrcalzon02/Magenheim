using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.DeepFractures;

public enum DungeonDepthBand
{
    UpperFracture = 0,
    MiddleWorks = 1,
    DeepDomains = 2,
    Heart = 3
}

public enum DeepFractureScale
{
    Small,
    Full,
    Grand
}

public enum StructuralDamageState
{
    Intact,
    Weathered,
    Fractured,
    Collapsed
}

public enum OccupationProfile
{
    Empty,
    WispRoaming,
    CrawlerColony,
    ShardlingFeeding,
    RevenantHold,
    SentryDefense,
    GuardianObjective,
    GolemLair,
    Mixed
}

public enum ResourceState
{
    Sparse,
    Normal,
    Rich,
    GreaterGeode,
    AncientInfrastructure,
    HeartReward
}

public enum ConnectionState
{
    Open,
    Blocked,
    Collapsed,
    Locked,
    ShortcutDormant,
    ShortcutOpened
}

public enum EncounterRole
{
    Nuisance,
    Territorial,
    Skirmisher,
    Parasite,
    Soldier,
    Sentry,
    Pursuer,
    Ambusher,
    Gatekeeper,
    Elite,
    Fortification,
    Colossus,
    HeartGuardian
}

public enum CreatureChassis
{
    AnnoyanceWisp,
    GeodeCrawler,
    Shardling,
    CrystalParasite,
    CrystalRevenant,
    FacetSentry,
    StoneSentinel,
    CrystalHound,
    Burrower,
    StoneGuardian,
    CrystalGolem,
    ObeliskWarden,
    DeepColossus
}

public enum CrystalComponentFunction
{
    ElementalAttack,
    Armor,
    Movement,
    Regeneration,
    Resistance,
    EnvironmentalControl,
    DeathAnchor
}

public sealed record DeepFracturePieceFamily(
    string Id,
    string Name,
    IReadOnlyList<DungeonDepthBand> AllowedBands,
    int MaximumRecommendedReuse,
    bool SupportsElementalMutation,
    bool SupportsOccupationMutation,
    bool CanHostHeartEncounter)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new InvalidOperationException("Deep Fracture piece ID is required.");

        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException($"Deep Fracture piece '{Id}' requires a display name.");

        if (AllowedBands is null || AllowedBands.Count == 0)
            throw new InvalidOperationException($"Deep Fracture piece '{Id}' requires at least one depth band.");

        if (AllowedBands.Distinct().Count() != AllowedBands.Count)
            throw new InvalidOperationException($"Deep Fracture piece '{Id}' contains duplicate depth bands.");

        if (MaximumRecommendedReuse < 1 || MaximumRecommendedReuse > 3)
            throw new InvalidOperationException($"Deep Fracture piece '{Id}' reuse must remain between one and three instances.");

        if (CanHostHeartEncounter && !AllowedBands.Contains(DungeonDepthBand.Heart))
            throw new InvalidOperationException($"Deep Fracture piece '{Id}' can host a Heart encounter but is not allowed in the Heart band.");
    }
}

public sealed record DeepFractureScaleRule(
    DeepFractureScale Scale,
    int MinimumMajorModules,
    int MaximumMajorModules)
{
    public void Validate()
    {
        if (MinimumMajorModules < 1)
            throw new InvalidOperationException($"{Scale} Deep Fracture minimum module count must be positive.");

        if (MaximumMajorModules < MinimumMajorModules)
            throw new InvalidOperationException($"{Scale} Deep Fracture maximum module count cannot be below its minimum.");
    }
}

public sealed record EnemyChassisDefinition(
    CreatureChassis Chassis,
    string Name,
    EncounterRole PrimaryRole,
    bool SupportsElementalAlignment,
    bool SupportsCrystalComponents)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException($"{Chassis} requires a display name.");
    }
}

public sealed record CrystalComponentDefinition(
    string Id,
    CrystalComponentFunction Function,
    bool RequiredForPermanentKill = false)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new InvalidOperationException("Crystal component ID is required.");
    }
}

public sealed record EnemyEncounterDefinition(
    CreatureChassis Chassis,
    ElementalAlignment? Alignment,
    EncounterRole Role,
    IReadOnlyList<CrystalComponentDefinition> Components,
    string TerritoryPieceId)
{
    public void Validate(IReadOnlyDictionary<string, DeepFracturePieceFamily> pieceFamilies)
    {
        if (pieceFamilies is null)
            throw new ArgumentNullException(nameof(pieceFamilies));

        if (string.IsNullOrWhiteSpace(TerritoryPieceId) || !pieceFamilies.ContainsKey(TerritoryPieceId))
            throw new InvalidOperationException($"Enemy encounter territory '{TerritoryPieceId}' is not a registered Deep Fracture piece.");

        if (Components is null)
            throw new InvalidOperationException("Enemy encounter components are required.");

        var componentIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var component in Components)
        {
            if (component is null)
                throw new InvalidOperationException("Enemy encounter cannot contain a null crystal component.");

            component.Validate();
            if (!componentIds.Add(component.Id))
                throw new InvalidOperationException($"Enemy encounter contains duplicate crystal component '{component.Id}'.");
        }
    }
}

public sealed record DungeonModuleState(
    string InstanceId,
    string PieceFamilyId,
    DungeonDepthBand DepthBand,
    ElementalAlignment? ElementalState,
    StructuralDamageState StructuralDamage,
    OccupationProfile Occupation,
    ResourceState Resources,
    ConnectionState Connections);

public sealed record DeepFractureDungeonPlan(
    DeepFractureScale Scale,
    IReadOnlyList<DungeonModuleState> Modules);

public sealed record DeepFracturePlanValidationResult(
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
