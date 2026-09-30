using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>
/// Renders an authored rigid-segment creature body while Valheim's donor creature remains the
/// invisible gameplay chassis. AI, hit boxes, attacks, networking, persistence and pathing stay on
/// the donor. Magenheim owns the visible segmented anatomy and its presentation animation.
/// </summary>
internal static class RigidCreatureSegmentBinder
{
    private const string BonePrefix = "creaturebone:";

    internal static string Apply(GameObject prefab, string modelId, float chassisScale)
    {
        if (!prefab) throw new ArgumentNullException(nameof(prefab));
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Creature model id is required.", nameof(modelId));
        if (float.IsNaN(chassisScale) || float.IsInfinity(chassisScale) || chassisScale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(chassisScale));

        var rig = ModelAssets.CreatureRig(modelId)
            ?? throw new InvalidOperationException(modelId + " declares no creatureRig payload.");
        if (!string.Equals((string?)rig["kind"], "rigid-segment-creature-v1", StringComparison.Ordinal))
            throw new InvalidOperationException(
                modelId + " uses unsupported creature rig kind '" + (string?)rig["kind"] + "'.");

        var fps = (float?)rig["fps"] ?? 24f;
        if (float.IsNaN(fps) || float.IsInfinity(fps) || fps <= 0f || fps > 240f)
            throw new InvalidOperationException(modelId + " has invalid creature animation fps.");

        // Capture donor silhouette renderers before loading the authored body. The donor remains the
        // invisible gameplay/network chassis; only mesh-bearing silhouette renderers are disabled.
        // Particle/projectile renderers are not touched so donor combat telegraphs may still work.
        var donorRenderers = prefab
            .GetComponentsInChildren<Renderer>(true)
            .Where(value => value is MeshRenderer || value is SkinnedMeshRenderer)
            .ToArray();

        var specs = ParseBones(rig, modelId);
        var clips = ParseActions(rig, specs, modelId);
        var transforms = new Dictionary<string, Transform>(StringComparer.Ordinal);
        var rest = new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        var restPosition = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        var restScale = new Dictionary<string, Vector3>(StringComparer.Ordinal);
        var skeletonBuilt = false;
        Transform? visualRoot = null;

        Action<string, Transform> arrange = (path, part) =>
        {
            if (!path.StartsWith(BonePrefix, StringComparison.Ordinal))
                return;

            visualRoot ??= part.parent
                ?? throw new InvalidOperationException(modelId + " creature visual part has no model root.");

            if (!skeletonBuilt)
            {
                BuildSkeleton(visualRoot, specs, transforms, rest, restPosition, restScale, modelId);
                skeletonBuilt = true;
            }

            var slash = path.IndexOf('/', BonePrefix.Length);
            var boneName = slash < 0
                ? path.Substring(BonePrefix.Length)
                : path.Substring(BonePrefix.Length, slash - BonePrefix.Length);
            if (!transforms.TryGetValue(boneName, out var bone))
                throw new InvalidOperationException(
                    modelId + " visual part '" + path + "' references unknown creature bone '" + boneName + "'.");

            // The exported rigid part is already expressed in the canonical model frame. Preserve
            // its world/model pose when parenting it beneath the rest-pose bone; the resulting local
            // transform becomes the inverse rest frame and thereafter follows bone animation.
            part.SetParent(bone, true);
        };

        var root = ModelAssets.Load(prefab, modelId, arrange: arrange);
        if (!skeletonBuilt)
            throw new InvalidOperationException(modelId + " exported no creaturebone-bound visual parts.");

        // The clone root is scaled for the vanilla donor chassis/hitbox. Authored creature sources
        // are already modeled in final metres, so cancel the inherited chassis scale on the visual
        // root instead of shrinking the custom anatomy a second time.
        root.transform.localScale = Vector3.Scale(
            root.transform.localScale,
            Vector3.one * (1f / chassisScale));

        foreach (var renderer in donorRenderers)
            if (renderer)
                renderer.enabled = false;

        var character = prefab.GetComponent<Character>()
            ?? throw new InvalidOperationException(prefab.name + " has no Character for creature visual driving.");
        var driver = root.AddComponent<RigidCreaturePresentationDriver>();
        driver.Bind(character, transforms, rest, restPosition, restScale, clips, fps);
        return transforms.Count + " bones / " + clips.Count + " authored action(s)";
    }

