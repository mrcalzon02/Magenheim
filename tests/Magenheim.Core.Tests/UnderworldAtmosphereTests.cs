using System;
using Magenheim.Core.Underworld;

internal static class UnderworldAtmosphereTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(
                $"Underworld atmosphere assertion {assertions} failed: {message}");
        }

        UnderworldAtmosphereState State(
            UnderworldTerrainBiome biome,
            double height = UnderworldTerrainLifecycle.BaseElevationMeters,
            double water = 0d,
            double hazard = 0d,
            UnderworldAtmosphereEvent atmosphereEvent = UnderworldAtmosphereEvent.None,
            double eventIntensity = 0d,
            double resistance = 0d,
            double suppression = 0d) =>
            UnderworldAtmosphere.Evaluate(new UnderworldAtmosphereInput(
                biome, height, water, hazard, atmosphereEvent, eventIntensity, resistance, suppression));

        foreach (UnderworldTerrainBiome biome in Enum.GetValues(typeof(UnderworldTerrainBiome)))
        {
            var state = State(biome);
            Assert(state.VisualDensity01 >= 0d && state.VisualDensity01 <= 1d,
                $"{biome} visual density must remain normalized.");
            Assert(state.ParticleDensity01 >= 0d && state.ParticleDensity01 <= 1d,
                $"{biome} particle density must remain normalized.");
            Assert(state.Exposure01 >= 0d && state.Exposure01 <= 1d,
                $"{biome} exposure must remain normalized.");
            Assert(state.VisibilityMeters >= 8d,
                $"{biome} must retain a bounded nonzero visibility distance.");
        }

        var decay = State(UnderworldTerrainBiome.GreatDecay, hazard: 0.5d);
        var fungal = State(UnderworldTerrainBiome.FungalForest, hazard: 0.5d);
        Assert(decay.VisualDensity01 > fungal.VisualDensity01 &&
               decay.VisibilityMeters < fungal.VisibilityMeters,
            "Great Decay must remain the persistent heavy-obscuration biome.");

        var sulfurClear = State(UnderworldTerrainBiome.SulfurousWastes, hazard: 0d);
        var sulfurHot = State(UnderworldTerrainBiome.SulfurousWastes, hazard: 1d);
        Assert(sulfurHot.VisualDensity01 > sulfurClear.VisualDensity01 &&
               sulfurHot.Exposure01 > sulfurClear.Exposure01,
            "Sulfur miasma must intensify around hazardous geothermal terrain.");

        var blackwaterDry = State(UnderworldTerrainBiome.BlackwaterDeep, water: 0d);
        var blackwaterWet = State(UnderworldTerrainBiome.BlackwaterDeep, water: 8d);
        Assert(blackwaterWet.VisualDensity01 > blackwaterDry.VisualDensity01,
            "Blackwater mist must intensify over deep water.");

        var frozen = State(UnderworldTerrainBiome.FrozenCaverns, hazard: 0.4d);
        var whiteout = State(UnderworldTerrainBiome.FrozenCaverns, hazard: 0.4d,
            atmosphereEvent: UnderworldAtmosphereEvent.Whiteout, eventIntensity: 1d);
        Assert(whiteout.VisualDensity01 > frozen.VisualDensity01 &&
               whiteout.VisibilityMeters < frozen.VisibilityMeters &&
               whiteout.AudioDamping01 > frozen.AudioDamping01,
            "Frozen whiteout must reduce visibility and acoustically deaden the biome.");

        var resisted = State(UnderworldTerrainBiome.SulfurousWastes, hazard: 1d, resistance: 1d);
        Assert(resisted.Exposure01 < sulfurHot.Exposure01 &&
               Math.Abs(resisted.VisualDensity01 - sulfurHot.VisualDensity01) < 0.000001d,
            "Hazard resistance must not magically erase the atmosphere.");

        var suppressed = State(UnderworldTerrainBiome.GreatDecay, hazard: 0.8d,
            atmosphereEvent: UnderworldAtmosphereEvent.BlackBloom, eventIntensity: 1d,
            suppression: 1d);
        var blooming = State(UnderworldTerrainBiome.GreatDecay, hazard: 0.8d,
            atmosphereEvent: UnderworldAtmosphereEvent.BlackBloom, eventIntensity: 1d);
        Assert(suppressed.VisualDensity01 < blooming.VisualDensity01 &&
               suppressed.Exposure01 < blooming.Exposure01 &&
               suppressed.VisibilityMeters > blooming.VisibilityMeters,
            "Local suppression must create a clearer, safer pocket without changing biome identity.");

        Assert(fungal.VisibilityMeters > 500d && fungal.VisibilityMeters < 1100d,
            "Fungal forest must communicate background spore haze without obscuring nearby paths.");
        Assert(State(UnderworldTerrainBiome.FractureZones).VisibilityMeters > 8000d,
            "Clear rocky biomes must retain the monumental skyline.");
        Assert(whiteout.VisibilityMeters < 1000d, "Whiteout must still obscure the skyline.");

        var wrongEvent = State(UnderworldTerrainBiome.FrozenCaverns,
            atmosphereEvent: UnderworldAtmosphereEvent.BlackBloom, eventIntensity: 1d);
        var noEvent = State(UnderworldTerrainBiome.FrozenCaverns);
        Assert(wrongEvent == noEvent,
            "Biome-incompatible events must fail neutral instead of leaking atmosphere across biomes.");

        var sporefall = State(UnderworldTerrainBiome.FungalForest,
            atmosphereEvent: UnderworldAtmosphereEvent.Sporefall, eventIntensity: 1d);
        Assert(sporefall.ParticleDensity01 > fungal.ParticleDensity01 &&
               sporefall.Exposure01 == 0d,
            "Healthy Sporefall should thicken visible spores without becoming a poison event.");

        Assert(UnderworldAtmosphere.EventApplies(UnderworldTerrainBiome.BlackwaterDeep,
                   UnderworldAtmosphereEvent.DeepFog) &&
               UnderworldAtmosphere.EventApplies(UnderworldTerrainBiome.FrozenCaverns,
                   UnderworldAtmosphereEvent.DeepFog) &&
               !UnderworldAtmosphere.EventApplies(UnderworldTerrainBiome.SulfurousWastes,
                   UnderworldAtmosphereEvent.DeepFog),
            "Deep Fog eligibility must remain confined to moisture/cold fog ecologies.");

        Assert(UnderworldSkyLighting.Exposure(0d) == UnderworldSkyLighting.NightExposure,
            "Roof glow must remain visible at midnight.");
        Assert(UnderworldSkyLighting.Exposure(.5d) == UnderworldSkyLighting.DayExposure,
            "Roof exposure must follow the native noon phase.");
        for (var i = -100; i <= 200; i++)
        {
            var phase = i / 100d;
            var exposure = UnderworldSkyLighting.Exposure(phase);
            Assert(exposure >= .38d && exposure <= .80d, "Roof must remain subdued throughout the cycle.");
            Assert(Math.Abs(exposure - UnderworldSkyLighting.Exposure(phase + 1d)) < 1e-12d,
                "Lighting must repeat with the native day, including time skips.");
        }
        Assert(Math.Abs(UnderworldSkyLighting.Exposure(.999999d) - UnderworldSkyLighting.Exposure(.000001d)) < 1e-10d,
            "Midnight must not flash on day rollover.");
        Assert(UnderworldSkyLighting.FogBrightness(0d) < UnderworldSkyLighting.FogBrightness(.5d),
            "Ambient fog must dim with night instead of washing out the sky.");
        return assertions;
    }
}
