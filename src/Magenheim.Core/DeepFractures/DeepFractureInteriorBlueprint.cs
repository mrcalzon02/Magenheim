using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.DeepFractures;

public enum DeepFractureRuntimeConnectionMode
{
    PhysicalPassage,
    TraversalLink
}

public sealed record DeepFractureInteriorPoint(double X, double Y, double Z)
{
    public void Validate(string owner)
    {
        if (double.IsNaN(X) || double.IsInfinity(X)
            || double.IsNaN(Y) || double.IsInfinity(Y)
            || double.IsNaN(Z) || double.IsInfinity(Z))
            throw new InvalidOperationException($"Deep Fracture interior point for '{owner}' must contain only finite coordinates.");
    }

    public double HorizontalDistanceTo(DeepFractureInteriorPoint other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        var dx = X - other.X;
        var dz = Z - other.Z;
        return Math.Sqrt((dx * dx) + (dz * dz));
    }
}

public sealed record DeepFractureInteriorProjectionPolicy(
    int SmallColumns,
    int FullColumns,
    int GrandColumns,
    double GridSpacing,
    double VerticalBandStep,
    double ModuleFootprint,
    double BoundaryPadding)
{
    public static DeepFractureInteriorProjectionPolicy Default => new(
        SmallColumns: 4,
        FullColumns: 6,
        GrandColumns: 8,
        GridSpacing: 128d,
        VerticalBandStep: 36d,
        ModuleFootprint: 96d,
        BoundaryPadding: 96d);

    public void Validate()
    {
        ValidateColumns(SmallColumns, nameof(SmallColumns));
        ValidateColumns(FullColumns, nameof(FullColumns));
        ValidateColumns(GrandColumns, nameof(GrandColumns));
        ValidateFinitePositive(GridSpacing, nameof(GridSpacing));
        ValidateFinitePositive(VerticalBandStep, nameof(VerticalBandStep));
        ValidateFinitePositive(ModuleFootprint, nameof(ModuleFootprint));
        ValidateFinitePositive(BoundaryPadding, nameof(BoundaryPadding));

        if (GridSpacing < ModuleFootprint + 8d)
            throw new InvalidOperationException("Deep Fracture interior grid spacing must exceed the nominal module footprint by at least eight metres.");
        if (SmallColumns > FullColumns || FullColumns > GrandColumns)
            throw new InvalidOperationException("Deep Fracture interior projection columns must scale monotonically from Small through Grand.");
    }

    public int GetColumns(DeepFractureScale scale)
        => scale switch
        {
            DeepFractureScale.Small => SmallColumns,
            DeepFractureScale.Full => FullColumns,
            DeepFractureScale.Grand => GrandColumns,
            _ => throw new InvalidOperationException($"Unknown Deep Fracture scale {scale}."),
        };

    private static void ValidateColumns(int value, string name)
    {
        if (value < 2 || value > 12)
            throw new InvalidOperationException($"{name} must be between two and twelve columns.");
    }

    private static void ValidateFinitePositive(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            throw new InvalidOperationException($"{name} must be finite and greater than zero.");
    }
}

public sealed record DeepFractureInteriorModulePlacement(
    int SequenceIndex,
    string ModuleInstanceId,
    string PieceFamilyId,
    DungeonDepthBand DepthBand,
    DeepFractureInteriorPoint Center,
    int YawDegrees,
    IReadOnlyList<ElementalAlignment> ElementalStates,
    OccupationProfile Occupation,
    ResourceState Resources,
    IReadOnlyList<PlannedEnemyEncounter> Encounters);

public sealed record DeepFractureInteriorConnectionPlacement(
    string Id,
    string FromModuleId,
    string ToModuleId,
    DungeonConnectionRole Role,
    DungeonConnectorKind Kind,
    DungeonConnectionState State,
    DeepFractureRuntimeConnectionMode RuntimeMode,
    bool IsBidirectional,
    bool RequiredForHeartReachability);

