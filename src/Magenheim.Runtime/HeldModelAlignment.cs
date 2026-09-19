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
///
/// 2026-09-19 addendum, after the 0.0.77 field test reported the battleaxe held "like a guitar"
/// and the spear "still slightly wrong": reconstructing the exact pre-rotation bounds from the
/// shipped payloads (not the post-rotation bounds the log prints) and replaying <see cref="Measure"/>
/// byte-for-byte in isolation showed that six of nine non-crossbow weapons resolve to one identical
/// rotation -- our local X/Y/Z axes map to the donor's X/Z/Y with signs (+1,+1,-1) -- and every
/// reported failure was a *near-tie* in that replay: mace's own two minor axes are 2.7% apart
/// (0.410 vs 0.421), battleaxe's own long axis is 7% off-centre (a double-headed shape balances
/// almost evenly about the grip), and the crossbow's declared-forward axis was 9.8% off-centre.
/// Bounds ranking and Reach() both treat those margins as decisive, when they are measurement noise
/// on a near-symmetric shape. See <see cref="NearTieMargin"/>.
/// </remarks>
internal static class HeldModelAlignment
{
    private const float DegenerateExtent = 1e-4f;

    /// <summary>
    /// How much bigger one candidate must be than another, in <see cref="AxisOrder"/> and
    /// <see cref="Reach"/> alike, before it is trusted over the other.
    /// </summary>
    /// <remarks>
    /// Chosen from the measured gap in the committed weapon library, not picked to make a test pass:
    /// every axis margin measured across all ten crystal weapons (donor and replacement, all three
    /// axes) falls either under 29% or over 30%, with no example between. 0.25 sits in that gap.
    /// Below it, mace's axis order and the battleaxe/spear/crossbow signs are still noise-driven;
    /// at 0.30, bow -- one of the weapons confirmed correct since 0.0.75 -- starts misclassifying a
    /// real, reliable margin as a tie and regresses. Recorded in
    /// docs/validation/2026-09-19-held-model-orientation-resolved.md alongside the full margin table.
    /// </remarks>
    private const float NearTieMargin = 0.25f;

    /// <summary>
    /// Models whose forward axis is not their longest axis, so bounds ranking cannot find it.
    /// </summary>
    /// <remarks>
    /// The crossbow is the case that proves the limit of measuring alone, and the one entry here
    /// that <see cref="NearTieMargin"/> cannot replace. Its prod spans 1.322m across (X) while its
    /// stock runs only 0.927m along Y -- a 42.7% gap, nowhere near a near-tie -- so rank-matching
    /// reliably and confidently picks the wrong axis: the widest axis is genuinely lateral, and
    /// exactly as wide as it looks in the model, not a measurement artifact. Declaring the stock
    /// axis (Y) as forward is honest; guessing a rotation for it would not be. Re-verified
    /// 2026-09-19 by reconstructing the exact pre-rotation bounds from the shipped payload: a
    /// same-session field report had misread this exact override as wrong from the *post*-rotation
    /// bounds the runtime log prints, which are already permuted by whatever rotation was applied
    /// and so cannot be read as if they were the model's own local axes. Y is correct.
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
    /// <remarks>
    /// A candidate must beat the current holder by more than <see cref="NearTieMargin"/> to take its
    /// rank, not merely be nominally bigger. Without this, the mace's own X and Z extents -- 0.410m
    /// and 0.421m, 2.7% apart -- reordered a full 90-degree axis reassignment on what is really
    /// measurement noise on a near-round mace head.
    /// </remarks>
    private static int[] AxisOrder(Vector3 size)
    {
        var order = new[] { 0, 1, 2 };
        for (var i = 0; i < 2; i++)
            for (var j = i + 1; j < 3; j++)
                if (size[order[j]] > size[order[i]] * (1f + NearTieMargin))
                    (order[i], order[j]) = (order[j], order[i]);
        return order;
    }

    /// <summary>+1 when the axis reaches further in the positive direction from the attach origin.</summary>
    /// <remarks>
    /// When the two ends are within <see cref="NearTieMargin"/> of each other, this axis carries no
    /// reliable "which way is forward" signal on this particular mesh -- a spear's shaft is close to
    /// round in cross-section, a double-headed battleaxe balances close to evenly about the grip --
    /// and comparing anyway turns a coin flip into a wrong rotation. This is the exact mechanism
    /// behind the battleaxe's reported "guitar" hold: its own long-axis extents, 0.750 and 0.806,
    /// are 7% apart. Defaulting to +1 here is not a guess about that one model; it defers to whichever
    /// side of the sign product in <see cref="Measure"/> -- donor or replacement -- is NOT a near tie,
    /// since both sides of that product go through this same function independently.
    /// </remarks>
    private static float Reach(float min, float max)
    {
        var a = Mathf.Abs(min);
        var b = Mathf.Abs(max);
        var denominator = Mathf.Max(a, b);
        if (denominator > 1e-9f && Mathf.Abs(a - b) / denominator < NearTieMargin) return 1f;
        return b >= a ? 1f : -1f;
    }

    /// <summary>The bounds extreme closest to the attach origin: where a held implement is gripped.</summary>
    private static float NearEnd(float min, float max) => Mathf.Abs(min) <= Mathf.Abs(max) ? min : max;

}
