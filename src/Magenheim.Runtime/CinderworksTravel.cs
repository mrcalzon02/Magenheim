using System;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class CinderworksTravel
{
    internal const string EntrancePortalName = "Magenheim_Cinderworks_EntrancePortal";
    internal const string ReturnPortalName = "Magenheim_Cinderworks_ReturnPortal";

    internal static CinderworksTravelPortal AttachEntrancePortal(Transform anchor)
    {
        if (anchor is null) throw new ArgumentNullException(nameof(anchor));
        var existing = anchor.GetComponentsInChildren<CinderworksTravelPortal>(true)
            .SingleOrDefault(x => string.Equals(x.gameObject.name, EntrancePortalName, StringComparison.Ordinal));
        if (existing is not null) return existing;
        var root = new GameObject(EntrancePortalName) { layer = anchor.gameObject.layer };
        root.transform.SetParent(anchor, false);
        AddCollider(root);
        return root.AddComponent<CinderworksTravelPortal>();
    }

    internal static void Bind(Transform interiorRoot, GameObject entranceRoom)
    {
        if (interiorRoot is null) throw new ArgumentNullException(nameof(interiorRoot));
        if (entranceRoom is null) throw new ArgumentNullException(nameof(entranceRoom));
        var interiorAnchor = interiorRoot.parent
            ?? throw new InvalidOperationException("Cinderworks interior root has no anchor.");
        if (!string.Equals(interiorAnchor.name, CinderworksEntranceVisuals.InteriorAnchorName, StringComparison.Ordinal))
            throw new InvalidOperationException("Cinderworks interior root is not on the authoritative buried anchor.");
        var locationRoot = interiorAnchor.parent
            ?? throw new InvalidOperationException("Cinderworks interior anchor has no location root.");
        var exteriorAnchor = locationRoot.GetComponentsInChildren<Transform>(true)
            .SingleOrDefault(x => string.Equals(x.name, CinderworksEntranceVisuals.EntrancePortalAnchorName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Cinderworks entrance portal anchor is missing.");

        var entrancePortal = AttachEntrancePortal(exteriorAnchor);
        var returnPortal = GetOrCreateReturnPortal(entranceRoom.transform);
        var room = entranceRoom.GetComponent<Room>()
            ?? throw new InvalidOperationException("Cinderworks entrance room has no Room component.");
        var halfDepth = Mathf.Max(8f, room.m_size.z * .5f);
        var interiorArrival = entranceRoom.transform.TransformPoint(
            new Vector3(-room.m_size.x * .28f, 1.05f, -halfDepth * .48f));
        returnPortal.transform.localPosition =
            new Vector3(-room.m_size.x * .30f, 1.0f, -halfDepth * .70f);
        var exteriorArrival = exteriorAnchor.TransformPoint(new Vector3(0f, .55f, 3.6f));
        entrancePortal.Configure(interiorArrival, entranceRoom.transform.rotation, "Cinderworks");
        returnPortal.Configure(exteriorArrival, exteriorAnchor.rotation, "Sulfurous Wastes");
    }

    private static CinderworksTravelPortal GetOrCreateReturnPortal(Transform room)
    {
        var existing = room.GetComponentsInChildren<CinderworksTravelPortal>(true)
            .SingleOrDefault(x => string.Equals(x.gameObject.name, ReturnPortalName, StringComparison.Ordinal));
        if (existing is not null) return existing;
        var root = new GameObject(ReturnPortalName) { layer = room.gameObject.layer };
        root.transform.SetParent(room, false);
        AddCollider(root);
        return root.AddComponent<CinderworksTravelPortal>();
    }

    private static void AddCollider(GameObject root)
    {
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.1f, 0f);
        collider.size = new Vector3(2.8f, 2.6f, .60f);
        collider.isTrigger = false;
    }
}

internal sealed class CinderworksTravelPortal : MonoBehaviour, Hoverable, Interactable
{
    private bool _configured;
    private Vector3 _target;
    private Quaternion _rotation = Quaternion.identity;
    private string _label = "Cinderworks";

    internal void Configure(Vector3 target, Quaternion rotation, string label)
    {
        _target = target; _rotation = rotation;
        _label = string.IsNullOrWhiteSpace(label) ? "Cinderworks" : label;
        _configured = true;
    }

    public string GetHoverText() => _configured
        ? $"[<color=yellow><b>$KEY_Use</b></color>] Enter {_label}"
        : "The furnace passage is dormant";
    public string GetHoverName() => _label;
    public float GetHoverOffset() => 0f;
    public bool Interact(Humanoid character, bool hold, bool alt)
    {
        if (hold || !_configured || character is null) return false;
        return character.TeleportTo(_target, _rotation, false);
    }
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}
