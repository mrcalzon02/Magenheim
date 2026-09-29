#!/usr/bin/env python3
"""Derive all twelve Underworld weapons from authoritative Crystal-grade Blender sources.

Every derivative opens its existing Crystal weapon/staff source, preserves that chassis, grip origin,
painted atlas and confirmed held envelope, then adds biome-authentic PBR hardware. The derivative is
rejected if its additions lengthen the chassis or materially expand the cross-section.
"""
import json
import math
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from underworld_material_library import bind_underworld_material

import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"models"/"source"
REVISION="underworld-crystal-derivatives-r2"
SOURCE_LENGTH_AXIS=2  # Blender Z; export-model-assets maps this to runtime Y.

SPECS={
 "underworld-weapon-worldroot-club":("crystal-weapon-mace","worldroot-club"),
 "underworld-weapon-worldroot-bow":("crystal-weapon-bow","worldroot-bow"),
 "underworld-weapon-flowstone-maul":("crystal-weapon-mace","flowstone-maul"),
 "underworld-weapon-blackwater-harpoon":("crystal-weapon-spear","blackwater-harpoon"),
 "underworld-weapon-emberiron-axe":("crystal-weapon-axe","emberiron-axe"),
 "underworld-weapon-emberiron-greatsword":("crystal-weapon-greatsword","emberiron-greatsword"),
 "underworld-weapon-rimesilver-spear":("crystal-weapon-spear","rimesilver-spear"),
 "underworld-weapon-icebind-staff":("staff-frost-crystal","icebind-staff"),
 "underworld-weapon-titanbone-atgeir":("crystal-weapon-atgeir","titanbone-atgeir"),
 "underworld-weapon-shardstone-crossbow":("crystal-weapon-crossbow","shardstone-crossbow"),
 "underworld-weapon-amber-blade":("crystal-weapon-sword","amber-blade"),
 "underworld-weapon-crown-sceptre":("crystal-weapon-mace","crown-sceptre"),
}

def material(name,colour,metallic=0.0,roughness=.72,emission=None,semantic=None):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.use_backface_culling=True
 bs=m.node_tree.nodes.get("Principled BSDF")
 bs.inputs["Base Color"].default_value=(*colour,1);bs.inputs["Metallic"].default_value=metallic;bs.inputs["Roughness"].default_value=roughness
 if emission is not None and "Emission Color" in bs.inputs:
  bs.inputs["Emission Color"].default_value=(*emission,1);bs.inputs["Emission Strength"].default_value=.28
 bind_underworld_material(bpy,m,semantic or name)
 m["magenheim_material_name"]=name
 return m

def finish(obj,path,mat):
 obj.name=path;obj["game_node_path"]=path;obj["game_collision"]=False;obj["game_crystal"]=json.dumps(None)
 obj.data.materials.clear();obj.data.materials.append(mat)
 if not obj.data.uv_layers:
  obj.data.uv_layers.new(name="UVMap");bpy.context.view_layer.objects.active=obj;obj.select_set(True)
  bpy.ops.object.mode_set(mode="EDIT");bpy.ops.mesh.select_all(action="SELECT");bpy.ops.uv.cube_project(cube_size=.22)
  bpy.ops.object.mode_set(mode="OBJECT");obj.select_set(False)
 return obj

def torus(path,z,major,minor,mat,x=0,y=0,rotation=(0,0,0)):
 bpy.ops.mesh.primitive_torus_add(major_segments=20,minor_segments=8,major_radius=major,minor_radius=minor,location=(x,y,z),rotation=rotation)
 return finish(bpy.context.object,path,mat)

def sphere(path,loc,radius,mat):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=radius,location=loc)
 return finish(bpy.context.object,path,mat)

