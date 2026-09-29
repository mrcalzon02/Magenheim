using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public enum UnderworldRimeSepulcherRoute
{
    Shelter,
    ClearGallery,
    FrostField,
    WhiteoutChoke,
    IceShear,
}

public sealed record UnderworldRimeSepulcherRoomDefinition(
    UnderworldDungeonRoomDefinition Room,
    UnderworldRimeSepulcherRoute Route,
    double ColdExposure01,
    double WhiteoutIntensity01,
    bool HasShelter,
    bool RimeboundAdvantage)
{
    public void Validate()
    {
        if (Room is null) throw new InvalidOperationException("Rime Sepulcher room authority is required.");
        Room.Validate(
            UnderworldFrozenRimeSepulcherCatalog.RoomIdPrefix,
            UnderworldFrozenRimeSepulcherCatalog.ModelIdPrefix);
        Unit(ColdExposure01, nameof(ColdExposure01));
        Unit(WhiteoutIntensity01, nameof(WhiteoutIntensity01));

        switch (Route)
        {
            case UnderworldRimeSepulcherRoute.Shelter:
                if (!HasShelter || ColdExposure01 > .12d || WhiteoutIntensity01 > .08d || RimeboundAdvantage)
                    throw new InvalidOperationException(
                        "Shelter rooms must provide real low-exposure recovery without boon gating.");
                break;
            case UnderworldRimeSepulcherRoute.ClearGallery:
                if (WhiteoutIntensity01 > .18d || ColdExposure01 > .40d)
                    throw new InvalidOperationException(
                        "Clear galleries must preserve long-range readability.");
                break;
            case UnderworldRimeSepulcherRoute.FrostField:
                if (ColdExposure01 < .36d || ColdExposure01 > .72d || !RimeboundAdvantage)
                    throw new InvalidOperationException(
                        "Frost fields must create meaningful mitigatable cold pressure.");
                break;
            case UnderworldRimeSepulcherRoute.WhiteoutChoke:
                if (WhiteoutIntensity01 < .55d || ColdExposure01 < .42d || !RimeboundAdvantage)
                    throw new InvalidOperationException(
                        "Whiteout chokes must be high-obscuration mitigatable routes.");
                break;
            case UnderworldRimeSepulcherRoute.IceShear:
                if (ColdExposure01 < .48d || !RimeboundAdvantage)
                    throw new InvalidOperationException(
                        "Ice-shear crossings must be high-exposure traversal routes.");
                break;
            default:
                throw new InvalidOperationException("Unknown Rime Sepulcher route.");
        }
    }

    private static void Unit(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1d)
            throw new InvalidOperationException(name + " must be finite in 0..1.");
    }
}

/// <summary>
/// Frozen Caverns ordinary-dungeon authority. The dungeon reuses the existing Frozen atmosphere
/// and Whiteout gameplay language: resistance lowers exposure, while visual obscuration remains
/// materially present. It never creates a parallel cold meter or boon-only mandatory route.
/// </summary>
public static class UnderworldFrozenRimeSepulcherCatalog
{
    public const string RoomIdPrefix = "magenheim.underworld.dungeon.frozen_caverns.room.";
    public const string ModelIdPrefix = "underworld-dungeon-frozen-rime-sepulcher-";
    public const string EntranceRoomId = RoomIdPrefix + "rime-mouth";

    public static IReadOnlyList<UnderworldRimeSepulcherRoomDefinition> Rooms { get; } =
        Array.AsReadOnly(new[]
        {
            Room("rime-mouth","Rime Mouth",UnderworldDungeonRoomRole.Entrance,
                UnderworldRimeSepulcherRoute.ClearGallery,.24,.08,false,false,34,38,22),
            Room("long-glass-gallery","Long Glass Gallery",UnderworldDungeonRoomRole.MainRoute,
                UnderworldRimeSepulcherRoute.ClearGallery,.30,.10,false,false,38,54,24),
            Room("burial-colonnade","Burial Colonnade",UnderworldDungeonRoomRole.MainRoute,
                UnderworldRimeSepulcherRoute.ClearGallery,.34,.12,false,false,42,48,24),
            Room("silent-crossing","Silent Crossing",UnderworldDungeonRoomRole.Junction,
                UnderworldRimeSepulcherRoute.FrostField,.44,.20,false,true,42,42,22),
            Room("icewell-shaft","Icewell Shaft",UnderworldDungeonRoomRole.Vertical,
                UnderworldRimeSepulcherRoute.IceShear,.62,.26,false,true,30,34,40),
            Room("whiteout-narthex","Whiteout Narthex",UnderworldDungeonRoomRole.Hazard,
                UnderworldRimeSepulcherRoute.WhiteoutChoke,.58,.74,false,true,34,44,20),
            Room("needle-pass","Needle Pass",UnderworldDungeonRoomRole.Hazard,
                UnderworldRimeSepulcherRoute.IceShear,.68,.34,false,true,28,52,22),
            Room("rimesilver-ossuary","Rimesilver Ossuary",UnderworldDungeonRoomRole.Resource,
                UnderworldRimeSepulcherRoute.FrostField,.48,.18,false,true,38,40,20),
            Room("clear-ice-lens-vault","Clear-Ice Lens Vault",UnderworldDungeonRoomRole.Resource,
                UnderworldRimeSepulcherRoute.ClearGallery,.32,.10,false,false,36,42,22),
            Room("still-air-crypt","Still-Air Crypt",UnderworldDungeonRoomRole.Landmark,
                UnderworldRimeSepulcherRoute.Shelter,.06,.02,true,false,38,40,20),
            Room("frost-tick-niche","Frost Tick Niche",UnderworldDungeonRoomRole.Encounter,
                UnderworldRimeSepulcherRoute.FrostField,.46,.20,false,true,34,38,18),
            Room("iceblind-hunt","Iceblind Hunt",UnderworldDungeonRoomRole.Encounter,
                UnderworldRimeSepulcherRoute.WhiteoutChoke,.56,.66,false,true,42,46,22),
            Room("cryolith-guard","Cryolith Guard",UnderworldDungeonRoomRole.Encounter,
                UnderworldRimeSepulcherRoute.IceShear,.64,.28,false,true,46,48,28),
            Room("frozen-archive","Frozen Archive",UnderworldDungeonRoomRole.Landmark,
                UnderworldRimeSepulcherRoute.ClearGallery,.28,.08,false,false,44,48,26),
            Room("shelter-chapel","Shelter Chapel",UnderworldDungeonRoomRole.Landmark,
                UnderworldRimeSepulcherRoute.Shelter,.08,.03,true,false,40,42,22),
            Room("white-silence-antechamber","White Silence Antechamber",UnderworldDungeonRoomRole.Landmark,
                UnderworldRimeSepulcherRoute.WhiteoutChoke,.62,.82,false,true,50,54,30),
        });

