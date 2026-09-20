namespace Magenheim.Runtime;

internal enum UnderworldGateRole
{
    EnterUnderworld,
    ReturnToSurface,
}

/// <summary>
/// Marks which side of the Deep Gate this prefab represents.
/// It carries no player state or transition lifecycle; gate interaction delegates ordinary player
/// movement to Valheim and only switches the Magenheim-owned Underworld instance adapter.
/// </summary>
internal sealed class UnderworldGateEndpoint : UnityEngine.MonoBehaviour
{
    internal UnderworldGateRole Role { get; set; } = UnderworldGateRole.EnterUnderworld;
}
