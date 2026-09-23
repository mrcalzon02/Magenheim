using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Rides an authored humanoid body on a vanilla donor's own skeleton.
/// </summary>
/// <remarks>
/// A replacement body attached to a creature's root is a statue gliding over the ground while the
/// hidden donor walks and swings. Valheim already owns the skeleton, animator, attacks, AI,
/// networking and saving, so the body is instead cut into segments, each authored against one bone
/// of a canonical humanoid skeleton that travels in the model payload (<c>rig</c>), and each segment
/// is parented to the matching bone of the donor.
///
/// Bones are found by humanoid ROLE through the donor avatar's own bone map, not by guessed names,
/// and it reads the avatar description rather than <c>Animator.GetBoneTransform</c>, which needs a
/// live, initialised animator that a registered prefab does not have.
///
/// Donor and canonical skeletons differ in proportion and in rest pose, so each segment is
/// retargeted rather than copied: the segment's canonical bone (joint to tip) is mapped onto the
/// donor bone's joint and direction, stretched along the bone to the donor's bone length and scaled
/// across it by the donor's overall height. Arms authored hanging still land correctly on a donor
/// whose rest pose is a T.
/// </remarks>
internal static class HumanoidSegmentBinder
{
    internal const string BonePrefix = "bone:";

    /// <summary>Resolves a donor's humanoid roles to its transforms, or explains why it cannot.</summary>
    internal sealed class DonorSkeleton
    {
        private readonly Dictionary<string, Transform> _roles;
        private DonorSkeleton(Transform root, Dictionary<string, Transform> roles) { Root = root; _roles = roles; }
        internal Transform Root { get; }
        internal int Count => _roles.Count;
        internal Transform? this[string role] => _roles.TryGetValue(role, out var bone) ? bone : null;

        internal static DonorSkeleton Resolve(GameObject prefab) => Resolve(prefab, prefab);

