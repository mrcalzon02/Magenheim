using System;
using System.Linq;
using Magenheim.Core.DeepFractures;
using Magenheim.Core.Worldgen;

internal static class DeepFractureLocationRegistrationTests
{
    public static int Run()
    {
        var assertions = 0;

        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException($"Deep Fracture location assertion {assertions} failed: {message}");
        }

        var location = DeepFractureLocationCatalog.SurfaceFractureEntrance;
        location.Validate();
        Assert(location.RegistrationKey == "magenheim.location.deep_fracture_entrance", "Surface fracture registration key drifted.");
        Assert(location.PrefabName == "Magenheim_DeepFracture_Entrance", "Surface fracture prefab identity drifted.");
        Assert(location.BiomeArea == SpawnArea.All, "Surface fracture entrance must permit both median and edge biome areas.");
        Assert(location.Biomes.Count == 8, "Surface fracture entrance must target all eight designed land biomes.");
        Assert(location.Biomes.Contains("Meadows") && location.Biomes.Contains("DeepNorth"), "Surface fracture biome coverage lost an endpoint biome.");
        Assert(!location.ClearArea, "Surface fracture should preserve surrounding vegetation rather than clear a large surface footprint.");

        var addPlan = DeepFractureLocationRegistrationPlanner.Build(
            new[] { location },
            Array.Empty<ObservedDeepFractureLocation>());
        Assert(!addPlan.HasErrors, "Unoccupied canonical fracture entrance should not produce a planning error.");
        Assert(addPlan.Entries.Single().Action == DeepFractureLocationRegistrationAction.Add, "Unoccupied canonical fracture entrance should be addable.");

        var occupied = new[]
        {
            new ObservedDeepFractureLocation(location.PrefabName, "foreign-or-earlier-host-registration"),
        };
        var occupiedBefore = occupied[0];
        var skipPlan = DeepFractureLocationRegistrationPlanner.Build(new[] { location }, occupied);
        Assert(skipPlan.Entries.Single().Action == DeepFractureLocationRegistrationAction.Skip, "Occupied fracture prefab identity must be skipped non-destructively.");
        Assert(Equals(occupiedBefore, occupied[0]), "Collision planning must not mutate the observed host registration.");

        var unrelatedPlan = DeepFractureLocationRegistrationPlanner.Build(
            new[] { location },
            new[] { new ObservedDeepFractureLocation("OtherMod_Unrelated_Location", "othermod") });
        Assert(unrelatedPlan.Entries.Single().Action == DeepFractureLocationRegistrationAction.Add, "Unrelated foreign locations must not block Magenheim's own identity.");

        var duplicateDesiredPlan = DeepFractureLocationRegistrationPlanner.Build(
            new[] { location, location },
            Array.Empty<ObservedDeepFractureLocation>());
        Assert(duplicateDesiredPlan.HasErrors, "Duplicate desired location identities must fail closed.");
        Assert(duplicateDesiredPlan.Entries.Last().Action == DeepFractureLocationRegistrationAction.Error, "Second duplicate desired location must be an error rather than a second add.");

        var foreignKey = location with { RegistrationKey = "othermod.location.deep_fracture" };
        var foreignKeyPlan = DeepFractureLocationRegistrationPlanner.Build(
            new[] { foreignKey },
            Array.Empty<ObservedDeepFractureLocation>());
        Assert(foreignKeyPlan.HasErrors, "Deep Fracture location planner must reject foreign registration namespaces.");

        var foreignPrefab = location with { PrefabName = "OtherMod_DeepFracture_Entrance" };
        var foreignPrefabPlan = DeepFractureLocationRegistrationPlanner.Build(
            new[] { foreignPrefab },
            Array.Empty<ObservedDeepFractureLocation>());
        Assert(foreignPrefabPlan.HasErrors, "Deep Fracture location planner must reject foreign prefab identities.");

        var duplicateBiome = location with { Biomes = new[] { "Meadows", "Meadows" } };
        var duplicateBiomePlan = DeepFractureLocationRegistrationPlanner.Build(
            new[] { duplicateBiome },
            Array.Empty<ObservedDeepFractureLocation>());
        Assert(duplicateBiomePlan.HasErrors, "Duplicate biome configuration must fail before runtime translation.");

        return assertions;
    }
}