    public static void Validate()
    {
        var dungeon = UnderworldDungeonCatalog.FrozenCaverns;
        if (Rooms.Count < dungeon.TargetRoomFamilyMinimum ||
            Rooms.Count > dungeon.TargetRoomFamilyMaximum)
            throw new InvalidOperationException(
                "Rime Sepulcher room kit must stay inside the 15-20 family target.");

        foreach (var room in Rooms) room.Validate();

        if (Rooms.Select(x => x.Room.Id).Distinct(StringComparer.Ordinal).Count() != Rooms.Count)
            throw new InvalidOperationException("Rime Sepulcher room ids must be unique.");
        if (Rooms.Select(x => x.Room.ModelId).Distinct(StringComparer.Ordinal).Count() != Rooms.Count)
            throw new InvalidOperationException("Rime Sepulcher model ids must be unique.");

        foreach (UnderworldRimeSepulcherRoute route in Enum.GetValues(typeof(UnderworldRimeSepulcherRoute)))
            if (!Rooms.Any(x => x.Route == route))
                throw new InvalidOperationException("Rime Sepulcher must expose every route state.");

        if (Rooms.Count(x => x.Route == UnderworldRimeSepulcherRoute.Shelter && x.HasShelter) < 2)
            throw new InvalidOperationException("Rime Sepulcher requires at least two real recovery shelters.");

        if (Rooms.Count(x => x.RimeboundAdvantage) < 7)
            throw new InvalidOperationException(
                "Rime Sepulcher needs enough pressure routes for Rimebound/Rimeward to matter.");
        if (Rooms.Count(x => !x.RimeboundAdvantage) < 6)
            throw new InvalidOperationException(
                "Rime Sepulcher must retain substantial non-boon routing.");

        if (!Rooms.Any(x => x.Room.Role == UnderworldDungeonRoomRole.Junction) ||
            !Rooms.Any(x => x.Room.Role == UnderworldDungeonRoomRole.Vertical) ||
            !Rooms.Any(x => x.Room.Role == UnderworldDungeonRoomRole.Hazard) ||
            !Rooms.Any(x => x.Room.Role == UnderworldDungeonRoomRole.Resource) ||
            !Rooms.Any(x => x.Room.Role == UnderworldDungeonRoomRole.Encounter) ||
            !Rooms.Any(x => x.Room.Role == UnderworldDungeonRoomRole.Landmark))
            throw new InvalidOperationException(
                "Rime Sepulcher lacks required ordinary-dungeon gameplay roles.");
    }

    public static IReadOnlyList<string> RoomFamilyIds() =>
        Array.AsReadOnly(Rooms.Select(x => x.Room.Id).ToArray());

    public static IReadOnlyList<UnderworldDungeonRoomDefinition> SpatialDefinitions() =>
        Array.AsReadOnly(Rooms.Select(x => x.Room).ToArray());

    public static UnderworldRimeSepulcherRoomDefinition Require(string id) =>
        Rooms.Single(x => string.Equals(x.Room.Id, id, StringComparison.Ordinal));

    private static UnderworldRimeSepulcherRoomDefinition Room(
        string suffix,string name,UnderworldDungeonRoomRole role,
        UnderworldRimeSepulcherRoute route,double cold,double whiteout,
        bool shelter,bool advantage,double width,double depth,double height) =>
        new(
            new UnderworldDungeonRoomDefinition(
                RoomIdPrefix + suffix,
                ModelIdPrefix + suffix,
                name,role,width,depth,height),
            route,cold,whiteout,shelter,advantage);
}
