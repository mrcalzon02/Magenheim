using System;
using System.Collections.Generic;
using System.Linq;
using NQuat = System.Numerics.Quaternion;
using NVec3 = System.Numerics.Vector3;

namespace UnityEngine;

public enum HideFlags { HideAndDontSave }
public class Object
{
    public HideFlags hideFlags;
    public static void Destroy(Object? o) {}
    public static void DestroyImmediate(Object? o) { if(o is GameObject g)g.transform.SetParent(null); }
    public string name = string.Empty;
    public static implicit operator bool(Object? value) => value is not null;
    public static bool operator !(Object? value) => value is null;
}

public class Component : Object
{
    public GameObject gameObject = null!;
    public Transform transform => gameObject.transform;
    public T[] GetComponentsInChildren<T>(bool includeInactive=false) where T:Component => gameObject.GetComponentsInChildren<T>(includeInactive);
}

public enum PrimitiveType { Cube, Cylinder, Sphere }
public struct Vector3Int { public int x,y,z; public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;} }
public class Shader:Object { public static Shader Find(string name)=>new(){name=name}; }
public class GameObject : Object
{
    private readonly List<Component> _components = new();
    public static GameObject CreatePrimitive(PrimitiveType type) {
        var go=new GameObject(type.ToString());
        var data=type==PrimitiveType.Cube ? Magenheim.Core.Geometry.MeshPrimitives.Box() : Magenheim.Core.Geometry.MeshPrimitives.Cylinder(20);
        go.AddComponent<MeshFilter>().sharedMesh=new Mesh {vertices=data.Vertices.Select(v=>new Vector3(v.X,v.Y*(type==PrimitiveType.Cylinder?2:1),v.Z)).ToArray(),triangles=data.Triangles.ToArray()};
        if(type==PrimitiveType.Sphere){
            var v=new List<Vector3>();var t=new List<int>();const int sides=24,rings=16;
            for(int r=0;r<=rings;r++)for(int i=0;i<=sides;i++){float a=MathF.PI*r/rings,b=2*MathF.PI*i/sides;v.Add(new Vector3(.5f*MathF.Sin(a)*MathF.Cos(b),.5f*MathF.Cos(a),.5f*MathF.Sin(a)*MathF.Sin(b)));}
            for(int r=0;r<rings;r++)for(int i=0;i<sides;i++){int a=r*(sides+1)+i,b=a+sides+1;if(r>0)t.AddRange(new[]{a,a+1,b});if(r<rings-1)t.AddRange(new[]{a+1,b+1,b});}
            go.GetComponent<MeshFilter>()!.sharedMesh=new Mesh{vertices=v.ToArray(),triangles=t.ToArray()};
        }
        go.AddComponent<MeshRenderer>().sharedMaterial=new Material();go.AddComponent<BoxCollider>();return go;
    }
    public int layer;
    public bool activeSelf { get; private set; } = true;
    public Transform transform { get; }

    public GameObject(string name = "GameObject")
    {
        this.name = name;
        transform = new Transform { gameObject = this };
        _components.Add(transform);
    }

    public void SetActive(bool active) => activeSelf = active;

    public T AddComponent<T>() where T : Component, new()
    {
        var component = new T { gameObject = this };
        _components.Add(component);
        return component;
    }

    public T? GetComponent<T>() where T : Component => _components.OfType<T>().FirstOrDefault();

    public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : Component
    {
        var list = new List<T>();
        Collect(this, list, includeInactive);
        return list.ToArray();

        static void Collect(GameObject go, List<T> target, bool includeInactive)
        {
            if (!includeInactive && !go.activeSelf) return;
            target.AddRange(go._components.OfType<T>());
            foreach (var child in go.transform.Children) Collect(child.gameObject, target, includeInactive);
        }
    }
}

public class Transform : Component
{
    internal List<Transform> Children { get; } = new();
    public Transform? parent { get; private set; }
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localScale = Vector3.one;
    public Quaternion localRotation = Quaternion.identity;
    public Vector3 localEulerAngles { get => localRotation.eulerAngles; set => localRotation = Quaternion.Euler(value); }
    public int childCount => Children.Count;
    public Transform GetChild(int index) => Children[index];

