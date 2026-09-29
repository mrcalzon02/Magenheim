using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public enum UnderworldCinderworksHeatRoute
{
    Safe,
    Warm,
    Hot,
    VentCycle,
    SlagChannel,
}

public sealed record UnderworldCinderworksRoomDefinition(
    UnderworldDungeonRoomDefinition Room,
    UnderworldCinderworksHeatRoute HeatRoute,
    double ThermalPressure01,
    bool HasRecoveryPocket,
    bool FurnaceBloodAdvantage)
{
    public void Validate()
    {
        if (Room is null) throw new InvalidOperationException("Cinderworks room authority is required.");
        Room.Validate(
            UnderworldSulfurCinderworksCatalog.RoomIdPrefix,
            UnderworldSulfurCinderworksCatalog.ModelIdPrefix);
        if (!Enum.IsDefined(typeof(UnderworldCinderworksHeatRoute), HeatRoute))
            throw new InvalidOperationException("Unknown Cinderworks heat-route state.");
        if (double.IsNaN(ThermalPressure01) || double.IsInfinity(ThermalPressure01) ||
            ThermalPressure01 < 0d || ThermalPressure01 > 1d)
            throw new InvalidOperationException("Cinderworks thermal pressure must be finite in 0..1.");

        switch (HeatRoute)
        {
            case UnderworldCinderworksHeatRoute.Safe:
                if (ThermalPressure01 > .15d || !HasRecoveryPocket || FurnaceBloodAdvantage)
                    throw new InvalidOperationException(
                        "Safe Cinderworks rooms must provide real recovery without requiring Furnace Blood.");
                break;
            case UnderworldCinderworksHeatRoute.Warm:
                if (ThermalPressure01 <= .10d || ThermalPressure01 > .40d)
                    throw new InvalidOperationException("Warm Cinderworks rooms require bounded low pressure.");
                break;
            case UnderworldCinderworksHeatRoute.Hot:
                if (ThermalPressure01 < .35d || ThermalPressure01 > .72d)
                    throw new InvalidOperationException("Hot Cinderworks rooms require sustained but non-maximal pressure.");
                if (!FurnaceBloodAdvantage)
                    throw new InvalidOperationException("Hot Cinderworks routes must reward Furnace Blood.");
                break;
            case UnderworldCinderworksHeatRoute.VentCycle:
                if (ThermalPressure01 < .55d || !FurnaceBloodAdvantage)
                    throw new InvalidOperationException("Vent-cycle routes must be high-pressure Furnace Blood routes.");
                break;
            case UnderworldCinderworksHeatRoute.SlagChannel:
                if (ThermalPressure01 < .60d || !FurnaceBloodAdvantage)
                    throw new InvalidOperationException("Slag-channel routes must be high-pressure Furnace Blood routes.");
                break;
        }
    }
}

/// <summary>
/// Sulfurous Wastes biome-dungeon production authority. Cinderworks uses heat as route pressure:
/// ordinary players always retain a viable safe/warm path, while Furnace Blood materially widens
/// the set of hot/vent/slag shortcuts they can exploit. Heat is never decorative lava-floor spam.
/// </summary>
public static class UnderworldSulfurCinderworksCatalog
{
    public const string RoomIdPrefix = "magenheim.underworld.dungeon.sulfurous_wastes.room.";
    public const string ModelIdPrefix = "underworld-dungeon-sulfur-cinderworks-";
    public const string EntranceRoomId = RoomIdPrefix + "cinder-gate";

    public static IReadOnlyList<UnderworldCinderworksRoomDefinition> Rooms { get; } =
        Array.AsReadOnly(new[]
        {
            Room("cinder-gate", "Cinder Gate", UnderworldDungeonRoomRole.Entrance,
                UnderworldCinderworksHeatRoute.Warm, .22, false, false, 34, 38, 22),
            Room("slag-nave", "Slag Nave", UnderworldDungeonRoomRole.MainRoute,
                UnderworldCinderworksHeatRoute.Warm, .30, false, false, 38, 48, 22),
            Room("furnace-gallery", "Furnace Gallery", UnderworldDungeonRoomRole.MainRoute,
                UnderworldCinderworksHeatRoute.Hot, .52, false, true, 36, 50, 24),
            Room("bellows-junction", "Bellows Junction", UnderworldDungeonRoomRole.Junction,
                UnderworldCinderworksHeatRoute.Warm, .34, false, false, 42, 42, 24),
            Room("chimney-shaft", "Chimney Shaft", UnderworldDungeonRoomRole.Vertical,
                UnderworldCinderworksHeatRoute.VentCycle, .66, false, true, 30, 34, 38),
            Room("vent-choir", "Vent Choir", UnderworldDungeonRoomRole.Hazard,
                UnderworldCinderworksHeatRoute.VentCycle, .78, false, true, 40, 44, 24),
            Room("slag-runoff", "Slag Runoff", UnderworldDungeonRoomRole.Hazard,
                UnderworldCinderworksHeatRoute.SlagChannel, .82, false, true, 32, 52, 20),
            Room("emberiron-foundry", "Emberiron Foundry", UnderworldDungeonRoomRole.Resource,
                UnderworldCinderworksHeatRoute.Hot, .48, false, true, 42, 44, 22),
            Room("sulfur-kiln", "Sulfur Kiln", UnderworldDungeonRoomRole.Resource,
                UnderworldCinderworksHeatRoute.Warm, .36, false, false, 34, 40, 20),
            Room("quench-vault", "Quench Vault", UnderworldDungeonRoomRole.Landmark,
                UnderworldCinderworksHeatRoute.Safe, .08, true, false, 38, 40, 22),
            Room("ashmite-conveyor", "Ashmite Conveyor", UnderworldDungeonRoomRole.Encounter,
                UnderworldCinderworksHeatRoute.Warm, .32, false, false, 34, 46, 18),
            Room("cinder-hound-yard", "Cinder Hound Yard", UnderworldDungeonRoomRole.Encounter,
                UnderworldCinderworksHeatRoute.Hot, .56, false, true, 42, 46, 20),
            Room("furnace-golem-crucible", "Furnace Golem Crucible", UnderworldDungeonRoomRole.Encounter,
                UnderworldCinderworksHeatRoute.SlagChannel, .74, false, true, 46, 48, 28),
            Room("broken-smeltery", "Broken Smeltery", UnderworldDungeonRoomRole.Landmark,
                UnderworldCinderworksHeatRoute.Warm, .28, false, false, 44, 50, 26),
            Room("pressure-lock", "Pressure Lock", UnderworldDungeonRoomRole.MainRoute,
                UnderworldCinderworksHeatRoute.Safe, .10, true, false, 28, 38, 18),
            Room("furnace-heart-antechamber", "Furnace Heart Antechamber", UnderworldDungeonRoomRole.Landmark,
                UnderworldCinderworksHeatRoute.Hot, .64, false, true, 50, 52, 30),
        });

