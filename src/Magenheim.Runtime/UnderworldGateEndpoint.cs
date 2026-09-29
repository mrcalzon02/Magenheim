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
    internal const float ServerInteractionRange = 6f;
    internal UnderworldGateRole Role { get; set; } = UnderworldGateRole.EnterUnderworld;

    internal static bool IsMatchingGateWithinRange(Player player, UnderworldGateRole role)
    {
        if (!player) return false;
        var playerScene = player.transform.root.gameObject.scene.handle;
        var point = player.transform.position;
        foreach (var endpoint in UnityEngine.Object.FindObjectsByType<UnderworldGateEndpoint>(FindObjectsSortMode.None))
        {
            if (!endpoint || !endpoint.isActiveAndEnabled || endpoint.Role != role) continue;
            if (endpoint.transform.root.gameObject.scene.handle != playerScene) continue;

            var best = float.PositiveInfinity;
            foreach (var collider in endpoint.GetComponentsInChildren<Collider>(true))
            {
                if (!collider || !collider.enabled || collider.isTrigger) continue;
                best = Mathf.Min(best, Vector3.Distance(point, collider.bounds.ClosestPoint(point)));
            }
            if (float.IsPositiveInfinity(best))
            {
                foreach (var renderer in endpoint.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer || !renderer.enabled) continue;
                    best = Mathf.Min(best, Vector3.Distance(point, renderer.bounds.ClosestPoint(point)));
                }
            }

            if (best <= ServerInteractionRange) return true;
        }
        return false;
    }

    public string GetHoverText() =>
        $"[<color=yellow><b>$KEY_Use</b></color>] {(Role == UnderworldGateRole.EnterUnderworld ? "Descend" : "Return to Surface")}";

    public string GetHoverName() =>
        Role == UnderworldGateRole.EnterUnderworld ? "Deep Gate" : "Deep Gate — Surface";

    public float GetHoverOffset() => 0f;

    public bool Interact(Humanoid character, bool hold, bool alt)
    {
        if (hold || character is not Player) return false;
        if (UnderworldGateTransitRpc.TryTransit(Role, (Player)character, out var diagnostic)) return true;
        if (character is Player failedPlayer && !string.IsNullOrEmpty(diagnostic))
            failedPlayer.Message(MessageHud.MessageType.Center, diagnostic);
        return false;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}
