using System;
using System.Collections.Generic;
using System.Linq;

namespace Magenheim.Core.Underworld;

public enum UnderworldCarrionCatacombsRoute
{
    Sanctuary,
    PreservedRuin,
    TaintedRuin,
    RootIngress,
    BlackBloom,
}

public sealed record UnderworldCarrionCatacombsRoomDefinition(
    UnderworldDungeonRoomDefinition Room,
    UnderworldCarrionCatacombsRoute Route,
    double Contamination01,
    double BlackBloomIntensity01,
    bool HasWorksite,
    bool DefiantAdvantage)
{
    public void Validate()
    {
        if (Room is null)
            throw new InvalidOperationException("Carrion Catacombs room authority is required.");
        Room.Validate(
            UnderworldGreatDecayCarrionCatacombsCatalog.RoomIdPrefix,
            UnderworldGreatDecayCarrionCatacombsCatalog.ModelIdPrefix);
        Unit(Contamination01, nameof(Contamination01));
        Unit(BlackBloomIntensity01, nameof(BlackBloomIntensity01));

        switch (Route)
        {
            case UnderworldCarrionCatacombsRoute.Sanctuary:
                if (!HasWorksite || Contamination01 > .14d ||
                    BlackBloomIntensity01 > .08d || DefiantAdvantage)
                    throw new InvalidOperationException(
                        "Sanctuary rooms must be low-pressure work/recovery spaces without Defiant gating.");
                break;
            case UnderworldCarrionCatacombsRoute.PreservedRuin:
                if (Contamination01 > .32d || BlackBloomIntensity01 > .12d)
                    throw new InvalidOperationException(
                        "Preserved ruin routes must retain readable ancient architecture.");
                break;
            case UnderworldCarrionCatacombsRoute.TaintedRuin:
                if (Contamination01 < .28d || Contamination01 > .58d || !DefiantAdvantage)
                    throw new InvalidOperationException(
                        "Tainted ruins must create meaningful mitigatable contamination.");
                break;
            case UnderworldCarrionCatacombsRoute.RootIngress:
                if (Contamination01 < .42d || BlackBloomIntensity01 < .20d || !DefiantAdvantage)
                    throw new InvalidOperationException(
                        "Root-ingress routes must materially reward Defiant mitigation.");
                break;
            case UnderworldCarrionCatacombsRoute.BlackBloom:
                if (Contamination01 < .52d || BlackBloomIntensity01 < .58d || !DefiantAdvantage)
                    throw new InvalidOperationException(
                        "Black Bloom routes must be severe but suppressible Great Decay pressure.");
                break;
            default:
                throw new InvalidOperationException("Unknown Carrion Catacombs route.");
        }
    }

    private static void Unit(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1d)
            throw new InvalidOperationException(name + " must be finite in 0..1.");
    }
}

/// <summary>
/// Great Decay ordinary-dungeon authority. The route sequence intentionally moves from recognizable
/// funerary construction into root/rot occupation. Defiant Flesh/armour and the Defiant Censer use
/// the existing atmosphere resistance/suppression verbs; the dungeon does not create a second meter.
/// </summary>
public static class UnderworldGreatDecayCarrionCatacombsCatalog
{
    public const string RoomIdPrefix = "magenheim.underworld.dungeon.great_decay.room.";
    public const string ModelIdPrefix = "underworld-dungeon-great-decay-carrion-catacombs-";
    public const string EntranceRoomId = RoomIdPrefix + "ossuary-gate";

    public static IReadOnlyList<UnderworldCarrionCatacombsRoomDefinition> Rooms { get; } =
        Array.AsReadOnly(new[]
        {
            Room("ossuary-gate","Ossuary Gate",UnderworldDungeonRoomRole.Entrance,
                UnderworldCarrionCatacombsRoute.PreservedRuin,.18,.04,false,false,36,38,22),
            Room("processional-hall","Processional Hall",UnderworldDungeonRoomRole.MainRoute,
                UnderworldCarrionCatacombsRoute.PreservedRuin,.24,.06,false,false,40,54,24),
            Room("sunken-reliquary","Sunken Reliquary",UnderworldDungeonRoomRole.MainRoute,
                UnderworldCarrionCatacombsRoute.TaintedRuin,.34,.10,false,true,42,46,24),
            Room("root-split-crossing","Root-Split Crossing",UnderworldDungeonRoomRole.Junction,
                UnderworldCarrionCatacombsRoute.RootIngress,.48,.28,false,true,42,42,24),
            Room("bone-chute","Bone Chute",UnderworldDungeonRoomRole.Vertical,
                UnderworldCarrionCatacombsRoute.RootIngress,.52,.30,false,true,30,36,42),
            Room("miasma-nave","Miasma Nave",UnderworldDungeonRoomRole.Hazard,
                UnderworldCarrionCatacombsRoute.BlackBloom,.62,.72,false,true,40,48,26),
            Room("carrion-sluice","Carrion Sluice",UnderworldDungeonRoomRole.Hazard,
                UnderworldCarrionCatacombsRoute.BlackBloom,.68,.78,false,true,32,52,20),
            Room("amber-mortuary","Amber Mortuary",UnderworldDungeonRoomRole.Resource,
                UnderworldCarrionCatacombsRoute.TaintedRuin,.38,.12,false,true,40,40,22),
            Room("bone-gravel-crypt","Bone Gravel Crypt",UnderworldDungeonRoomRole.Resource,
                UnderworldCarrionCatacombsRoute.RootIngress,.46,.24,false,true,38,42,20),
            Room("censer-court","Censer Court",UnderworldDungeonRoomRole.Landmark,
                UnderworldCarrionCatacombsRoute.Sanctuary,.08,.02,true,false,42,42,24),
            Room("rotling-warrens","Rotling Warrens",UnderworldDungeonRoomRole.Encounter,
                UnderworldCarrionCatacombsRoute.RootIngress,.50,.26,false,true,36,40,18),
            Room("spore-husk-cloister","Spore Husk Cloister",UnderworldDungeonRoomRole.Encounter,
                UnderworldCarrionCatacombsRoute.TaintedRuin,.42,.16,false,true,40,44,22),
            Room("graft-warden-hall","Graft Warden Hall",UnderworldDungeonRoomRole.Encounter,
                UnderworldCarrionCatacombsRoute.RootIngress,.56,.34,false,true,46,48,28),
            Room("vanishing-archive","Vanishing Archive",UnderworldDungeonRoomRole.Landmark,
                UnderworldCarrionCatacombsRoute.PreservedRuin,.26,.08,false,false,44,50,26),
            Room("defiant-work-chapel","Defiant Work Chapel",UnderworldDungeonRoomRole.Landmark,
                UnderworldCarrionCatacombsRoute.Sanctuary,.10,.03,true,false,42,44,24),
            Room("corpse-orchard-antechamber","Corpse Orchard Antechamber",UnderworldDungeonRoomRole.Landmark,
                UnderworldCarrionCatacombsRoute.BlackBloom,.72,.88,false,true,52,56,32),
        });

