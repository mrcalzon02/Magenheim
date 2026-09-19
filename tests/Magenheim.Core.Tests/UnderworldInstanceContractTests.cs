using System;
using Magenheim.Core.Underworld;

internal static class UnderworldInstanceContractTests
{
    public static int Run()
    {
        var assertions = 0;
        void Assert(bool condition, string message)
        {
            assertions++;
            if (!condition)
                throw new InvalidOperationException($"Underworld instance-contract assertion {assertions} failed: {message}");
        }

        Assert(UnderworldInstanceContract.DedicatedInstanceWorldSpace, "Underworld must remain a dedicated instance world-space.");
        Assert(UnderworldInstanceContract.DedicatedInstanceMap, "Underworld must own a dedicated instance map.");
        Assert(UnderworldInstanceContract.DedicatedInstanceExploration, "Underworld must own independent exploration state.");
        Assert(UnderworldInstanceContract.DedicatedInstanceTerrainAuthority, "Underworld must own terrain authority.");
        Assert(UnderworldInstanceContract.DedicatedInstanceBiomeEnvironmentAuthority, "Underworld must own biome/environment authority.");
        Assert(UnderworldInstanceContract.DedicatedInstancePersistenceNamespace, "Underworld must own its persistence namespace.");
        Assert(!UnderworldInstanceContract.SurfaceFarLandmassIsUnderworld, "A distant Surface landmass must never become Underworld authority.");
        Assert(!UnderworldInstanceContract.SurfaceCoordinateProjectionDefinesUnderworld, "Surface coordinate projection must never define Underworld geography.");
        Assert(!UnderworldInstanceContract.SurfaceMapIsUnderworldMap, "The Surface minimap must never be the Underworld map.");
        Assert(!UnderworldInstanceContract.PlayerPopulationControlsInstanceLifecycle, "Player population must never control Underworld lifecycle.");
        Assert(!UnderworldInstanceContract.SeparateUserSelectedSave, "The Underworld must not become a user-selected second save.");
        return assertions;
    }
}
