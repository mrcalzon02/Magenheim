using System;
using System.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Binds the surface fracture entrance to DF-01 and installs a deterministic return node inside
/// the expedition. Both destinations are derived from the spawned location hierarchy, so travel
/// survives save/load without client-local mutable routing state.
/// </summary>
internal static class DeepFractureSurfaceTravel
{
    internal const string SurfacePortalName = "Magenheim_DeepFracture_SurfacePortal";
    internal const string ReturnPortalName = "Magenheim_DeepFracture_ReturnPortal";

    internal static DeepFractureTraversalPortal AttachSurfacePortal(Transform anchor)
    {
        if (anchor is null)
            throw new ArgumentNullException(nameof(anchor));

        var existing = anchor.GetComponentsInChildren<DeepFractureTraversalPortal>(true)
            .SingleOrDefault(portal => string.Equals(portal.gameObject.name, SurfacePortalName, StringComparison.Ordinal));
        if (existing is not null)
            return existing;

        var portalObject = new GameObject(SurfacePortalName);
        portalObject.transform.SetParent(anchor, false);
        portalObject.transform.localPosition = Vector3.zero;
        portalObject.transform.localRotation = Quaternion.identity;
        return portalObject.AddComponent<DeepFractureTraversalPortal>();
    }

    internal static void Bind(Transform interiorRoot, GameObject descentDistrict)
    {
        if (interiorRoot is null)
            throw new ArgumentNullException(nameof(interiorRoot));
        if (descentDistrict is null)
            throw new ArgumentNullException(nameof(descentDistrict));

        var anchor = interiorRoot.parent
            ?? throw new InvalidOperationException("Deep Fracture interior root is detached from its entrance anchor.");
        if (!string.Equals(anchor.name, DeepFractureEntranceVisuals.InteriorAnchorName, StringComparison.Ordinal))
            throw new InvalidOperationException("Deep Fracture interior root is not attached to the authoritative entrance anchor.");

        var surfacePortal = AttachSurfacePortal(anchor);
        var returnPortal = GetOrCreateReturnPortal(descentDistrict.transform);

        // DF-01 is the authoritative arrival/return district. Offsets keep both travel endpoints
        // clear of the descending stair geometry and prevent immediate portal re-trigger overlap.
        var interiorArrival = descentDistrict.transform.TransformPoint(new Vector3(0f, 2f, 30f));
        var surfaceArrival = anchor.TransformPoint(new Vector3(0f, 0.5f, 2.5f));
        var interiorFacing = descentDistrict.transform.rotation;
        var surfaceFacing = anchor.rotation;

        surfacePortal.Configure(interiorArrival, interiorFacing, "Deep Fracture");
        returnPortal.Configure(surfaceArrival, surfaceFacing, "Surface Fracture");
    }

    private static DeepFractureTraversalPortal GetOrCreateReturnPortal(Transform descentDistrict)
    {
        var existing = descentDistrict.GetComponentsInChildren<DeepFractureTraversalPortal>(true)
            .SingleOrDefault(portal => string.Equals(portal.gameObject.name, ReturnPortalName, StringComparison.Ordinal));
        if (existing is not null)
            return existing;

        var portalObject = new GameObject(ReturnPortalName);
        portalObject.transform.SetParent(descentDistrict, false);
        portalObject.transform.localPosition = new Vector3(0f, 1f, 34f);
        portalObject.transform.localRotation = Quaternion.identity;
        return portalObject.AddComponent<DeepFractureTraversalPortal>();
    }
}
