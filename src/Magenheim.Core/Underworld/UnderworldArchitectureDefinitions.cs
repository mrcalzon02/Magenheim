using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Magenheim.Core.Underworld;

public enum UnderworldBuildPieceKind
{
    Foundation,
    Plinth,
    Beam,
    Pillar,
    ReinforcedBeam,
    ArchRib,
}

public enum UnderworldBuildTier
{
    Rootstone,
    RootforgedIron,
    Silverbound,
}

public sealed record UnderworldBuildDimensions(
    int WidthMeters,
    int HeightMeters,
    int DepthMeters);

public sealed record UnderworldBuildCost(
    string ResourceId,
    int Amount);

public sealed record UnderworldBuildPieceDefinition(
    string Id,
    string PrefabName,
    string DisplayName,
    UnderworldBuildPieceKind Kind,
    UnderworldBuildTier Tier,
    UnderworldBuildDimensions Dimensions,
    string CraftingStationPrefabName,
    IReadOnlyList<UnderworldBuildCost> Costs);

public sealed record UnderworldArchitectureDefinitionSet(
    int SchemaVersion,
    IReadOnlyList<UnderworldBuildPieceDefinition> Pieces,
    string Fingerprint);

/// <summary>
/// Pure authority for Underworld construction-piece identities and gameplay-significant
/// catalog data. Runtime registration must consume this authority rather than recreate it.
/// </summary>
public static class UnderworldArchitectureValidator
{
    public const int CurrentSchemaVersion = 1;

    public const string BuildIdPrefix = "magenheim.underworld.build.";
    public const string PrefabPrefix = "Magenheim_Underworld_";
    public const string UnderstoneResourceId = "magenheim.underworld.resource.understone";
    public const string WorldrootTimberResourceId = "magenheim.underworld.resource.worldroot_timber";
    public const string IronResourceId = "Iron";

    public static UnderworldArchitectureDefinitionSet ValidateAndFreeze(
        int schemaVersion,
        IEnumerable<UnderworldBuildPieceDefinition> pieces)
    {
        if (schemaVersion != CurrentSchemaVersion)
            throw new InvalidOperationException(
                $"Unsupported Underworld architecture schema {schemaVersion}. Expected {CurrentSchemaVersion}.");
        if (pieces is null)
            throw new ArgumentNullException(nameof(pieces));

        var frozenPieces = pieces.Select(ValidatePiece).ToArray();
        if (frozenPieces.Length == 0)
            throw new InvalidOperationException("At least one Underworld build-piece definition is required.");

        RejectDuplicate(frozenPieces.Select(value => value.Id), "Underworld build-piece id");
        RejectDuplicate(frozenPieces.Select(value => value.PrefabName), "Underworld build-piece prefab");

        return new UnderworldArchitectureDefinitionSet(
            schemaVersion,
            Array.AsReadOnly(frozenPieces),
            ComputeFingerprint(schemaVersion, frozenPieces));
    }

    private static UnderworldBuildPieceDefinition ValidatePiece(UnderworldBuildPieceDefinition piece)
    {
        if (piece is null)
            throw new InvalidOperationException("Underworld build-piece definitions cannot contain null entries.");
        if (piece.Dimensions is null)
            throw new InvalidOperationException($"Underworld build piece '{piece.Id}' requires dimensions.");
        if (piece.Costs is null || piece.Costs.Count == 0)
            throw new InvalidOperationException($"Underworld build piece '{piece.Id}' requires at least one build cost.");

        var id = RequirePrefixed(piece.Id, BuildIdPrefix, "Underworld build-piece id");
        var prefab = RequirePrefixed(piece.PrefabName, PrefabPrefix, "Underworld build-piece prefab");
        var displayName = RequireText(piece.DisplayName, "Underworld build-piece display name");
        var station = RequireText(piece.CraftingStationPrefabName, "Underworld build-piece crafting station");

        if (!Enum.IsDefined(typeof(UnderworldBuildPieceKind), piece.Kind))
            throw new InvalidOperationException($"Underworld build piece '{id}' has an unknown piece kind.");
        if (!Enum.IsDefined(typeof(UnderworldBuildTier), piece.Tier))
            throw new InvalidOperationException($"Underworld build piece '{id}' has an unknown build tier.");

        var dimensions = piece.Dimensions;
        if (dimensions.WidthMeters <= 0 || dimensions.HeightMeters <= 0 || dimensions.DepthMeters <= 0)
            throw new InvalidOperationException(
                $"Underworld build piece '{id}' dimensions must all be positive meters.");

        var costs = piece.Costs.Select(cost => ValidateCost(id, cost)).ToArray();
        RejectDuplicate(costs.Select(value => value.ResourceId), $"resource cost for '{id}'");

        return piece with
        {
            Id = id,
            PrefabName = prefab,
            DisplayName = displayName,
            CraftingStationPrefabName = station,
            Dimensions = new UnderworldBuildDimensions(
                dimensions.WidthMeters,
                dimensions.HeightMeters,
                dimensions.DepthMeters),
            Costs = Array.AsReadOnly(costs),
        };
    }

