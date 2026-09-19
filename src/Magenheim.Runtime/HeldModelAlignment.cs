using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Models whose forward axis is not their longest axis, so bounds ranking cannot find it.
    /// </summary>
    /// <remarks>
    /// The crossbow is the case that proves the limit of measuring alone. Its prod spans 1.32m
    /// across while its stock runs only 0.93m fore-and-aft, so the widest axis is lateral and
    /// rank-matching maps the prod onto the donor's stock. Measured from the committed payload, the
    /// prod sits at the +Y end and the stock runs along Y, so Y is forward. Declaring that is
    /// honest; guessing a rotation for it would not be.
    /// </remarks>
    private static readonly Dictionary<string, int> ForwardAxisOverride = new(StringComparer.Ordinal)
    {
        ["crystal-weapon-crossbow"] = 1,
    };

    /// <summary>Hand-authored trim applied after the measured alignment, in attach space.</summary>
    /// <remarks>
    /// Measurement gets a held model into the donor's frame; it cannot know that a grip reads a few
    /// centimetres low, because the donor's own grip is wherever its artist put it. That last step
    /// comes from looking at the thing in a hand, so it is authored here rather than derived, and
    /// the numbers are in attach space to match what Report prints. An entry is a deliberate claim
    /// that a model needs trim, so the table stays empty until in-game observation puts one in it.
    /// </remarks>
    private static readonly Dictionary<string, ModelTrim> Trim = new(StringComparer.Ordinal);

    /// <summary>A trim entry. Explicitly not a tuple: Magenheim.Runtime has no System.ValueTuple.</summary>
    private readonly struct ModelTrim
    {
        internal ModelTrim(Vector3 rotation, Vector3 offset) { Rotation = rotation; Offset = offset; }
        internal Vector3 Rotation { get; }
        internal Vector3 Offset { get; }
    }

    internal static Quaternion Measure(Transform attach, Bounds donor, Bounds replacement, int? forwardAxis = null)
    {
        if (attach is null) return Quaternion.identity;
        if (!Usable(donor) || !Usable(replacement)) return Quaternion.identity;

        var donorOrder = AxisOrder(donor.size);
        var ourOrder = AxisOrder(replacement.size);
        if (forwardAxis is int forward)
        {
            // Promote the declared forward axis to first rank so it maps onto the donor's longest.
            var promoted = new List<int> { forward };
            promoted.AddRange(ourOrder.Where(a => a != forward));
            ourOrder = promoted.ToArray();
        }

        // Map our longest extent onto the donor's longest, our shortest onto the donor's shortest,
        // and the remaining axis follows. Signs come from which side of the attach origin the mass
        // sits on, so a reversed asset is corrected rather than merely re-axised.
        var rotation = new Matrix4x4();
        for (var rank = 0; rank < 3; rank++)
        {
            var from = ourOrder[rank];
            var to = donorOrder[rank];
            // Direction comes from which end of the axis reaches furthest from the attach origin --
            // the hand is the origin and a held implement extends away from it, so the far extreme
            // is the working end. The centre of mass was the earlier cue and it is unstable: a
            // battleaxe whose head and haft nearly balance about the hand flipped upside down.
            var sign = Reach(donor.min[to], donor.max[to]) * Reach(replacement.min[from], replacement.max[from]);
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
                var evidence = donor.size[donorOrder[rank]] + replacement.size[ourOrder[rank]];
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
        var correction = Measure(attach, donor, replacement,
            ForwardAxisOverride.TryGetValue(id, out var declared) ? declared : (int?)null);
        root.transform.localRotation = correction * root.transform.localRotation;

        // Rotation alone leaves the model seated wherever its own origin happens to be. Magenheim
        // sources are centred on their bounds, so the hand grips the middle of the weapon: on a
        // knife that is the crystal rather than the hilt. Re-measure after rotating and slide the
        // model so the end nearest the attach point sits where the donor puts its own near end,
        // which is where Valheim's animations expect a grip.
        if (TryMeasureBounds(attach, root.GetComponentsInChildren<Renderer>(true), root.transform, null, out var rotated))
        {
            var axis = AxisOrder(donor.size)[0];
            var offset = Vector3.zero;
            offset[axis] = NearEnd(donor.min[axis], donor.max[axis]) - NearEnd(rotated.min[axis], rotated.max[axis]);
            for (var other = 0; other < 3; other++)
                if (other != axis) offset[other] = donor.center[other] - rotated.center[other];
            root.transform.localPosition += offset;
        }

        if (Trim.TryGetValue(id, out var trim))
        {
            root.transform.localRotation = Quaternion.Euler(trim.Rotation) * root.transform.localRotation;
            root.transform.localPosition += trim.Offset;
        }

        TryMeasureBounds(attach, root.GetComponentsInChildren<Renderer>(true), root.transform, null, out var seated);
        Report(log, id, correction, donor, seated);
    }

    internal static void Report(Action<string>? log, string id, Quaternion rotation, Bounds donor = default, Bounds seated = default)
    {
        if (log is null) return;
        var euler = rotation.eulerAngles;
        var trivial = Quaternion.Angle(rotation, Quaternion.identity) < 1f;
        // Both frames are printed because a report from a hand is qualitative -- "a bit low", "too
        // far back" -- and turning that into a trim value needs to know where the model actually
        // sits against where the donor sat. Without these two lines the next correction is a guess.
        var frames = Usable(seated) && Usable(donor)
            ? $" donor[c={Fmt(donor.center)} s={Fmt(donor.size)}] ours[c={Fmt(seated.center)} s={Fmt(seated.size)}]"
            : string.Empty;
        log(
            $"Held model '{id}' aligned to its donor by ({euler.x:0.#},{euler.y:0.#},{euler.z:0.#})" +
            (trivial ? " (already in the donor's frame)" : string.Empty) + frames);
    }

    private static string Fmt(Vector3 v) => $"{v.x:0.###},{v.y:0.###},{v.z:0.###}";

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

    /// <summary>+1 when the axis reaches further in the positive direction from the attach origin.</summary>
    private static float Reach(float min, float max) => Mathf.Abs(max) >= Mathf.Abs(min) ? 1f : -1f;

    /// <summary>The bounds extreme closest to the attach origin: where a held implement is gripped.</summary>
    private static float NearEnd(float min, float max) => Mathf.Abs(min) <= Mathf.Abs(max) ? min : max;

}
