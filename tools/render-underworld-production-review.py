#!/usr/bin/env python3
"""Render neutral + biome-context review images for the one-run production scope."""
import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1]
MODELS=ROOT/"assets"/"models"
requested=set(sys.argv[sys.argv.index('--')+1:]) if '--' in sys.argv else set()
review_root=ROOT/'dist'/('underworld-selected-review' if requested else 'underworld-production-review')
OUT=review_root/"renders"
OUT.mkdir(parents=True,exist_ok=True)
scope=json.loads((ROOT/"tools"/"underworld-production-scope.json").read_text())
catalog=json.loads((MODELS/"catalog.json").read_text())
if requested:
    # Targeted payload gates admit additions before the next whole-library catalog refresh.
    # Reviews may read those real triplets without rewriting that global validation artifact.
    known={entry['id'] for entry in catalog}
    for model_id in sorted(requested-known):
        source=MODELS/'source'/(model_id+'.blend')
        if source.parent != MODELS/'source' or not all((MODELS/folder/(model_id+suffix)).is_file()
                for folder,suffix in (('source','.blend'),('glb','.glb'),('runtime','.model.json'))):
            raise RuntimeError('Selected review has no authored source/export triplet: '+model_id)
        catalog.append({'id':model_id,'source':'source/'+source.name})

def admitted(model_id):
    if requested: return model_id in requested
    return any(model_id.startswith(prefix) for prefix in scope["review_prefixes"])

def group(model_id):
    if model_id.startswith("underworld-resource-"): return "raw-materials"
    if model_id.startswith("underworld-refined-"): return "refined-materials"
    if model_id.startswith("crystal-weapon-"): return "crystal-weapons"
    if model_id.startswith("underworld-weapon-"): return "underworld-weapons"
    if model_id.startswith("rootforged-"): return "rootforged"
    if model_id.startswith("underworld-geothermal-vent-"): return "geothermal-vents"
    if model_id.startswith("underworld-station-"): return "stations"
    if model_id.startswith("underworld-tool-"): return "tools"
    if model_id.startswith("underworld-armor-"): return "armour-"+model_id.split("-")[2]
    if model_id.startswith("staff-") or model_id.startswith("Magenheim_Staff_"): return "staves"
    return "other"

def context(model_id):
    low=model_id.lower()
    if any(x in low for x in ("worldroot","mycelial","sporeweave","sporelight","spire","glowcap")): return ((.055,.095,.075,1),(.30,.78,.48),(.16,.46,.30))
    if any(x in low for x in ("tidal","palewater","diving","blackwater","flowstone","pale","pearl","deep-salt")): return ((.035,.070,.090,1),(.38,.72,.82),(.14,.36,.48))
    if any(x in low for x in ("furnace","ember","slag","sulfur","charred","staff-fire")): return ((.11,.045,.025,1),(.95,.38,.10),(.52,.16,.04))
    if any(x in low for x in ("silence","rime","ice","staff-frost")): return ((.065,.085,.11,1),(.64,.84,.96),(.26,.46,.72))
    if any(x in low for x in ("anchor","stoneanchor","fracture","shardstone","titanbone")): return ((.075,.060,.09,1),(.63,.48,.88),(.30,.18,.48))
    if any(x in low for x in ("crown","defiant","censer","decay","rotwood","carrion","ossuary","bone-gravel","staff-venom")): return ((.09,.075,.035,1),(.86,.58,.16),(.38,.28,.08))
    if "staff-storm" in low: return ((.055,.065,.11,1),(.45,.62,.96),(.28,.22,.62))
    if "staff-earth" in low: return ((.075,.060,.09,1),(.63,.48,.88),(.30,.18,.48))
    if "staff-radiance" in low: return ((.17,.15,.10,1),(.98,.90,.58),(.58,.48,.18))
    if "staff-seidr" in low: return ((.10,.045,.11,1),(.88,.48,.92),(.46,.18,.54))
    if "staff-spirit" in low: return ((.08,.11,.12,1),(.70,.92,.96),(.30,.56,.62))
    return ((.12,.14,.17,1),(.82,.86,.92),(.34,.42,.54))

def render(entry,condition):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc=bpy.context.scene
    sc.render.engine="BLENDER_EEVEE"
    sc.render.resolution_x=sc.render.resolution_y=320
    sc.render.resolution_percentage=100
    sc.render.image_settings.file_format="PNG"
    sc.render.image_settings.color_mode="RGBA"
    sc.render.film_transparent=False
    sc.view_settings.view_transform="AgX"
    sc.world=bpy.data.worlds.new("ProductionReview")
    sc.world.use_nodes=True
    bg=sc.world.node_tree.nodes["Background"]
    if condition=="neutral":
        bg.inputs[0].default_value=(.15,.17,.20,1);key=(.90,.92,.95);fill=(.36,.40,.46)
    else:
        bgc,key,fill=context(entry["id"]);bg.inputs[0].default_value=bgc
    bg.inputs[1].default_value=.48
    with bpy.data.libraries.load(str(MODELS/entry["source"]),link=False) as (src,dst):
        dst.objects=src.objects
    all_objects=[o for o in dst.objects if o]
    for o in all_objects: sc.collection.objects.link(o)
    meshes=[o for o in all_objects if o.type=="MESH"]
    if not meshes: raise RuntimeError(entry["id"]+": no meshes for review")
    bpy.context.view_layer.update()
    rot=Matrix.Rotation(math.radians(-28),4,"Z")@Matrix.Rotation(math.radians(-58),4,"X")@Matrix.Rotation(math.radians(8),4,"Y")
    for o in all_objects:o.matrix_world=rot@o.matrix_world
    bpy.context.view_layer.update()
    pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
    lo=Vector([min(p[i] for p in pts) for i in range(3)])
    hi=Vector([max(p[i] for p in pts) for i in range(3)])
    center=(lo+hi)/2;extent=max((hi-lo).x,(hi-lo).y)
    scale=1.82/max(extent,.001);place=Matrix.Scale(scale,4)@Matrix.Translation(-center)
    for o in all_objects:o.matrix_world=place@o.matrix_world
    cd=bpy.data.cameras.new("Camera");cd.type="ORTHO";cd.ortho_scale=2.2
    cam=bpy.data.objects.new("Camera",cd);sc.collection.objects.link(cam);cam.location=(0,0,12);sc.camera=cam
    for name,pos,power,size,color in (
      ("Key",(-2.4,-1.6,6.2),1050,5.5,key),("Fill",(2.8,1.8,5.0),520,6.0,fill)):
        ld=bpy.data.lights.new(name,"AREA");ld.energy=power;ld.shape="DISK";ld.size=size;ld.color=color
        light=bpy.data.objects.new(name,ld);sc.collection.objects.link(light);light.location=pos
    target=OUT/f"{entry['id']}--{condition}.png"
    sc.render.filepath=str(target)
    bpy.ops.render.render(write_still=True)
    print("RENDERED",entry["id"],condition,flush=True)

selected=sorted((e for e in catalog if admitted(e["id"])),key=lambda e:e["id"])
if requested:
    if {entry['id'] for entry in selected} != requested: raise RuntimeError('Selected review contains unknown models')
elif len(selected)!=150: raise RuntimeError(f"Production review scope must contain exactly 150 admitted models, found {len(selected)}")
index=[]
for entry in selected:
    for condition in ("neutral","context"): render(entry,condition)
    index.append({"id":entry["id"],"group":group(entry["id"])})
(review_root/"index.json").write_text(json.dumps(index,indent=2)+"\n")
print("RENDERED production review",len(selected),"models x 2 lighting conditions",flush=True)
