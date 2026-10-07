"""Individualized First Bloom surface rebuild, not whole-model decimation.

Keep silhouette and articulation; fold gills into cap surfaces, describe bark
with fluted cross-sections, and bake source UV materials into one compact atlas.
"""
import bpy, bmesh, math, json, sys, hashlib
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
import importlib.util
spec=importlib.util.spec_from_file_location('boss_base',ROOT/'tools/author-boss-concept-models.py')
B=importlib.util.module_from_spec(spec);spec.loader.exec_module(B)
B.TEX=ROOT/'assets/textures/underworld/bosses/concept-detail-r2'
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-refinement';OUT.mkdir(parents=True,exist_ok=True)
TEX=ROOT/'assets/textures/underworld/bosses/first-bloom-refinement';TEX.mkdir(parents=True,exist_ok=True)
ID='underworld-boss-first-bloom-refinement'
CAP=int(6206*1.5)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/source/underworld-creature-crowncap-brute.blend'))
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
arm.animation_data.action=None;arm.data.pose_position='REST'
for b in arm.pose.bones:b.matrix_basis=Matrix.Identity(4)
for o in list(bpy.context.scene.objects):
 if o!=arm:bpy.data.objects.remove(o,do_unlink=True)
B.arm=arm;B.parts={};B.mats={}
categories={}

def record(o,bone,category):
 categories[o.name]=category
 vg=o.vertex_groups.new(name=bone);vg.add(list(range(len(o.data.vertices))),1,'REPLACE')
 return o

def profile(name,rings,key,bone,sides=12,category='bark'):
 o=B.prof(name,rings,key,bone,sides,irregular=.045)
 # Ribs follow the body instead of floating over it as separate tubes.
 if category=='bark':
  for v in o.data.vertices:
   j,k=divmod(v.index,sides);_,rx,ry,cx,cy=rings[j]
   a=math.tau*k/sides;factor=1+.08*math.cos(a*5+j*.3)
   v.co.x=cx+(v.co.x-cx)*factor;v.co.y=cy+(v.co.y-cy)*factor
 return record(o,bone,category)

def tube(name,points,radii,key,bone,sides=6,category='digits'):
 return record(B.tube(name,points,radii,key,bone,sides),bone,category)

def cap(name,loc,rx,ry,bone,n,tilt):
 # Shared rim: upper mantle and corrugated underside form ONE closed surface.
 rings=[(.055,.16),(.015,.58),(-.055,.94),(-.065,1.0),(-.020,1.015),(.050,.94),(.145,.58),(.205,.045)]
 verts=[];faces=[]
 for j,(z,r) in enumerate(rings):
  for k in range(n):
   a=math.tau*k/n;wave=.055*math.sin(a*5+.6)+.027*math.sin(a*9-1.2)
   fold=(.027 if j<3 else .004)*((-1)**k)*math.sin(r*math.pi)
   verts.append((rx*math.cos(a)*r*(1+wave*r*r),ry*math.sin(a)*r*(1+wave*r*r),z+fold+.017*math.sin(a*7)*r))
 for j in range(len(rings)-1):
  for k in range(n):
   a=j*n+k;b=j*n+(k+1)%n;faces.append((a,b,b+n,a+n))
 faces.extend([tuple(reversed(range(n))),tuple(7*n+k for k in range(n))])
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
 me.materials.append(B.material('ochre-shelf'));me.materials.append(B.material('living-gill'))
 o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
 o.matrix_world=Matrix.Translation(Vector(loc))@Matrix.Rotation(tilt,4,'Y')
 uv=me.uv_layers.new(name='BossConstructionUV')
 for p in me.polygons:
  p.use_smooth=len(p.vertices)==4
  p.material_index=1 if p.index<2*n else 0
  seam=0 in [v%n for v in p.vertices] and n-1 in [v%n for v in p.vertices]
  for li in p.loop_indices:
   j,k=divmod(me.loops[li].vertex_index,n);uv.data[li].uv=(1 if seam and k==0 else k/n,j/7)
 return record(o,bone,'caps')

# One uninterrupted hip-to-shoulder trunk replaces the separate buttresses.
trunk=profile('ContinuousFlutedTrunk',[(.73,.22,.21,0,-.025),(.93,.34,.25,0,-.04),(1.18,.31,.25,0,-.06),(1.45,.28,.24,0,-.07),(1.70,.38,.29,.02,-.08),(1.95,.49,.32,0,-.11),(2.20,.65,.29,-.035,-.13),(2.44,.42,.25,0,-.09)],'root-bark','Chest',20)
pelvis=trunk.vertex_groups.new(name='Pelvis')
for v in trunk.data.vertices:
 w=max(0,min(1,(1.65-v.co.z)/.55))
 if w>0:pelvis.add([v.index],w,'REPLACE');trunk.vertex_groups['Chest'].add([v.index],1-w,'REPLACE')