        /// <summary>Roles come from <paramref name="avatarSource"/>'s humanoid avatar; bones are found by
        /// name under <paramref name="target"/>. A death ragdoll carries the living donor's bone names
        /// but usually no Animator of its own.</summary>
        internal static DonorSkeleton Resolve(GameObject target, GameObject avatarSource)
        {
            var animator = avatarSource.GetComponentInChildren<Animator>(true)
                ?? throw new InvalidOperationException($"{avatarSource.name} has no Animator.");
            var avatar = animator.avatar;
            if (!avatar || !avatar.isHuman)
                throw new InvalidOperationException(
                    $"{avatarSource.name} animator avatar '{(avatar ? avatar.name : "none")}' is not humanoid; bones cannot be resolved by role.");
            var byName = new Dictionary<string, Transform>(StringComparer.Ordinal);
            var searchRoot = target == avatarSource ? animator.transform : target.transform;
            foreach (var transform in searchRoot.GetComponentsInChildren<Transform>(true))
                if (!byName.ContainsKey(transform.name)) byName.Add(transform.name, transform);
            var roles = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var human in avatar.humanDescription.human)
                if (byName.TryGetValue(human.boneName, out var bone))
                    roles[human.humanName.Replace(" ", "")] = bone;
            return new DonorSkeleton(target.transform, roles);
        }
    }

    /// <summary>One canonical bone: joint, the point its segment points toward, and how to find that
    /// point on a donor (<c>child</c> role, or <c>up</c>/<c>forward</c>/<c>continue:&lt;role&gt;</c>).</summary>
    private sealed class CanonicalBone
    {
        internal CanonicalBone(JToken data)
        {
            Joint = ModelAssets.Vector(data["joint"]!);
            Tip = ModelAssets.Vector(data["tip"]!);
            Toward = (string?)data["toward"] ?? throw new InvalidOperationException("Canonical bone lacks 'toward'.");
            Stretch = (bool?)data["stretch"] ?? false;
            Parent = (string?)data["parent"];
        }
        internal bool Stretch { get; }
        internal string? Parent { get; }
        internal Vector3 Joint { get; }
        internal Vector3 Tip { get; }
        internal string Toward { get; }
    }

    /// <summary>Returns a part arranger for <see cref="ModelAssets.Load"/> that binds every
    /// <c>bone:</c> part to the donor, plus a one-line binding report.</summary>
    internal static Action<string, Transform> Arranger(GameObject prefab, JToken rig, float girth, out Func<string> report, GameObject? avatarSource = null)
    {
        var skeleton = DonorSkeleton.Resolve(prefab, avatarSource ?? prefab);
        var bones = ((JObject)rig["bones"]!).Properties().ToDictionary(p => p.Name, p => new CanonicalBone(p.Value), StringComparer.Ordinal);
        var canonicalHeight = (float)rig["height"]!;
        var donorHeight = Height(skeleton);
        if (donorHeight <= 0.1f) throw new InvalidOperationException($"{prefab.name} measured height {donorHeight:0.00}m; skeleton unusable.");
        var across = donorHeight / canonicalHeight * girth;
        var bound = 0;
        var stretch = 1f;
        var substituted = new HashSet<string>(StringComparer.Ordinal);
        report = () => $"{bound} segments on {skeleton.Count} donor bones, donor height {donorHeight:0.00}m (canonical {canonicalHeight:0.00}m), max limb stretch {stretch:0.00}" +
                       (substituted.Count > 0 ? ", unmapped roles riding ancestors: " + string.Join(", ", substituted.OrderBy(s => s)) : string.Empty);

        return (path, part) =>
        {
            if (!path.StartsWith(BonePrefix, StringComparison.Ordinal)) return;
            var authored = path.Substring(BonePrefix.Length).Split('/')[0];
            if (!bones.ContainsKey(authored))
                throw new InvalidOperationException($"Part {path} names bone {authored}, which the canonical rig does not define.");
            // A donor avatar need not map every role (Draugr have no Neck). The part then rides its
            // nearest mapped canonical ancestor, retargeted through that ancestor's frame, which
            // keeps it where it was authored relative to the body.
            var role = authored;
            while (skeleton[role] is null)
            {
                role = bones[role].Parent ?? throw new InvalidOperationException(
                    $"{prefab.name} maps no humanoid {authored} bone or any ancestor of it for {path}.");
                if (role != authored) substituted.Add(authored + "->" + role);
            }
            var canonical = bones[role];
            var bone = skeleton[role]!;

            var root = skeleton.Root;
            var joint = bone.position;
            var tip = DonorTip(skeleton, bones, bone, canonical, donorHeight / canonicalHeight);
            var donorAxis = tip - joint;
            var canonicalAxis = canonical.Tip - canonical.Joint;
            if (donorAxis.sqrMagnitude < 1e-8f || canonicalAxis.sqrMagnitude < 1e-8f)
                throw new InvalidOperationException($"Degenerate {role} bone for {path}.");

            // Canonical coordinates are model space: +Z forward, +Y up. The donor's are its root's.
            var canonicalFrame = Frame(canonicalAxis, Vector3.forward, Vector3.up);
            var donorFrame = Frame(donorAxis, root.forward, root.up);
            var frame = new GameObject(part.name + ".frame") { layer = part.gameObject.layer }.transform;
            frame.SetParent(part.parent, false);
            frame.SetPositionAndRotation(joint, donorFrame);
            // Limbs stretch along the bone so their joints meet the donor's; torso, head, hands and
            // feet keep their authored proportion, because a short or variable donor bone (hips to
            // spine) would otherwise squash them. The stretch is bounded so one odd donor bone
            // cannot turn a segment into a needle; the report prints the largest applied.
            var along = across;
            if (canonical.Stretch)
            {
                along = Mathf.Clamp(donorAxis.magnitude / canonicalAxis.magnitude, across * 0.6f, across * 1.6f);
                stretch = Mathf.Max(stretch, along / across);
            }
            frame.localScale = new Vector3(across, along, across);
            part.SetParent(frame, false);
            var inverse = Quaternion.Inverse(canonicalFrame);
            part.localRotation = inverse;
            part.localPosition = inverse * -canonical.Joint;
            part.localScale = Vector3.one;
            // Follow the bone from here on; the pose now held is the donor's rest pose.
            frame.SetParent(bone, true);
            bound++;
        };
    }

    private static Quaternion Frame(Vector3 axis, Vector3 forward, Vector3 up)
    {
        var y = axis.normalized;
        // Feet and anything else lying along forward cannot take forward as a second axis; the same
        // rule runs on both skeletons, so the choice is consistent between them.
        var z = forward - y * Vector3.Dot(forward, y);
        if (z.sqrMagnitude < 0.09f) z = -up - y * Vector3.Dot(-up, y);
        return Quaternion.LookRotation(z.normalized, y);
    }

    private static Vector3 DonorTip(DonorSkeleton skeleton, Dictionary<string, CanonicalBone> bones, Transform bone, CanonicalBone canonical, float ratio)
    {
        // An unmapped target role (Chest toward a missing Neck) follows the canonical chain onward
        // (to the Head); only the direction of a non-stretching segment depends on it.
        while (bones.TryGetValue(canonical.Toward, out var next) && skeleton[canonical.Toward] is null)
            canonical = next;
        var length = (canonical.Tip - canonical.Joint).magnitude * ratio;
        var root = skeleton.Root;
        switch (canonical.Toward)
        {
            case "up": return bone.position + root.up * length;
            case "forward":
                var flat = root.forward;
                return bone.position + flat * length;
        }
        if (canonical.Toward.StartsWith("continue:", StringComparison.Ordinal))
        {
            var parent = skeleton[canonical.Toward.Substring("continue:".Length)]
                ?? throw new InvalidOperationException($"No humanoid {canonical.Toward} bone to continue from.");
            return bone.position + (bone.position - parent.position).normalized * length;
        }
        var child = skeleton[canonical.Toward]
            ?? throw new InvalidOperationException($"No humanoid {canonical.Toward} bone to point toward.");
        return child.position;
    }

    private static float Height(DonorSkeleton skeleton)
    {
        var head = skeleton["Head"] ?? throw new InvalidOperationException("Donor has no humanoid Head bone.");
        var left = skeleton["LeftFoot"] ?? throw new InvalidOperationException("Donor has no humanoid LeftFoot bone.");
        var right = skeleton["RightFoot"] ?? throw new InvalidOperationException("Donor has no humanoid RightFoot bone.");
        var up = skeleton.Root.up;
        return Vector3.Dot(head.position - (Vector3.Dot(left.position, up) < Vector3.Dot(right.position, up) ? left.position : right.position), up);
    }
}
