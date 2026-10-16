"""Actual saved historical/native sources using recorded study cameras.

Neither the historical file nor its textures is written. Native donors use the
same directions/light recipe and normalized framing, clearly labeled as such.
"""
import bpy,json,sys,argparse,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--study',required=True);p.add_argument('--source',required=True);p.add_argument('--label',required=True);p.add_argument('--views',required=True);p.add_argument('--native',action='store_true');p.add_argument('--ground',action='store_true')
args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
study=ROOT/args.study;source=ROOT/args.source;out=study/'before';out.mkdir(exist_ok=True)
original_hash=hashlib.sha256(source.read_bytes()).hexdigest();layout=json.loads((study/'camera-layout.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(source));scene=bpy.context.scene
for arm in [o for o in scene.objects if o.type=='ARMATURE']:
    arm.animation_data.action=None;arm.data.pose_position='REST'
    for b in arm.pose.bones:b.matrix_basis=Matrix.Identity(4)
for o in list(scene.objects):
    if o.type in ('CAMERA','LIGHT'):bpy.data.objects.remove(o,do_unlink=True)
meshes=[o for o in scene.objects if o.type=='MESH'];originals={o.name:list(o.data.materials) for o in meshes}
points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
if args.native:center=(hi+lo)/2;size=max(hi-lo)
else:
    size=layout['front']['ortho_scale']/1.18;center=Vector(layout['front']['location'])-Vector((0,2,.06))*size*2
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.image_settings.file_format='PNG';scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
scene.world=bpy.data.worlds.new('Matched history world');scene.world.use_nodes=True
bg=scene.world.node_tree.nodes['Background'];bg.inputs[0].default_value=(.055,.064,.073,1);bg.inputs[1].default_value=.25
for label,direction,power in [('Key',(-1,1.4,2),120),('Fill',(1,1,.6),45),('Rim',(0,-1.8,1.3),140)]:
    d=bpy.data.lights.new(label,'AREA');d.energy=power*size*size;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(label,d);scene.collection.objects.link(o);o.location=center+Vector(direction)*size;o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
if args.ground:
    bpy.ops.mesh.primitive_plane_add(size=size*4,location=(center.x,center.y,lo.z-.002))
    gm=bpy.data.materials.new('Ground charcoal');gm.use_nodes=True;gm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.025,.03,.035,1);gm.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.9;bpy.context.object.data.materials.append(gm)
cd=bpy.data.cameras.new('History camera');cd.type='ORTHO';cam=bpy.data.objects.new('History camera',cd);scene.collection.objects.link(cam);scene.camera=cam
clay=bpy.data.materials.new('Neutral anatomy clay');clay.use_nodes=True;bs=clay.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=(.30,.32,.34,1);bs.inputs['Roughness'].default_value=.83
for view in args.views.split(','):
    cfg=layout[view];scene.render.resolution_x,scene.render.resolution_y=cfg['resolution']
    if args.native:
        direction=(Vector(cfg['location'])-center).normalized()
        # Use the actual recorded rotation; derive its forward direction so
        # different source origins cannot reverse the requested viewpoint.
        cam.rotation_euler=cfg['rotation'];direction=cam.rotation_euler.to_matrix()@Vector((0,0,1));cam.location=center+direction*size*4;cd.ortho_scale=size*1.18
    else:cam.location=cfg['location'];cam.rotation_euler=cfg['rotation'];cd.ortho_scale=cfg['ortho_scale']
    for o in meshes:
        o.data.materials.clear()
        for mat in originals[o.name]:o.data.materials.append(clay if 'clay' in view else mat)
    if view in ('side','side-clay','crown-side','crown-profile','crown-profile-clay'):assert cam.rotation_euler.to_matrix().col[0].dot(Vector((0,1,0)))<0
    scene.render.filepath=str(out/(args.label+'--'+view+'.png'));bpy.ops.render.render(write_still=True)
    print('RENDERED saved '+args.label+' '+view,flush=True)
assert hashlib.sha256(source.read_bytes()).hexdigest()==original_hash
current=json.loads((study/'refinement-metrics.json').read_text())
(out/(args.label+'-provenance.json')).write_text(json.dumps(dict(source=args.source,source_sha256=original_hash,source_unchanged=True,current_source_sha256=hashlib.sha256((ROOT/current['source']).read_bytes()).hexdigest(),camera_configuration={view:layout[view] for view in args.views.split(',')},cameras='recorded current study directions and normalized native framing' if args.native else 'exact recorded current study transforms, ortho scale and resolution',lighting='same normalized three-area-light recipe and AgX',native=args.native,views=args.views.split(',')),indent=2)+'\n')
print('VERIFIED historical source unchanged and matched render provenance')
