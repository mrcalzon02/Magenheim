#!/usr/bin/env python3
"""Author missing raw and all refined Underworld inventory material models."""
import json,math,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from underworld_material_library import bind_underworld_material
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/"assets"/"models"/"source"
SPECS=json.loads((ROOT/"tools"/"underworld-material-item-specs.json").read_text())
REVISION="underworld-material-items-r1"
MIN_PARTS=8

def mat(model,semantic,index):
    name="magenheim.material-item."+model+"."+index+"."+semantic
    m=bpy.data.materials.new(name);m.use_nodes=True;m.use_backface_culling=True
    bs=m.node_tree.nodes.get("Principled BSDF");bs.inputs["Base Color"].default_value=(1,1,1,1)
    bind_underworld_material(bpy,m,semantic)
    m["magenheim_material_name"]=name
    return m

def finish(o,path,m):
    o.name=path;o["game_node_path"]=path;o["game_collision"]=False;o["game_crystal"]=json.dumps(None)
    o.data.materials.clear();o.data.materials.append(m)
    if not o.data.uv_layers:o.data.uv_layers.new(name="MaterialItemUV")
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT");bpy.ops.uv.cube_project(cube_size=.18)
    bpy.ops.object.mode_set(mode="OBJECT");o.select_set(False);return o

def cube(path,loc,scale,m,bevel=.015,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc,rotation=rot);o=bpy.context.object;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new("worked-edge","BEVEL");mod.width=bevel;mod.segments=2
    return finish(o,path,m)

def rock(path,loc,scale,m,rot=(0,0,0)):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1,location=loc);o=bpy.context.object
    o.scale=scale;o.rotation_euler=rot;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,path,m)

def cyl(path,loc,radius,depth,m,rot=(0,0,0),vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc,rotation=rot)
    return finish(bpy.context.object,path,m)

def cone(path,loc,r1,r2,depth,m,rot=(0,0,0),vertices=8):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r1,radius2=r2,depth=depth,location=loc,rotation=rot)
    return finish(bpy.context.object,path,m)

def torus(path,loc,major,minor,m,rot=(0,0,0)):
    bpy.ops.mesh.primitive_torus_add(major_segments=20,minor_segments=8,major_radius=major,minor_radius=minor,location=loc,rotation=rot)
    return finish(bpy.context.object,path,m)

def tube(path,a,b,r,m,vertices=10):
    av,bv=Vector(a),Vector(b);d=bv-av
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=d.length,location=(av+bv)*.5)
    o=bpy.context.object;o.rotation_mode="QUATERNION";o.rotation_quaternion=Vector((0,0,1)).rotation_difference(d.normalized())
    return finish(o,path,m)

def blob(path,loc,r,m,flatten=.7):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=7,radius=r,location=loc);o=bpy.context.object
    o.scale=(1,1,flatten);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return finish(o,path,m)

