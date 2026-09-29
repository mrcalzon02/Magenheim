"""Bind shared Underworld source maps into Blender production materials."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"material-source"/"underworld"

EXACT={
"armour.sporeweave.base":"sporeweave-fibre","armour.sporeweave.structure":"worldroot-bark","armour.sporeweave.accent":"glowcap",
"armour.palewater.base":"pale-fibre","armour.palewater.structure":"flowstone","armour.palewater.accent":"blackwater-pearl",
"armour.emberiron.base":"charred-root","armour.emberiron.structure":"emberiron","armour.emberiron.accent":"ember-heat",
"armour.rimeward.base":"rimewood","armour.rimeward.structure":"rimesilver","armour.rimeward.accent":"clear-ice",
"armour.stoneanchor.base":"titanbone","armour.stoneanchor.structure":"shardstone","armour.stoneanchor.accent":"fracture-crystal",
"armour.defiant.base":"rotwood","armour.defiant.structure":"bone","armour.defiant.accent":"carrion-amber",
"tool.sporelight.root":"worldroot-bark",
"underworld.geothermal-vent.heat":"ember-heat",
}
RULES=(
("spore-crystal","glowcap"),("sporeweave-fibre","sporeweave-fibre"),("worldroot-heartwood","worldroot-heartwood"),("worldroot","worldroot-bark"),
("understone","understone"),("glowcap","glowcap"),("flowstone","flowstone"),("pale-fibre","pale-fibre"),
("pearl-metal","pearl-metal"),("pearl","blackwater-pearl"),("slagstone","slagstone"),("sulfur","sulfur-crust"),("charred","charred-root"),
("emberiron","emberiron"),("furnace.heart","ember-heat"),("tool.slag.heat","ember-heat"),
("rimewood","rimewood"),("rimesilver","rimesilver"),("clear-ice","clear-ice"),("focus-ice","clear-ice"),
("shardstone","shardstone"),("titanbone","titanbone"),("fracture","fracture-crystal"),
("rotwood","rotwood"),("decay-spore","decay-spore"),("bone-gravel","bone"),(".bone","bone"),("carrion-amber","carrion-amber"),
(".amber","carrion-amber"),("dark-iron","forged-iron"),("forged-iron","forged-iron"),
("forged-brace","forged-iron"),("dark-metal","forged-iron"),("dark-binding","forged-iron"),
(".binding","forged-iron"),(".iron","forged-iron"),
)

def material_key(name):
    low=(name or "").lower()
    if low in EXACT:return EXACT[low]
    for token,key in RULES:
        if token in low:return key
    raise ValueError("No Underworld material source mapping for "+repr(name))

def _load(bpy,key,suffix,noncolor=False):
    path=SOURCE/f"{key}-{suffix}.png"
    if not path.is_file(): raise FileNotFoundError("Missing Underworld source map: "+str(path))
    image=bpy.data.images.get("magenheim.underworld."+key+"."+suffix)
    if image is None:
        image=bpy.data.images.load(str(path),check_existing=False);image.name="magenheim.underworld."+key+"."+suffix
    if noncolor: image.colorspace_settings.name="Non-Color"
    image.pack();return image

def bind_underworld_material(bpy,material,semantic=None):
    semantic=semantic or material.name
    key=material_key(semantic)
    material["magenheim_material_name"]=semantic
    material["magenheim_material_source_key"]=key
    tree=material.node_tree
    bsdf=tree.nodes.get("Principled BSDF")
    if bsdf is None: raise ValueError("Principled material required: "+semantic)
    for node in list(tree.nodes):
        if node.type in {"TEX_IMAGE","NORMAL_MAP"} or node.name.startswith("Magenheim Underworld"):
            tree.nodes.remove(node)
    albedo=tree.nodes.new("ShaderNodeTexImage");albedo.name="Magenheim Underworld Albedo";albedo.image=_load(bpy,key,"albedo")
    tree.links.new(albedo.outputs["Color"],bsdf.inputs["Base Color"]);bsdf.inputs["Base Color"].default_value=(1,1,1,1)
    rough=tree.nodes.new("ShaderNodeTexImage");rough.name="Magenheim Underworld Roughness";rough.image=_load(bpy,key,"roughness",True)
    tree.links.new(rough.outputs["Color"],bsdf.inputs["Roughness"])
    packed=tree.nodes.new("ShaderNodeTexImage");packed.name="Magenheim Underworld MetallicSmoothness";packed.image=_load(bpy,key,"metallic-smoothness",True)
    separate=tree.nodes.new("ShaderNodeSeparateColor");separate.name="Magenheim Underworld MetallicChannel"
    tree.links.new(packed.outputs["Color"],separate.inputs["Color"])
    tree.links.new(separate.outputs["Red"],bsdf.inputs["Metallic"])
    normal=tree.nodes.new("ShaderNodeTexImage");normal.name="Magenheim Underworld Normal";normal.image=_load(bpy,key,"normal",True)
    nmap=tree.nodes.new("ShaderNodeNormalMap");nmap.name="Magenheim Underworld NormalMap";nmap.inputs["Strength"].default_value=.42
    tree.links.new(normal.outputs["Color"],nmap.inputs["Color"]);tree.links.new(nmap.outputs["Normal"],bsdf.inputs["Normal"])
    emission=SOURCE/f"{key}-emission.png"
    if emission.is_file() and "Emission Color" in bsdf.inputs:
        em=tree.nodes.new("ShaderNodeTexImage");em.name="Magenheim Underworld Emission";em.image=_load(bpy,key,"emission")
        tree.links.new(em.outputs["Color"],bsdf.inputs["Emission Color"])
        if bsdf.inputs["Emission Strength"].default_value<=0:bsdf.inputs["Emission Strength"].default_value=.8
    return key