    public static void Validate()
    {
        var dungeon = UnderworldDungeonCatalog.SulfurousWastes;
        if (Rooms.Count < dungeon.TargetRoomFamilyMinimum ||
            Rooms.Count > dungeon.TargetRoomFamilyMaximum)
            throw new InvalidOperationException(
                "Cinderworks room kit must stay inside the 15-20 family target.");

        foreach (var definition in Rooms) definition.Validate();

        if (Rooms.Select(value => value.Room.Id).Distinct(StringComparer.Ordinal).Count() != Rooms.Count)
            throw new InvalidOperationException("Cinderworks room ids must be unique.");
        if (Rooms.Select(value => value.Room.ModelId).Distinct(StringComparer.Ordinal).Count() != Rooms.Count)
            throw new InvalidOperationException("Cinderworks model ids must be unique.");

        foreach (UnderworldCinderworksHeatRoute route in Enum.GetValues(typeof(UnderworldCinderworksHeatRoute)))
            if (!Rooms.Any(value => value.HeatRoute == route))
                throw new InvalidOperationException("Cinderworks must expose every heat-route state.");

        if (Rooms.Count(value => value.HeatRoute == UnderworldCinderworksHeatRoute.Safe &&
                                 value.HasRecoveryPocket) < 2)
            throw new InvalidOperationException("Cinderworks needs at least two genuine recovery rooms.");

        var aggressive = Rooms.Count(value =>
            value.HeatRoute is UnderworldCinderworksHeatRoute.Hot or
                UnderworldCinderworksHeatRoute.VentCycle or
                UnderworldCinderworksHeatRoute.SlagChannel);
        if (aggressive < 7)
            throw new InvalidOperationException(
                "Cinderworks needs enough high-pressure rooms for Furnace Blood to alter routing.");

        if (Rooms.Count(value => !value.FurnaceBloodAdvantage) < 7)
            throw new InvalidOperationException(
                "Cinderworks must retain a substantial non-boon route vocabulary.");

        if (!Rooms.Any(value => value.Room.Role == UnderworldDungeonRoomRole.Junction) ||
            !Rooms.Any(value => value.Room.Role == UnderworldDungeonRoomRole.Vertical) ||
            !Rooms.Any(value => value.Room.Role == UnderworldDungeonRoomRole.Hazard) ||
            !Rooms.Any(value => value.Room.Role == UnderworldDungeonRoomRole.Resource) ||
            !Rooms.Any(value => value.Room.Role == UnderworldDungeonRoomRole.Encounter) ||
            !Rooms.Any(value => value.Room.Role == UnderworldDungeonRoomRole.Landmark))
            throw new InvalidOperationException(
                "Cinderworks lacks required ordinary-dungeon gameplay roles.");
    }

    public static IReadOnlyList<string> RoomFamilyIds() =>
        Array.AsReadOnly(Rooms.Select(value => value.Room.Id).ToArray());

    public static IReadOnlyList<UnderworldDungeonRoomDefinition> SpatialDefinitions() =>
        Array.AsReadOnly(Rooms.Select(value => value.Room).ToArray());

    public static UnderworldCinderworksRoomDefinition Require(string roomFamilyId) =>
        Rooms.Single(value =>
            string.Equals(value.Room.Id, roomFamilyId, StringComparison.Ordinal));

    private static UnderworldCinderworksRoomDefinition Room(
        string suffix,
        string displayName,
        UnderworldDungeonRoomRole role,
        UnderworldCinderworksHeatRoute heatRoute,
        double thermalPressure01,
        bool recovery,
        bool furnaceBloodAdvantage,
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
            heatRoute,
            thermalPressure01,
            recovery,
            furnaceBloodAdvantage);
}
