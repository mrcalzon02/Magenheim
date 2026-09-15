using System;

namespace Magenheim.Core.Socketing;

public enum SocketMigrationDisposition
{
    NoMigrationRequired = 0,
    Ready = 1,
    CapacityConflict = 2,
    Blocked = 3,
}

public sealed record SocketMigrationPlan(
    SocketMigrationDisposition Disposition,
    SocketState SourceMagenheimState,
    int TargetCapacity,
    bool PreserveSourceMetadata,
    string ExpectedTargetStateToken,
    string Diagnostic)
{
    public bool IsReady => Disposition == SocketMigrationDisposition.Ready;
}

/// <summary>
/// Plans migration without mutating either provider. Runtime adapters must perform
/// write -> readback -> equality verification before marking Jewelcrafting authoritative.
/// Existing Magenheim metadata is deliberately retained as recovery data until that
/// verification succeeds.
/// </summary>
public static class SocketMigrationPlanner
{
    public static SocketMigrationPlan PlanMagenheimToExternal(
        SocketState source,
        EquipmentSocketSnapshot target,
        bool integrationEnabled,
        bool preserveSourceMetadata = true)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (target is null) throw new ArgumentNullException(nameof(target));
        target.Validate();

        if (!integrationEnabled)
            return Blocked(source, target, preserveSourceMetadata, "External socket migration is disabled by configuration.");

        if (source.UnlockedSlots == 0 && source.InstalledCrystals.Count == 0)
        {
            return new SocketMigrationPlan(
                SocketMigrationDisposition.NoMigrationRequired,
                source,
                target.Capacity,
                preserveSourceMetadata,
                target.ProviderStateToken,
                "The item has no Magenheim socket state to migrate.");
        }

        if (source.InstalledCrystals.Count > target.Capacity)
        {
            return new SocketMigrationPlan(
                SocketMigrationDisposition.CapacityConflict,
                source,
                target.Capacity,
                preserveSourceMetadata,
                target.ProviderStateToken,
                $"Migration requires {source.InstalledCrystals.Count} slot(s), but the external provider exposes only {target.Capacity}; no state may be discarded.");
        }

        if (target.MagenheimCrystals.Count > 0)
            return Blocked(source, target, preserveSourceMetadata, "The external provider already contains Magenheim crystals; automatic migration is blocked to prevent duplicate installation.");

        return new SocketMigrationPlan(
            SocketMigrationDisposition.Ready,
            source,
            target.Capacity,
            preserveSourceMetadata,
            target.ProviderStateToken,
            "Migration is ready. The runtime must verify the target state token, write all crystals, read them back, and only then mark external storage authoritative.");
    }

    private static SocketMigrationPlan Blocked(
        SocketState source,
        EquipmentSocketSnapshot target,
        bool preserveSourceMetadata,
        string diagnostic) => new(
            SocketMigrationDisposition.Blocked,
            source,
            target.Capacity,
            preserveSourceMetadata,
            target.ProviderStateToken,
            diagnostic);
}