public sealed record DeepFractureInteriorBlueprint(
    DeepFractureScale Scale,
    int Seed,
    double InteriorRadius,
    IReadOnlyList<DeepFractureInteriorModulePlacement> Modules,
    IReadOnlyList<DeepFractureInteriorConnectionPlacement> Connections);

public sealed record DeepFractureInteriorBlueprintValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public static class DeepFractureInteriorBlueprintCompiler
{
    public static DeepFractureInteriorBlueprint Build(
        DeepFractureDungeonPlan dungeonPlan,
        DeepFractureEncounterPlan encounterPlan,
        DeepFractureInteriorProjectionPolicy? policyOverride = null)
    {
        if (dungeonPlan is null)
            throw new ArgumentNullException(nameof(dungeonPlan));
        if (encounterPlan is null)
            throw new ArgumentNullException(nameof(encounterPlan));

        var dungeonValidation = DeepFracturePlanValidator.Validate(dungeonPlan);
        if (!dungeonValidation.IsValid)
            throw new InvalidOperationException("Deep Fracture interior projection requires a valid dungeon plan: " + string.Join(" | ", dungeonValidation.Errors));

        var encounterValidation = DeepFractureEncounterValidator.Validate(dungeonPlan, encounterPlan);
        if (!encounterValidation.IsValid)
            throw new InvalidOperationException("Deep Fracture interior projection requires a valid encounter plan: " + string.Join(" | ", encounterValidation.Errors));

        var policy = policyOverride ?? DeepFractureInteriorProjectionPolicy.Default;
        policy.Validate();

        var columns = policy.GetColumns(dungeonPlan.Scale);
        var encounterByModule = encounterPlan.Districts.ToDictionary(district => district.ModuleInstanceId, StringComparer.Ordinal);
        var centers = new DeepFractureInteriorPoint[dungeonPlan.Modules.Count];

        for (var index = 0; index < dungeonPlan.Modules.Count; index++)
        {
            var module = dungeonPlan.Modules[index];
            var row = index / columns;
            var logicalColumn = index % columns;
            var column = row % 2 == 0
                ? logicalColumn
                : (columns - 1) - logicalColumn;

            centers[index] = new DeepFractureInteriorPoint(
                X: column * policy.GridSpacing,
                Y: -((int)module.DepthBand * policy.VerticalBandStep),
                Z: row * policy.GridSpacing);
        }

        var placements = new List<DeepFractureInteriorModulePlacement>(dungeonPlan.Modules.Count);
        for (var index = 0; index < dungeonPlan.Modules.Count; index++)
        {
            var module = dungeonPlan.Modules[index];
            var district = encounterByModule[module.InstanceId];
            placements.Add(new DeepFractureInteriorModulePlacement(
                SequenceIndex: index,
                ModuleInstanceId: module.InstanceId,
                PieceFamilyId: module.PieceFamilyId,
                DepthBand: module.DepthBand,
                Center: centers[index],
                YawDegrees: ResolveYaw(index, centers),
                ElementalStates: module.ElementalStates.ToArray(),
                Occupation: module.Occupation,
                Resources: module.Resources,
                Encounters: district.Encounters.ToArray()));
        }

        var connections = dungeonPlan.Connections
            .Select(connection => new DeepFractureInteriorConnectionPlacement(
                connection.Id,
                connection.FromModuleId,
                connection.ToModuleId,
                connection.Role,
                connection.Kind,
                connection.State,
                RuntimeMode(connection.Role),
                connection.IsBidirectional,
                connection.RequiredForHeartReachability))
            .ToArray();

        var furthestHorizontalCenter = centers
            .Select(center => Math.Sqrt((center.X * center.X) + (center.Z * center.Z)))
            .DefaultIfEmpty(0d)
            .Max();
        var interiorRadius = furthestHorizontalCenter + (policy.ModuleFootprint * 0.5d) + policy.BoundaryPadding;

        var blueprint = new DeepFractureInteriorBlueprint(
            dungeonPlan.Scale,
            dungeonPlan.Seed,
            interiorRadius,
            placements.ToArray(),
            connections);

        var validation = DeepFractureInteriorBlueprintValidator.Validate(dungeonPlan, encounterPlan, blueprint, policy);
        if (!validation.IsValid)
            throw new InvalidOperationException("Generated Deep Fracture interior blueprint failed validation: " + string.Join(" | ", validation.Errors));

        return blueprint;
    }

