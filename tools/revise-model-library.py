"""One-time, versioned art revision of saved sources, not a legacy regeneration.

Run with Blender --background --python tools/revise-model-library.py.
Existing UVs, object identities, pivots and gameplay metadata are retained.
The revision marker makes re-running safe. Export separately after inspection.
"""
import bpy, bmesh, json, math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'assets/models'
REVISION = 'surface-and-silhouette-1'
report = []

def replace_geometry(obj, vertices, faces):
    """Keep the object identity and material while replacing an inadequate shape."""
    inverse = obj.matrix_world.inverted()
    mesh = bpy.data.meshes.new(obj.name + '-revised')
    mesh.from_pydata([inverse @ Vector(v) for v in vertices], [], faces)
    for mat in obj.data.materials:
        mesh.materials.append(mat)
    obj.data = mesh
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.uv.smart_project(island_margin=.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)

def ribbon(obj, points, width, depth):
    # A continuous tapered four-sided section through a designed centerline.
    vertices=[]
    for i,(x,z) in enumerate(points):
        w=width*(1-.35*i/max(1,len(points)-1))
        vertices.extend([(x-w,-depth,z),(x+w,-depth,z),(x+w,depth,z),(x-w,depth,z)])
    faces=[(3,2,1,0)]
    for i in range(len(points)-1):
        for j in range(4):
            faces.append((4*i+j,4*i+(j+1)%4,4*(i+1)+(j+1)%4,4*(i+1)+j))
    faces.append(tuple(4*(len(points)-1)+j for j in range(4)))
    replace_geometry(obj,vertices,faces)

def add_facet(name, center, size, mat):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=center)
    obj=bpy.context.object
    obj.name=name
    for v in obj.data.vertices:
        v.co=Vector((v.co.x*size[0],v.co.y*size[1],v.co.z*size[2]))
    obj.data.materials.append(mat)
    obj['game_node_path']=name
    obj['game_collision']=False
    obj['game_crystal']='null'
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(island_margin=.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)
    return obj

