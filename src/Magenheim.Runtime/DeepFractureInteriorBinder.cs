using System;
using System.Collections.Generic;
using System.Linq;
using Magenheim.Core.DeepFractures;
using UnityEngine;

namespace Magenheim.Runtime;

internal sealed class DeepFractureInteriorBinder : IDeepFractureInteriorBinder
{
    internal const string InteriorRootName = "Magenheim_DeepFracture_InteriorRoot";
    internal const string InteriorEnvironment = "Crypt";

    public DeepFractureInteriorBinding AttachInterior(GameObject locationContainer)
    {
        if (locationContainer is null) throw new ArgumentNullException(nameof(locationContainer));
        var anchor = locationContainer.GetComponentsInChildren<Transform>(includeInactive: true).SingleOrDefault(transform => string.Equals(transform.name, DeepFractureEntranceVisuals.InteriorAnchorName, StringComparison.Ordinal)) ?? throw new InvalidOperationException($"Deep Fracture location '{locationContainer.name}' has no authoritative interior anchor '{DeepFractureEntranceVisuals.InteriorAnchorName}'.");
        var existing = anchor.Cast<Transform>().SingleOrDefault(transform => string.Equals(transform.name, InteriorRootName, StringComparison.Ordinal));
        if (existing is not null) throw new InvalidOperationException("Deep Fracture interior authority is already attached to this entrance; duplicate binders are refused.");
        DeepFractureSurfaceTravel.AttachSurfacePortal(anchor);
        var root = new GameObject(InteriorRootName);root.transform.SetParent(anchor, worldPositionStays: false);root.transform.localPosition = Vector3.zero;root.transform.localRotation = Quaternion.identity;root.AddComponent<ZNetView>();root.AddComponent<DeepFractureEncounterAuthority>();root.AddComponent<DeepFractureInteriorRuntime>();
        return new DeepFractureInteriorBinding(true, checked((float)DeepFractureExpeditionPlanner.MaximumSupportedInteriorRadius()), InteriorEnvironment);
    }
}

internal sealed class DeepFractureInteriorRuntime : MonoBehaviour
{
    private bool _built;
    private void Start()
    {
        if (_built) return;
        var authority = GetComponent<DeepFractureEncounterAuthority>() ?? throw new InvalidOperationException("Deep Fracture interior has no persistent encounter authority.");
        var seed = StableLocationSeed(transform.position);var expedition = DeepFractureExpeditionPlanner.Build(seed);var districts = BuildDistricts(expedition.Interior, authority, seed);DeepFracturePassageAssembler.Assemble(transform, expedition.Interior);BuildTraversalLinks(expedition.Interior);
        var descent = districts.TryGetValue("DF-01", out var descentDistrict) ? descentDistrict : throw new InvalidOperationException("Deep Fracture expedition did not place required DF-01 Fracture Descent district.");
        DeepFractureSurfaceTravel.Bind(transform, descent);_built = true;
    }

    private Dictionary<string, GameObject> BuildDistricts(DeepFractureInteriorBlueprint blueprint, DeepFractureEncounterAuthority authority, int locationSeed)
    {
        var instances = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        foreach (var placement in blueprint.Modules.OrderBy(module => module.SequenceIndex))
        {
            var source = DeepFractureRoomRegistrar.ResolveDistrict(placement.PieceFamilyId);var instance = Instantiate(source.gameObject, transform, false);instance.name = $"{DeepFractureRoomVisuals.RoomPrefabName(placement.PieceFamilyId)}_{placement.ModuleInstanceId}";instance.transform.localPosition = ToVector3(placement.Center);instance.transform.localRotation = Quaternion.Euler(0f, placement.YawDegrees, 0f);instance.SetActive(true);
            var room = instance.GetComponent<Room>() ?? throw new InvalidOperationException($"Instantiated Deep Fracture district '{instance.name}' has no Room component.");DeepFractureRoomVisuals.ApplyElementalState(room, placement.ElementalStates);DeepFractureEncounterSpawner.Populate(instance, placement, authority, locationSeed);
            if (instances.ContainsKey(placement.PieceFamilyId)) throw new InvalidOperationException($"Deep Fracture expedition placed duplicate district family '{placement.PieceFamilyId}', so a unique travel anchor cannot be selected.");instances.Add(placement.PieceFamilyId, instance);
        }
        return instances;
    }

