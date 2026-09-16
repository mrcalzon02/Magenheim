namespace Magenheim.Runtime;

internal enum UnderworldGateRole
{
    EnterUnderworld,
    ReturnToSurface,
}

/// <summary>
/// Declares which side of the paired Deep Gate a prefab instance represents. The actual target
/// is never a free coordinate: entry persists the exact surface source anchor and the return side
/// resolves that persisted anchor through UnderworldTransitionRules.BeginReturn.
/// </summary>
internal sealed class UnderworldGateEndpoint : UnityEngine.MonoBehaviour
{
    internal UnderworldGateRole Role { get; set; } = UnderworldGateRole.EnterUnderworld;
}