for side,s in [('L',-1),('R',1)]:
 for upper,lower in [('UpperArm','ForeArm'),('Thigh','Shin')]:
  bone=upper+'_'+side;lowerbone=lower+'_'+side;b=arm.data.bones[bone];c=arm.data.bones[lowerbone]
  a=b.head_local;joint=c.head_local;end=c.tail_local
  pts=[a,a.lerp(joint,.30),a.lerp(joint,.70),joint,joint.lerp(end,.30),joint.lerp(end,.70),end]
  radii=[.13,.205,.18,.14,.195,.165,.12] if upper=='UpperArm' else [.15,.23,.19,.145,.195,.16,.12]
  o=tube('Continuous'+upper+side,pts,radii,'root-bark',bone,10,'bark')
  for v in o.data.vertices:
   j,k=divmod(v.index,10);center=pts[j];v.co=center+(v.co-center)*(1+.095*math.cos(k*math.tau/10*4+j*.24))
  # Shared elbow/knee ring; weight transition replaces two disconnected end caps.
  vg=o.vertex_groups.new(name=lowerbone)
  for v in o.data.vertices:
   w=(0,0,.15,.5,.85,1,1)[v.index//10]
   if w>0:vg.add([v.index],w,'REPLACE');o.vertex_groups[bone].add([v.index],1-w,'REPLACE')
  parent=b.parent
  if parent:
   vg=o.vertex_groups.new(name=parent.name)
   for v in list(o.data.vertices)[:10]:vg.add([v.index],.25,'REPLACE');o.vertex_groups[bone].add([v.index],.75,'REPLACE')
 # Fused palm mass seats the fingers into the wrist instead of a loose bundle.
 hand=arm.data.bones['Hand_'+side]
 tube('RootPalm'+side,[hand.head_local,hand.head_local.lerp(hand.tail_local,.45),hand.tail_local],[.14,.23,.17],'root-bark','Hand_'+side,12,'bark')
 # Branch digits remain geometry because their negative spaces define the silhouette.
 for k in range(4):
  x=s*.93+(k-1.5)*.105
  tube('RootFinger'+side+str(k),[(x,.20,.78),(x+s*.06,.34,.54),(x+s*.09,.43,.29),(x+s*.04,.49,.24)],[.074,.063,.035,.008],'root-bark','Hand_'+side,6)
 a=arm.data.bones['Foot_'+side].head_local
 for k in range(4):tube('RootToe'+side+str(k),[a,(a.x+(k-1.5)*.11,.36,.12),(a.x+(k-1.5)*.22,.69,.05)],[.14,.12,.014],'root-bark','Foot_'+side,6)
 for k in range(3):cap('ArmBracket'+side+str(k),(s*(.74+k*.10),-.015,2.24-k*.36),.31,.30,'UpperArm_'+side if k==0 else 'ForeArm_'+side,16,s*.25)
 # Closed folded panel replaces sixteen independent tubular chest gills per side.
 n=12;verts=[]
 for row in range(3):
  for k in range(n):
   t=k/(n-1);width=.06+.20*math.sin(t*math.pi);x=s*(.045+row*width)
   y=.435-row*.048+(.015 if k%2 else -.015)
   verts.append((x,y,1.61+t*.69+row*.10*math.sin(t*math.pi)))
 faces=[]
 for row in range(2):
  for k in range(n-1):a=row*n+k;faces.append((a,a+1,a+n+1,a+n))
 me=bpy.data.meshes.new('FoldedChest');me.from_pydata(verts,[],faces);me.update();me.materials.append(B.material('living-gill'))
 o=bpy.data.objects.new('FoldedChest'+side,me);bpy.context.collection.objects.link(o)
 bpy.context.view_layer.objects.active=o;o.select_set(True)
 solid=o.modifiers.new('TissueThickness','SOLIDIFY');solid.thickness=.015;bpy.ops.object.modifier_apply(modifier=solid.name)
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False)
 o.data.uv_layers.active.name='BossConstructionUV';record(o,'Chest','gills')

for k,(x,y,z,rx,ry,tilt) in enumerate([(-.46,-.04,2.73,.93,.71,-.15),(.50,-.14,2.85,.80,.63,.23),(-.20,-.20,3.02,.68,.56,-.11),(.12,-.20,3.19,.46,.41,.16),(-.10,-.18,3.34,.30,.29,-.20)]):cap('Mantle'+str(k),(x,y,z),rx,ry,'Head',40,tilt)
for k in range(4):
 a=k*2.39996;cap('BackBracket'+str(k),(math.cos(a)*(.24+k*.07),math.sin(a)*.20-.20,1.40+k*.24),.24,.19,'Chest',12,math.sin(a)*.4)
