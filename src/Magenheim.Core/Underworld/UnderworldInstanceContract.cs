namespace Magenheim.Core.Underworld;

/// <summary>
/// Hard architectural contract for the Underworld. These are not configuration switches.
/// Runtime adapters must conform to this contract rather than reinterpret the Underworld to fit
/// Surface-world implementation conveniences.
/// </summary>
public static class UnderworldInstanceContract
{
    public const string ContractId = "magenheim.underworld.multi-world-save.v2";

    public const bool DedicatedInstanceWorldSpace = true;
    public const bool DedicatedInstanceMap = true;
    public const bool DedicatedInstanceExploration = true;
    public const bool DedicatedInstanceTerrainAuthority = true;
    public const bool DedicatedInstanceBiomeEnvironmentAuthority = true;
    public const bool DedicatedInstancePersistenceNamespace = true;
    public const bool UsesVanillaMinimapMechanics = true;
    public const bool SingleSaveContainsMultipleWorldInstances = true;
    public const bool NativeWorldServicesAreInstanceScoped = true;
    public const bool PlayersMayOccupyDifferentInstancesConcurrently = true;

    public const bool ParallelMapEngine = false;
    public const bool ParallelFogOfWarEngine = false;
    public const bool ParallelPinEngine = false;
    public const bool SurfaceFarLandmassIsUnderworld = false;
    public const bool SurfaceCoordinateProjectionDefinesUnderworld = false;
    public const bool SurfaceMapIsUnderworldMap = false;
    public const bool PlayerPopulationControlsInstanceLifecycle = false;
    public const bool SeparateUserSelectedSave = false;
    public const bool CoordinateOffsetDefinesInstanceIdentity = false;
    public const bool VerticalEngineLayerDefinesInstanceIdentity = false;
    public const bool WholeServerWorldSwapDefinesInstanceTransit = false;
    public const bool SecondServerProcessDefinesInstanceTransit = false;
}
