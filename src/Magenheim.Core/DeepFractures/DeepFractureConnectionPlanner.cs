using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.DeepFractures;

public sealed record DeepFractureConnectionPolicy(
    double MainRouteFraction,
    double BranchLoopChance,
    int MinimumShortcutSpan,
    int SmallShortcutCount,
    int FullShortcutCount,
    int GrandShortcutCount)
{
    public static DeepFractureConnectionPolicy Default => new(
        0.70d,
        0.45d,
        4,
        1,
        2,
        3);

    public void Validate()
    {
        ValidateFraction(MainRouteFraction, nameof(MainRouteFraction));
        ValidateFraction(BranchLoopChance, nameof(BranchLoopChance));

        if (MainRouteFraction < 0.50d)
            throw new InvalidOperationException("Deep Fracture main route fraction must be at least 0.50 so the primary descent remains legible.");
        if (MinimumShortcutSpan < 2)
            throw new InvalidOperationException("Deep Fracture shortcuts must skip at least two module positions.");
        if (SmallShortcutCount < 0 || FullShortcutCount < 0 || GrandShortcutCount < 0)
            throw new InvalidOperationException("Deep Fracture shortcut counts cannot be negative.");
    }

    public int GetShortcutCount(DeepFractureScale scale)
        => scale switch
        {
            DeepFractureScale.Small => SmallShortcutCount,
            DeepFractureScale.Full => FullShortcutCount,
            DeepFractureScale.Grand => GrandShortcutCount,
            _ => throw new InvalidOperationException($"Unknown Deep Fracture scale {scale}."),
        };

    private static void ValidateFraction(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1d)
            throw new InvalidOperationException($"{name} must be finite and between 0 and 1 inclusive.");
    }
}

public static class DeepFractureConnectionPlanner
{
    private const uint ConnectionStreamSalt = 0xC011EC7u;

    private static readonly DungeonConnectorKind[] UpperConnectorKinds =
    {
        DungeonConnectorKind.FaultCorridor,
        DungeonConnectorKind.ShortPassage,
        DungeonConnectorKind.Bridge,
    };

    private static readonly DungeonConnectorKind[] MiddleConnectorKinds =
    {
        DungeonConnectorKind.ShortPassage,
        DungeonConnectorKind.VerticalShaft,
        DungeonConnectorKind.Bridge,
        DungeonConnectorKind.AncientLift,
    };

    private static readonly DungeonConnectorKind[] DeepConnectorKinds =
    {
        DungeonConnectorKind.FaultCorridor,
        DungeonConnectorKind.VerticalShaft,
        DungeonConnectorKind.Bridge,
        DungeonConnectorKind.AncientLift,
    };

    private static readonly DungeonConnectorKind[] ShortcutKinds =
    {
        DungeonConnectorKind.AncientLift,
        DungeonConnectorKind.CrystalTransit,
        DungeonConnectorKind.ReopenedFracture,
    };

    public static DeepFractureDungeonPlan Attach(
        DeepFractureDungeonPlan structuralPlan,
        DeepFractureConnectionPolicy? policyOverride = null)
    {
        if (structuralPlan is null)
            throw new ArgumentNullException(nameof(structuralPlan));
        if (structuralPlan.Modules is null || structuralPlan.Modules.Count < 2)
            throw new InvalidOperationException("Deep Fracture connection planning requires at least an entrance and Heart module.");
        if (structuralPlan.Connections is null)
            throw new InvalidOperationException("Deep Fracture structural plan requires a connection collection.");
        if (structuralPlan.Connections.Count != 0)
            throw new InvalidOperationException("Deep Fracture connection planning must attach once to an unconnected structural plan; stacked connection mutators are not allowed.");

        var policy = policyOverride ?? DeepFractureConnectionPolicy.Default;
        policy.Validate();

        var random = new DeterministicSequence(structuralPlan.Seed, ConnectionStreamSalt);
        var spineIndices = BuildSpineIndices(structuralPlan.Modules, policy, random);
        var connections = new List<DungeonConnection>();
        var connectedPairs = new HashSet<string>(StringComparer.Ordinal);
        var nextConnectionNumber = 1;

        for (var index = 0; index < spineIndices.Count - 1; index++)
        {
            var fromIndex = spineIndices[index];
            var toIndex = spineIndices[index + 1];
            AddConnection(
                structuralPlan,
                connections,
                connectedPairs,
                ref nextConnectionNumber,
                fromIndex,
                toIndex,
                DungeonConnectionRole.MainRoute,
                SelectConnectorKind(structuralPlan.Modules[fromIndex], structuralPlan.Modules[toIndex], random),
                DungeonConnectionState.Open,
                true,
                true);

            AddBranchSegment(
                structuralPlan,
                connections,
                connectedPairs,
                ref nextConnectionNumber,
                fromIndex,
                toIndex,
                random,
                policy);
        }

        AddShortcuts(
            structuralPlan,
            connections,
            connectedPairs,
            ref nextConnectionNumber,
            spineIndices,
            random,
            policy);

        return structuralPlan with { Connections = connections.ToArray() };
    }

