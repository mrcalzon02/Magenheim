using System;
using System.Linq;
using Magenheim.Core.Underworld;

internal static class UnderworldCarrionCatacombsContaminationPolicyTests
{
    public static int Run()
    {
        var assertions=0;
        void Check(bool value,string message)
        {
            assertions++;
            if(!value)throw new InvalidOperationException(
                "Carrion contamination assertion "+assertions+" failed: "+message);
        }

        foreach(var room in UnderworldGreatDecayCarrionCatacombsCatalog.Rooms)
            UnderworldCarrionCatacombsContaminationPolicy.Resolve(room).Validate();

        var sanctuary=UnderworldGreatDecayCarrionCatacombsCatalog.Rooms.First(x=>
            x.Route==UnderworldCarrionCatacombsRoute.Sanctuary);
        var sanctuaryState=UnderworldCarrionCatacombsContaminationPolicy.Resolve(sanctuary);
        Check(sanctuaryState.Event==UnderworldAtmosphereEvent.None&&sanctuaryState.RoomWide,
            "Sanctuary must keep a broad low-pressure work area.");
        Check(sanctuaryState.HazardFloor01<=.10d,
            "Sanctuary contamination floor must remain low.");

        var bloom=UnderworldGreatDecayCarrionCatacombsCatalog.Rooms.First(x=>
            x.Route==UnderworldCarrionCatacombsRoute.BlackBloom);
        var bloomState=UnderworldCarrionCatacombsContaminationPolicy.Resolve(bloom);
        Check(bloomState.Event==UnderworldAtmosphereEvent.BlackBloom&&
              bloomState.EventIntensity01>=.58d,
            "Black Bloom rooms must reuse the existing Great Decay event.");

        var root=UnderworldGreatDecayCarrionCatacombsCatalog.Rooms.First(x=>
            x.Route==UnderworldCarrionCatacombsRoute.RootIngress);
        var rootState=UnderworldCarrionCatacombsContaminationPolicy.Resolve(root);
        Check(rootState.Event==UnderworldAtmosphereEvent.BlackBloom&&
              rootState.HazardFloor01>=.42d,
            "Root ingress must create mitigatable contamination pressure.");

        var safePassage=UnderworldCarrionCatacombsContaminationPolicy.ResolvePassage(
            sanctuary,sanctuary);
        Check(safePassage.Event==UnderworldAtmosphereEvent.None&&safePassage.RoomWide,
            "Sanctuary-to-sanctuary passage must preserve a work/recovery route.");

        var bloomPassage=UnderworldCarrionCatacombsContaminationPolicy.ResolvePassage(
            sanctuary,bloom);
        Check(bloomPassage.Event==UnderworldAtmosphereEvent.BlackBloom&&
              bloomPassage.HazardFloor01>=.52d,
            "Passage entering a bloom route must preserve contamination pressure.");

        return assertions;
    }
}
