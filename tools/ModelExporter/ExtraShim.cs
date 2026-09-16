using UnityEngine;
public class Room:Component { public bool m_enabled; public Vector3Int m_size; }
public class Character:Component {}
public class ZNetView:Component {}
namespace Jotunn.Managers { public class PrefabManager { public static PrefabManager Instance=new(); public GameObject GetPrefab(string name){var g=new GameObject(name);g.AddComponent<MeshRenderer>().sharedMaterial=new Material();return g;} } }
namespace Magenheim.Runtime {
internal class DeepFractureTraversalPortal:Component {}
internal class DarkThroneEncounterRuntime:Component {}
internal class DarkThroneDaisRuntime:Component { public void Bind(DarkThroneEncounterRuntime r){} }
internal static class DarkThroneCrystalSpawnerFactory { public static void Create(Transform t,string n,Vector3 p,Magenheim.Core.DarkThrone.CrystalCreatureSpawnProfile f){} }
internal enum DeepFractureCrystalFunction { Regeneration, Resistance, Armor, Locomotion, Control, Burrow, Regrowth, WorldCore, DeathAnchor, EnvironmentalControl }
internal class CapturedCrystal:Component { public string id="",function=""; public float health; }
internal static class DeepFractureCrystalComponent { public static void Attach(GameObject g,Character c,ZNetView z,string s,DeepFractureCrystalFunction f,float h){var a=g.AddComponent<CapturedCrystal>();a.id=s;a.function=f.ToString();a.health=h;} }
}
