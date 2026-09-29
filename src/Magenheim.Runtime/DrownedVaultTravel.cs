using System;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

internal static class DrownedVaultTravel
{
    internal const string EntrancePortalName = "Magenheim_DrownedVaults_EntrancePortal";
    internal const string ReturnPortalName = "Magenheim_DrownedVaults_ReturnPortal";

    internal static DrownedVaultTravelPortal AttachEntrancePortal(Transform anchor)
    {
        if (anchor is null) throw new ArgumentNullException(nameof(anchor));
        var existing = anchor.GetComponentsInChildren<DrownedVaultTravelPortal>(true)
            .SingleOrDefault(value =>
                string.Equals(value.gameObject.name, EntrancePortalName, StringComparison.Ordinal));
        if (existing is not null) return existing;

        var root = new GameObject(EntrancePortalName) { layer = anchor.gameObject.layer };
        root.transform.SetParent(anchor, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        AddInteractionCollider(root);
        return root.AddComponent<DrownedVaultTravelPortal>();
    }

    internal static void Bind(Transform interiorRoot, GameObject entranceRoom)
    {
        if (interiorRoot is null) throw new ArgumentNullException(nameof(interiorRoot));
        if (entranceRoom is null) throw new ArgumentNullException(nameof(entranceRoom));

        var interiorAnchor = interiorRoot.parent
            ?? throw new InvalidOperationException(
                "Drowned Vault interior root is detached from its interior anchor.");
        if (!string.Equals(
                interiorAnchor.name,
                DrownedVaultEntranceVisuals.InteriorAnchorName,
                StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Drowned Vault interior is not attached to the authoritative buried anchor.");

        var locationRoot = interiorAnchor.parent
            ?? throw new InvalidOperationException(
                "Drowned Vault buried interior anchor is detached from its location root.");
        var exteriorAnchor = locationRoot.GetComponentsInChildren<Transform>(true)
            .SingleOrDefault(value =>
                string.Equals(
                    value.name,
                    DrownedVaultEntranceVisuals.EntrancePortalAnchorName,
                    StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                "Drowned Vault location has no authoritative exterior sinkhole anchor.");

        var entrancePortal = AttachEntrancePortal(exteriorAnchor);
        var returnPortal = GetOrCreateReturnPortal(entranceRoom.transform);
        var room = entranceRoom.GetComponent<Room>()
            ?? throw new InvalidOperationException(
                "Drowned Vault entrance room has no Room component.");

        var halfWidth = Mathf.Max(8f, room.m_size.x * .5f);
        var halfDepth = Mathf.Max(8f, room.m_size.z * .5f);
        // Mixed entrance rooms author dry shelves on the +/-X edges. Arrive on one of them,
        // above the native Y=0 waterline, never in the center of the pool.
        var interiorArrival = entranceRoom.transform.TransformPoint(
            new Vector3(-halfWidth * .66f, 1.25f, -halfDepth * .42f));
        returnPortal.transform.localPosition =
            new Vector3(-halfWidth * .72f, 1.15f, -halfDepth * .62f);

        var exteriorArrival =
            exteriorAnchor.TransformPoint(new Vector3(0f, 1.15f, 3.8f));
        entrancePortal.Configure(
            interiorArrival,
            entranceRoom.transform.rotation,
            "Drowned Vaults");
        returnPortal.Configure(
            exteriorArrival,
            exteriorAnchor.rotation,
            "Blackwater Deep");
    }

    private static DrownedVaultTravelPortal GetOrCreateReturnPortal(Transform room)
    {
        var existing = room.GetComponentsInChildren<DrownedVaultTravelPortal>(true)
            .SingleOrDefault(value =>
                string.Equals(value.gameObject.name, ReturnPortalName, StringComparison.Ordinal));
        if (existing is not null) return existing;

        var root = new GameObject(ReturnPortalName) { layer = room.gameObject.layer };
        root.transform.SetParent(room, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        AddInteractionCollider(root);
        return root.AddComponent<DrownedVaultTravelPortal>();
    }

    private static void AddInteractionCollider(GameObject root)
    {
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.0f, 0f);
        collider.size = new Vector3(3.2f, 3.4f, .70f);
        collider.isTrigger = false;
    }
}

internal sealed class DrownedVaultTravelPortal : MonoBehaviour, Hoverable, Interactable
{
    private bool _configured;
    private Vector3 _target;
    private Quaternion _rotation = Quaternion.identity;
    private string _label = "Drowned Vaults";

    internal void Configure(Vector3 target, Quaternion rotation, string label)
    {
        _target = target;
        _rotation = rotation;
        _label = string.IsNullOrWhiteSpace(label) ? "Drowned Vaults" : label;
        _configured = true;
    }

    public string GetHoverText() =>
        _configured
            ? $"[<color=yellow><b>$KEY_Use</b></color>] Enter {_label}"
            : "The drowned passage is dormant";

    public string GetHoverName() => _label;
    public float GetHoverOffset() => 0f;

    public bool Interact(Humanoid character, bool hold, bool alt)
    {
        if (hold || !_configured || character is null) return false;
        return character.TeleportTo(_target, _rotation, false);
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
}