for file in sorted((ASSETS / 'source').glob('*.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(file))
    scene = bpy.context.scene
    if scene.get('art_revision') == REVISION:
        continue
    creature = file.stem.startswith('deep-fracture-') and ('creature-' in file.stem or '-visual-' in file.stem)
    edits = []
    materials = set()
    if file.stem == 'crystal-weapon-bow':
        # Both tips curve toward the string. Adjacent sections share endpoints.
        for sign,prefix in [(1,'upper'),(-1,'lower')]:
            points=[(0,sign*.23),(-.12,sign*.53),(-.27,sign*.84),(-.22,sign*1.17)]
            for i,segment in enumerate(('inner','mid','outer')):
                ribbon(scene.objects[prefix+'-'+segment],points[i:i+2],.062,.038)
            ribbon(scene.objects[prefix+'-cap'],[points[-1],(-.20,sign*1.23)],.041,.045)
            ribbon(scene.objects['string-'+prefix],[(-.20,sign*1.23),(-.20,0)],.006,.006)
        edits.append(['bow', 'continuous recurve limbs and aligned string'])
    if file.stem in ('crystal-weapon-axe','crystal-weapon-battleaxe'):
        pairs=[('axe-head','edge',1)] if file.stem.endswith('-axe') else [('left-head','left-edge',-1),('right-head','right-edge',1)]
        for head,edge,sign in pairs:
            z=.59 if head=='axe-head' else .75
            profile=[(.02,z-.12),(.20,z-.14),(.48,z-.32),(.56,z-.19),(.58,z+.13),(.46,z+.25),(.19,z+.13),(.02,z+.11)]
            vertices=[(sign*x,y,h) for y in (-.055,.055) for x,h in profile]
            n=len(profile)
            faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
            replace_geometry(scene.objects[head],vertices,faces)
            ribbon(scene.objects[edge],[(sign*.50,z-.29),(sign*.565,z-.17),(sign*.585,z+.12),(sign*.46,z+.25)],.018,.058)
        edits.append(['axe', 'bearded cutting profile replaces block head and sideways spike'])
    for obj in list(scene.objects):
        if obj.type != 'MESH':
            continue
        mesh = obj.data
        # Weld the per-face duplicated primitive vertices before chamfering;
        # bmesh retains loop UVs, including discontinuous UV islands.
        bm = bmesh.new()
        bm.from_mesh(mesh)
        before = len(bm.verts)
        bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=0.000001)
        bm.to_mesh(mesh)
        bm.free()
        mesh.update()
        if len(mesh.vertices) < before:
            edits.append([obj.name, 'welded', before-len(mesh.vertices)])
        materials.update(mesh.materials)
        # Preserve crystal hit boxes and all walkable/snap-bearing surfaces.
        bound = bool(obj.get('game_collision', False)) or obj.get('game_crystal', 'null') != 'null'
        dims = obj.dimensions
        width = min(min(dims) * .045, .025 if creature else .012)
        if not bound and width > .0001:
            bevel = obj.modifiers.new('Crafted edge chamfer', 'BEVEL')
            bevel.width = width
            bevel.segments = 2
            bevel.limit_method = 'ANGLE'
            bevel.angle_limit = math.radians(26)
            bevel.use_clamp_overlap = True
            edits.append([obj.name, 'chamfer', round(width, 5)])
        # Readable anatomy: slimmer bodies expose legs and bright weak points.
        # Do not move or resize registered destructible crystals.
        if creature and not bound:
            name = obj.name.lower()
            if any(x in name for x in ('torso', 'chest', 'rib-body', 'abdomen', 'thorax')):
                for v in mesh.vertices:
                    v.co.x *= .86
                    v.co.y *= .91
                edits.append([obj.name, 'body profile refined'])
            if 'CrystalHound' in file.stem and name == 'skull':
                for v in mesh.vertices:
                    v.co.x *= .8
                edits.append([obj.name, 'narrow canine skull'])
    if creature:
        candidates=[o for o in scene.objects if o.type=='MESH' and any(s==o.name for s in ('skull','helmet','head','head-block','crown','crystal-crown','sensor','drill-head','thorax'))]
        if candidates:
            head=candidates[0]
            points=[head.matrix_world@Vector(p) for p in head.bound_box]
            lo=Vector([min(p[i] for p in points) for i in range(3)])
            hi=Vector([max(p[i] for p in points) for i in range(3)])
            size=hi-lo
            eye_material=bpy.data.materials.new('inset-elemental-eyes')
            eye_material.use_nodes=True
            bs=eye_material.node_tree.nodes.get('Principled BSDF')
            bs.inputs['Base Color'].default_value=(.5,.85,.9,1)
            bs.inputs['Emission Color'].default_value=(.25,.65,.8,1)
            bs.inputs['Emission Strength'].default_value=.45
            for side in (-1,1):
                center=((lo.x+hi.x)/2+side*size.x*.22,lo.y+size.y*.08,lo.z+size.z*.55)
                add_facet('inset-eye-'+str(side),center,(size.x*.105,size.y*.13,size.z*.045),eye_material)
            edits.append(['face', 'paired inset mineral eyes'])
    for mat in materials:
        bs = mat.node_tree.nodes.get('Principled BSDF') if mat.use_nodes else None
        if not bs:
            continue
        color = list(bs.inputs['Base Color'].default_value)
        emission = bs.inputs['Emission Color']
        strength = bs.inputs['Emission Strength']
        energy = max(emission.default_value[:3]) * strength.default_value
        if creature:
            if energy < .5:
                # Dark mineral body; use colored crystals to carry alignment.
                bs.inputs['Base Color'].default_value = tuple(.055 + c*.23 for c in color[:3]) + (color[3],)
                strength.default_value = 0
                bs.inputs['Roughness'].default_value = .78
                bs.inputs['Metallic'].default_value = .12
            else:
                strength.default_value *= .28
                bs.inputs['Roughness'].default_value = .24
                bs.inputs['Metallic'].default_value = .18
        else:
            name = mat.name.lower()
            if any(s in name for s in ('wood', 'grip', 'cloth')):
                bs.inputs['Roughness'].default_value = .78
                bs.inputs['Metallic'].default_value = 0
            elif any(s in name for s in ('iron', 'metal', 'bronze', 'crown')):
                bs.inputs['Metallic'].default_value = .72
                bs.inputs['Roughness'].default_value = .34
            elif 'stone' in name:
                bs.inputs['Roughness'].default_value = .86
            elif 'crystal' in name or 'rainbow' in name:
                bs.inputs['Roughness'].default_value = .22
                strength.default_value *= .65
        edits.append([mat.name, 'material response revised'])
    scene['art_revision'] = REVISION
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(file), compress=True)
    report.append({'id': file.stem, 'changes': edits})
    print('REVISED', file.stem, len(edits), flush=True)

if report:
    (ASSETS/'revision-log.json').write_text(json.dumps(report, indent=2))
