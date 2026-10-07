using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Magenheim.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

// Engine seams only; the scope implementation and target selection are linked from production.
namespace HarmonyLib
{
    public sealed class CodeInstruction {
        public OpCode opcode; public object? operand;
        public List<string> labels=new(); public List<string> blocks=new();
        public CodeInstruction(OpCode code,object? value=null) {opcode=code;operand=value;}
        public bool Calls(MethodInfo method) => (opcode==OpCodes.Call || opcode==OpCodes.Callvirt) && Equals(operand,method);
    }
    [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch : Attribute
    { public HarmonyPatch() {} public HarmonyPatch(Type type, string method) {} }
    public sealed class Harmony { public Harmony(string id) {} public void PatchAll(Type type) {} public void UnpatchSelf() {} }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyPriority : Attribute { public HarmonyPriority(int value) {} }
    public static class Priority { public const int First=800, Last=0; }
    public static class AccessTools {
        public static FieldInfo? Field(Type type,string name) => type.GetField(name,BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
        public static MethodInfo? Method(Type type,string name) => type.GetMethod(name,BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
        public static MethodInfo? Method(Type type,string name,Type[] parameters) => type.GetMethod(name,BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic,null,parameters,null);
    }
}
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object? value) => value is not null; }
    public class GameObject : Object { public Scene scene; public string name=""; }
    public class Component : Object { public GameObject gameObject = new(); public ZNetView? View; public T? GetComponent<T>() where T:class => View as T; }
    public class MonoBehaviour : Component {}
    public static class Time { public static int frameCount = 513; public static float fixedTime=10f, unscaledTime=10f; }
    public class Renderer : Component { public bool forceRenderingOff, enabled=true; public string name => gameObject.name; }
    public static class Resources { public static Renderer[] Renderers=Array.Empty<Renderer>();
        public static T[] FindObjectsOfTypeAll<T>() where T:class => Renderers.OfType<T>().ToArray(); }
    public static class Debug { public static void LogWarning(string message) {} }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public int handle; public string name => handle.ToString(); public bool isLoaded => true; public bool IsValid() => handle > 0; }
    public static class SceneManager { static Scene active = new() { handle = 1 }; public static Scene GetActiveScene() => active; public static bool SetActiveScene(Scene scene) { active = scene; return true; } }
}
namespace Magenheim.Core.Underworld
{
    public struct UnderworldWorldInstanceId { public int Value; public bool IsSurface => Value == 0; public bool IsUnderworld => Value == 1; public static UnderworldWorldInstanceId Underworld => new() {Value=1}; }
}
public sealed class World { public long m_uid; }
public sealed class ZoneSystem : MonoBehaviour {
    public sealed class ZoneLocation { public Heightmap.Biome m_biome; public bool m_centerFirst; public int Quantity=6; }
    public static ZoneSystem? s_instance; public float m_startTime,m_lastFixedTime; public bool m_locationsGenerated;
    public int GenerationRequests; private void ValidateVegetation() {} public void UpdateWorldRates() {}
    public void GenerateLocationsIfNeeded() { if (!m_locationsGenerated) GenerationRequests++; }
    private void Update() {} private void Start() {}
}
public sealed class WorldGenerator { public static WorldGenerator? m_instance; }
public sealed class ZDOMan { public static ZDOMan? s_instance; }
public sealed class ZDO { public byte TempRemoveEarmark; public int Helmet; public int GetInt(int key,int fallback) => Helmet; }
public static class ZDOVars { public const int s_helmetItem=1; }
public static class RoyaltyHashShim { public static int GetStableHashCode(this string value) => value=="Magenheim_NullMantle" ? 999 : 222; }
public sealed class ZNetView : MonoBehaviour { public ZDO? Zdo; public bool Owner=true; public bool IsOwner()=>Owner; public ZDO? GetZDO() => Zdo; public bool IsValid() => Zdo!=null; }
public sealed class ZNet { public static ZNet? instance; public static World? m_world; public ZDOMan? m_zdoMan; }
public sealed class Pathfinding : MonoBehaviour { public static Pathfinding? instance; private void Update() {} }
public class Character : MonoBehaviour { private void Update() {} }
public sealed class ItemDrop { public sealed class ItemData { public GameObject? m_dropPrefab; public bool m_equipped; } }
public class Humanoid : Character { public ItemDrop.ItemData? m_helmetItem; }
public sealed class Player : Humanoid { public static Player? m_localPlayer; public bool Teleporting; public bool Crowned; public bool InCrownMode()=>Crowned; public bool IsTeleporting() => Teleporting; private void Update() {} }
public sealed class MonsterAI : MonoBehaviour { public bool UpdateAI(float dt)=>true; }
public sealed class SpawnSystem : MonoBehaviour { private void Update() {} }
public sealed class ZNetScene : MonoBehaviour { private void Update() {} private void CreateDestroyObjects() {} private void IsAreaReady() {} }
public sealed class Heightmap : MonoBehaviour { public enum Biome {None=0,Meadows=1,Mountain=4} }
public sealed class Tail : MonoBehaviour { private void LateUpdate() {} }
public sealed class GameCamera : MonoBehaviour { private void LateUpdate() {} }
namespace Magenheim.Runtime
{
    internal static class NowhereKingRewardRegistrar { internal const string NullMantlePrefabName="Magenheim_NullMantle"; }
    internal static class UnderworldZoneCatalogGuard { public static void Validate(ZoneSystem zone) {} }
    internal static class UnderworldZoneInstanceState { public static bool SavedLocationsReady; public static void NotifyZoneReady(ZoneSystem zone) { zone.m_locationsGenerated=SavedLocationsReady; } }
    public sealed class MagenheimPlugin : MonoBehaviour { public const string PluginGuid = "scope-regression"; }
    internal sealed class ValheimPathfindingInstanceState { public IDisposable Enter(Pathfinding value) => new Empty(); sealed class Empty : IDisposable { public void Dispose() {} } }
    internal sealed class ValheimWorldInstanceContext
    {
        public Magenheim.Core.Underworld.UnderworldWorldInstanceId InstanceId;
        public Scene Scene;
        public ZoneSystem ZoneSystem = new(); public WorldGenerator WorldGenerator = new();
        public ZDOMan ZdoMan = new(); public World World = new(); public ValheimPathfindingInstanceState? PathfindingState;
    }
    internal sealed class ValheimWorldInstanceRegistry
    {
        public readonly List<ValheimWorldInstanceContext> Contexts = new(); public bool HasUnderworldContext => Contexts.Count > 1;
        public bool TryGetContextForScene(int scene, out ValheimWorldInstanceContext? context) { context = Contexts.FirstOrDefault(c => c.Scene.handle == scene); return context is not null; }
        public bool TryGetContext(Magenheim.Core.Underworld.UnderworldWorldInstanceId id, out ValheimWorldInstanceContext? context) { context = Contexts.FirstOrDefault(c => c.InstanceId.Value == id.Value); return context is not null; }
    }
    internal static class UnderworldZNetSceneInstanceScopePatch
    {
        public static IEnumerable<MethodBase> TargetMethods() => typeof(ZNetScene).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
    }
}
internal static class Program
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Main()
    {
        var mantleItem = new ItemDrop.ItemData { m_dropPrefab=new GameObject {name="Magenheim_NullMantle"}, m_equipped=true };
        var equipmentPlayer = new Player {View=new ZNetView {Zdo=new ZDO {Helmet=999}}};
        Check(!NullMantleEquipmentState.IsEquipped(equipmentPlayer), "Owned player inherited stale inventory/network mantle state.");
        equipmentPlayer.m_helmetItem=mantleItem;
        Check(NullMantleEquipmentState.IsEquipped(equipmentPlayer), "Actual native mantle slot was not recognized.");
        equipmentPlayer.m_helmetItem=new ItemDrop.ItemData {m_dropPrefab=new GameObject {name="HelmetCrownofValheim"},m_equipped=true};
        Check(!NullMantleEquipmentState.IsEquipped(equipmentPlayer), "Vanilla crown enabled Null Mantle presentation.");
        equipmentPlayer.m_helmetItem=null;
        Check(!NullMantleEquipmentState.IsEquipped(equipmentPlayer), "Unequip retained Null Mantle presentation.");
        equipmentPlayer.View.Owner=false;
        Check(NullMantleEquipmentState.IsEquipped(equipmentPlayer), "Remote synchronized mantle was not recognized.");
        equipmentPlayer.View.Zdo!.Helmet=222;
        Check(!NullMantleEquipmentState.IsEquipped(equipmentPlayer), "Remote crown enabled Null Mantle presentation.");
        var sourceLocation = new ZoneSystem.ZoneLocation {m_biome=(Heightmap.Biome)1024};
        var childCatalog = new List<ZoneSystem.ZoneLocation> {
            new() {m_biome=Heightmap.Biome.Meadows}, new() {m_biome=Heightmap.Biome.Mountain},
            new() {m_biome=(Heightmap.Biome)(-1)}, sourceLocation};
        Check(UnderworldNativeLocationPreparation.Prepare(childCatalog)==3 && childCatalog.Count==1,
            "Underworld generation retained Surface/wildcard locations.");
        Check(childCatalog[0].m_centerFirst && childCatalog[0].Quantity==6 &&
            !sourceLocation.m_centerFirst && !ReferenceEquals(childCatalog[0],sourceLocation),
            "Underworld location sampling changed shared catalog state or native settings.");
        var registry = new ValheimWorldInstanceRegistry();
        var surface = new ValheimWorldInstanceContext { Scene = new Scene { handle = 1 }, InstanceId = new() { Value = 0 }, World = new() { m_uid = 842248273 } };
        var child = new ValheimWorldInstanceContext { Scene = new Scene { handle = 2 }, InstanceId = new() { Value = 1 }, World = new() { m_uid = 7364048498236462650 } };
        registry.Contexts.Add(surface); registry.Contexts.Add(child);
        ValheimWorldInstanceExecution.BindRegistry(registry);
        var royalPlayer = new Player { Crowned=true, View=new ZNetView { Zdo=new ZDO {Helmet=222} } };
        var royalEnemy = new MonsterAI();
        foreach (var mantle in new[] {false,true})
        foreach (var enemyInUnderworld in new[] {false,true})
        foreach (var crowned in new[] {false,true})
        {
            royalPlayer.Crowned=crowned;
            royalPlayer.View.Zdo.Helmet=mantle ? 999 : 222;
            royalEnemy.gameObject.scene=enemyInUnderworld ? child.Scene : surface.Scene;
            // An opposite ambient context must never determine which creature obeys the crown.
            using (ValheimWorldInstanceExecution.Enter(enemyInUnderworld ? surface : child))
                Check(WorldRoyaltyRuntime.InCrownModeForEnemy(royalPlayer,royalEnemy)==
                    (crowned && mantle==enemyInUnderworld), "Royalty leaked across worlds or ignored synchronized equipment.");
        }
        var crownCall = new HarmonyLib.CodeInstruction(OpCodes.Callvirt, typeof(Player).GetMethod(nameof(Player.InCrownMode)));
        crownCall.labels.Add("native-arrival"); crownCall.blocks.Add("native-block");
        var rewrite = typeof(WorldRoyaltyRuntime).GetMethod("Transpiler",BindingFlags.NonPublic|BindingFlags.Static)!;
        var rewritten=((IEnumerable<HarmonyLib.CodeInstruction>)rewrite.Invoke(null,new object[] {new[] {crownCall}})!).ToArray();
        Check(rewritten.Length==2 && rewritten[0].opcode==OpCodes.Ldarg_0 &&
            rewritten[0].labels.Contains("native-arrival") && rewritten[0].blocks.Contains("native-block") &&
            rewritten[1].Calls(typeof(WorldRoyaltyRuntime).GetMethod(nameof(WorldRoyaltyRuntime.InCrownModeForEnemy),BindingFlags.Static|BindingFlags.NonPublic)!),
            "Royalty rewrite lost the native player/enemy stack, labels or block boundaries.");
        foreach(var invalid in new[] {Array.Empty<HarmonyLib.CodeInstruction>(),new[] {crownCall,crownCall}})
        {
            var rejected=false;
            try { ((IEnumerable<HarmonyLib.CodeInstruction>)rewrite.Invoke(null,new object[] {invalid})!).ToArray(); }
            catch(InvalidOperationException) {rejected=true;}
            Check(rejected,"Changed native crown decision was silently accepted.");
        }
        ZoneSystem.s_instance = surface.ZoneSystem; WorldGenerator.m_instance = surface.WorldGenerator;
        ZDOMan.s_instance = surface.ZdoMan; ZNet.m_world = surface.World;
        ZNet.instance = new() { m_zdoMan = surface.ZdoMan };
        Player.m_localPlayer = new(); Player.m_localPlayer.gameObject.scene = child.Scene;
        Player.m_localPlayer.Teleporting = true;
        Check(!UnderworldTeleportBiomeDiscovery.Prefix(Player.m_localPlayer), "Entry discovered Underworld biome from old Surface coordinates.");
        Player.m_localPlayer.Teleporting = false;
        Check(UnderworldTeleportBiomeDiscovery.Prefix(Player.m_localPlayer), "Completed entry did not resume native biome discovery.");
        Player.m_localPlayer.gameObject.scene = surface.Scene;
        Player.m_localPlayer.Teleporting = true;
        Check(UnderworldTeleportBiomeDiscovery.Prefix(Player.m_localPlayer), "Surface teleport behavior changed.");
        Player.m_localPlayer.gameObject.scene = child.Scene;
        var keepSurface = new ZDO(); var streamChild = new ZDO(); var stale = new ZDO();
        var surfaceView = new ZNetView { Zdo = keepSurface }; surfaceView.gameObject.scene = surface.Scene;
        var childView = new ZNetView { Zdo = streamChild }; childView.gameObject.scene = child.Scene;
        var views = new Dictionary<ZDO, ZNetView> { [keepSurface] = surfaceView, [streamChild] = childView, [stale] = new ZNetView() };
        var lifetime = typeof(UnderworldNetworkViewLifetimePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)!;
        using (ValheimWorldInstanceExecution.Enter(child))
            lifetime.Invoke(null, new object[] { views });
        Check(keepSurface.TempRemoveEarmark == 1, "Child streaming would destroy Surface views.");
        Check(streamChild.TempRemoveEarmark == 0, "Child streaming cannot unload its own out-of-range views.");
        Check(!views.ContainsKey(stale), "Stale view survived native index cleanup.");
        var localZdo = new ZDO();
        var localView = new ZNetView { Zdo = localZdo, gameObject = Player.m_localPlayer.gameObject };
        views.Add(localZdo, localView);
        // Native nearby-sector lists can temporarily omit the player while its teleport
        // transform/reference position gets ahead of the synchronised ZDO position.
        using (ValheimWorldInstanceExecution.Enter(child))
            lifetime.Invoke(null, new object[] { views });
        Check(localZdo.TempRemoveEarmark == 1, "Destination streaming can unload the local player during native teleport.");
        localZdo.TempRemoveEarmark = 0;
        using (ValheimWorldInstanceExecution.Enter(surface))
            lifetime.Invoke(null, new object[] { views });
        Check(localZdo.TempRemoveEarmark == 1, "Source streaming can unload the transferred local player.");
        keepSurface.TempRemoveEarmark = 0;
        using (ValheimWorldInstanceExecution.Enter(surface))
            lifetime.Invoke(null, new object[] { views });
        Check(streamChild.TempRemoveEarmark == 1, "Return streaming would destroy Underworld views.");
        Check(keepSurface.TempRemoveEarmark == 0, "Surface streaming cannot unload its own out-of-range views.");
        var generic = UnderworldValheimBehaviourInstanceScopePatch.TargetMethods().ToHashSet();
        var owners = new[] { UnderworldZoneSystemInstanceScopePatch.TargetMethods(), UnderworldSpawnSystemInstanceScopePatch.TargetMethods(), UnderworldCharacterInstanceScopePatch.TargetMethods(), UnderworldZNetSceneInstanceScopePatch.TargetMethods() };
        foreach (var owner in owners) Check(!generic.Overlaps(owner), "Generic/specialized world scopes overlap.");
        Check(generic.Any(m => m.DeclaringType == typeof(Tail)), "Ordinary callbacks lost scene routing.");
        var camera = new GameCamera(); camera.gameObject.scene = surface.Scene;
        var surfaceDraw = new Renderer(); surfaceDraw.gameObject.scene=surface.Scene;
        var hiddenSurfaceDraw = new Renderer {forceRenderingOff=true}; hiddenSurfaceDraw.gameObject.scene=surface.Scene;
        var childDraw = new Renderer(); childDraw.gameObject.scene=child.Scene;
        var cavernRoof = new Renderer(); cavernRoof.gameObject.scene=surface.Scene;
        cavernRoof.gameObject.name="Magenheim_CavernSky_Background";
        Resources.Renderers=new[]{surfaceDraw,hiddenSurfaceDraw,childDraw,cavernRoof};
        var visibility=new UnderworldSceneVisibility();
        visibility.Apply(surface.Scene.handle);
        Check(surfaceDraw.forceRenderingOff && hiddenSurfaceDraw.forceRenderingOff,
            "Surface scenery was still drawn through the Underworld camera.");
        Check(!childDraw.forceRenderingOff && !cavernRoof.forceRenderingOff && surfaceDraw.enabled,
            "Render isolation changed child scenery, authored roof or native LOD enable state.");
        visibility.Restore();
        Check(!surfaceDraw.forceRenderingOff && hiddenSurfaceDraw.forceRenderingOff,
            "Return from Underworld did not restore original Surface render states.");
        UnderworldValheimBehaviourInstanceScopePatch.Prefix(camera, out var cameraScope);
        Check(ReferenceEquals(ZoneSystem.s_instance, child.ZoneSystem), "Shared Surface camera queried Surface terrain while viewing the Underworld.");
        Check(ValheimWorldInstanceExecution.TryGetQueryScene(out var cameraPhysics) && cameraPhysics.handle == child.Scene.handle, "Camera collision queries used the wrong physics scene.");
        UnderworldValheimBehaviourInstanceScopePatch.Finalizer(cameraScope);
        Check(ValheimWorldInstanceExecution.Active is null && ReferenceEquals(ZoneSystem.s_instance, surface.ZoneSystem), "Camera scope leaked after rendering.");
        Player.m_localPlayer.gameObject.scene = surface.Scene;
        UnderworldValheimBehaviourInstanceScopePatch.Prefix(camera, out cameraScope);
        Check(cameraScope is null, "Surface camera unnecessarily entered an instance scope.");
        Player.m_localPlayer.gameObject.scene = child.Scene;
        var netScene = new ZNetScene(); netScene.gameObject.scene = surface.Scene;
        // Replay the local-player ZNetScene callback, including a nested ordinary
        // Surface callback. Finalizers must leave all native globals at the parent.
        using (ValheimWorldInstanceExecution.Enter(child))
        {
            Check(ReferenceEquals(ZNet.m_world, child.World), "Child callback did not bind its World.");
            var tail = new Tail(); tail.gameObject.scene = surface.Scene;
            UnderworldValheimBehaviourInstanceScopePatch.Prefix(tail, out var scope);
            Check(ReferenceEquals(ZNet.m_world, surface.World), "Nested Surface callback used child World.");
            UnderworldValheimBehaviourInstanceScopePatch.Finalizer(scope);
            Check(ReferenceEquals(ZNet.m_world, child.World), "Nested callback did not restore child World.");
        }
        Check(ValheimWorldInstanceExecution.Active is null, "Child context leaked past callback.");
        Check(ReferenceEquals(ZNet.m_world, surface.World) && ReferenceEquals(ZoneSystem.s_instance, surface.ZoneSystem) && ReferenceEquals(ZDOMan.s_instance, surface.ZdoMan) && ReferenceEquals(ZNet.instance.m_zdoMan, surface.ZdoMan), "Native services leaked after transit callback.");
        Check(SceneManager.GetActiveScene().handle == surface.Scene.handle, "Active scene leaked after callback.");
        var start = typeof(UnderworldZoneSystemStartIsolationPatch).GetMethod("Prefix", BindingFlags.Static|BindingFlags.NonPublic)!;
        child.ZoneSystem.gameObject.scene = child.Scene;
        var startArguments = new object?[] {child.ZoneSystem,null};
        Check((bool)start.Invoke(null,startArguments)! == false, "Child Start installed duplicate campaign handlers.");
        Check(child.ZoneSystem.GenerationRequests == 1, "Fresh child world never started native location generation.");
        Check(UnderworldZoneSystemStartIsolationPatch.InitialLocationGenerationPending, "Initial loading released before child locations were generated.");
        (startArguments[1] as IDisposable)?.Dispose();
        UnderworldZoneInstanceState.SavedLocationsReady=true;
        startArguments = new object?[] {child.ZoneSystem,null};
        start.Invoke(null,startArguments);
        Check(child.ZoneSystem.GenerationRequests == 1, "Saved child locations were regenerated before native restore.");
        Check(!UnderworldZoneSystemStartIsolationPatch.InitialLocationGenerationPending, "Loaded native locations still block initial loading.");
        (startArguments[1] as IDisposable)?.Dispose();
        Check(ValheimWorldInstanceExecution.Active is null && ReferenceEquals(ZoneSystem.s_instance,surface.ZoneSystem), "Child generation Start leaked its world scope.");
        Console.WriteLine("PASS: Disjoint callback scopes, streaming isolation, teleport discovery and fresh/saved native child generation startup.");
    }
}