profile('RecessedRootMask',[(2.39,.11,.075,0,.22),(2.49,.22,.14,0,.22),(2.62,.27,.17,0,.20),(2.75,.21,.13,.02,.17),(2.81,.045,.06,.03,.12)],'root-bark','Head',16,'face')
for s in (-1,1):
 tube('Brow'+str(s),[(s*.025,.36,2.69),(s*.14,.385,2.71),(s*.235,.30,2.69)],[.037,.065,.012],'root-bark','Head',8,'face')
 o=B.mass('RecessedEye'+str(s),(s*.105,.376,2.645),(.025,.014,.025),'living-gill','Head',8);record(o,'Head','face')
tube('ArticulatedJaw',[(-.15,.34,2.49),(0,.39,2.43),(.15,.34,2.49)],[.054,.079,.034],'root-bark','Jaw',8,'face')
o=B.mass('MouthCrease',(0,.371,2.51),(.118,.025,.048),'oral-tissue','Jaw',8);record(o,'Jaw','face')

meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];counts={}
for o in meshes:
 o.data.calc_loop_triangles();cat=categories[o.name];counts[cat]=counts.get(cat,0)+len(o.data.loop_triangles)
triangles=sum(counts.values());assert triangles<=CAP,(triangles,CAP,counts)
# Bake source-UV material detail to a single atlas, not donor textures.
for mat in B.mats.values():
 for node in list(mat.node_tree.nodes):
  if node.type=='TEX_COORD':
   uvnode=mat.node_tree.nodes.new('ShaderNodeUVMap');uvnode.uv_map='BossConstructionUV'
   for link in list(node.outputs['UV'].links):mat.node_tree.links.new(uvnode.outputs['UV'],link.to_socket)
# Add fine radial gill normals to the underside material for texture-scale detail.
gill=B.mats['living-gill'];nodes=gill.node_tree.nodes;links=gill.node_tree.links
uvnode=nodes.new('ShaderNodeUVMap');uvnode.uv_map='BossConstructionUV'
sep=nodes.new('ShaderNodeSeparateXYZ');links.new(uvnode.outputs['UV'],sep.inputs[0])
mul=nodes.new('ShaderNodeMath');mul.operation='MULTIPLY';mul.inputs[1].default_value=math.tau*48;links.new(sep.outputs['X'],mul.inputs[0])
wave=nodes.new('ShaderNodeMath');wave.operation='SINE';links.new(mul.outputs[0],wave.inputs[0])
bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.22;bump.inputs['Distance'].default_value=.018;links.new(wave.outputs[0],bump.inputs['Height'])
bs=gill.node_tree.nodes['Principled BSDF']
if bs.inputs['Normal'].links:links.new(bs.inputs['Normal'].links[0].from_socket,bump.inputs['Normal'])
links.new(bump.outputs['Normal'],bs.inputs['Normal'])
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=trunk;bpy.ops.object.join();o=bpy.context.object;o.name='FirstBloom_CoherentSurface'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
uv=o.data.uv_layers.new(name='FirstBloomAtlas');o.data.uv_layers.active=uv;uv.active_render=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(angle_limit=math.radians(70),island_margin=.012);bpy.ops.object.mode_set(mode='OBJECT')
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8
scene.render.bake.margin=4;scene.render.bake.use_selected_to_active=False
atlas={};original_links={}
for mat in o.data.materials:
 bs=mat.node_tree.nodes['Principled BSDF'];original_links[mat.name]={s:list(bs.inputs[s].links)[0].from_socket if bs.inputs[s].links else None for s in ('Roughness','Emission Color')}