    public void SetParent(Transform? newParent, bool worldPositionStays = false)
    {
        parent?.Children.Remove(this);
        parent = newParent;
        newParent?.Children.Add(this);
    }

    public Transform? Find(string path)
    {
        var current = this;
        foreach (var segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = current.Children.FirstOrDefault(c => c.gameObject.name == segment);
            if (current is null) return null;
        }
        return current;
    }

    public bool IsChildOf(Transform possibleParent)
    {
        for (var current = parent; current is not null; current = current.parent)
            if (ReferenceEquals(current, possibleParent)) return true;
        return false;
    }

    public Vector3 TransformPoint(Vector3 point)
    {
        var local = localRotation * Vector3.Scale(point, localScale) + localPosition;
        return parent is null ? local : parent.TransformPoint(local);
    }
}

public struct Vector2
{
    public float x, y;
    public Vector2(float x, float y) { this.x = x; this.y = y; }
    public static Vector2 zero => new(0f,0f);
    public static Vector2 one => new(1f,1f);
    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x+b.x,a.y+b.y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x-b.x,a.y-b.y);
    public static Vector2 operator *(Vector2 a, float s) => new(a.x*s,a.y*s);
    public static Vector2 operator /(Vector2 a, float s) => new(a.x/s,a.y/s);
}

