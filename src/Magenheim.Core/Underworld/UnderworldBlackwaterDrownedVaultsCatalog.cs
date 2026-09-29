using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public enum UnderworldDrownedVaultRouteMode
{
    Dry,
    Mixed,
    Flooded,
    AirPocket,
    VerticalWater,
}

public sealed record UnderworldDrownedVaultRoomDefinition(
    UnderworldDungeonRoomDefinition Room,
    UnderworldDrownedVaultRouteMode RouteMode,
    double WaterDepthMeters)
{
    public void Validate()
    {
        if (Room is null) throw new InvalidOperationException("Drowned Vault room authority is required.");
        Room.Validate(UnderworldBlackwaterDrownedVaultsCatalog.RoomIdPrefix,
            UnderworldBlackwaterDrownedVaultsCatalog.ModelIdPrefix);
        if (!Enum.IsDefined(typeof(UnderworldDrownedVaultRouteMode), RouteMode))
            throw new InvalidOperationException("Unknown Drowned Vault route mode.");
        if (double.IsNaN(WaterDepthMeters) || double.IsInfinity(WaterDepthMeters) || WaterDepthMeters < 0d)
            throw new InvalidOperationException("Drowned Vault water depth must be finite and non-negative.");
        if (RouteMode == UnderworldDrownedVaultRouteMode.Dry && WaterDepthMeters != 0d)
            throw new InvalidOperationException("Dry Drowned Vault rooms cannot declare standing water.");
        if (RouteMode != UnderworldDrownedVaultRouteMode.Dry && WaterDepthMeters <= 0d)
            throw new InvalidOperationException("Wet Drowned Vault rooms require positive structural water depth.");
        if (WaterDepthMeters >= Room.HeightMeters)
            throw new InvalidOperationException("Drowned Vault water cannot consume the entire room height.");
    }
}

/// <summary>
/// Blackwater Deep's ordinary biome-dungeon production kit. Water state is explicit gameplay
/// authority: the room art/runtime binder must preserve dry shelves, flooded routes and air pockets
/// rather than treating water as cosmetic dressing.
/// </summary>
public static class UnderworldBlackwaterDrownedVaultsCatalog
{
    public const string RoomIdPrefix = "magenheim.underworld.dungeon.blackwater_deep.room.";
    public const string ModelIdPrefix = "underworld-dungeon-blackwater-drowned-vaults-";
    public const string EntranceRoomId = RoomIdPrefix + "drowned-sinkhole";

    public static IReadOnlyList<UnderworldDrownedVaultRoomDefinition> Rooms { get; } =
        Array.AsReadOnly(new[]
        {
            Room("drowned-sinkhole", "Drowned Sinkhole", UnderworldDungeonRoomRole.Entrance,
                UnderworldDrownedVaultRouteMode.Mixed, 2.5, 34, 38, 22),
            Room("tide-gallery", "Tide Gallery", UnderworldDungeonRoomRole.MainRoute,
                UnderworldDrownedVaultRouteMode.Mixed, 1.8, 36, 48, 20),
            Room("dry-ledger", "Dry Ledger", UnderworldDungeonRoomRole.MainRoute,
                UnderworldDrownedVaultRouteMode.Dry, 0, 30, 42, 18),
            Room("collapsed-dock", "Collapsed Dock", UnderworldDungeonRoomRole.Landmark,
                UnderworldDrownedVaultRouteMode.Mixed, 2.8, 44, 46, 24),
            Room("siphon-hall", "Siphon Hall", UnderworldDungeonRoomRole.Hazard,
                UnderworldDrownedVaultRouteMode.Flooded, 5.5, 34, 50, 20),
            Room("bell-chamber", "Bell Chamber", UnderworldDungeonRoomRole.Resource,
                UnderworldDrownedVaultRouteMode.AirPocket, 3.5, 38, 40, 26),
            Room("split-cistern", "Split Cistern", UnderworldDungeonRoomRole.Junction,
                UnderworldDrownedVaultRouteMode.Mixed, 3.0, 42, 42, 24),
            Room("drowned-shaft", "Drowned Shaft", UnderworldDungeonRoomRole.Vertical,
                UnderworldDrownedVaultRouteMode.VerticalWater, 12.0, 28, 32, 34),
            Room("pearl-vault", "Pearl Vault", UnderworldDungeonRoomRole.Resource,
                UnderworldDrownedVaultRouteMode.Flooded, 4.5, 34, 38, 20),
            Room("high-water-archive", "High-Water Archive", UnderworldDungeonRoomRole.Landmark,
                UnderworldDrownedVaultRouteMode.AirPocket, 2.5, 40, 44, 28),
            Room("lamprey-run", "Lamprey Run", UnderworldDungeonRoomRole.Encounter,
                UnderworldDrownedVaultRouteMode.Flooded, 4.0, 30, 46, 18),
            Room("deep-hunter-lair", "Deep Hunter Lair", UnderworldDungeonRoomRole.Encounter,
                UnderworldDrownedVaultRouteMode.Flooded, 7.0, 46, 48, 26),
            Room("sunken-quay", "Sunken Quay", UnderworldDungeonRoomRole.Encounter,
                UnderworldDrownedVaultRouteMode.Mixed, 2.0, 44, 52, 20),
            Room("broken-causeway", "Broken Causeway", UnderworldDungeonRoomRole.MainRoute,
                UnderworldDrownedVaultRouteMode.Dry, 0, 26, 54, 20),
            Room("undertow-sluice", "Undertow Sluice", UnderworldDungeonRoomRole.Hazard,
                UnderworldDrownedVaultRouteMode.VerticalWater, 8.0, 30, 38, 28),
            Room("abyssal-sanctum", "Abyssal Sanctum", UnderworldDungeonRoomRole.Landmark,
                UnderworldDrownedVaultRouteMode.Mixed, 4.0, 50, 52, 30),
        });