    private static List<int> BuildSpineIndices(
        IReadOnlyList<DungeonModuleState> modules,
        DeepFractureConnectionPolicy policy,
        DeterministicSequence random)
    {
        var required = new HashSet<int> { 0, modules.Count - 1 };

        AddFirstBandIndex(required, modules, DungeonDepthBand.UpperFracture, skipEntrance: true);
        AddFirstBandIndex(required, modules, DungeonDepthBand.MiddleWorks, skipEntrance: false);
        AddFirstBandIndex(required, modules, DungeonDepthBand.DeepDomains, skipEntrance: false);

        var target = (int)Math.Ceiling((modules.Count - 2) * policy.MainRouteFraction) + 2;
        if (target < required.Count)
            target = required.Count;
        if (target > modules.Count)
            target = modules.Count;

        var candidates = Enumerable.Range(1, modules.Count - 2)
            .Where(index => !required.Contains(index))
            .ToList();

        while (required.Count < target && candidates.Count > 0)
        {
            var selected = random.NextInt(candidates.Count);
            required.Add(candidates[selected]);
            candidates.RemoveAt(selected);
        }

        return required.OrderBy(index => index).ToList();
    }

    private static void AddFirstBandIndex(
        HashSet<int> required,
        IReadOnlyList<DungeonModuleState> modules,
        DungeonDepthBand band,
        bool skipEntrance)
    {
        for (var index = skipEntrance ? 1 : 0; index < modules.Count; index++)
        {
            if (modules[index].DepthBand == band)
            {
                required.Add(index);
                return;
            }
        }
    }

    private static void AddBranchSegment(
        DeepFractureDungeonPlan plan,
        List<DungeonConnection> connections,
        HashSet<string> connectedPairs,
        ref int nextConnectionNumber,
        int leftSpineIndex,
        int rightSpineIndex,
        DeterministicSequence random,
        DeepFractureConnectionPolicy policy)
    {
        if (rightSpineIndex - leftSpineIndex <= 1)
            return;

        var previous = leftSpineIndex;
        for (var index = leftSpineIndex + 1; index < rightSpineIndex; index++)
        {
            AddConnection(
                plan,
                connections,
                connectedPairs,
                ref nextConnectionNumber,
                previous,
                index,
                DungeonConnectionRole.Branch,
                SelectConnectorKind(plan.Modules[previous], plan.Modules[index], random),
                DungeonConnectionState.Open,
                true,
                false);
            previous = index;
        }

        if (random.NextDouble() < policy.BranchLoopChance)
        {
            AddConnection(
                plan,
                connections,
                connectedPairs,
                ref nextConnectionNumber,
                previous,
                rightSpineIndex,
                DungeonConnectionRole.Loop,
                SelectConnectorKind(plan.Modules[previous], plan.Modules[rightSpineIndex], random),
                DungeonConnectionState.Open,
                true,
                false);
        }
    }

