using UnityEngine;

namespace Magenheim.Runtime;

internal enum UnderworldGateRole
{
    EnterUnderworld,
    ReturnToSurface,
}

/// <summary>
/// Thin physical Deep Gate endpoint. It owns no persistent player state or world truth; interaction
/// delegates movement to Valheim and context switching to <see cref="UnderworldGateTransitRuntime"/>.
/// </summary>
internal sealed class UnderworldGateEndpoint : MonoBehaviour, Hoverable, Interactable
{
    internal UnderworldGateRole Role { get; set; } = UnderworldGateRole.EnterUnderworld;

    public string GetHoverText() =>
        $"[<color=yellow><b>$KEY_Use</b></color>] {(Role == UnderworldGateRole.EnterUnderworld ? "Descend" : "Return to Surface")}";

    public string GetHoverName() =>
        Role == UnderworldGateRole.EnterUnderworld ? "Deep Gate" : "Deep Gate — Surface";

    public float GetHoverOffset() => 0f;

    public bool Interact(Humanoid character, bool hold, bool alt)
    {
        if (hold || character is null) return false;
        if (UnderworldGateTransitRuntime.TryTransit(Role, character, out var diagnostic)) return true;
        if (character is Player player && !string.IsNullOrEmpty(diagnostic))
            player.Message(MessageHud.MessageType.Center, diagnostic);
        return false;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}