for kind in ('albedo','normal','roughness','emission'):
 im=bpy.data.images.new('first-bloom-'+kind,width=512,height=512,alpha=False);im.colorspace_settings.name='sRGB' if kind in ('albedo','emission') else 'Non-Color'
 for mat in o.data.materials:
  node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=im;mat.node_tree.nodes.active=node;node.select=True
  if kind in ('roughness','emission'):
   bs=mat.node_tree.nodes['Principled BSDF'];output=mat.node_tree.nodes.get('Material Output')
   emit=mat.node_tree.nodes.new('ShaderNodeEmission');socket='Roughness' if kind=='roughness' else 'Emission Color'
   source=original_links[mat.name][socket]
   if source:mat.node_tree.links.new(source,emit.inputs['Color'])
   else:emit.inputs['Color'].default_value=(0,0,0,1) if kind=='emission' else (.8,.8,.8,1)
   mat.node_tree.links.new(emit.outputs[0],output.inputs['Surface'])
 if kind=='albedo':scene.render.bake.use_pass_direct=False;scene.render.bake.use_pass_indirect=False;scene.render.bake.use_pass_color=True
 bpy.ops.object.bake(type={'albedo':'DIFFUSE','normal':'NORMAL','roughness':'EMIT','emission':'EMIT'}[kind])
 path=TEX/(ID+'-'+kind+'.png');im.filepath_raw=str(path);im.file_format='PNG';im.save();im.pack();atlas[kind]=im
 for mat in o.data.materials:
  bs=mat.node_tree.nodes['Principled BSDF'];output=mat.node_tree.nodes.get('Material Output');mat.node_tree.links.new(bs.outputs['BSDF'],output.inputs['Surface'])
mat=bpy.data.materials.new('FirstBloom_Atlas512');mat.use_nodes=True;bs=mat.node_tree.nodes['Principled BSDF']
uvnode=mat.node_tree.nodes.new('ShaderNodeUVMap');uvnode.uv_map='FirstBloomAtlas'
for kind,socket in [('albedo','Base Color'),('roughness','Roughness'),('normal',None),('emission','Emission Color')]:
 node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=atlas[kind];mat.node_tree.links.new(uvnode.outputs[0],node.inputs['Vector'])
 if kind=='normal':
  normal=mat.node_tree.nodes.new('ShaderNodeNormalMap');normal.uv_map='FirstBloomAtlas';mat.node_tree.links.new(node.outputs['Color'],normal.inputs['Color']);mat.node_tree.links.new(normal.outputs[0],bs.inputs['Normal'])
 else:mat.node_tree.links.new(node.outputs['Color'],bs.inputs[socket])
bs.inputs['Emission Strength'].default_value=.65
o.data.materials.clear();o.data.materials.append(mat)
for p in o.data.polygons:p.material_index=0
old=o.data.uv_layers.get('BossConstructionUV')
if old:o.data.uv_layers.remove(old)
points=[o.matrix_world@Vector(c) for c in o.bound_box];height=max(p.z for p in points)-min(p.z for p in points);factor=4.65/height
for v in o.data.vertices:v.co*=factor
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
for bone in arm.data.edit_bones:bone.head*=factor;bone.tail*=factor
bpy.ops.object.mode_set(mode='OBJECT')
for action in bpy.data.actions:
 for layer in action.layers:
  for strip in layer.strips:
   for slot in action.slots:
    bag=strip.channelbag(slot)
    if bag:
     for curve in bag.fcurves:
      if curve.data_path.endswith('.location'):
       for kp in curve.keyframe_points:kp.co.y*=factor;kp.handle_left.y*=factor;kp.handle_right.y*=factor
for action in bpy.data.actions:
 if action.name in str(scene['magenheim_authored_actions']).split(','):action.use_fake_user=True
o.parent=arm;mod=o.modifiers.new('InheritedRig','ARMATURE');mod.object=arm
scene['magenheim_model_id']=ID;scene['boss_target_metres']=4.65;scene['refinement_head_focus']=[0,.20*factor,2.76*factor]
scene['refinement_status']='individualized surface and cost study; not runtime admitted'
scene['magenheim_skinning']='review-only-skinned-creature'
bpy.data.orphans_purge(do_local_ids=True,do_linked_ids=True,do_recursive=True)
bpy.context.preferences.filepaths.save_version=0
studies=ROOT/'assets/models/studies';studies.mkdir(parents=True,exist_ok=True)
source=studies/(ID+'.blend');bpy.ops.wm.save_as_mainfile(filepath=str(source),compress=True)
bpy.ops.export_scene.gltf(filepath=str(OUT/(ID+'.glb')),export_format='GLB',export_animations=False,export_yup=True)
report=dict(id=ID,source=source.relative_to(ROOT).as_posix(),triangles=triangles,donor_triangles=6206,soft_cap=CAP,ratio=triangles/6206,previous_triangles=91516,reduction_percent=(1-triangles/91516)*100,renderers=1,materials=1,atlas_size=512,category_triangles=counts,rig='retained crowncap-brute baseline; mixed weights at trunk and limb roots',texture_bake='self material bake from owned r2 maps plus radial gill normals; no high-to-low geometry transfer',status='art candidate; live performance and deformation acceptance pending')
(OUT/'refinement-metrics.json').write_text(json.dumps(report,indent=2)+'\n')
print('REVISED First Bloom: '+str(triangles)+' triangles / soft cap '+str(CAP)+' / one mesh and material / 512px atlas',flush=True)