    private static void AddShortcuts(
        DeepFractureDungeonPlan plan,
        List<DungeonConnection> connections,
        HashSet<string> connectedPairs,
        ref int nextConnectionNumber,
        IReadOnlyList<int> spineIndices,
        DeterministicSequence random,
        DeepFractureConnectionPolicy policy)
    {
        var desiredCount = policy.GetShortcutCount(plan.Scale);
        if (desiredCount == 0)
            return;

        var candidates = new List<ShortcutCandidate>();
        for (var earlierPosition = 0; earlierPosition < spineIndices.Count - 1; earlierPosition++)
        {
            for (var laterPosition = earlierPosition + 1; laterPosition < spineIndices.Count; laterPosition++)
            {
                var earlierIndex = spineIndices[earlierPosition];
                var laterIndex = spineIndices[laterPosition];
                if (laterIndex - earlierIndex < policy.MinimumShortcutSpan)
                    continue;
                if (laterIndex == plan.Modules.Count - 1)
                    continue;

                var pairKey = PairKey(plan.Modules[earlierIndex].InstanceId, plan.Modules[laterIndex].InstanceId);
                if (!connectedPairs.Contains(pairKey))
                    candidates.Add(new ShortcutCandidate(earlierIndex, laterIndex));
            }
        }

        for (var shortcut = 0; shortcut < desiredCount; shortcut++)
        {
            if (candidates.Count == 0)
                throw new InvalidOperationException($"Deep Fracture connection policy requested {desiredCount} shortcuts but no additional valid shortcut pair remains.");

            var selectedIndex = random.NextInt(candidates.Count);
            var selected = candidates[selectedIndex];
            candidates.RemoveAt(selectedIndex);

            var pairKey = PairKey(plan.Modules[selected.EarlierIndex].InstanceId, plan.Modules[selected.LaterIndex].InstanceId);
            candidates.RemoveAll(candidate =>
                PairKey(plan.Modules[candidate.EarlierIndex].InstanceId, plan.Modules[candidate.LaterIndex].InstanceId) == pairKey);

            AddConnection(
                plan,
                connections,
                connectedPairs,
                ref nextConnectionNumber,
                selected.LaterIndex,
                selected.EarlierIndex,
                DungeonConnectionRole.Shortcut,
                ShortcutKinds[random.NextInt(ShortcutKinds.Length)],
                DungeonConnectionState.Dormant,
                true,
                false);
        }
    }

    private static DungeonConnectorKind SelectConnectorKind(
        DungeonModuleState from,
        DungeonModuleState to,
        DeterministicSequence random)
    {
        if (from.DepthBand != to.DepthBand)
        {
            var transitionKinds = new[]
            {
                DungeonConnectorKind.VerticalShaft,
                DungeonConnectorKind.AncientLift,
                DungeonConnectorKind.ShortPassage,
            };
            return transitionKinds[random.NextInt(transitionKinds.Length)];
        }

        var kinds = to.DepthBand switch
        {
            DungeonDepthBand.UpperFracture => UpperConnectorKinds,
            DungeonDepthBand.MiddleWorks => MiddleConnectorKinds,
            DungeonDepthBand.DeepDomains => DeepConnectorKinds,
            DungeonDepthBand.Heart => MiddleConnectorKinds,
            _ => UpperConnectorKinds,
        };
        return kinds[random.NextInt(kinds.Length)];
    }

    private static void AddConnection(
        DeepFractureDungeonPlan plan,
        List<DungeonConnection> connections,
        HashSet<string> connectedPairs,
        ref int nextConnectionNumber,
        int fromIndex,
        int toIndex,
        DungeonConnectionRole role,
        DungeonConnectorKind kind,
        DungeonConnectionState state,
        bool bidirectional,
        bool requiredForHeart)
    {
        var from = plan.Modules[fromIndex];
        var to = plan.Modules[toIndex];
        var pairKey = PairKey(from.InstanceId, to.InstanceId);
        if (!connectedPairs.Add(pairKey))
            throw new InvalidOperationException($"Deep Fracture connection planner attempted to duplicate the module pair '{pairKey}'.");

        var id = $"magenheim.fracture.{unchecked((uint)plan.Seed):x8}.connection.{nextConnectionNumber:000}";
        nextConnectionNumber++;
        connections.Add(new DungeonConnection(
            id,
            from.InstanceId,
            to.InstanceId,
            role,
            kind,
            state,
            bidirectional,
            requiredForHeart));
    }

    private static string PairKey(string left, string right)
        => string.CompareOrdinal(left, right) <= 0 ? left + "|" + right : right + "|" + left;

    private sealed record ShortcutCandidate(int EarlierIndex, int LaterIndex);
}
