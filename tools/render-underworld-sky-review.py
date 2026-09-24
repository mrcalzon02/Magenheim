"""Projection review of the exact packaged sky texture. Not a Valheim screenshot."""
from pathlib import Path
import math
import bpy
from mathutils import Vector

root = Path(__file__).resolve().parents[1]
out = root / "artifacts/review/sky"
out.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 16
scene.render.resolution_x = 1000
scene.render.resolution_y = 750
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.view_settings.view_transform = "Standard"
scene.world.use_nodes = True
nodes = scene.world.node_tree.nodes
nodes.clear()
image = nodes.new("ShaderNodeTexEnvironment")
image.image = bpy.data.images.load(str(root / "assets/textures/underworld/sky/lava-fungal-roof-v2.png"))
background = nodes.new("ShaderNodeBackground")
output = nodes.new("ShaderNodeOutputWorld")
# Match the runtime's one-time HDR composition: base + normalized colour * mask * 4.
mask = nodes.new("ShaderNodeTexEnvironment")
mask.image = bpy.data.images.load(str(root / "assets/textures/underworld/sky/lava-fungal-roof-emission.png"))
mask.image.colorspace_settings.name = "Non-Color"
links = scene.world.node_tree.links
separate = nodes.new("ShaderNodeSeparateColor")
links.new(image.outputs["Color"], separate.inputs["Color"])
maximum = nodes.new("ShaderNodeMath"); maximum.operation = "MAXIMUM"
links.new(separate.outputs["Red"], maximum.inputs[0]); links.new(separate.outputs["Green"], maximum.inputs[1])
maximum2 = nodes.new("ShaderNodeMath"); maximum2.operation = "MAXIMUM"
links.new(maximum.outputs[0], maximum2.inputs[0]); links.new(separate.outputs["Blue"], maximum2.inputs[1])
floor = nodes.new("ShaderNodeMath"); floor.operation = "MAXIMUM"; floor.inputs[1].default_value = .001
links.new(maximum2.outputs[0], floor.inputs[0])
divide = nodes.new("ShaderNodeMath"); divide.operation = "DIVIDE"
links.new(mask.outputs["Color"], divide.inputs[0]); links.new(floor.outputs[0], divide.inputs[1])
scale = nodes.new("ShaderNodeMath"); scale.operation = "MULTIPLY_ADD"
scale.inputs[1].default_value = 4; scale.inputs[2].default_value = 1
links.new(divide.outputs[0], scale.inputs[0])
colour = nodes.new("ShaderNodeVectorMath"); colour.operation = "SCALE"
links.new(image.outputs["Color"], colour.inputs[0]); links.new(scale.outputs[0], colour.inputs[3])
links.new(colour.outputs[0], background.inputs["Color"])
scene.world.node_tree.links.new(background.outputs["Background"], output.inputs["Surface"])
bpy.ops.object.camera_add(location=(0, 0, 0))
camera = bpy.context.object
camera.data.lens = 18
scene.camera = camera
for name, direction in (("roof", (0, .1, 1)), ("horizon", (1, 0, .45)), ("wrap", (-1, 0, .65))):
    camera.rotation_euler = Vector(direction).to_track_quat("-Z", "Y").to_euler()
    for phase, exposure in (("day", .80), ("night", .38)):
        background.inputs["Strength"].default_value = exposure
        scene.render.filepath = str(out / f"{name}-{phase}.png")
        bpy.ops.render.render(write_still=True)
print("Rendered roof, horizon and panorama wrap at day/night exposures.")
