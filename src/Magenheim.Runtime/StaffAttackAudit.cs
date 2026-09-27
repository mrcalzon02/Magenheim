namespace Magenheim.Runtime;

/// <summary>
/// Marker retained for source-history continuity after the temporary staff attack audit was retired.
/// The multi-burst failure was traced to the StaffIceShards donor attack ending before deferred
/// projectile bursts could execute; StaffBurstContract now owns that compatibility correction.
/// </summary>
internal static class StaffAttackAudit
{
}