    private static UnderworldBuildCost ValidateCost(string pieceId, UnderworldBuildCost cost)
    {
        if (cost is null)
            throw new InvalidOperationException($"Underworld build piece '{pieceId}' contains a null build cost.");

        var resource = RequireText(cost.ResourceId, $"resource id for '{pieceId}'");
        if (cost.Amount <= 0)
            throw new InvalidOperationException(
                $"Underworld build piece '{pieceId}' resource '{resource}' must have a positive amount.");

        return cost with { ResourceId = resource };
    }

    private static void RejectDuplicate(IEnumerable<string> values, string field)
    {
        var duplicate = values
            .GroupBy(value => value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate {field} '{duplicate.Key}'.");
    }

    private static string RequirePrefixed(string value, string prefix, string field)
    {
        var normalized = RequireText(value, field);
        if (!normalized.StartsWith(prefix, StringComparison.Ordinal) || normalized.Length == prefix.Length)
            throw new InvalidOperationException($"{field} '{normalized}' must begin with '{prefix}'.");
        return normalized;
    }

    private static string RequireText(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{field} cannot be empty.");
        if (value.Any(char.IsControl) || value.Contains("|"))
            throw new InvalidOperationException($"{field} cannot contain fingerprint separators or control characters.");
        return value.Trim();
    }

    private static string ComputeFingerprint(
        int schemaVersion,
        IReadOnlyList<UnderworldBuildPieceDefinition> pieces)
    {
        var builder = new StringBuilder();
        builder.Append("underworld-architecture-schema=").Append(schemaVersion).Append('\n');

        foreach (var piece in pieces.OrderBy(value => value.Id, StringComparer.Ordinal))
        {
            builder.Append("piece|")
                .Append(piece.Id).Append('|')
                .Append(piece.PrefabName).Append('|')
                .Append(piece.DisplayName).Append('|')
                .Append((int)piece.Kind).Append('|')
                .Append((int)piece.Tier).Append('|')
                .Append(piece.Dimensions.WidthMeters).Append('x')
                .Append(piece.Dimensions.HeightMeters).Append('x')
                .Append(piece.Dimensions.DepthMeters).Append('|')
                .Append(piece.CraftingStationPrefabName)
                .Append('\n');

            foreach (var cost in piece.Costs.OrderBy(value => value.ResourceId, StringComparer.Ordinal))
            {
                builder.Append("cost|")
                    .Append(piece.Id).Append('|')
                    .Append(cost.ResourceId).Append('|')
                    .Append(cost.Amount)
                    .Append('\n');
            }
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        var hex = new StringBuilder(bytes.Length * 2);
        foreach (var value in bytes)
            hex.Append(value.ToString("x2"));
        return hex.ToString();
    }
}

public static class UnderworldArchitectureCatalog
{
    public static UnderworldArchitectureDefinitionSet CreateInitialStructuralSlice() =>
        UnderworldArchitectureValidator.ValidateAndFreeze(
            UnderworldArchitectureValidator.CurrentSchemaVersion,
            new[]
            {
                Piece(
                    "understone_foundation_2x2",
                    "UnderstoneFoundation_2x2",
                    "Understone Foundation 2x2m",
                    UnderworldBuildPieceKind.Foundation,
                    UnderworldBuildTier.Rootstone,
                    2, 1, 2,
                    "piece_stonecutter",
                    Cost(UnderworldArchitectureValidator.UnderstoneResourceId, 6)),
                Piece(
                    "great_column_plinth",
                    "GreatColumnPlinth",
                    "Great Understone Column Plinth",
                    UnderworldBuildPieceKind.Plinth,
                    UnderworldBuildTier.Rootstone,
                    4, 2, 4,
                    "piece_stonecutter",
                    Cost(UnderworldArchitectureValidator.UnderstoneResourceId, 16)),
                Piece(
                    "worldroot_beam_2m",
                    "WorldrootBeam_2m",
                    "Worldroot Beam 2m",
                    UnderworldBuildPieceKind.Beam,
                    UnderworldBuildTier.Rootstone,
                    2, 1, 1,
                    "piece_workbench",
                    Cost(UnderworldArchitectureValidator.WorldrootTimberResourceId, 2)),
                Piece(
                    "worldroot_beam_4m",
                    "WorldrootBeam_4m",
                    "Worldroot Beam 4m",
                    UnderworldBuildPieceKind.Beam,
                    UnderworldBuildTier.Rootstone,
                    4, 1, 1,
                    "piece_workbench",
                    Cost(UnderworldArchitectureValidator.WorldrootTimberResourceId, 4)),
                Piece(
                    "worldroot_beam_8m",
                    "WorldrootBeam_8m",
                    "Worldroot Beam 8m",
                    UnderworldBuildPieceKind.Beam,
                    UnderworldBuildTier.Rootstone,
                    8, 1, 1,
                    "piece_workbench",
                    Cost(UnderworldArchitectureValidator.WorldrootTimberResourceId, 8)),
                Piece(
                    "worldroot_pillar_2m",
                    "WorldrootPillar_2m",
                    "Worldroot Pillar 2m",
                    UnderworldBuildPieceKind.Pillar,
                    UnderworldBuildTier.Rootstone,
                    1, 2, 1,
                    "piece_workbench",
                    Cost(UnderworldArchitectureValidator.WorldrootTimberResourceId, 2)),
                Piece(
                    "worldroot_pillar_4m",
                    "WorldrootPillar_4m",
                    "Worldroot Pillar 4m",
                    UnderworldBuildPieceKind.Pillar,
                    UnderworldBuildTier.Rootstone,
                    1, 4, 1,
                    "piece_workbench",
                    Cost(UnderworldArchitectureValidator.WorldrootTimberResourceId, 4)),
                Piece(
                    "worldroot_pillar_8m",
                    "WorldrootPillar_8m",
                    "Worldroot Pillar 8m",
                    UnderworldBuildPieceKind.Pillar,
                    UnderworldBuildTier.Rootstone,
                    1, 8, 1,
                    "piece_workbench",
                    Cost(UnderworldArchitectureValidator.WorldrootTimberResourceId, 8)),
                Piece(
                    "iron_banded_worldroot_beam_4m",
                    "IronBandedWorldrootBeam_4m",
                    "Iron-Banded Worldroot Beam 4m",
                    UnderworldBuildPieceKind.ReinforcedBeam,
                    UnderworldBuildTier.RootforgedIron,
                    4, 1, 1,
                    "piece_stonecutter",
                    Cost(UnderworldArchitectureValidator.WorldrootTimberResourceId, 4),
                    Cost(UnderworldArchitectureValidator.IronResourceId, 2)),
                Piece(
                    "rootforged_arch_rib_4m",
                    "RootforgedArchRib_4m",
                    "Rootforged Arch Rib 4m",
                    UnderworldBuildPieceKind.ArchRib,
                    UnderworldBuildTier.RootforgedIron,
                    4, 4, 1,
                    "piece_stonecutter",
                    Cost(UnderworldArchitectureValidator.WorldrootTimberResourceId, 6),
                    Cost(UnderworldArchitectureValidator.IronResourceId, 4)),
                Piece(
                    "rootforged_arch_rib_8m",
                    "RootforgedArchRib_8m",
                    "Rootforged Arch Rib 8m",
                    UnderworldBuildPieceKind.ArchRib,
                    UnderworldBuildTier.RootforgedIron,
                    8, 8, 1,
                    "piece_stonecutter",
                    Cost(UnderworldArchitectureValidator.WorldrootTimberResourceId, 12),
                    Cost(UnderworldArchitectureValidator.IronResourceId, 8)),
            });

    private static UnderworldBuildPieceDefinition Piece(
        string idSuffix,
        string prefabSuffix,
        string displayName,
        UnderworldBuildPieceKind kind,
        UnderworldBuildTier tier,
        int widthMeters,
        int heightMeters,
        int depthMeters,
        string craftingStationPrefabName,
        params UnderworldBuildCost[] costs) =>
        new(
            UnderworldArchitectureValidator.BuildIdPrefix + idSuffix,
            UnderworldArchitectureValidator.PrefabPrefix + prefabSuffix,
            displayName,
            kind,
            tier,
            new UnderworldBuildDimensions(widthMeters, heightMeters, depthMeters),
            craftingStationPrefabName,
            Array.AsReadOnly(costs));

    private static UnderworldBuildCost Cost(string resourceId, int amount) =>
        new(resourceId, amount);
}
