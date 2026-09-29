using System;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class RootwarrenTravel
{
    internal const string EntrancePortalName = "Magenheim_Rootwarren_EntrancePortal";
    internal const string ReturnPortalName = "Magenheim_Rootwarren_ReturnPortal";

    internal static RootwarrenTravelPortal AttachEntrancePortal(Transform anchor)
    {
        if (anchor is null) throw new ArgumentNullException(nameof(anchor));

        var existing = anchor.GetComponentsInChildren<RootwarrenTravelPortal>(true)
            .SingleOrDefault(value =>
                string.Equals(value.gameObject.name, EntrancePortalName, StringComparison.Ordinal));
        if (existing is not null) return existing;

        var root = new GameObject(EntrancePortalName) { layer = anchor.gameObject.layer };
        root.transform.SetParent(anchor, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        AddInteractionCollider(root);
        return root.AddComponent<RootwarrenTravelPortal>();
    }

    internal static void Bind(Transform interiorRoot, GameObject entranceRoom)
    {
        if (interiorRoot is null) throw new ArgumentNullException(nameof(interiorRoot));
        if (entranceRoom is null) throw new ArgumentNullException(nameof(entranceRoom));

        var anchor = interiorRoot.parent
            ?? throw new InvalidOperationException(
                "Rootwarren interior root is detached from its entrance anchor.");
        if (!string.Equals(
                anchor.name,
                RootwarrenEntranceVisuals.InteriorAnchorName,
                StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Rootwarren interior root is not attached to the authoritative entrance anchor.");

        var entrancePortal = AttachEntrancePortal(anchor);
        var returnPortal = GetOrCreateReturnPortal(entranceRoom.transform);

        var room = entranceRoom.GetComponent<Room>()
            ?? throw new InvalidOperationException(
                "Rootwarren entrance room has no Room component.");
        var halfDepth = Mathf.Max(8f, room.m_size.z * .5f);

        var interiorArrival = entranceRoom.transform.TransformPoint(
            new Vector3(0f, 1.1f, -halfDepth * .48f));
        var returnLocal = new Vector3(0f, 1.05f, -halfDepth * .78f);
        returnPortal.transform.localPosition = returnLocal;

        var exteriorArrival = anchor.TransformPoint(new Vector3(0f, .45f, 3.4f));
        entrancePortal.Configure(
            interiorArrival,
            entranceRoom.transform.rotation,
            "Rootwarren");
        returnPortal.Configure(
            exteriorArrival,
            anchor.rotation,
            "Fungal Forest");
    }

    private static RootwarrenTravelPortal GetOrCreateReturnPortal(Transform room)
    {
        var existing = room.GetComponentsInChildren<RootwarrenTravelPortal>(true)
            .SingleOrDefault(value =>
                string.Equals(value.gameObject.name, ReturnPortalName, StringComparison.Ordinal));
        if (existing is not null) return existing;

        var root = new GameObject(ReturnPortalName) { layer = room.gameObject.layer };
        root.transform.SetParent(room, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        AddInteractionCollider(root);
        return root.AddComponent<RootwarrenTravelPortal>();
    }

    private static void AddInteractionCollider(GameObject root)
    {
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.1f, 0f);
        collider.size = new Vector3(2.8f, 2.6f, .55f);
        collider.isTrigger = false;
    }
}

/// <summary>
/// Same-instance travel edge for a Rootwarren entrance/return pair. It never changes Underworld
/// world-instance membership; the location remains a dungeon inside the Fungal Forest.
/// </summary>
internal sealed class RootwarrenTravelPortal : MonoBehaviour, Hoverable, Interactable
{
    private bool _configured;
    private Vector3 _target;
    private Quaternion _rotation = Quaternion.identity;
    private string _label = "Rootwarren";

    internal void Configure(Vector3 target, Quaternion rotation, string label)
    {
        _target = target;
        _rotation = rotation;
        _label = string.IsNullOrWhiteSpace(label) ? "Rootwarren" : label;
        _configured = true;
    }

    public string GetHoverText() =>
        _configured
            ? $"[<color=yellow><b>$KEY_Use</b></color>] Enter {_label}"
            : "The root passage is dormant";

    public string GetHoverName() => _label;
    public float GetHoverOffset() => 0f;

    public bool Interact(Humanoid character, bool hold, bool alt)
    {
        if (hold || !_configured || character is null) return false;
        return character.TeleportTo(_target, _rotation, false);
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}
