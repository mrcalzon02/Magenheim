"""First Bloom's concept anatomy: fungal cathedral, rib chamber, rooted legs."""
import bpy,math,random
from mathutils import Vector,Matrix

def build(B,arm,record,profile,tube,unused_cap):
 # Narrow the donor's exaggerated bow-legged rest stance to the rooted concept.
 bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
 for b in arm.data.edit_bones:
  if b.name.startswith(('Thigh_','Shin_','Foot_')):b.head.x*=.75;b.tail.x*=.75
 bpy.ops.object.mode_set(mode='OBJECT');arm.select_set(False)
 def shell(name,loc,rx,ry,bone,n=16,tilt=0,phase=0):
  # Thin bracket, rolled pale lip, stalk-side attachment and real underside.
  rings=[(-.015,.055),(-.05,.60),(-.065,1),(-.015,1.025),(.055,.73),(.12,.04)]
  verts=[];faces=[]
  for j,(z,r) in enumerate(rings):
   for k in range(n):
    a=math.tau*k/n;wave=.07*math.sin(a*5+phase)+.035*math.sin(a*9+.7)
    fold=.012*(-1)**k if j<2 else 0
    verts.append((rx*math.cos(a)*r*(1+wave*r),ry*math.sin(a)*r*(1+wave*r),z+fold+.014*math.sin(a*3+phase)*r))
  for j in range(5):
   for k in range(n):
    a=j*n+k;b=j*n+(k+1)%n;faces.append((a,b,b+n,a+n))
  faces.extend([tuple(reversed(range(n))),tuple(5*n+k for k in range(n))])
  me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
  for key in ('ochre-shelf','ivory'):me.materials.append(B.material(key))
  o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
  o.matrix_world=Matrix.Translation(Vector(loc))@Matrix.Rotation(tilt,4,'Y')
  uv=me.uv_layers.new(name='BossConstructionUV')
  for p in me.polygons:
   p.use_smooth=len(p.vertices)==4;p.material_index=1 if p.index<3*n else 0
   seam=0 in [v%n for v in p.vertices] and n-1 in [v%n for v in p.vertices]
   for li in p.loop_indices:
    j,k=divmod(me.loops[li].vertex_index,n);uv.data[li].uv=(1 if seam and k==0 else k/n,j/5)
  return record(o,bone,'canopy' if bone in ('Head','Chest') else 'limb-growth')

 def sheet(name,points,widths,bone):
  # Closed irregular ribbons, with branches forming actual intervening holes.
  verts=[]
  for p,w in zip(points,widths):verts.extend([(p[0]-w,p[1],p[2]),(p[0]+w,p[1]+.006,p[2])])
  faces=[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(points)-1)]
  me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();me.materials.append(B.material('ivory'))
  o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  mod=o.modifiers.new('TissueThickness','SOLIDIFY');mod.thickness=.008;bpy.ops.object.modifier_apply(modifier=mod.name)
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False)
  o.data.uv_layers.active.name='BossConstructionUV'
  return record(o,bone,'draped-tissue')

 # Torso is an actual front-open shell. Its front boundary turns into a recessed
 # oval bowl, rather than covering a plain torso with two bright flat plates.
 n=24;rings=[(.73,.23,.20,0,-.10),(1.05,.29,.23,0,-.10),(1.43,.28,.25,0,-.12),(1.68,.40,.30,0,-.15),(1.94,.55,.33,0,-.17),(2.20,.57,.29,0,-.20),(2.44,.33,.22,0,-.21)]
 verts=[];faces=[]
 for j,(z,rx,ry,cx,cy) in enumerate(rings):
  for k in range(n):
   a=math.tau*k/n;flute=1+.085*math.cos(a*5+j*.32)
   verts.append((cx+math.cos(a)*rx*flute,cy+math.sin(a)*ry*flute,z))
 for j in range(6):
  for k in range(n):
   # +Y is front. Opening spans torso rings 2..5, angles 30..150 degrees.
   if 2<=j<5 and 2<=k<10:continue
   a=j*n+k;b=j*n+(k+1)%n;faces.append((a,b,b+n,a+n))
 faces.extend([tuple(reversed(range(n))),tuple(6*n+k for k in range(n))])
 me=bpy.data.meshes.new('HollowCathedralTrunk');me.from_pydata(verts,[],faces);me.update();me.materials.append(B.material('root-bark'))
 trunk=bpy.data.objects.new('HollowCathedralTrunk',me);bpy.context.collection.objects.link(trunk)
 bpy.ops.object.select_all(action='DESELECT');trunk.select_set(True);bpy.context.view_layer.objects.active=trunk
 solid=trunk.modifiers.new('BarkShellThickness','SOLIDIFY');solid.thickness=.025;bpy.ops.object.modifier_apply(modifier=solid.name)
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT')
 for p in trunk.data.polygons:p.use_smooth=True
 trunk.data.uv_layers.active.name='BossConstructionUV';record(trunk,'Chest','trunk')
 pelvis=trunk.vertex_groups.new(name='Pelvis')
 for v in trunk.data.vertices:
  w=max(0,min(1,(1.50-v.co.z)/.50))
  if w>0:pelvis.add([v.index],w,'REPLACE');trunk.vertex_groups['Chest'].add([v.index],1-w,'REPLACE')
 # The dark concave chamber closes the aperture behind curved paired ribs.
 n=24;verts=[];faces=[]
 for j,(r,y) in enumerate([(1,.22),(.73,.025),(.31,-.07),(.025,-.08)]):
  for k in range(n):
   a=math.tau*k/n;verts.append((.44*r*math.cos(a),y,1.91+.43*r*math.sin(a)))
 for j in range(3):
  for k in range(n):a=j*n+k;b=j*n+(k+1)%n;faces.append((a,b,b+n,a+n))
 faces.append(tuple(3*n+k for k in range(n)))
 me=bpy.data.meshes.new('RecessedChamber');me.from_pydata(verts,[],faces);me.update();me.materials.append(B.material('oral-tissue'))
 o=bpy.data.objects.new('RecessedChamber',me);bpy.context.collection.objects.link(o)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 mod=o.modifiers.new('ChamberWall','SOLIDIFY');mod.thickness=.014;bpy.ops.object.modifier_apply(modifier=mod.name)
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False)
 o.data.uv_layers.active.name='BossConstructionUV';record(o,'Chest','chamber')
 for p in o.data.polygons:p.use_smooth=True
 rib_material=B.material('ivory').copy();rib_material.name='FirstBloom_AmberRibBark';B.mats['rib-tissue']=rib_material
 for s in (-1,1):
  for k in range(11):
   a=-1.35+k*2.7/10;z=1.91+.40*math.sin(a);x=s*.43*math.cos(a)
   rib=tube('ChamberRib'+str(s)+'_'+str(k),[(s*.025,.14,1.85+.10*math.sin(a)),(s*.18,.17,1.91+.28*math.sin(a)),(x,.25,z)],[.017,.027,.013],'ivory','Chest',5,'chamber');rib.data.materials[0]=rib_material
  for k in range(3):
   z=1.80+k*.105
   tube('InnerGlow'+str(s)+str(k),[(s*.05,.12,z),(s*.12,.13,z+.055),(s*.19,.12,z+.10)],[.019,.020,.006],'living-gill','Chest',5,'chamber')
 tube('AmberChamberHeart',[(0,.08,1.67),(0,.11,1.88),(0,.085,2.12)],[.024,.07,.010],'living-gill','Chest',8,'chamber')
 # No humanoid eyes or smiling mouth: branching fungal struts fill the crown.
 tube('CrownSpine',[(0,-.14,2.25),(-.03,-.17,2.65),(.06,-.18,3.02),(0,-.17,3.48)],[.12,.11,.07,.025],'root-bark','Head',10,'trunk')
 canopy=[(-.43,-.08,2.52,.66,.45,-.15),(.38,-.19,2.66,.61,.41,.20),(-.29,-.21,2.83,.55,.38,-.17),(.25,-.16,2.99,.46,.33,.20),(-.16,-.19,3.14,.38,.29,-.14),(.12,-.18,3.29,.30,.25,.15),(-.05,-.18,3.43,.22,.19,-.07),(.02,-.18,3.56,.14,.13,.03)]
 for k,(x,y,z,rx,ry,tilt) in enumerate(canopy):
  shell('CathedralMantle'+str(k),(x,y,z),rx,ry,'Head',24 if k<4 else (14 if k<6 else 10),tilt,k*.8)
  if k<6:
   for m in range(3):
    transform=Matrix.Translation(Vector((x,y,z)))@Matrix.Rotation(tilt,4,'Y')
    top=transform@Vector(((m-1)*rx*.52,ry*.68,-.075));bottom=top+Vector((-.018*(m-1),.018,-.30-(m%2)*.13))
    sheet('MantleCurtain'+str(k)+'_'+str(m),[top,top.lerp(bottom,.45)+Vector((.018,0,0)),bottom],[.027,.014,.001],'Head')
 # Rear view also carries the concept's layered fungal growth, rather than a
 # bare uninterrupted back. Reallocate loops from small crown tips for these.
 for k,(x,y,z,rx) in enumerate([(.08,-.28,1.17,.26),(-.13,-.38,1.60,.34),(.10,-.43,2.05,.43)]):
  shell('RearTrunkBracket'+str(k),(x,y,z),rx,.25,'Chest',10,(-1)**k*.13,k*.81)
 # Long limbs have twisted bark profiles, split root fingers and layered growth
 # all the way to the feet. Small shelves get fewer loops than focal canopy.
 for side,s in [('L',-1),('R',1)]:
  for upper,lower in [('UpperArm','ForeArm'),('Thigh','Shin')]:
   bone=upper+'_'+side;lb=lower+'_'+side;b=arm.data.bones[bone];c=arm.data.bones[lb]
   a=b.head_local;joint=c.head_local;end=c.tail_local
   pts=[a,a.lerp(joint,.32),a.lerp(joint,.7),joint,joint.lerp(end,.33),joint.lerp(end,.72),end]
   radii=[.16,.19,.16,.13,.17,.15,.12] if upper=='UpperArm' else [.22,.25,.21,.19,.25,.24,.23]
   o=tube('Rooted'+upper+side,pts,radii,'root-bark',bone,10,'limbs')
   for v in o.data.vertices:
    j,k=divmod(v.index,10);center=pts[j];v.co=center+(v.co-center)*(1+.16*math.cos(k*math.tau/10*3+j*.75))
   vg=o.vertex_groups.new(name=lb)
   for v in o.data.vertices:
    w=(0,0,.15,.5,.85,1,1)[v.index//10]
    if w>0:vg.add([v.index],w,'REPLACE');o.vertex_groups[bone].add([v.index],1-w,'REPLACE')
   # Brackets grow from the outer/front bark, staggered through the limb.
   for k in range(5):
    t=.12+k*.19;p=a.lerp(end,t);p.x+=s*.11;p.y+=.10
    shell('LimbBracket'+upper+side+str(k),p,.22+(k%2)*.045,.20,bone if t<.5 else lb,10,-s*.20,k*.63)
   # Pale tissue is localized between growths, not an opaque blanket.
   for k in range(2):
    p=a.lerp(end,.22+k*.34);x=p.x+s*.09
    sheet('LimbTissue'+upper+side+str(k),[(x,p.y+.22,p.z),(x+s*.025,p.y+.23,p.z-.15),(x+s*.04,p.y+.20,p.z-.28)],[.036,.019,.002],bone if k==0 else lb)
  hand=arm.data.bones['Hand_'+side];a=hand.head_local
  tube('KnottedPalm'+side,[a,a+Vector((s*.03,.14,-.17)),a+Vector((s*.02,.23,-.25))],[.16,.19,.12],'root-bark','Hand_'+side,10,'digits')
  for k in range(3):
   x=a.x+(k-1)*.13
   tube('CurledRootFinger'+side+str(k),[(x,a.y+.16,a.z-.17),(x+s*.06,a.y+.22,a.z-.32),(x+s*.12,a.y+.26,a.z-.48),(x+s*.06,a.y+.30,a.z-.53)],[.070,.056,.032,.004],'root-bark','Hand_'+side,6,'digits')
  tube('OpposingRootThumb'+side,[(a.x-s*.15,a.y+.1,a.z-.06),(a.x-s*.23,a.y+.19,a.z-.24),(a.x-s*.16,a.y+.29,a.z-.34)],[.07,.05,.008],'root-bark','Hand_'+side,6,'digits')
  foot=arm.data.bones['Foot_'+side].head_local
  # Forking buttresses break the bare cylindrical waist into rooted hips.
  for k in range(2):
   tube('HipFork'+side+str(k),[(s*.14,.13,1.32),(s*.25,.17,1.08),(foot.x*.72+(k-.5)*.10,.18,.65),(foot.x,.21,.31)],[.05,.09,.065,.014],'root-bark','Thigh_'+side,6,'ground-roots')
  for k in range(5):
   a=(k-2)*.50;x=foot.x+math.sin(a)*.16
   tube('GroundButtress'+side+str(k),[(foot.x,foot.y,foot.z+.30),(x,foot.y+.14,.16),(foot.x+math.sin(a)*.48,foot.y+.37+.16*math.cos(a),.045)],[.11,.13,.010],'root-bark','Foot_'+side,6,'ground-roots')
  for k in range(3):shell('RootSkirt'+side+str(k),(foot.x+s*.10,.18+k*.14,.10+k*.13),.31,.23,'Foot_'+side,10,s*.16,k)
 # Broad forked pale scaffold marks the chest, leaving the portal readable.
 for s in (-1,1):
  for k in range(3):
   x=s*(.19+k*.11)
   sheet('ChestVeil'+str(s)+str(k),[(x,.27,2.49),(x+s*.04,.30,2.36),(x+s*.015,.29,2.13),(x+s*.07,.25,1.70-k*.05)],[.035,.027,.020,.001],'Chest')
   tube('VeilCrosslink'+str(s)+str(k),[(x,.285,2.43),(s*.11,.27,2.28),(0,.20,2.12)],[.020,.022,.006],'ivory','Chest',4,'draped-tissue')
 return trunk

def materials(B):
 # Owned procedural pigmentation with meaningful material families. Bake to
 # the same compact atlas; do not borrow lighting or pixels from the concept.
 for key,mat in B.mats.items():
  nodes=mat.node_tree.nodes;links=mat.node_tree.links;nodes.clear()
  output=nodes.new('ShaderNodeOutputMaterial');bs=nodes.new('ShaderNodeBsdfPrincipled');bs.name='Principled BSDF';links.new(bs.outputs[0],output.inputs['Surface'])
  coord=nodes.new('ShaderNodeTexCoord');noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=6 if key=='ochre-shelf' else 9;noise.inputs['Detail'].default_value=3;noise.inputs['Roughness'].default_value=.72;links.new(coord.outputs['Object'],noise.inputs['Vector'])
  ramp=nodes.new('ShaderNodeValToRGB');links.new(noise.outputs['Fac'],ramp.inputs[0]);links.new(ramp.outputs['Color'],bs.inputs['Base Color'])
  palettes={
   'root-bark':[(.12,(.025,.018,.012,1)),(.40,(.11,.069,.034,1)),(.58,(.22,.16,.084,1)),(.73,(.075,.09,.022,1)),(.88,(.27,.22,.12,1))],
   'ochre-shelf':[(.15,(.10,.045,.012,1)),(.38,(.31,.16,.045,1)),(.52,(.46,.31,.10,1)),(.60,(.18,.20,.035,1)),(.74,(.09,.12,.018,1)),(.89,(.39,.29,.12,1))],
   'ivory':[(.2,(.20,.16,.11,1)),(.45,(.43,.37,.27,1)),(.72,(.72,.65,.48,1))],
   'rib-tissue':[(.2,(.065,.025,.007,1)),(.45,(.22,.11,.035,1)),(.72,(.43,.29,.12,1))],
   'oral-tissue':[(.2,(.014,.009,.004,1)),(.7,(.065,.028,.009,1))],
   'living-gill':[(.2,(.28,.055,.002,1)),(.6,(1,.35,.025,1)),(.85,(1,.68,.17,1))]}
  scale={'root-bark':.25,'ochre-shelf':.45,'ivory':.55,'rib-tissue':.65,'oral-tissue':.3,'living-gill':1}[key]
  colors=[(p,tuple(c*scale for c in color[:3])+(1,)) for p,color in palettes[key]]
  cr=ramp.color_ramp;cr.elements.remove(cr.elements[-1]);cr.elements[0].position=colors[0][0];cr.elements[0].color=colors[0][1]
  for pos,color in colors[1:]:cr.elements.new(pos).color=color
  bs.inputs['Roughness'].default_value=.86 if key!='living-gill' else .60
  if key=='living-gill':links.new(ramp.outputs['Color'],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=1
  fine=nodes.new('ShaderNodeTexNoise');fine.inputs['Scale'].default_value=75;fine.inputs['Detail'].default_value=2;links.new(coord.outputs['Object'],fine.inputs['Vector'])
  bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.30;bump.inputs['Distance'].default_value=.025;links.new(fine.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],bs.inputs['Normal'])
  if key in ('root-bark','ivory'):
   wave=nodes.new('ShaderNodeTexWave');wave.wave_type='BANDS';wave.bands_direction='X';wave.inputs['Scale'].default_value=9;wave.inputs['Distortion'].default_value=7;wave.inputs['Detail Scale'].default_value=2;links.new(coord.outputs['Object'],wave.inputs['Vector'])
   ridge=nodes.new('ShaderNodeBump');ridge.inputs['Strength'].default_value=.32;ridge.inputs['Distance'].default_value=.035;links.new(wave.outputs['Color'],ridge.inputs['Height']);links.new(bump.outputs['Normal'],ridge.inputs['Normal']);links.new(ridge.outputs['Normal'],bs.inputs['Normal'])

def unwrap_atlas(o):
 # Preserve the authored circumferential seam instead of fragmenting every
 # corrugated cap and rib into many small projection islands.
 old=o.data.uv_layers['BossConstructionUV'];by_edge={}
 for poly in o.data.polygons:
  ids=list(poly.loop_indices)
  for j,li in enumerate(ids):
   nxt=ids[(j+1)%len(ids)];edge=o.data.loops[li].edge_index
   pair={o.data.loops[li].vertex_index:old.data[li].uv.copy(),o.data.loops[nxt].vertex_index:old.data[nxt].uv.copy()}
   by_edge.setdefault(edge,[]).append((pair,len(poly.vertices)>4))
 for edge,entries in by_edge.items():
  seam=any(cap for pair,cap in entries)
  if len(entries)==2:
   a=entries[0][0];b=entries[1][0]
   seam|=any((a[v]-b[v]).length>1e-5 for v in a)
  o.data.edges[edge].use_seam=seam
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False)
 bpy.ops.uv.unwrap(method='ANGLE_BASED',margin=.008)
 bpy.ops.uv.average_islands_scale();bpy.ops.uv.pack_islands(margin=.008,rotate=True)
 bpy.ops.object.mode_set(mode='OBJECT')