    private static IReadOnlyDictionary<string, BoneSpec> ParseBones(
        JToken rig,
        string modelId)
    {
        if (!(rig["bones"] is JArray rows) || rows.Count == 0)
            throw new InvalidOperationException(modelId + " creature rig contains no bones.");

        var result = new Dictionary<string, BoneSpec>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var name = (string?)row["name"];
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException(modelId + " creature rig contains an unnamed bone.");
            if (result.ContainsKey(name))
                throw new InvalidOperationException(modelId + " creature rig repeats bone '" + name + "'.");

            var parent = (string?)row["parent"];
            var head = ModelAssets.Vector(row["head"]!);
            var yAxis = ModelAssets.Vector(row["yAxis"]!);
            var zAxis = ModelAssets.Vector(row["zAxis"]!);
            if (yAxis.sqrMagnitude < .000001f || zAxis.sqrMagnitude < .000001f)
                throw new InvalidOperationException(modelId + " creature bone '" + name + "' has a degenerate rest frame.");

            result.Add(name, new BoneSpec(name, parent, head, yAxis.normalized, zAxis.normalized));
        }

        foreach (var bone in result.Values)
            if (!string.IsNullOrWhiteSpace(bone.Parent) && !result.ContainsKey(bone.Parent))
                throw new InvalidOperationException(
                    modelId + " creature bone '" + bone.Name + "' names missing parent '" + bone.Parent + "'.");

        if (!result.Values.Any(value => string.IsNullOrWhiteSpace(value.Parent)))
            throw new InvalidOperationException(modelId + " creature rig has no root bone.");

