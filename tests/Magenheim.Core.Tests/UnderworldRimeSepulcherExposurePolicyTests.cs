using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldRimeSepulcherExposurePolicyTests
{
    public static int Run()
    {
        var assertions=0;
        void Check(bool value,string message)
        {
            assertions++;
            if(!value)throw new InvalidOperationException(
                "Rime exposure assertion "+assertions+" failed: "+message);
        }

        foreach(var room in UnderworldFrozenRimeSepulcherCatalog.Rooms)
            UnderworldRimeSepulcherExposurePolicy.Resolve(room).Validate();

        var shelter=UnderworldFrozenRimeSepulcherCatalog.Rooms.First(x=>
            x.Route==UnderworldRimeSepulcherRoute.Shelter);
        var shelterState=UnderworldRimeSepulcherExposurePolicy.Resolve(shelter);
        Check(shelterState.Event==UnderworldAtmosphereEvent.None&&shelterState.RoomWide,
            "Shelter must locally clear weather across the room.");
        Check(shelterState.HazardFloor01<=.06d,
            "Shelter must remain low-exposure.");

        var whiteout=UnderworldFrozenRimeSepulcherCatalog.Rooms.First(x=>
            x.Route==UnderworldRimeSepulcherRoute.WhiteoutChoke);
        var whiteoutState=UnderworldRimeSepulcherExposurePolicy.Resolve(whiteout);
        Check(whiteoutState.Event==UnderworldAtmosphereEvent.Whiteout&&
              whiteoutState.EventIntensity01>=.55d,
            "Whiteout choke must force the existing Whiteout event.");

        var shear=UnderworldFrozenRimeSepulcherCatalog.Rooms.First(x=>
            x.Route==UnderworldRimeSepulcherRoute.IceShear);
        var shearState=UnderworldRimeSepulcherExposurePolicy.Resolve(shear);
        Check(shearState.Event==UnderworldAtmosphereEvent.DeepFog&&
              shearState.HazardFloor01>=.48d,
            "Ice shear must use high cold exposure without pretending to be Whiteout.");

        var safePassage=UnderworldRimeSepulcherExposurePolicy.ResolvePassage(shelter,shelter);
        Check(safePassage.Event==UnderworldAtmosphereEvent.None&&safePassage.RoomWide,
            "Shelter-to-shelter passage must preserve recovery/readability.");

        var chokePassage=UnderworldRimeSepulcherExposurePolicy.ResolvePassage(shelter,whiteout);
        Check(chokePassage.Event==UnderworldAtmosphereEvent.Whiteout,
            "Passage entering Whiteout route must preserve the obscuration mechanic.");

        return assertions;
    }
}
