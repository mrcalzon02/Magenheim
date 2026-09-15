using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Explicit non-random travel edge used for Deep Fracture loop/shortcut connections.
/// Targets are restored deterministically from the dungeon seed after generation or load.
/// </summary>
internal sealed class DeepFractureTraversalPortal : MonoBehaviour, Hoverable, Interactable
{
    private bool _configured;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation = Quaternion.identity;
    private string _label = "Fracture Transit";

    internal void Configure(Vector3 targetPosition, Quaternion targetRotation, string label)
    {
        _targetPosition = targetPosition;
        _targetRotation = targetRotation;
        _label = string.IsNullOrWhiteSpace(label) ? "Fracture Transit" : label;
        _configured = true;
    }

    internal void ClearConfiguration()
    {
        _configured = false;
        _label = "Fracture Transit";
    }

    public string GetHoverText()
        => _configured
            ? $"[<color=yellow><b>$KEY_Use</b></color>] Traverse {_label}"
            : "Fracture transit is dormant";

    public string GetHoverName() => _label;

    public bool Interact(Humanoid character, bool hold, bool alt)
    {
        if (hold || !_configured || character is null)
            return false;

        return character.TeleportTo(_targetPosition, _targetRotation, false);
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}

/// <summary>
/// Marker restricting the exact-plan Harmony boundary to Magenheim-owned generators only.
/// </summary>
internal sealed class DeepFractureGeneratorMarker : MonoBehaviour
{
}