        return result;
    }

    private static IReadOnlyList<ActionClip> ParseActions(
        JToken rig,
        IReadOnlyDictionary<string, BoneSpec> bones,
        string modelId)
    {
        if (!(rig["actions"] is JArray rows) || rows.Count == 0)
            throw new InvalidOperationException(modelId + " creature rig contains no authored actions.");

        var result = new List<ActionClip>();
        foreach (var row in rows)
        {
            var name = (string?)row["name"];
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException(modelId + " creature action is unnamed.");
            if (!(row["samples"] is JArray samples) || samples.Count == 0)
                throw new InvalidOperationException(modelId + " creature action '" + name + "' has no samples.");

            var frames = new List<Dictionary<string, BonePose>>(samples.Count);
            foreach (var sample in samples)
            {
                if (!(sample is JObject objectSample))
                    throw new InvalidOperationException(modelId + " action '" + name + "' has malformed sample data.");

                var frame = new Dictionary<string, BonePose>(StringComparer.Ordinal);
                foreach (var property in objectSample.Properties())
                {
                    if (!bones.ContainsKey(property.Name))
                        throw new InvalidOperationException(
                            modelId + " action '" + name + "' animates unknown bone '" + property.Name + "'.");
                    if (!(property.Value is JObject pose) ||
                        !(pose["rotation"] is JArray quaternion) ||
                        quaternion.Count != 4 ||
                        !(pose["position"] is JArray position) ||
                        position.Count != 3 ||
                        !(pose["scale"] is JArray scale) ||
                        scale.Count != 3)
                        throw new InvalidOperationException(
                            modelId + " action '" + name + "' has malformed pose for '" + property.Name + "'.");

                    var q = new Quaternion(
                        (float)quaternion[0]!,
                        (float)quaternion[1]!,
                        (float)quaternion[2]!,
                        (float)quaternion[3]!);
                    var p = new Vector3(
                        (float)position[0]!,
                        (float)position[1]!,
                        (float)position[2]!);
                    var s = new Vector3(
                        (float)scale[0]!,
                        (float)scale[1]!,
                        (float)scale[2]!);
                    if (!Finite(q) || !Finite(p) || !Finite(s) || s.x <= 0f || s.y <= 0f || s.z <= 0f)
                        throw new InvalidOperationException(
                            modelId + " action '" + name + "' has non-finite/invalid pose for '" + property.Name + "'.");
                    frame[property.Name] = new BonePose(q.normalized, p, s);
                }
                frames.Add(frame);
            }

            result.Add(new ActionClip(name, frames));
        }

        return result.AsReadOnly();
    }

    private static void BuildSkeleton(
        Transform root,
        IReadOnlyDictionary<string, BoneSpec> specs,
        IDictionary<string, Transform> transforms,
        IDictionary<string, Quaternion> rest,
        IDictionary<string, Vector3> restPosition,
        IDictionary<string, Vector3> restScale,
        string modelId)
    {
        foreach (var spec in specs.Values)
        {
            var boneObject = new GameObject("magenheim.creature-bone." + spec.Name)
            {
                layer = root.gameObject.layer,
            };
            var bone = boneObject.transform;
            bone.SetParent(root, false);
            bone.localPosition = spec.Head;
            bone.localRotation = Frame(spec.YAxis, spec.ZAxis);
            bone.localScale = Vector3.one;
            transforms.Add(spec.Name, bone);
        }

        // Reparent only after every bone exists. worldPositionStays preserves the exported canonical
        // rest pose; the new local transform is therefore exactly the rest pose relative to parent.
        foreach (var spec in specs.Values)
        {
            if (string.IsNullOrWhiteSpace(spec.Parent))
                continue;
            var bone = transforms[spec.Name];
            var parent = transforms[spec.Parent];
            bone.SetParent(parent, true);
        }

        foreach (var pair in transforms)
        {
            rest.Add(pair.Key, pair.Value.localRotation);
            restPosition.Add(pair.Key, pair.Value.localPosition);
            restScale.Add(pair.Key, pair.Value.localScale);
        }

        if (transforms.Count != specs.Count)
            throw new InvalidOperationException(modelId + " creature skeleton did not materialize completely.");
    }

    private static Quaternion Frame(Vector3 yAxis, Vector3 zAxis)
    {
        var y = yAxis.normalized;
        var z = zAxis - y * Vector3.Dot(zAxis, y);
        if (z.sqrMagnitude < .0001f)
        {
            z = Vector3.forward - y * Vector3.Dot(Vector3.forward, y);
            if (z.sqrMagnitude < .0001f)
                z = Vector3.right - y * Vector3.Dot(Vector3.right, y);
        }
        return Quaternion.LookRotation(z.normalized, y);
    }

    private static bool Finite(Quaternion value) =>
        !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
        !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
        !float.IsNaN(value.z) && !float.IsInfinity(value.z) &&
        !float.IsNaN(value.w) && !float.IsInfinity(value.w);

    private static bool Finite(Vector3 value) =>
        !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
        !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
        !float.IsNaN(value.z) && !float.IsInfinity(value.z);

    private sealed class BoneSpec
    {
        internal BoneSpec(string name, string? parent, Vector3 head, Vector3 yAxis, Vector3 zAxis)
        {
            Name = name;
            Parent = parent;
            Head = head;
            YAxis = yAxis;
            ZAxis = zAxis;
        }

        internal string Name { get; }
        internal string? Parent { get; }
        internal Vector3 Head { get; }
        internal Vector3 YAxis { get; }
        internal Vector3 ZAxis { get; }
    }

    internal sealed class BonePose
    {
        internal BonePose(Quaternion rotation, Vector3 position, Vector3 scale)
        {
            Rotation = rotation;
            Position = position;
            Scale = scale;
        }

        internal Quaternion Rotation { get; }
        internal Vector3 Position { get; }
        internal Vector3 Scale { get; }
    }

    internal sealed class ActionClip
    {
        internal ActionClip(string name, IReadOnlyList<Dictionary<string, BonePose>> frames)
        {
            Name = name;
            Frames = frames;
        }

        internal string Name { get; }
        internal IReadOnlyList<Dictionary<string, BonePose>> Frames { get; }
    }
}