def tube(path,start,end,radius,mat,vertices=10):
 a,b=Vector(start),Vector(end);direction=b-a
 if direction.length<=1e-5:raise ValueError(path+": degenerate tube")
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=direction.length,location=(a+b)*.5)
 o=bpy.context.object;o.rotation_mode="QUATERNION";o.rotation_quaternion=Vector((0,0,1)).rotation_difference(direction.normalized())
 return finish(o,path,mat)

def bounds():
 meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"]
 if not meshes:raise RuntimeError("Derivative source contains no meshes")
 points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
 return [(min(p[i] for p in points),max(p[i] for p in points)) for i in range(3)]

def palette(model_id,kind):
 p="magenheim.underworld-weapon."+model_id+"."
 if kind.startswith("worldroot"):
  return material(p+"worldroot.timber",(.30,.38,.20),0,.78,semantic="worldroot"),material(p+"spore-crystal.crystal",(.46,.82,.52),0,.28,(.12,.34,.16),semantic="spore-crystal"),None
 if kind.startswith("flowstone") or kind=="blackwater-harpoon":
  return material(p+"flowstone",(.28,.38,.40),0,.78,semantic="flowstone"),material(p+"blackwater-pearl",(.56,.80,.84),.08,.22,(.08,.22,.26),semantic="blackwater-pearl"),material(p+"pale-fibre",(.54,.57,.51),0,.72,semantic="pale-fibre")
 if kind.startswith("emberiron"):
  return material(p+"emberiron",(.27,.15,.09),.82,.30,semantic="emberiron"),material(p+"furnace.heart",(.92,.28,.045),.02,.18,(.76,.12,.02),semantic="furnace.heart"),material(p+"charred-root",(.15,.07,.035),0,.86,semantic="charred-root")
 if kind.startswith("rime") or kind=="icebind-staff":
  return material(p+"rimesilver",(.69,.76,.79),.88,.20,semantic="rimesilver"),material(p+"clear-ice",(.68,.89,.96),0,.15,(.12,.34,.44),semantic="clear-ice"),material(p+"rimewood",(.33,.41,.43),0,.78,semantic="rimewood")
 if kind.startswith("titanbone"):
  return material(p+"titanbone",(.53,.49,.39),0,.78,semantic="titanbone"),material(p+"fracture-crystal",(.60,.42,.80),0,.18,(.24,.10,.38),semantic="fracture-crystal"),material(p+"forged-brace",(.15,.14,.16),.78,.34,semantic="forged-brace")
 if kind.startswith("shardstone"):
  return material(p+"shardstone",(.25,.22,.28),0,.82,semantic="shardstone"),material(p+"fracture-crystal",(.60,.42,.80),0,.18,(.24,.10,.38),semantic="fracture-crystal"),material(p+"titanbone",(.53,.49,.39),0,.78,semantic="titanbone")
 return material(p+"rotwood",(.20,.12,.055),0,.84,semantic="rotwood"),material(p+"carrion-amber",(.76,.45,.11),.03,.18,(.36,.13,.02),semantic="carrion-amber"),material(p+"bone",(.39,.34,.26),0,.78,semantic="bone")

def rings(prefix,zs,r,mat):
 for i,z in enumerate(zs):torus(prefix+"-"+str(i),z,r,.006,mat)

def worldroot_club(a,b,c):
 rings("worldroot/grip-collar",(-.39,-.16,.34),.036,a)
 for i,(s,e) in enumerate([
  ((.025,0,.31),(.105,.035,.55)),((-.025,0,.31),(-.105,-.035,.55)),
  ((0,.025,.33),(.07,-.085,.60)),((0,-.025,.33),(-.07,.085,.60))]):tube("worldroot/head-brace-"+str(i),s,e,.012,a)
 for i,p in enumerate(((.105,.03,.56),(-.095,-.035,.59),(.04,-.09,.53))):sphere("worldroot/spore-node-"+str(i),p,.021 if i else .026,b)

