using System;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Rotates a replacement held-item mesh into the frame of the vanilla donor it is standing in for.
/// </summary>
/// <remarks>
/// Every crystal weapon clones a different vanilla donor — the sword a `SwordBlackmetal`, the mace a
/// `MaceSilver`, the crossbow a `CrossbowArbalest` — and Valheim authors each of those meshes in its
/// own local frame under the item's `attach` transform. `ModelAssets.Load` parented the Magenheim
/// mesh there at identity, so a weapon presents correctly only where the donor's native frame
/// happens to match Magenheim's authoring convention. That is exactly the reported pattern: axe,
/// battleaxe and bow correct, crossbow reversed, greatsword rolled, knife, mace and spear wrong.
///
/// The correction is measured from the donor rather than assumed, which is what makes this safe to
/// apply to the whole family. The donor's own visible mesh is measured in attach space, the
/// replacement is measured in the same space, and the replacement is rotated so its longest and
/// shortest extents lie along the donor's longest and shortest extents, with the sign chosen so the
/// mass that sits away from the hand still sits away from the hand.
///
/// The three weapons already reported correct are the test: for those the donor's frame and ours
/// already agree, so the measured correction comes out at or near identity. The result is logged
/// per model for exactly that reason — if an asset that reads correctly today acquires a non-trivial
/// rotation, the method is wrong and the log says so before anyone has to look at a hand.
/// </remarks>
internal static class HeldModelAlignment
{
    private const float DegenerateExtent = 1e-4f;

    internal static Quaternion Measure(Transform attach, Bounds donor, Bounds replacement)
    {
        if (attach is null) return Quaternion.identity;
        if (!Usable(donor) || !Usable(replacement)) return Quaternion.identity;

        var donorOrder = AxisOrder(donor.size);
        var ourOrder = AxisOrder(replacement.size);

        // Map our longest extent onto the donor's longest, our shortest onto the donor's shortest,
        // and the remaining axis follows. Signs come from which side of the attach origin the mass
        // sits on, so a reversed asset is corrected rather than merely re-axised.
        var rotation = new Matrix4x4();
        for (var rank = 0; rank < 3; rank++)
        {
            var from = ourOrder[rank];
            var to = donorOrder[rank];
            var sign = Sign(donor.center[to]) * Sign(replacement.center[from]);
            var column = Vector4.zero;
            column[to] = sign;
            rotation.SetColumn(from, column);
        }
        rotation.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));

        // A sign product of -1 on all three axes is a reflection, not a rotation. Flip the axis with
        // the least evidence behind it -- the one whose centres sit closest to the attach origin.
        if (rotation.determinant < 0f)
        {
            var weakest = 0;
            var weakestEvidence = float.MaxValue;
            for (var rank = 0; rank < 3; rank++)
            {
                var evidence = Mathf.Abs(donor.center[donorOrder[rank]]) + Mathf.Abs(replacement.center[ourOrder[rank]]);
                if (evidence >= weakestEvidence) continue;
                weakestEvidence = evidence;
                weakest = rank;
            }
            var from = ourOrder[weakest];
            var to = donorOrder[weakest];
            var column = rotation.GetColumn(from);
            column[to] = -column[to];
            rotation.SetColumn(from, column);
        }

        return rotation.rotation;
    }

    /// <summary>Renderer bounds expressed in the attach transform's local space.</summary>
    internal static bool TryMeasureBounds(Transform attach, Renderer[] renderers, Transform? only, Transform? exclude, out Bounds bounds)
    {
        bounds = default;
        var found = false;
        foreach (var renderer in renderers)
        {
            if (!renderer || renderer is ParticleSystemRenderer) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            var mesh = filter ? filter.sharedMesh : null;
            if (!mesh) continue;
            if (only is not null && !renderer.transform.IsChildOf(only)) continue;
            if (exclude is not null && renderer.transform.IsChildOf(exclude)) continue;

            var local = mesh.bounds;
            for (var corner = 0; corner < 8; corner++)
            {
                var point = new Vector3(
                    (corner & 1) == 0 ? local.min.x : local.max.x,
                    (corner & 2) == 0 ? local.min.y : local.max.y,
                    (corner & 4) == 0 ? local.min.z : local.max.z);
                var attachSpace = attach.InverseTransformPoint(renderer.transform.TransformPoint(point));
                if (!found) { bounds = new Bounds(attachSpace, Vector3.zero); found = true; }
                else bounds.Encapsulate(attachSpace);
            }
        }
        return found;
    }

    /// <summary>Measures the donor's frame, rotates the replacement into it, and reports the result.</summary>
    internal static void Apply(Transform attach, Renderer[] donorRenderers, GameObject root, string id, Action<string>? log)
    {
        if (attach is null || root is null) return;
        if (!TryMeasureBounds(attach, donorRenderers, null, root.transform, out var donor)) return;
        if (!TryMeasureBounds(attach, root.GetComponentsInChildren<Renderer>(true), root.transform, null, out var replacement)) return;
        var correction = Measure(attach, donor, replacement);
        root.transform.localRotation = correction * root.transform.localRotation;
        Report(log, id, correction);
    }

    internal static void Report(Action<string>? log, string id, Quaternion rotation)
    {
        if (log is null) return;
        var euler = rotation.eulerAngles;
        var trivial = Quaternion.Angle(rotation, Quaternion.identity) < 1f;
        log(
            $"Held model '{id}' aligned to its donor by ({euler.x:0.#},{euler.y:0.#},{euler.z:0.#})" +
            (trivial ? " (already in the donor's frame)" : string.Empty));
    }

    private static bool Usable(Bounds bounds) =>
        bounds.size.x > DegenerateExtent && bounds.size.y > DegenerateExtent && bounds.size.z > DegenerateExtent;

    /// <summary>Axis indices ordered longest extent first, shortest last.</summary>
    private static int[] AxisOrder(Vector3 size)
    {
        var order = new[] { 0, 1, 2 };
        for (var i = 0; i < 2; i++)
            for (var j = i + 1; j < 3; j++)
                if (size[order[j]] > size[order[i]])
                    (order[i], order[j]) = (order[j], order[i]);
        return order;
    }

    private static float Sign(float value) => value < 0f ? -1f : 1f;
}