internal sealed class RigidCreaturePresentationDriver : MonoBehaviour
{
    private Character _character = null!;
    private Humanoid? _humanoid;
    private Rigidbody? _body;
    private IReadOnlyDictionary<string, Transform> _bones = null!;
    private IReadOnlyDictionary<string, Quaternion> _rest = null!;
    private IReadOnlyDictionary<string, Vector3> _restPosition = null!;
    private IReadOnlyDictionary<string, Vector3> _restScale = null!;
    private IReadOnlyList<RigidCreatureSegmentBinder.ActionClip> _clips = null!;
    private float _fps;
    private RigidCreatureSegmentBinder.ActionClip? _current;
    private float _startedAt;
    private float _lastHealth;
    private float _hitUntil;
    private RigidCreatureSegmentBinder.ActionClip? _forced;
    private float _forcedUntil;

    internal void Bind(
        Character character,
        IReadOnlyDictionary<string, Transform> bones,
        IReadOnlyDictionary<string, Quaternion> rest,
        IReadOnlyDictionary<string, Vector3> restPosition,
        IReadOnlyDictionary<string, Vector3> restScale,
        IReadOnlyList<RigidCreatureSegmentBinder.ActionClip> clips,
        float fps)
    {
        _character = character ?? throw new ArgumentNullException(nameof(character));
        _bones = bones ?? throw new ArgumentNullException(nameof(bones));
        _rest = rest ?? throw new ArgumentNullException(nameof(rest));
        _restPosition = restPosition ?? throw new ArgumentNullException(nameof(restPosition));
        _restScale = restScale ?? throw new ArgumentNullException(nameof(restScale));
        _clips = clips ?? throw new ArgumentNullException(nameof(clips));
        _fps = fps;
        _humanoid = character.GetComponent<Humanoid>();
        _body = character.GetComponent<Rigidbody>();
        _lastHealth = character.GetHealth();
        Select(ChooseIdle());
    }

    internal void PlayOneShot(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;
        var clip = Find(token);
        if (clip is null)
            return;

        _forced = clip;
        _forcedUntil = Time.time + Mathf.Max(.08f, clip.Frames.Count / _fps);
        Select(clip);
    }

    private void Update()
    {
        if (!_character || _clips is null || _clips.Count == 0)
            return;

        var health = _character.GetHealth();
        if (!_character.IsDead() && health + .01f < _lastHealth)
            _hitUntil = Time.time + .22f;
        _lastHealth = health;

        var desired = Choose();
        if (!ReferenceEquals(desired, _current))
            Select(desired);

        ApplyCurrent();
        if (_forced is not null && Time.time >= _forcedUntil)
            _forced = null;
    }

    private RigidCreatureSegmentBinder.ActionClip Choose()
    {
        if (_character.IsDead())
            return Find("death") ?? _current ?? ChooseIdle();

        if (Time.time < _hitUntil)
            return Find("hit") ?? _current ?? ChooseIdle();

        if (_forced is not null && Time.time < _forcedUntil)
            return _forced;

        if (_humanoid is not null && _humanoid.InAttack())
            return Find("attackfront") ??
                   Find("crusherattack") ??
                   Find("cutterattack") ??
                   Find("clawleft") ??
                   Find("clawright") ??
                   Find("tailstrike") ??
                   Find("pressurerelease") ??
                   Find("bite") ??
                   Find("latch") ??
                   Find("cast") ??
                   Find("lunge") ??
                   Find("ram") ??
                   Find("leap") ??
                   Find("heavyslam") ??
                   Find("slam") ??
                   Find("swipe") ??
                   Find("strike") ??
                   Find("sweep") ??
                   Find("pounce") ??
                   Find("charge") ??
                   Find("attack") ??
                   _current ??
                   ChooseIdle();

        if (HorizontalSpeedSquared() > .035f)
            return Find("sprint") ??
                   Find("cruise") ??
                   Find("swimforward") ??
                   Find("scuttle") ??
                   Find("walk") ??
                   Find("flight") ??
                   Find("glide") ??
                   Find("drift") ??
                   Find("crawl") ??
                   _current ??
                   ChooseIdle();

        return ChooseIdle();
    }

