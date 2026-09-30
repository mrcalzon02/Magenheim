using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime-only provenance carried by every expanded vanilla donor-room clone.
/// This lets installed-game DDE validation compare the placed private room against the donor
/// geometry/bounds that existed before Magenheim scaled it, without mutating the donor prefab.
/// </summary>
internal sealed class UnderworldVanillaDungeonRoomAuditMetadata : MonoBehaviour
{
    internal string DonorRoomName { get; set; } = string.Empty;
    internal Vector3Int DonorRoomSize { get; set; }
    internal Vector3[] DonorConnectionLocalPositions { get; set; } = System.Array.Empty<Vector3>();
    internal float LinearScale { get; set; }
}
