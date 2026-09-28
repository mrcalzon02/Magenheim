"""Semantic authored-surface binding shared by Blender source repair and export.

Real image artwork always wins. This module changes only materials that have no usable image or
still carry an obsolete magenheim.surface.* runtime-generated bake. Those old bakes are the known
1,303-material defect: they froze a pre-classifier-fix semantic into the Blender source.

The replacement library is neutral grayscale on purpose. Existing material Base Color continues to
own elemental/alignment tint while the map owns readable surface structure.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SURFACES = ROOT / "assets" / "models" / "textures"

ITEM_TEXTURE_OVERRIDES = {
    "magenheim.furniture.furniture-crystal-bench.dark-stone": "furniture-crystal-bench-dark-stone.png",
    "magenheim.furniture.furniture-crystal-bench.wood": "furniture-crystal-bench-wood.png",
    "magenheim.furniture.furniture-crystal-bench.iron": "furniture-crystal-bench-iron.png",
    "magenheim.furniture.furniture-crystal-bench.crystal": "furniture-crystal-bench-crystal.png",
    "magenheim.furniture.furniture-geode-table.dark-stone": "furniture-geode-table-dark-stone.png",
    "magenheim.furniture.furniture-geode-table.wood": "furniture-geode-table-wood.png",
    "magenheim.furniture.furniture-geode-table.iron": "furniture-geode-table-iron.png",
    "magenheim.crystal-bed.stone": "crystal-bed-stone.png",
    "magenheim.crystal-bed.iron": "crystal-bed-iron.png",
    "magenheim.crystal-bed.mineral-water": "crystal-bed-mineral-water.png",
    "magenheim.crystal-bed.crystal-growth": "crystal-bed-crystal-growth.png",    "magenheim.crystal-enchanting-dais.dark-stone": "crystal-enchanting-dais-dark-stone.png",
    "magenheim.crystal-enchanting-dais.face-stone": "crystal-enchanting-dais-face-stone.png",
    "magenheim.crystal-enchanting-dais.iron": "crystal-enchanting-dais-iron.png",
    "magenheim.crystal-enchanting-dais.node-crystal-earth": "crystal-enchanting-dais-crystal.png",
    "magenheim.crystal-enchanting-dais.node-crystal-fire": "crystal-enchanting-dais-crystal.png",
    "magenheim.crystal-enchanting-dais.node-crystal-frost": "crystal-enchanting-dais-crystal.png",
    "magenheim.crystal-enchanting-dais.node-crystal-storm": "crystal-enchanting-dais-crystal.png",
    "magenheim.crystal-enchanting-dais.node-crystal-venom": "crystal-enchanting-dais-crystal.png",
    "magenheim.crystal-enchanting-dais.node-crystal-radiance": "crystal-enchanting-dais-crystal.png",
    "magenheim.crystal-enchanting-dais.node-crystal-seidr": "crystal-enchanting-dais-crystal.png",
    "magenheim.crystal-enchanting-dais.node-crystal-spirit": "crystal-enchanting-dais-crystal.png",
    "magenheim.crystal-enchanting-dais.central-crystal": "crystal-enchanting-dais-crystal.png",
    "magenheim.architecture.architecture-crystal-hearth.stone": "architecture-crystal-hearth-stone.png",
    "magenheim.architecture.architecture-crystal-hearth.darkstone": "architecture-crystal-hearth-darkstone.png",
    "magenheim.architecture.architecture-crystal-hearth.iron": "architecture-crystal-hearth-iron.png",
    "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.0": "architecture-crystal-hearth-rainbow-crystal.png",
    "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.1": "architecture-crystal-hearth-rainbow-crystal.png",
    "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.2": "architecture-crystal-hearth-rainbow-crystal.png",
    "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.3": "architecture-crystal-hearth-rainbow-crystal.png",
    "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.4": "architecture-crystal-hearth-rainbow-crystal.png",
    "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.5": "architecture-crystal-hearth-rainbow-crystal.png",
    "magenheim.architecture.architecture-crystal-hearth.rainbow-crystal.6": "architecture-crystal-hearth-rainbow-crystal.png",

}

CLASSIFIERS = (
    ("liquid", ("water", "liquid", "solution")),
    ("leather", ("hide", "leather", "pelt")),
    ("stone", ("stone", "marble", "rock", "earth", "strata", "slate", "basalt")),
    ("timber", ("wood", "timber", "root", "shaft", "bark")),
    ("metal", ("iron", "bronze", "silver", "gold", "metal", "band", "collar", "brace", "rail", "rim")),
    ("cloth", ("cloth", "banner", "fabric")),
    ("bone", ("bone", "ivory", "antler")),
    ("crystal", ("crystal", "frost", "rime", "ice", "spirit", "radiance", "venom", "seidr",
                 "fate", "eitr", "gem", "shard", "growth", "focus", "core", "light")),
)


def semantic_token(value):
    segments = (value or "").split(".")
    last = len(segments) - 1
    while last > 0 and segments[last].isdigit():
        last -= 1
    return segments[last].lower() if segments else ""


def surface_family(value):
    token = semantic_token(value)
    for family, needles in CLASSIFIERS:
        if any(needle in token for needle in needles):
            return family
    return "generic"


def surface_path(family):
    path = SURFACES / ("surface-" + family + "-authored.png")
    if not path.is_file():
        raise FileNotFoundError("Missing authored surface family: " + str(path))
    return path


def desired_texture_path(material_name):
    override = ITEM_TEXTURE_OVERRIDES.get(material_name)
    if override:
        path = SURFACES / override
        if not path.is_file():
            raise FileNotFoundError("Missing model-specific authored surface: " + str(path))
        return path
    return surface_path(surface_family(material_name))


def load_surface_image(bpy, material_name):
    path = desired_texture_path(material_name)
    name = "magenheim.authored." + path.stem
    existing = bpy.data.images.get(name)
    if existing is not None:
        return existing
    image = bpy.data.images.load(str(path), check_existing=False)
    image.name = name
    image.pack()
    return image


def bind_surface_if_needed(bpy, material):
    """Return True only when a blank/obsolete material was changed."""
    tree = material.node_tree
    if tree is None:
        raise ValueError("Node material required: " + material.name)
    image_nodes = [node for node in tree.nodes if node.type == "TEX_IMAGE"]
    override = ITEM_TEXTURE_OVERRIDES.get(material.name)
    usable = next((node for node in image_nodes
                   if node.image is not None
                   and not node.image.name.startswith("magenheim.surface.")
                   and not (override and node.image.name.startswith("magenheim.authored.surface-"))), None)
    if usable is not None:
        return False

    image = load_surface_image(bpy, material.name)
    node = image_nodes[0] if image_nodes else tree.nodes.new("ShaderNodeTexImage")
    node.image = image
    bsdf = tree.nodes.get("Principled BSDF")
    if bsdf is None:
        raise ValueError("Principled material required: " + material.name)
    if not any(link.from_node == node and link.to_socket == bsdf.inputs["Base Color"] for link in tree.links):
        tree.links.new(node.outputs["Color"], bsdf.inputs["Base Color"])
    image.pack()
    return True