    public static void Validate()
    {
        var dungeon = UnderworldDungeonCatalog.BlackwaterDeep;
        if (Rooms.Count < dungeon.TargetRoomFamilyMinimum || Rooms.Count > dungeon.TargetRoomFamilyMaximum)
            throw new InvalidOperationException("Drowned Vault room kit must stay inside the 15-20 family target.");

        foreach (var definition in Rooms) definition.Validate();

        if (Rooms.Select(value => value.Room.Id).Distinct(StringComparer.Ordinal).Count() != Rooms.Count)
            throw new InvalidOperationException("Drowned Vault room ids must be unique.");
        if (Rooms.Select(value => value.Room.ModelId).Distinct(StringComparer.Ordinal).Count() != Rooms.Count)
            throw new InvalidOperationException("Drowned Vault model ids must be unique.");

        var modes = Rooms.GroupBy(value => value.RouteMode).ToDictionary(group => group.Key, group => group.Count());
        foreach (UnderworldDrownedVaultRouteMode mode in Enum.GetValues(typeof(UnderworldDrownedVaultRouteMode)))
            if (!modes.ContainsKey(mode))
                throw new InvalidOperationException("Drowned Vaults must expose every structural water route mode.");

        if (modes[UnderworldDrownedVaultRouteMode.Dry] < 2)
            throw new InvalidOperationException("Drowned Vaults require multiple genuinely dry route alternatives.");
        if (Rooms.Count(value => value.RouteMode == UnderworldDrownedVaultRouteMode.Flooded ||
                                 value.RouteMode == UnderworldDrownedVaultRouteMode.VerticalWater) < 6)
            throw new InvalidOperationException("Drowned Vaults require substantial underwater traversal.");
        if (modes[UnderworldDrownedVaultRouteMode.AirPocket] < 2)
            throw new InvalidOperationException("Drowned Vaults require multiple deliberate recovery/air chambers.");
        if (!Rooms.Any(value => value.Room.Role == UnderworldDungeonRoomRole.Junction) ||
            !Rooms.Any(value => value.Room.Role == UnderworldDungeonRoomRole.Vertical) ||
            !Rooms.Any(value => value.Room.Role == UnderworldDungeonRoomRole.Hazard) ||
            !Rooms.Any(value => value.Room.Role == UnderworldDungeonRoomRole.Resource) ||
            !Rooms.Any(value => value.Room.Role == UnderworldDungeonRoomRole.Encounter) ||
            !Rooms.Any(value => value.Room.Role == UnderworldDungeonRoomRole.Landmark))
            throw new InvalidOperationException("Drowned Vault room kit lacks required gameplay roles.");
    }

    public static IReadOnlyList<string> RoomFamilyIds() =>
        Array.AsReadOnly(Rooms.Select(value => value.Room.Id).ToArray());

    public static IReadOnlyList<UnderworldDungeonRoomDefinition> SpatialDefinitions() =>
        Array.AsReadOnly(Rooms.Select(value => value.Room).ToArray());

    public static UnderworldDrownedVaultRoomDefinition Require(string roomFamilyId) =>
        Rooms.Single(value => string.Equals(value.Room.Id, roomFamilyId, StringComparison.Ordinal));

    private static UnderworldDrownedVaultRoomDefinition Room(
        string suffix,
        string displayName,
        UnderworldDungeonRoomRole role,
        UnderworldDrownedVaultRouteMode mode,
        double waterDepth,
        double width,
        double depth,
        double height) =>
        new(
            new UnderworldDungeonRoomDefinition(
                RoomIdPrefix + suffix,
                ModelIdPrefix + suffix,
                displayName,
                role,
                width,
                depth,
                height),
            mode,
            waterDepth);
}
