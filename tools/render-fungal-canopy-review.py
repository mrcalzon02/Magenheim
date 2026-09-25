"""Fungal canopy source review: front, side, three-quarter and silhouette, four models/page.

Run tools/blender.ps1 render-fungal-canopy-review. Reads the saved source models.
Each model uses the same scale across its four views; rows normalize independently.
"""
import bpy
import math
import sys
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'artifacts/review/fungal'
OUT.mkdir(parents=True, exist_ok=True)
ids = ['underworld-flora-fungal-glowcap', 'underworld-flora-fungal-spirestalk',
       'underworld-flora-fungal-puffcap', 'underworld-flora-fungal-tanglecap']
pages = [0]


def emission(name, color):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    output = nodes.new('ShaderNodeOutputMaterial')
    shader = nodes.new('ShaderNodeEmission')
    shader.inputs['Color'].default_value = (*color, 1)
    mat.node_tree.links.new(shader.outputs[0], output.inputs['Surface'])
    return mat


def label(text, x, y, size, mat):
    data = bpy.data.curves.new('Label', 'FONT')
    data.body, data.size, data.align_x = text, size, 'CENTER'
    obj = bpy.data.objects.new('Label', data)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = (x, y, 3)
    obj.data.materials.append(mat)


for page in pages:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 24
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 2000
    scene.render.resolution_y = 2000
    scene.render.resolution_percentage = 100
    scene.world = bpy.data.worlds.new('Neutral studio')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.16, .19, .22, 1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value = .65
    ink = emission('Labels', (.8, .87, .91))
    silhouette = emission('Silhouette', (.005, .008, .012))
    rotations = [Matrix.Rotation(math.radians(-90),4,'X'),
                 Matrix.Rotation(math.radians(-90),4,'X') @ Matrix.Rotation(math.radians(90),4,'Z'),
                 Matrix.Rotation(math.radians(-65),4,'X') @ Matrix.Rotation(math.radians(-35),4,'Z')]
    rotations.append(rotations[2])
    for col, title in enumerate(['FRONT', 'SIDE', 'THREE-QUARTER', 'SILHOUETTE']):
        label(title, (col-1.5)*3.1, 6.05, .14, ink)
    for row, model_id in enumerate(ids[page*4:page*4+4]):
        with bpy.data.libraries.load(str((ROOT / 'assets/models/source') / (model_id+'.blend')), link=False) as (src,dst):
            dst.objects = src.objects
        objects = [o for o in dst.objects if o and o.type == 'MESH']
        for obj in objects:
            scene.collection.objects.link(obj)
        bpy.context.view_layer.update()
        originals = {o:o.matrix_world.copy() for o in objects}
        points = [o.matrix_world @ v.co for o in objects for v in o.data.vertices]
        height = max(p.z for p in points)-min(p.z for p in points)
        tris = 0
        for obj in objects:
            obj.data.calc_loop_triangles()
            tris += len(obj.data.loop_triangles)
        bounds=[]
        for rot in rotations:
            pts=[rot @ p for p in points]
            lo=Vector([min(p[i] for p in pts) for i in range(3)])
            hi=Vector([max(p[i] for p in pts) for i in range(3)])
            bounds.append((lo,hi))
        scale=2.45/max(max((hi-lo).x,(hi-lo).y) for lo,hi in bounds)
        for col,(rot,(lo,hi)) in enumerate(zip(rotations,bounds)):
            x,y=(col-1.5)*3.1,4.5-row*3.0
            place=Matrix.Translation(Vector((x,y,0))) @ Matrix.Scale(scale,4) @ Matrix.Translation(-(lo+hi)/2)
            for original in objects:
                obj=original.copy()
                if col==3:
                    obj.data=original.data.copy()
                    obj.data.materials.clear()
                    obj.data.materials.append(silhouette)
                scene.collection.objects.link(obj)
                obj.matrix_world=place @ rot @ originals[original]
        for obj in objects:
            bpy.data.objects.remove(obj,do_unlink=True)
        short=model_id.replace('underworld-flora-blackwater-','').replace('underworld-resource-','')
        label(f'{short}   |   {height:.2f} m tall   |   {tris:,} triangles',0,3.1-row*3,.15,ink)
        print(f'VERIFIED {model_id} height={height:.3f} triangles={tris}',flush=True)
    camera_data=bpy.data.cameras.new('Camera')
    camera=bpy.data.objects.new('Camera',camera_data)
    scene.collection.objects.link(camera)
    camera.location=(0,0,30)
    camera_data.type='ORTHO'
    camera_data.ortho_scale=13
    scene.camera=camera
    for name,pos,power,size in [('Key',(-5,1,10),1700,9),('Fill',(6,4,8),1000,10)]:
        data=bpy.data.lights.new(name,'AREA')
        obj=bpy.data.objects.new(name,data)
        scene.collection.objects.link(obj)
        obj.location=pos
        data.energy,data.size=power,size
    scene.view_settings.view_transform='AgX'
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(OUT/f'fungal-canopy-review-{page+1:02}.png')
    bpy.ops.render.render(write_still=True)
    print(f'RENDERED {scene.render.filepath}',flush=True)
