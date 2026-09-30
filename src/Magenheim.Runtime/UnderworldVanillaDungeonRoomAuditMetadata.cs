using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Runtime-only provenance carried by every expanded vanilla donor-room clone.
/// This lets installed-game DDE validation compare the placed private room against the donor
/// geometry/bounds that existed before Magenheim scaled it, without mutating the donor prefab.
/// </summary>
internal sealed class UnderworldVanillaDungeonRoomAuditMetadata : MonoBehaviour
{
    [SerializeField] internal string DonorRoomName = string.Empty;
    [SerializeField] internal Vector3Int DonorRoomSize;
    [SerializeField] internal Vector3[] DonorConnectionLocalPositions = System.Array.Empty<Vector3>();
    [SerializeField] internal float LinearScale;
}