    public static void Validate()
    {
        var dungeon = UnderworldDungeonCatalog.GreatDecay;
        if (Rooms.Count < dungeon.TargetRoomFamilyMinimum ||
            Rooms.Count > dungeon.TargetRoomFamilyMaximum)
            throw new InvalidOperationException(
                "Carrion Catacombs room kit must stay inside the 15-20 family target.");

        foreach (var room in Rooms) room.Validate();

        if (Rooms.Select(x => x.Room.Id).Distinct(StringComparer.Ordinal).Count() != Rooms.Count)
            throw new InvalidOperationException("Carrion Catacombs room ids must be unique.");
        if (Rooms.Select(x => x.Room.ModelId).Distinct(StringComparer.Ordinal).Count() != Rooms.Count)
            throw new InvalidOperationException("Carrion Catacombs model ids must be unique.");

        foreach (UnderworldCarrionCatacombsRoute route in
                 Enum.GetValues(typeof(UnderworldCarrionCatacombsRoute)))
            if (!Rooms.Any(x => x.Route == route))
                throw new InvalidOperationException(
                    "Carrion Catacombs must expose every route state.");

        if (Rooms.Count(x => x.Route == UnderworldCarrionCatacombsRoute.Sanctuary &&
                             x.HasWorksite) < 2)
            throw new InvalidOperationException(
                "Carrion Catacombs requires at least two true Censer-friendly work/recovery spaces.");

        if (Rooms.Count(x => x.DefiantAdvantage) < 8)
            throw new InvalidOperationException(
                "Carrion Catacombs needs enough contaminated routing for Defiant progression to matter.");
        if (Rooms.Count(x => !x.DefiantAdvantage) < 5)
            throw new InvalidOperationException(
                "Carrion Catacombs must retain substantial non-Defiant routing.");

        if (!Rooms.Any(x => x.Room.Role == UnderworldDungeonRoomRole.Junction) ||
            !Rooms.Any(x => x.Room.Role == UnderworldDungeonRoomRole.Vertical) ||
            !Rooms.Any(x => x.Room.Role == UnderworldDungeonRoomRole.Hazard) ||
            !Rooms.Any(x => x.Room.Role == UnderworldDungeonRoomRole.Resource) ||
            !Rooms.Any(x => x.Room.Role == UnderworldDungeonRoomRole.Encounter) ||
            !Rooms.Any(x => x.Room.Role == UnderworldDungeonRoomRole.Landmark))
            throw new InvalidOperationException(
                "Carrion Catacombs lacks required ordinary-dungeon gameplay roles.");
    }

    public static IReadOnlyList<string> RoomFamilyIds() =>
        Array.AsReadOnly(Rooms.Select(x => x.Room.Id).ToArray());

    public static IReadOnlyList<UnderworldDungeonRoomDefinition> SpatialDefinitions() =>
        Array.AsReadOnly(Rooms.Select(x => x.Room).ToArray());

    public static UnderworldCarrionCatacombsRoomDefinition Require(string id) =>
        Rooms.Single(x => string.Equals(x.Room.Id, id, StringComparison.Ordinal));

    private static UnderworldCarrionCatacombsRoomDefinition Room(
        string suffix,string name,UnderworldDungeonRoomRole role,
        UnderworldCarrionCatacombsRoute route,double contamination,double bloom,
        bool worksite,bool advantage,double width,double depth,double height) =>
        new(
            new UnderworldDungeonRoomDefinition(
                RoomIdPrefix + suffix,
                ModelIdPrefix + suffix,
                name,role,width,depth,height),
            route,contamination,bloom,worksite,advantage);
}
