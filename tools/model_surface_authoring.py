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


def load_surface_image(bpy, family):
    name = "magenheim.authored.surface." + family
    existing = bpy.data.images.get(name)
    if existing is not None:
        return existing
    image = bpy.data.images.load(str(surface_path(family)), check_existing=False)
    image.name = name
    image.pack()
    return image


def bind_surface_if_needed(bpy, material):
    """Return True only when a blank/obsolete material was changed."""
    tree = material.node_tree
    if tree is None:
        raise ValueError("Node material required: " + material.name)
    image_nodes = [node for node in tree.nodes if node.type == "TEX_IMAGE"]
    usable = next((node for node in image_nodes
                   if node.image is not None and not node.image.name.startswith("magenheim.surface.")), None)
    if usable is not None:
        return False

    family = surface_family(material.name)
    image = load_surface_image(bpy, family)
    node = image_nodes[0] if image_nodes else tree.nodes.new("ShaderNodeTexImage")
    node.image = image
    bsdf = tree.nodes.get("Principled BSDF")
    if bsdf is None:
        raise ValueError("Principled material required: " + material.name)
    if not any(link.from_node == node and link.to_socket == bsdf.inputs["Base Color"] for link in tree.links):
        tree.links.new(node.outputs["Color"], bsdf.inputs["Base Color"])
    image.pack()
    return True