def build(form,a,b):
    if form=="rubble":
        for i in range(10):
            ang=i*2.39996;r=.055+.014*(i%3)
            rock("rubble-"+str(i),(math.cos(ang)*r,math.sin(ang)*r,.035+.012*(i%2)),(.052,.042,.032),a,(i*.13,i*.21,ang))
        for i in range(3): cone("inclusion-"+str(i),((i-1)*.05,-.025,.08),.018,.004,.07,b,vertices=7)
    elif form=="crystal":
        for i in range(7):
            ang=i*math.tau/7;r=0 if i==0 else .055
            cone("crystal-"+str(i),(math.cos(ang)*r,math.sin(ang)*r,.075),.048 if i==0 else .032,.005,.15+.02*(i%3),a,(.06*math.sin(ang),.06*math.cos(ang),ang),6)
        for i in range(4): rock("matrix-"+str(i),((i-1.5)*.04,.035*(-1 if i%2 else 1),.025),(.042,.034,.024),b)
    elif form=="bundle":
        pts=((-0.055,-.04),(0.055,-.04),(-.055,.04),(.055,.04))
        for i,(x,y) in enumerate(pts):cyl("member-"+str(i),(x,y,.16),.037,.32,a,(0,.06*(i-1.5),0),10)
        for j,z in enumerate((.075,.235)):torus("binding-"+str(j),(0,0,z),.086,.011,b)
        for i,(x,y) in enumerate(pts):cube("end-key-"+str(i),(x,y,.31),(.014,.014,.022),b,.005)
    elif form=="ore":
        for i in range(7):
            ang=i*2.2;r=.055+.012*(i%2);rock("ore-"+str(i),(math.cos(ang)*r,math.sin(ang)*r,.05),(.058,.047,.043),b,(i*.17,i*.11,ang))
        for i in range(5):
            ang=i*math.tau/5;rock("vein-"+str(i),(math.cos(ang)*.07,math.sin(ang)*.07,.075),(.025,.017,.017),a)
    elif form=="bone":
        for i in range(5):
            x=(i-2)*.038;tube("bone-"+str(i),(x,-.06,.035),(x+.02,.07,.16+.012*(i%2)),.017,a,9)
        for i,z in enumerate((.055,.088,.121,.154)):torus("bone-bind-"+str(i),(0,0,z),.075,.009,b)
    elif form=="spores":
        for i in range(6):
            ang=i*math.tau/6;x,y=math.cos(ang)*.065,math.sin(ang)*.065
            cyl("stem-"+str(i),(x,y,.055),.011,.08,b,vertices=9);blob("cap-"+str(i),(x,y,.105),.035,a,.52)
        rock("spore-cake",(0,0,.025),(.10,.08,.025),b)
    elif form=="planks":
        for i in range(4):cube("lamina-"+str(i),(0,(i-1.5)*.035,.035+i*.025),(.16,.045,.024),a,.009,(0,0,.025*(-1 if i%2 else 1)))
        for j,x in enumerate((-.115,.115)):
            cube("cross-key-"+str(j),(x,0,.095),(.017,.11,.017),b,.005)
            for k in (-1,1):cube("peg-"+str(j)+"-"+str(k),(x,k*.075,.118),(.009,.009,.012),b,.003)
    elif form=="cord":
        for i in range(5):torus("coil-"+str(i),(0,0,.028+i*.022),.090-.004*i,.011,a,(0,0,.08*i))
        for i,ang in enumerate((0,math.pi/2,math.pi,math.pi*1.5)):
            tube("tie-"+str(i),(math.cos(ang)*.07,math.sin(ang)*.07,.02),(math.cos(ang)*.07,math.sin(ang)*.07,.15),.008,b,8)
    elif form=="caps":
        for i in range(6):
            x=(i%3-1)*.065;y=-.04 if i<3 else .04
            cyl("dried-stem-"+str(i),(x,y,.045),.011,.07,b,vertices=9);blob("dried-cap-"+str(i),(x,y,.09),.04,a,.38)
        for j,z in enumerate((.035,.075)):torus("bundle-tie-"+str(j),(0,0,z),.115,.009,b)
    elif form=="plates":
        for i in range(3):cube("plate-"+str(i),(0,0,.025+i*.035),(.145,.095,.018),a,.010,(0,0,.025*(i-1)))
        for sx in (-1,1):
            for sy in (-1,1):cyl("rivet-"+str(sx)+"-"+str(sy),(sx*.105,sy*.060,.115),.011,.020,b,vertices=10)
        cube("maker-key",(0,-.096,.08),(.04,.012,.025),b,.005)
    elif form=="pearls":
        for i in range(6):
            ang=i*math.tau/6;rock("pearl-"+str(i),(math.cos(ang)*.06,math.sin(ang)*.06,.055+.008*(i%2)),(.031,.031,.031),a)
        torus("brine-collar",(0,0,.045),.095,.013,b);cube("seal-tab",(0,-.095,.05),(.035,.018,.03),b,.006)
    elif form=="ingots":
        poses=((-0.055,-.025,.035),(0.055,-.025,.035),(0,.035,.07),(0,-.01,.105))
        for i,p in enumerate(poses):cube("bar-"+str(i),p,(.065,.035,.022),a,.009,(0,0,.05*(i-1)))
        for i,x in enumerate((-.04,.04)):cube("stamp-"+str(i),(x,-.052,.107),(.014,.008,.010),b,.003)
        torus("bundle-band-low",(0,0,.055),.105,.009,b)
        torus("bundle-band-high",(0,0,.090),.098,.008,b)
    elif form=="grip":
        cyl("grip-core",(0,0,.15),.045,.30,a,vertices=14)
        for i,z in enumerate((.035,.085,.135,.185,.235,.285)):torus("grip-wrap-"+str(i),(0,0,z),.05,.009,b)
        cube("index-key",(0,-.05,.15),(.02,.012,.055),b,.005)
    elif form=="lens":
        for i,x in enumerate((-.055,0,.055)):
            cyl("lens-"+str(i),(x,0,.06+.012*i),.045,.018,a,(math.pi/2,0,0),18)
            torus("lens-frame-"+str(i),(x,0,.06+.012*i),.047,.007,b,(math.pi/2,0,0))
            tube("lens-prong-"+str(i),(x,-.04,.03),(x,-.04,.11),.007,b,8)
    elif form=="block":
        cube("core",(0,0,.075),(.12,.10,.075),a,.016)
        for i,z in enumerate((.025,.125)):torus("block-band-"+str(i),(0,0,z),.135,.009,b)
        for sx in (-1,1):
            for sy in (-1,1):cube("corner-"+str(sx)+"-"+str(sy),(sx*.105,sy*.085,.145),(.015,.015,.018),b,.004)
        cube("stamp",(0,-.102,.075),(.04,.012,.024),b,.005)
    elif form=="prism":
        for i,x in enumerate((-.055,0,.055)):
            cone("prism-"+str(i),(x,0,.085),.04,.007,.17,a,(0,.08*(i-1),0),6);torus("prism-collar-"+str(i),(x,0,.035),.043,.008,b)
        cube("prism-base",(0,0,.018),(.13,.07,.018),b,.007);cube("prism-key",(0,-.072,.045),(.035,.010,.022),b,.004)
    elif form=="seal":
        cyl("seal-core",(0,0,.055),.09,.025,a,vertices=20);torus("seal-rim",(0,0,.07),.092,.011,b)
        for i in range(6):
            ang=i*math.tau/6;cube("tooth-"+str(i),(math.cos(ang)*.083,math.sin(ang)*.083,.07),(.014,.014,.018),b,.004,(0,0,ang))
        torus("suspension-ring",(0,.105,.07),.025,.007,b,(math.pi/2,0,0));tube("seal-pin",(0,.08,.07),(0,.132,.07),.006,b,8)
    else:raise ValueError(form)

def author(model_id):
    primary,secondary,form=SPECS[model_id];bpy.ops.wm.read_factory_settings(use_empty=True)
    build(form,mat(model_id,primary,"primary"),mat(model_id,secondary,"secondary"))
    meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"]
    if len(meshes)<MIN_PARTS:raise RuntimeError(model_id+": inventory detail regression")
    scene=bpy.context.scene;scene["model_id"]=model_id;scene["magenheim_family"]="underworld_material_item"
    scene["magenheim_material_item_form"]=form;scene["magenheim_fidelity"]="endgame-material-item-r1"
    scene["magenheim_detail_parts"]=len(meshes);scene["runtime_lights"]="[]"
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/(model_id+".blend")),compress=True)
    print("AUTHORED",model_id,form,len(meshes),"parts",flush=True)

requested=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else list(SPECS)
unknown=[x for x in requested if x not in SPECS]
if unknown:raise SystemExit("Unknown material item model(s): "+", ".join(unknown))
for model_id in requested:author(model_id)
