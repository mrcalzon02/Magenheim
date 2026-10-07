using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Native solid contacts at authored snap planes, independent of wear visuals.</summary>
internal static class ConstructionJointContacts
{
    // Installed WearNTear expands support queries by .15m per side. Authored root
    // end caps can be recessed .13-.28m from their catalog snap planes. Small solid
    // joints bridge those local gaps without filling an arch or brace's opening.
    internal const float JointWidth = .30f;

    internal static BoxCollider Add(GameObject root, Vector3 point)
    {
        var contact = root.AddComponent<BoxCollider>();
        contact.center = point;
        contact.size = Vector3.one * JointWidth;
        return contact;
    }

    internal static void FoundationSnaps(GameObject prefab, float size, float thickness)
    {
        foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
            if (child != prefab.transform && child.CompareTag("snappoint")) child.tag = "Untagged";
        var index = 0;
        foreach (var y in new[] { 0f, thickness })
        foreach (var x in new[] { -size * .5f, 0f, size * .5f })
        foreach (var z in new[] { -size * .5f, 0f, size * .5f })
        {
            var snap = new GameObject("crystal-foundation-snap-" + index++) { tag = "snappoint" };
            snap.transform.SetParent(prefab.transform, false);
            snap.transform.localPosition = new Vector3(x, y, z);
        }
    }
}