    private RigidCreatureSegmentBinder.ActionClip ChooseIdle() =>
        Find("swimidle") ??
        Find("attachedidle") ??
        Find("lureidle") ??
        Find("hover") ??
        Find("clingidle") ??
        Find("concealidle") ??
        Find("groundidle") ??
        Find("pressureidle") ??
        Find("ventidle") ??
        Find("perchidle") ??
        Find("drift") ??
        Find("idle") ??
        Find("grazeroot") ??
        Find("glide") ??
        _clips[0];

    private RigidCreatureSegmentBinder.ActionClip? Find(string token)
    {
        foreach (var clip in _clips)
            if (clip.Name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                return clip;
        return null;
    }

    private float HorizontalSpeedSquared()
    {
        if (_body is null)
            return 0f;
        var velocity = _body.linearVelocity;
        return velocity.x * velocity.x + velocity.z * velocity.z;
    }

    private void Select(RigidCreatureSegmentBinder.ActionClip clip)
    {
        _current = clip;
        _startedAt = Time.time;
    }

    private void ApplyCurrent()
    {
        var clip = _current;
        if (clip is null || clip.Frames.Count == 0)
            return;

        var elapsedFrames = Mathf.Max(0f, (Time.time - _startedAt) * _fps);
        var looping = IsLooping(clip);
        float samplePosition;
        if (looping)
            samplePosition = clip.Frames.Count <= 1 ? 0f : elapsedFrames % clip.Frames.Count;
        else
            samplePosition = Mathf.Min(elapsedFrames, clip.Frames.Count - 1);

        var firstIndex = Mathf.Clamp(Mathf.FloorToInt(samplePosition), 0, clip.Frames.Count - 1);
        var secondIndex = looping
            ? (firstIndex + 1) % clip.Frames.Count
            : Mathf.Min(firstIndex + 1, clip.Frames.Count - 1);
        var amount = samplePosition - Mathf.Floor(samplePosition);
        var first = clip.Frames[firstIndex];
        var second = clip.Frames[secondIndex];

        foreach (var pair in _bones)
        {
            var rest = _rest[pair.Key];
            var basePosition = _restPosition[pair.Key];
            var baseScale = _restScale[pair.Key];
            var a = first.TryGetValue(pair.Key, out var firstPose)
                ? firstPose
                : null;
            var b = second.TryGetValue(pair.Key, out var secondPose)
                ? secondPose
                : null;
            var rotationA = a?.Rotation ?? Quaternion.identity;
            var rotationB = b?.Rotation ?? Quaternion.identity;
            var positionA = a?.Position ?? Vector3.zero;
            var positionB = b?.Position ?? Vector3.zero;
            var scaleA = a?.Scale ?? Vector3.one;
            var scaleB = b?.Scale ?? Vector3.one;
            pair.Value.localRotation = rest * Quaternion.Slerp(rotationA, rotationB, amount);
            pair.Value.localPosition = basePosition + Vector3.Lerp(positionA, positionB, amount);
            pair.Value.localScale = Vector3.Scale(baseScale, Vector3.Lerp(scaleA, scaleB, amount));
        }
    }

    private static bool IsLooping(RigidCreatureSegmentBinder.ActionClip clip) =>
        clip.Name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0 ||
        clip.Name.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0 ||
        clip.Name.IndexOf("scuttle", StringComparison.OrdinalIgnoreCase) >= 0 ||
        clip.Name.IndexOf("turn", StringComparison.OrdinalIgnoreCase) >= 0 ||
        clip.Name.IndexOf("alert", StringComparison.OrdinalIgnoreCase) >= 0;
}
