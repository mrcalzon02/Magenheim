"""Rest-pose review: leave rigged meshes in place and move only the camera.

This is an offline material preview, not live animation or shader acceptance.
"""
import bpy, sys, math, json
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[1]
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['before']
stage=args[0]
names=args[1:] or ['crowncap-brute','deep-hunter','furnace-golem','cryolith-guardian','rift-colossus','corpse-orchard']
out=ROOT/'dist/apex-creature-review'/stage;out.mkdir(parents=True,exist_ok=True)
metrics=[]
for name in names:
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/source'/('underworld-creature-'+name+'.blend')))
    scene=bpy.context.scene
    for arm in [o for o in scene.objects if o.type=='ARMATURE']:
        arm.data.pose_position='REST'
        if arm.animation_data:arm.animation_data.action=None
        for bone in arm.pose.bones:bone.matrix_basis=Matrix.Identity(4)
    bpy.context.view_layer.update()
    objects=[o for o in scene.objects if o.type=='MESH']
    points=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
    low=Vector([min(p[i] for p in points) for i in range(3)])
    high=Vector([max(p[i] for p in points) for i in range(3)])
    center=(low+high)/2;size=max(high-low)
    scene.render.engine='CYCLES';scene.cycles.samples=24
    scene.render.resolution_x=scene.render.resolution_y=768;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG';scene.view_settings.view_transform='AgX'
    scene.world=bpy.data.worlds.new('Apex review');scene.world.use_nodes=True
    scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.16,.19,.23,1)
    scene.world.node_tree.nodes['Background'].inputs[1].default_value=.65
    for label,direction,energy in [('Key',(-1,1.5,2),850),('Fill',(1,-1,1),550)]:
        data=bpy.data.lights.new(label,'AREA');data.energy=energy*size*size;data.size=size*1.4
        obj=bpy.data.objects.new(label,data);scene.collection.objects.link(obj);obj.location=center+Vector(direction)*size
        obj.rotation_euler=(center-obj.location).to_track_quat('-Z','Y').to_euler()
    data=bpy.data.cameras.new('Review camera');data.type='ORTHO';data.ortho_scale=size*1.35
    camera=bpy.data.objects.new('Review camera',data);scene.collection.objects.link(camera);scene.camera=camera
    for view,direction in [('three-quarter',(1,2,.65)),('front',(0,2,.15))]:
        camera.location=center+Vector(direction)*size*2
        camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=str(out/(name+'--'+view+'.png'));bpy.ops.render.render(write_still=True)
    for o in objects:o.data.calc_loop_triangles()
    metrics.append({'name':name,'dimensions':list(high-low),'triangles':sum(len(o.data.loop_triangles) for o in objects),'parts':len(objects)})
    print('RENDERED '+name,flush=True)
(out/'metrics.json').write_text(json.dumps(metrics,indent=2))