def worldroot_bow(a,b,c):
 rings("worldroot/riser-binding",(-.18,-.06,.07,.19),.034,a)
 for i,(s,e) in enumerate([
  ((-.02,.018,.17),(-.045,.02,.48)),((-.045,.02,.48),(-.025,.012,.76)),
  ((.02,-.018,-.17),(.045,-.02,-.48)),((.045,-.02,-.48),(.025,-.012,-.76))]):tube("worldroot/limb-rib-"+str(i),s,e,.009,a)
 sphere("worldroot/spore-upper",(-.04,.02,.30),.021,b);sphere("worldroot/spore-lower",(.015,-.02,-.30),.020,b)

def flowstone_maul(a,b,c):
 rings("flowstone/socket",(.25,.34),.043,a)
 for i in range(6):
  ang=i*math.tau/6;x,y=math.cos(ang)*.105,math.sin(ang)*.075
  tube("flowstone/cage-"+str(i),(x*.35,y*.35,.36),(x,y,.57),.011,a)
  sphere("flowstone/pearl-"+str(i),(x,y,.57),.018,b)
 rings("flowstone/pale-binding",(-.40,-.22),.032,c)

def blackwater_harpoon(a,b,c):
 rings("blackwater/socket-binding",(.46,.54),.027,c)
 for i,(z,sgn) in enumerate(((.70,1),(.82,-1),(.93,1))):
  tube("blackwater/barb-"+str(i),(0,0,z),(sgn*.070,0,z+.070),.010,a)
  sphere("blackwater/barb-pearl-"+str(i),(sgn*.070,0,z+.070),.014,b)
 sphere("blackwater/socket-pearl",(0,0,.57),.018,b)

def emberiron_axe(a,b,c):
 rings("emberiron/haft-collar",(.36,.44),.034,a)
 tube("emberiron/head-backstrap",(-.02,0,.43),(-.02,0,.61),.014,a)
 for i,p in enumerate(((.08,0,.47),(.17,0,.54),(.25,0,.59))):sphere("emberiron/heat-rivet-"+str(i),p,.014,b)
 rings("emberiron/grip-char",(-.38,-.18),.030,c)

def emberiron_greatsword(a,b,c):
 rings("emberiron/grip-collar",(-.42,-.30,-.10),.030,a)
 for side in (-1,1):
  tube("emberiron/ricasso-langet-"+str(side),(side*.045,0,-.02),(side*.050,0,.26),.012,a)
  sphere("emberiron/guard-ember-"+str(side),(side*.075,0,-.02),.014,b)
 tube("emberiron/spine-bind",(0,0,.28),(0,0,.54),.009,c)

def rimesilver_spear(a,b,c):
 rings("rime/socket",(.46,.55),.028,a)
 for i,z in enumerate((.66,.79,.91)):
  sphere("rime/iceglass-"+str(i),(0,0,z),.018 if i!=1 else .022,b)
  tube("rime/silver-vein-"+str(i),(-.025,0,z-.055),(.025,0,z+.055),.008,a)
 rings("rime/grip-laminate",(-.40,-.18),.025,c)

def icebind_staff(a,b,c):
 # Blender source convention is Z-long for held weapons/staves; export maps source Z to runtime Y.
 rings("icebind/crown-collar",(.56,.72),.052,a)
 sphere("icebind/focus",(0,0,.88),.065,b)
 for side in (-1,1):
  tube("icebind/focus-brace-"+str(side),(side*.045,0,.65),(side*.075,0,.84),.010,a)
 rings("icebind/rimewood-grip",(-.42,-.18),.032,c)

def titanbone_atgeir(a,b,c):
 rings("titanbone/socket",(.56,.66),.031,c)
 for side in (-1,1):
  tube("titanbone/wing-brace-"+str(side),(side*.025,0,.65),(side*.120,0,.78),.014,a)
  sphere("titanbone/fracture-node-"+str(side),(side*.115,0,.78),.020,b)
 rings("titanbone/grip-plate",(-.46,-.18),.029,a)

