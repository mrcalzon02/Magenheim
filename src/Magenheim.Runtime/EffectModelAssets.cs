using UnityEngine;

namespace Magenheim.Runtime;

/// <summary>Single-part editable meshes for short-lived effects; keeps legacy effect lifetimes.</summary>
internal static class EffectModelAssets
{
    private static Material? _source;

    internal static GameObject Create(string id)
    {
        if (!_source) _source = new Material(Shader.Find("Standard"));
        var host = new GameObject(id);
        var renderer = host.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _source;
        try
        {
            var visual = ModelAssets.Load(host, id);
            var mesh = visual.GetComponentInChildren<MeshFilter>();
            host.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            renderer.sharedMaterial = mesh.GetComponent<MeshRenderer>().sharedMaterial;
            renderer.enabled = true;
            visual.SetActive(false);
            Object.Destroy(visual);
            return host;
        }
        catch
        {
            Object.Destroy(host);
            throw;
        }
    }
}