    private void BuildTraversalLinks(DeepFractureInteriorBlueprint blueprint)
    {
        var modules = blueprint.Modules.ToDictionary(module => module.ModuleInstanceId, StringComparer.Ordinal);var source = DeepFractureRoomRegistrar.ResolveTraversalNode();
        foreach (var connection in blueprint.Connections.Where(connection => connection.RuntimeMode == DeepFractureRuntimeConnectionMode.TraversalLink))
        {
            if (!modules.TryGetValue(connection.FromModuleId, out var from) || !modules.TryGetValue(connection.ToModuleId, out var to)) throw new InvalidOperationException($"Traversal link '{connection.Id}' references an unplaced Deep Fracture district.");
            var fromPosition = PortalPosition(from.Center, to.Center);var toPosition = PortalPosition(to.Center, from.Center);var fromNode = CreateTraversalNode(source.gameObject, connection.Id, "A", fromPosition);var toNode = CreateTraversalNode(source.gameObject, connection.Id, "B", toPosition);var fromPortal = fromNode.GetComponent<DeepFractureTraversalPortal>() ?? throw new InvalidOperationException($"Traversal node '{fromNode.name}' has no portal component.");var toPortal = toNode.GetComponent<DeepFractureTraversalPortal>() ?? throw new InvalidOperationException($"Traversal node '{toNode.name}' has no portal component.");var fromFacing = Facing(fromPosition, toPosition);var toFacing = Facing(toPosition, fromPosition);fromPortal.Configure(toNode.transform.position + Vector3.up, toFacing, connection.Id);toPortal.Configure(fromNode.transform.position + Vector3.up, fromFacing, connection.Id);fromNode.transform.localRotation = fromFacing;toNode.transform.localRotation = toFacing;fromNode.SetActive(true);toNode.SetActive(true);
        }
    }

    private GameObject CreateTraversalNode(GameObject source,string connectionId,string side,DeepFractureInteriorPoint position){var instance=Instantiate(source,transform,false);instance.name=$"{DeepFractureRoomVisuals.TraversalNodePrefabName}_{connectionId}_{side}";instance.transform.localPosition=ToVector3(position);return instance;}
    private static DeepFractureInteriorPoint PortalPosition(DeepFractureInteriorPoint origin,DeepFractureInteriorPoint target){var dx=target.X-origin.X;var dz=target.Z-origin.Z;var length=Math.Sqrt(dx*dx+dz*dz);if(length<.001d)return new DeepFractureInteriorPoint(origin.X,origin.Y+1d,origin.Z);const double edgeOffset=38d;return new DeepFractureInteriorPoint(origin.X+dx/length*edgeOffset,origin.Y+1d,origin.Z+dz/length*edgeOffset);}
    private static Quaternion Facing(DeepFractureInteriorPoint origin,DeepFractureInteriorPoint target){var direction=new Vector3((float)(target.X-origin.X),0f,(float)(target.Z-origin.Z));return direction.sqrMagnitude<.0001f?Quaternion.identity:Quaternion.LookRotation(direction.normalized,Vector3.up);}
    private static Vector3 ToVector3(DeepFractureInteriorPoint point)=>new Vector3((float)point.X,(float)point.Y,(float)point.Z);
    internal static int StableLocationSeed(Vector3 worldPosition){var x=(int)Math.Round(worldPosition.x*100f,MidpointRounding.AwayFromZero);var y=(int)Math.Round(worldPosition.y*100f,MidpointRounding.AwayFromZero);var z=(int)Math.Round(worldPosition.z*100f,MidpointRounding.AwayFromZero);unchecked{uint hash=2166136261u;hash=(hash^(uint)x)*16777619u;hash=(hash^(uint)y)*16777619u;hash=(hash^(uint)z)*16777619u;return(int)hash;}}
}