public struct Vector3
{
    public float x,y,z;
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public static Vector3 zero => new(0,0,0); public static Vector3 one => new(1,1,1);
    public static Vector3 up => new(0,1,0); public static Vector3 down => new(0,-1,0);
    public static Vector3 right => new(1,0,0); public static Vector3 left => new(-1,0,0);
    public static Vector3 forward => new(0,0,1); public static Vector3 back => new(0,0,-1);
    public float magnitude => MathF.Sqrt(x*x+y*y+z*z); public float sqrMagnitude => x*x+y*y+z*z;
    public Vector3 normalized => magnitude <= 1e-7f ? zero : this/magnitude;
    public void Normalize(){var m=magnitude;if(m>1e-7f){x/=m;y/=m;z/=m;}}
    public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
    public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
    public static Vector3 operator -(Vector3 a)=>new(-a.x,-a.y,-a.z);
    public static Vector3 operator *(Vector3 a,float s)=>new(a.x*s,a.y*s,a.z*s);
    public static Vector3 operator *(float s,Vector3 a)=>a*s;
    public static Vector3 operator /(Vector3 a,float s)=>new(a.x/s,a.y/s,a.z/s);
    public static Vector3 Scale(Vector3 a,Vector3 b)=>new(a.x*b.x,a.y*b.y,a.z*b.z);
    public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    public static Vector3 Cross(Vector3 a,Vector3 b)=>new(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
    public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
}

public struct Quaternion
{
    internal NQuat value;
    internal Quaternion(NQuat value){this.value=value;}
    public static Quaternion identity => new(NQuat.Identity);
    public static Quaternion Euler(float x,float y,float z)=>Euler(new Vector3(x,y,z));
    public static Quaternion Euler(Vector3 e)
    {
        var qx=NQuat.CreateFromAxisAngle(NVec3.UnitX,e.x*Mathf.Deg2Rad);
        var qy=NQuat.CreateFromAxisAngle(NVec3.UnitY,e.y*Mathf.Deg2Rad);
        var qz=NQuat.CreateFromAxisAngle(NVec3.UnitZ,e.z*Mathf.Deg2Rad);
        return new Quaternion(NQuat.Normalize(qy*qx*qz));
    }
    public static Quaternion FromToRotation(Vector3 from,Vector3 to)
    {
        var a=from.normalized;var b=to.normalized;var dot=Math.Clamp(Vector3.Dot(a,b),-1f,1f);
        if(dot>.999999f)return identity;
        if(dot<-.999999f){var axis=Vector3.Cross(a,Vector3.right);if(axis.sqrMagnitude<1e-6f)axis=Vector3.Cross(a,Vector3.up);axis=axis.normalized;return new Quaternion(NQuat.CreateFromAxisAngle(new NVec3(axis.x,axis.y,axis.z),MathF.PI));}
        var c=Vector3.Cross(a,b).normalized;return new Quaternion(NQuat.CreateFromAxisAngle(new NVec3(c.x,c.y,c.z),MathF.Acos(dot)));
    }
    public static Quaternion operator *(Quaternion a,Quaternion b)=>new(NQuat.Normalize(a.value*b.value));
    public static Vector3 operator *(Quaternion q,Vector3 v){var r=NVec3.Transform(new NVec3(v.x,v.y,v.z),q.value);return new Vector3(r.X,r.Y,r.Z);}
    public Vector3 eulerAngles => Vector3.zero;
}

public struct Color
{
    public float r,g,b,a;
    public Color(float r,float g,float b,float a=1f){this.r=r;this.g=g;this.b=b;this.a=a;}
    public static Color white=>new(1,1,1,1);public static Color black=>new(0,0,0,1);public static Color clear=>new(0,0,0,0);
    public static Color red=>new(1,0,0,1);public static Color green=>new(0,1,0,1);public static Color blue=>new(0,0,1,1);
    public static Color yellow=>new(1,.9215686f,.01568628f,1);public static Color cyan=>new(0,1,1,1);public static Color magenta=>new(1,0,1,1);
    public static Color grey=>new(.5f,.5f,.5f,1);public static Color gray=>grey;
    public static Color Lerp(Color a,Color b,float t){t=Mathf.Clamp01(t);return new(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);}
    public static Color operator *(Color c,float s)=>new(c.r*s,c.g*s,c.b*s,c.a*s);public static Color operator *(float s,Color c)=>c*s;
}

public static class ColorUtility
{
    public static string ToHtmlStringRGB(Color c)=>$"{B(c.r):X2}{B(c.g):X2}{B(c.b):X2}";
    private static int B(float v)=>(int)MathF.Round(Math.Clamp(v,0f,1f)*255f);
}

public static class Mathf
{
    public const float PI=MathF.PI,Deg2Rad=PI/180f,Rad2Deg=180f/PI;
    public static float Sin(float v)=>MathF.Sin(v);public static float Cos(float v)=>MathF.Cos(v);public static float Tan(float v)=>MathF.Tan(v);
    public static float Atan2(float y,float x)=>MathF.Atan2(y,x);public static float Sqrt(float v)=>MathF.Sqrt(v);public static float Abs(float v)=>MathF.Abs(v);public static int Abs(int v)=>Math.Abs(v);
    public static float Min(float a,float b)=>MathF.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>MathF.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);
    public static float Clamp(float v,float min,float max)=>Math.Clamp(v,min,max);public static int Clamp(int v,int min,int max)=>Math.Clamp(v,min,max);public static float Clamp01(float v)=>Math.Clamp(v,0f,1f);
    public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);public static int RoundToInt(float v)=>(int)MathF.Round(v);public static int FloorToInt(float v)=>(int)MathF.Floor(v);public static int CeilToInt(float v)=>(int)MathF.Ceiling(v);public static float Pow(float a,float b)=>MathF.Pow(a,b);
}

public struct Bounds { public Vector3 center,size; }
public static class ImageConversion { public static bool LoadImage(Texture2D t,byte[] bytes,bool unreadable)=>bytes.Length>8 && bytes[0]==137 && bytes[1]==80; }
public class Mesh:Object
{
    public UnityEngine.Rendering.IndexFormat indexFormat; public Bounds bounds;
    public Vector3[] vertices=Array.Empty<Vector3>();public int[] triangles=Array.Empty<int>();public Vector3[] normals=Array.Empty<Vector3>();public Vector2[] uv=Array.Empty<Vector2>();
    public void SetVertices(List<Vector3> v){vertices=v.ToArray();}public void SetTriangles(List<int> t,int s){triangles=t.ToArray();}
    public void RecalculateNormals(){}public void RecalculateBounds(){if(vertices.Length==0)return;var min=new Vector3(vertices.Min(v=>v.x),vertices.Min(v=>v.y),vertices.Min(v=>v.z));var max=new Vector3(vertices.Max(v=>v.x),vertices.Max(v=>v.y),vertices.Max(v=>v.z));bounds=new Bounds{center=(min+max)*.5f,size=max-min};}public void RecalculateTangents(){}
}
public class MeshFilter:Component{public Mesh? sharedMesh;public Mesh? mesh{get=>sharedMesh;set=>sharedMesh=value;}}
public class Renderer:Component
{
    public bool enabled=true;public Material? sharedMaterial;public Material[] sharedMaterials=Array.Empty<Material>();public bool receiveShadows=true;public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
}
public class MeshRenderer:Renderer{} public class ParticleSystemRenderer:Renderer{} public class LODGroup:Component{public bool enabled=true;}
public class Light:Component{public Color color;public float intensity;public float range;}