    private static int ResolveYaw(int index, IReadOnlyList<DeepFractureInteriorPoint> centers)
    {
        if (centers.Count <= 1)
            return 0;

        var current = centers[index];
        var target = index < centers.Count - 1
            ? centers[index + 1]
            : centers[index - 1];
        var dx = target.X - current.X;
        var dz = target.Z - current.Z;

        if (Math.Abs(dx) >= Math.Abs(dz))
            return dx >= 0d ? 90 : 270;
        return dz >= 0d ? 0 : 180;
    }

    private static DeepFractureRuntimeConnectionMode RuntimeMode(DungeonConnectionRole role)
        => role == DungeonConnectionRole.MainRoute || role == DungeonConnectionRole.Branch
            ? DeepFractureRuntimeConnectionMode.PhysicalPassage
            : DeepFractureRuntimeConnectionMode.TraversalLink;
}

public static class DeepFractureInteriorBlueprintValidator
{
    public static DeepFractureInteriorBlueprintValidationResult Validate(
        DeepFractureDungeonPlan dungeonPlan,
        DeepFractureEncounterPlan encounterPlan,
        DeepFractureInteriorBlueprint blueprint,
        DeepFractureInteriorProjectionPolicy? policyOverride = null)
    {
        if (dungeonPlan is null)
            throw new ArgumentNullException(nameof(dungeonPlan));
        if (encounterPlan is null)
            throw new ArgumentNullException(nameof(encounterPlan));
        if (blueprint is null)
            throw new ArgumentNullException(nameof(blueprint));

        var errors = new List<string>();
        var policy = policyOverride ?? DeepFractureInteriorProjectionPolicy.Default;
        try
        {
            policy.Validate();
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
            return new DeepFractureInteriorBlueprintValidationResult(errors);
        }

        var dungeonValidation = DeepFracturePlanValidator.Validate(dungeonPlan);
        if (!dungeonValidation.IsValid)
        {
            errors.Add("Interior blueprint cannot validate against an invalid dungeon plan.");
            errors.AddRange(dungeonValidation.Errors);
            return new DeepFractureInteriorBlueprintValidationResult(errors);
        }

        var encounterValidation = DeepFractureEncounterValidator.Validate(dungeonPlan, encounterPlan);
        if (!encounterValidation.IsValid)
        {
            errors.Add("Interior blueprint cannot validate against an invalid encounter plan.");
            errors.AddRange(encounterValidation.Errors);
            return new DeepFractureInteriorBlueprintValidationResult(errors);
        }

        if (blueprint.Scale != dungeonPlan.Scale)
            errors.Add($"Interior blueprint scale {blueprint.Scale} does not match dungeon scale {dungeonPlan.Scale}.");
        if (blueprint.Seed != dungeonPlan.Seed)
            errors.Add($"Interior blueprint seed {blueprint.Seed} does not match dungeon seed {dungeonPlan.Seed}.");
        if (double.IsNaN(blueprint.InteriorRadius) || double.IsInfinity(blueprint.InteriorRadius) || blueprint.InteriorRadius <= 0d)
            errors.Add("Interior blueprint radius must be finite and greater than zero.");
        if (blueprint.Modules is null)
        {
            errors.Add("Interior blueprint requires module placements.");
            return new DeepFractureInteriorBlueprintValidationResult(errors);
        }
        if (blueprint.Connections is null)
        {
            errors.Add("Interior blueprint requires connection placements.");
            return new DeepFractureInteriorBlueprintValidationResult(errors);
        }
        if (blueprint.Modules.Count != dungeonPlan.Modules.Count)
            errors.Add($"Interior blueprint must place every dungeon module exactly once; expected {dungeonPlan.Modules.Count}, received {blueprint.Modules.Count}.");
        if (blueprint.Connections.Count != dungeonPlan.Connections.Count)
            errors.Add($"Interior blueprint must represent every dungeon connection exactly once; expected {dungeonPlan.Connections.Count}, received {blueprint.Connections.Count}.");

        var dungeonModuleById = dungeonPlan.Modules.ToDictionary(module => module.InstanceId, StringComparer.Ordinal);
        var districtById = encounterPlan.Districts.ToDictionary(district => district.ModuleInstanceId, StringComparer.Ordinal);
        var placementById = new Dictionary<string, DeepFractureInteriorModulePlacement>(StringComparer.Ordinal);

        foreach (var placement in blueprint.Modules)
        {
            if (placement is null)
            {
                errors.Add("Interior blueprint cannot contain a null module placement.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(placement.ModuleInstanceId) || !placementById.TryAdd(placement.ModuleInstanceId, placement))
            {
                errors.Add($"Interior blueprint module ID '{placement.ModuleInstanceId}' is empty or duplicated.");
                continue;
            }
            if (!dungeonModuleById.TryGetValue(placement.ModuleInstanceId, out var module))
            {
                errors.Add($"Interior blueprint references unknown dungeon module '{placement.ModuleInstanceId}'.");
                continue;
            }
            if (placement.SequenceIndex < 0 || placement.SequenceIndex >= dungeonPlan.Modules.Count
                || !string.Equals(dungeonPlan.Modules[placement.SequenceIndex].InstanceId, placement.ModuleInstanceId, StringComparison.Ordinal))
                errors.Add($"Interior blueprint sequence index for '{placement.ModuleInstanceId}' does not match dungeon module order.");
            if (!string.Equals(placement.PieceFamilyId, module.PieceFamilyId, StringComparison.Ordinal)
                || placement.DepthBand != module.DepthBand
                || placement.Occupation != module.Occupation
                || placement.Resources != module.Resources
                || !placement.ElementalStates.SequenceEqual(module.ElementalStates))
                errors.Add($"Interior blueprint placement '{placement.ModuleInstanceId}' drifted from dungeon module authority.");

            try
            {
                placement.Center.Validate(placement.ModuleInstanceId);
            }
            catch (InvalidOperationException exception)
            {
                errors.Add(exception.Message);
            }

            if (placement.YawDegrees != 0 && placement.YawDegrees != 90 && placement.YawDegrees != 180 && placement.YawDegrees != 270)
                errors.Add($"Interior blueprint placement '{placement.ModuleInstanceId}' yaw must be a cardinal rotation.");

            if (!districtById.TryGetValue(placement.ModuleInstanceId, out var district))
            {
                errors.Add($"Interior blueprint placement '{placement.ModuleInstanceId}' has no encounter district authority.");
            }
            else
            {
                var expectedEncounterIds = district.Encounters.Select(encounter => encounter.Id).ToArray();
                var projectedEncounterIds = placement.Encounters.Select(encounter => encounter.Id).ToArray();
                if (!expectedEncounterIds.SequenceEqual(projectedEncounterIds))
                    errors.Add($"Interior blueprint placement '{placement.ModuleInstanceId}' does not preserve its encounter groups exactly.");
            }
        }

        for (var leftIndex = 0; leftIndex < blueprint.Modules.Count; leftIndex++)
        {
            var left = blueprint.Modules[leftIndex];
            if (left is null)
                continue;
            for (var rightIndex = leftIndex + 1; rightIndex < blueprint.Modules.Count; rightIndex++)
            {
                var right = blueprint.Modules[rightIndex];
                if (right is null)
                    continue;
                if (left.Center.HorizontalDistanceTo(right.Center) < policy.ModuleFootprint)
                    errors.Add($"Interior module placements '{left.ModuleInstanceId}' and '{right.ModuleInstanceId}' overlap their nominal {policy.ModuleFootprint:0.#}m footprint.");
            }
        }

        var dungeonConnectionById = dungeonPlan.Connections.ToDictionary(connection => connection.Id, StringComparer.Ordinal);
        var connectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var placement in blueprint.Connections)
        {
            if (placement is null)
            {
                errors.Add("Interior blueprint cannot contain a null connection placement.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(placement.Id) || !connectionIds.Add(placement.Id))
            {
                errors.Add($"Interior blueprint connection ID '{placement.Id}' is empty or duplicated.");
                continue;
            }
            if (!dungeonConnectionById.TryGetValue(placement.Id, out var connection))
            {
                errors.Add($"Interior blueprint references unknown dungeon connection '{placement.Id}'.");
                continue;
            }
            if (!string.Equals(placement.FromModuleId, connection.FromModuleId, StringComparison.Ordinal)
                || !string.Equals(placement.ToModuleId, connection.ToModuleId, StringComparison.Ordinal)
                || placement.Role != connection.Role
                || placement.Kind != connection.Kind
                || placement.State != connection.State
                || placement.IsBidirectional != connection.IsBidirectional
                || placement.RequiredForHeartReachability != connection.RequiredForHeartReachability)
                errors.Add($"Interior blueprint connection '{placement.Id}' drifted from dungeon connection authority.");

            var expectedMode = connection.Role == DungeonConnectionRole.MainRoute || connection.Role == DungeonConnectionRole.Branch
                ? DeepFractureRuntimeConnectionMode.PhysicalPassage
                : DeepFractureRuntimeConnectionMode.TraversalLink;
            if (placement.RuntimeMode != expectedMode)
                errors.Add($"Interior blueprint connection '{placement.Id}' runtime mode does not match its graph role {connection.Role}.");
        }

        ValidatePhysicalReachability(dungeonPlan, blueprint, placementById, errors);

        foreach (var placement in blueprint.Modules)
        {
            if (placement is null)
                continue;
            var requiredRadius = Math.Sqrt((placement.Center.X * placement.Center.X) + (placement.Center.Z * placement.Center.Z))
                + (policy.ModuleFootprint * 0.5d);
            if (requiredRadius > blueprint.InteriorRadius)
                errors.Add($"Interior blueprint radius {blueprint.InteriorRadius:0.#} does not contain module '{placement.ModuleInstanceId}' at required radius {requiredRadius:0.#}.");
        }

        return new DeepFractureInteriorBlueprintValidationResult(errors);
    }

    private static void ValidatePhysicalReachability(
        DeepFractureDungeonPlan dungeonPlan,
        DeepFractureInteriorBlueprint blueprint,
        IReadOnlyDictionary<string, DeepFractureInteriorModulePlacement> placementById,
        List<string> errors)
    {
        if (dungeonPlan.Modules.Count == 0)
            return;

        var start = dungeonPlan.Modules[0].InstanceId;
        var adjacency = placementById.Keys.ToDictionary(id => id, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var connection in blueprint.Connections.Where(connection => connection is not null && connection.RuntimeMode == DeepFractureRuntimeConnectionMode.PhysicalPassage))
        {
            if (!adjacency.ContainsKey(connection.FromModuleId) || !adjacency.ContainsKey(connection.ToModuleId))
                continue;
            adjacency[connection.FromModuleId].Add(connection.ToModuleId);
            adjacency[connection.ToModuleId].Add(connection.FromModuleId);
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(start);
        visited.Add(start);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var next in adjacency[current])
            {
                if (visited.Add(next))
                    queue.Enqueue(next);
            }
        }

        foreach (var module in dungeonPlan.Modules)
        {
            if (!visited.Contains(module.InstanceId))
                errors.Add($"Interior blueprint module '{module.InstanceId}' is reachable only through a loop/shortcut traversal link; every district must retain a physical MainRoute/Branch path from DF-01.");
        }
    }
}
