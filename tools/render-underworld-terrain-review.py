"""Render Core-exported terrain samples; no duplicate Python world-generation formula.
Run Core tests with MAGENHEIM_TERRAIN_REVIEW=artifacts/review/terrain/authority.json first.
Then tools/blender.ps1 render-underworld-terrain-review.
These are geometry review views, not Valheim screenshots or atmosphere acceptance.
"""
import json, math
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "artifacts/review/terrain"
data = json.loads((OUT / "authority.json").read_text())
for item in data["scenes"]:
    for coarse in (False, True):
        bpy.ops.object.select_all(action="SELECT")
        bpy.ops.object.delete(use_global=False)
        edge, spacing = (129, 16) if coarse else (item["edge"], item["spacing"])
        heights = item["coarse"] if coarse else item["heights"]
        vertices = [(x * spacing - 1024, z * spacing - 1024, heights[z * edge + x])
                    for z in range(edge) for x in range(edge)]
        faces = []
        for z in range(edge - 1):
            for x in range(edge - 1):
                a = z * edge + x
                faces.extend(((a, a + 1, a + edge), (a + 1, a + edge + 1, a + edge)))
        mesh = bpy.data.meshes.new("CoreTerrain")
        mesh.from_pydata(vertices, [], faces)
        mesh.update()
        terrain = bpy.data.objects.new("AuthoritativeTerrain", mesh)
        bpy.context.collection.objects.link(terrain)
        material = bpy.data.materials.new("Cliff rock")
        material.diffuse_color = (.18, .23, .27, 1)
        terrain.data.materials.append(material)
        bpy.ops.object.camera_add(location=(5200, -6200, 3100))
        camera = bpy.context.object
        camera.rotation_euler = (Vector((0, 0, 2350)) - camera.location).to_track_quat("-Z", "Y").to_euler()
        if item["kind"] == "Plateau":
            camera.location = (5200, -6200, 7200)
            camera.rotation_euler = (Vector((0, 0, 2350)) - camera.location).to_track_quat("-Z", "Y").to_euler()
        camera.data.lens = 43
        camera.data.clip_end = 30000
        bpy.context.scene.camera = camera
        bpy.ops.object.light_add(type="SUN", location=(2000, -3000, 6500))
        bpy.context.object.rotation_euler = (math.radians(30), math.radians(-35), math.radians(-25))
        bpy.context.object.data.energy = 3
        scene = bpy.context.scene
        scene.render.engine = "CYCLES"
        scene.cycles.samples = 24
        scene.world.color = (.22, .25, .30)
        scene.render.resolution_x = 800
        scene.render.resolution_y = 1050
        scene.render.resolution_percentage = 100
        scene.view_settings.view_transform = "AgX"
        scene.render.image_settings.file_format = "PNG"
        scene.render.filepath = str(OUT / (item["kind"].lower() + ("-horizon" if coarse else "-detail") + ".png"))
        bpy.ops.render.render(write_still=True)
print("Rendered Core terrain: spire / plateau at 8m review and 16m horizon spacing.")