public class Material:Object
{
    public Color color=Color.white;public Texture? mainTexture;public Vector2 mainTextureScale=Vector2.one;public Vector2 mainTextureOffset=Vector2.zero;public int renderQueue; public Dictionary<string,float> Floats=new(); public Dictionary<string,Color> Colors=new();
    public Material(){}public Material(Shader shader){}public Material(Material source){name=source.name;color=source.color;mainTexture=source.mainTexture;mainTextureScale=source.mainTextureScale;mainTextureOffset=source.mainTextureOffset;renderQueue=source.renderQueue;}
    public bool HasProperty(string name)=>true;public void SetColor(string name,Color value){Colors[name]=value;if(name=="_Color")color=value;}public void SetFloat(string name,float value){Floats[name]=value;}public void SetInt(string name,int value){Floats[name]=value;}public void SetTexture(string name,Texture? value){}public void EnableKeyword(string keyword){}public void DisableKeyword(string keyword){}public void SetOverrideTag(string tag,string value){}
}

public class Texture:Object{} public enum TextureFormat{RGBA32} public enum TextureWrapMode{Clamp,Repeat} public enum FilterMode{Point,Bilinear,Trilinear}
public class Texture2D:Texture
{
    public int width,height,anisoLevel; public Color[] pixels=Array.Empty<Color>();
    public TextureWrapMode wrapMode;public FilterMode filterMode;public static Texture2D whiteTexture{get;}=new(1,1,TextureFormat.RGBA32,false);
    public Texture2D(int width,int height,TextureFormat format,bool mipChain){this.width=width;this.height=height;}public void SetPixels(Color[] colors){pixels=colors;}public void Apply(bool updateMipmaps=true,bool makeNoLongerReadable=false){}
}
public struct Rect{public float x,y,width,height;public Rect(float x,float y,float width,float height){this.x=x;this.y=y;this.width=width;this.height=height;}}
public class Sprite:Object{public static Sprite Create(Texture2D texture,Rect rect,Vector2 pivot,float pixelsPerUnit)=>new();}

public class Collider:Component{public bool enabled=true;public bool isTrigger;} public class SphereCollider:Collider{public Vector3 center;public float radius;} public class BoxCollider:Collider{public Vector3 center;public Vector3 size;} public class CapsuleCollider:Collider{public Vector3 center;public float radius;public float height;public int direction;} public class MeshCollider:Collider{public Mesh? sharedMesh;public bool convex;}

public struct GradientColorKey{public Color color;public float time;public GradientColorKey(Color color,float time){this.color=color;this.time=time;}} public struct GradientAlphaKey{public float alpha,time;public GradientAlphaKey(float alpha,float time){this.alpha=alpha;this.time=time;}} public class Gradient{public void SetKeys(GradientColorKey[] colors,GradientAlphaKey[] alphas){}}
public class ParticleSystem:Component
{
    public ColorOverLifetimeModule colorOverLifetime=>new();public MainModule main=>new();
    public struct ColorOverLifetimeModule{public bool enabled{set{}}public MinMaxGradient color{set{}}}
    public struct MainModule{public MinMaxGradient startColor{set{}}}
    public struct MinMaxGradient{public MinMaxGradient(Color color){}public MinMaxGradient(Gradient gradient){}}
}