def shardstone_crossbow(a,b,c):
 for i,z in enumerate((-.28,-.08,.16,.34)):tube("shardstone/stock-rib-"+str(i),(-.050,.070,z),(.050,.070,z),.012,c)
 for side in (-1,1):
  sphere("shardstone/fracture-prod-"+str(side),(side*.120,.060,.36),.018,b)
  tube("shardstone/bone-prod-brace-"+str(side),(side*.08,.055,.30),(side*.30,.055,.35),.012,c)
 tube("shardstone/rail",(0,.095,-.24),(0,.095,.28),.010,a)

def amber_blade(a,b,c):
 rings("amber/grip-collar",(-.42,-.16),.028,a)
 for side in (-1,1):
  sphere("amber/guard-seal-"+str(side),(side*.050,0,-.045),.016,b)
  tube("amber/bone-langet-"+str(side),(side*.028,0,-.02),(side*.028,0,.28),.009,c)
 for i,z in enumerate((.38,.62)):sphere("amber/blade-seal-"+str(i),(0,0,z),.012,b)

def crown_sceptre(a,b,c):
 rings("crown/rotwood-collar",(.22,.34),.038,a)
 for i in range(6):
  ang=i*math.tau/6;x,y=math.cos(ang)*.110,math.sin(ang)*.075
  tube("crown/bone-cage-"+str(i),(x*.25,y*.25,.36),(x,y,.56),.012,c)
  sphere("crown/amber-"+str(i),(x,y,.56),.018,b)
 torus("crown/halo",.62,.105,.012,b,rotation=(0,0,0))

BUILD={
 "worldroot-club":worldroot_club,"worldroot-bow":worldroot_bow,
 "flowstone-maul":flowstone_maul,"blackwater-harpoon":blackwater_harpoon,
 "emberiron-axe":emberiron_axe,"emberiron-greatsword":emberiron_greatsword,
 "rimesilver-spear":rimesilver_spear,"icebind-staff":icebind_staff,
 "titanbone-atgeir":titanbone_atgeir,"shardstone-crossbow":shardstone_crossbow,
 "amber-blade":amber_blade,"crown-sceptre":crown_sceptre,
}

def author(model_id):
 base_id,kind=SPECS[model_id];base=SOURCE/(base_id+".blend")
 if not base.is_file():raise FileNotFoundError(base)
 bpy.ops.wm.open_mainfile(filepath=str(base))
 base_bounds=bounds();scene=bpy.context.scene
 scene["model_id"]=model_id;scene["derived_from"]=base_id;scene["underworld_weapon_derivation"]="crystal-chassis-plus-biome-accent"
 scene["underworld_weapon_authoring"]=REVISION
 mats=palette(model_id,kind);BUILD[kind](*mats)
 final_bounds=bounds()
 # Preserve the confirmed longitudinal envelope exactly; small transverse growth is permitted only
 # inside a tight absolute/relative tolerance so HeldModelAlignment cannot silently change axis rank.
 for axis in range(3):
  blo,bhi=base_bounds[axis];flo,fhi=final_bounds[axis];span=max(bhi-blo,.001)
  allowance=.006 if axis==SOURCE_LENGTH_AXIS else max(.012,span*.08)
  if flo<blo-allowance or fhi>bhi+allowance:
   raise RuntimeError(f"{model_id}: biome accent expanded source axis {axis} from {blo:.4f}..{bhi:.4f} to {flo:.4f}..{fhi:.4f} (allowance {allowance:.4f}; source length axis={SOURCE_LENGTH_AXIS})")
 scene["runtime_lights"]=scene.get("runtime_lights","[]")
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(model_id+".blend")),compress=True)
 print("AUTHORED",model_id,"from",base_id,kind,flush=True)

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else list(SPECS)
unknown=[x for x in requested if x not in SPECS]
if unknown:raise SystemExit("Unknown Underworld weapon model(s): "+", ".join(unknown))
for model_id in requested:author(model_id)
