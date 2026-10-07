"""Preserve the approved pitch while increasing the chest's inward offset."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/first_bloom_looming_chest.py').read_text().replace('first-bloom-looming-r10','first-bloom-inset-r11').replace('inset=.065','inset=.125')
source=source.replace("(.001 if ordinal<len(ids)//2 else .004)","(.003 if ordinal<len(ids)//2 else .006)")
exec(compile(source,str(__file__),'exec'))
_geometry_build=build
_original_materials=materials
_seat_phase=0

def embed(point,host,depth=.003):
 global _seat_phase
 _seat_phase+=2.399963229728653
 from first_bloom_attachment_repair import bone_weights
 from mathutils import geometry
 bvh=tree(host);candidates=[]
 for face,tri in enumerate(host.data.loop_triangles):
  coords=[host.matrix_world@host.data.vertices[i].co for i in tri.vertices]
  hit=geometry.closest_point_on_tri(point,*coords);normal=(coords[1]-coords[0]).cross(coords[2]-coords[0])
  if normal.length<1e-10:continue
  candidates.append(((hit-point).length,hit,normal.normalized(),face,sum(coords,Vector())/3,(coords[1]-coords[0]).normalized()))
 # Avoid a closest-point edge or corner: finite-precision export can move such
 # a contact across the boundary even when the pre-export ray test succeeds.
 for distance,hit,normal,face,center,tangent in sorted(candidates,key=lambda c:c[0])[:24]:
  for inset in (.08,.20,.40,.60):
   contact=hit.lerp(center,inset)+(tangent*math.cos(_seat_phase)+normal.cross(tangent)*math.sin(_seat_phase))*.0002
   for d in (depth,.003,.005,.007,.001):
    for sign in (-1,1):
     seated=contact+normal*d*sign
     if inside(seated,bvh):
      try:return seated,bone_weights(host,face,contact)
      except ValueError:continue
 raise ValueError('cannot seat robust inset contact into '+host.name)

def build(B,arm,record,profile,tube,cap):
 trunk=_geometry_build(B,arm,record,profile,tube,cap)
 # Seat this crosslink through the supporting ribbon's mid-thickness, so its
 # contact follows both sides of the thin frame during the inherited sweep.
 records=json.loads(bpy.context.scene['tissue_attachment_contract'])
 item=next(r for r in records if r['tissue']=='VeilCrosslink12' and r['end']=='start')
 o=bpy.data.objects[item['tissue']];host=bpy.data.objects[item['support']]
 ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']]
 center=sum((o.data.vertices[i].co for i in ids),Vector())/len(ids);half=len(host.data.vertices)//2
 bvh=tree(host);blends=[(.40,.30,.30),(.30,.40,.30),(.25,.40,.35),(.35,.30,.35)]
 faces=[list(t.vertices) for t in host.data.loop_triangles if all(i<half for i in t.vertices)]
 faces.sort(key=lambda f:(sum((host.data.vertices[i].co for i in f),Vector())/3-center).length)
 face=next(f for f in faces if all(inside(host.matrix_world@sum(((host.data.vertices[i].co+host.data.vertices[i+half].co)*w*.5 for i,w in zip(f,blend)),Vector()),bvh) for blend in blends))
 for index,blend in zip(ids,blends):
  point=Vector();weights={}
  for i,w in zip(face,blend):
   for vertex in (i,i+half):
    point+=host.data.vertices[vertex].co*w*.5
    for influence in host.data.vertices[vertex].groups:
     name=host.vertex_groups[influence.group].name;weights[name]=weights.get(name,0)+influence.weight*w*.5
  assign(o,index,host.matrix_world@point,weights)
 assert all(inside(o.matrix_world@o.data.vertices[i].co,tree(host)) for i in ids)
 wall=bpy.data.objects['RecessedChamber'];wall.data.materials.append(B.material('root-bark'))
 for fin in bpy.context.scene.objects:
  if fin.name.startswith('ChamberRib'):fin.data.materials[0]=B.material('rib-tissue')
 rim=set(range(24))|set(range(96,120))
 for poly in wall.data.polygons:
  poly.material_index=1 if any(i in rim for i in poly.vertices) else 0
 (OUT/'chest-materials.json').write_text(json.dumps(dict(outer_band_material='root-bark, shared with body skin',outer_band_faces=sum(p.material_index==1 for p in wall.data.polygons),emission='central amber cavity plus warm yellow on deep central frill faces; exposed white edges and skin band remain unlit',additional_inset_authored_metres=.060),indent=2)+'\n')
 return trunk

def materials(B):
 _original_materials(B)
 mat=B.mats['oral-tissue'];nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes['Principled BSDF']
 coord=nodes.new('ShaderNodeTexCoord');sep=nodes.new('ShaderNodeSeparateXYZ');links.new(coord.outputs['Object'],sep.inputs[0])
 x=nodes.new('ShaderNodeMath');x.operation='DIVIDE';x.inputs[1].default_value=.34;links.new(sep.outputs['X'],x.inputs[0])
 z0=nodes.new('ShaderNodeMath');z0.operation='SUBTRACT';z0.inputs[1].default_value=1.81;links.new(sep.outputs['Z'],z0.inputs[0])
 z=nodes.new('ShaderNodeMath');z.operation='DIVIDE';z.inputs[1].default_value=.34;links.new(z0.outputs[0],z.inputs[0])
 combine=nodes.new('ShaderNodeCombineXYZ');links.new(x.outputs[0],combine.inputs['X']);links.new(z.outputs[0],combine.inputs['Z'])
 radius=nodes.new('ShaderNodeVectorMath');radius.operation='LENGTH';links.new(combine.outputs[0],radius.inputs[0])
 falloff=nodes.new('ShaderNodeMapRange');falloff.interpolation_type='SMOOTHSTEP';falloff.clamp=True
 falloff.inputs['From Min'].default_value=.05;falloff.inputs['From Max'].default_value=1;falloff.inputs['To Min'].default_value=1;falloff.inputs['To Max'].default_value=0;links.new(radius.outputs['Value'],falloff.inputs['Value'])
 noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=19;noise.inputs['Detail'].default_value=3;links.new(coord.outputs['Object'],noise.inputs['Vector'])
 ramp=nodes.new('ShaderNodeValToRGB');links.new(noise.outputs['Fac'],ramp.inputs[0]);ramp.color_ramp.elements[0].position=.30;ramp.color_ramp.elements[0].color=(.12,.012,.001,1);ramp.color_ramp.elements[1].position=.65;ramp.color_ramp.elements[1].color=(1,.35,.018,1)
 glow=nodes.new('ShaderNodeMixRGB');glow.blend_type='MULTIPLY';glow.inputs[0].default_value=1;links.new(ramp.outputs['Color'],glow.inputs[1]);links.new(falloff.outputs['Result'],glow.inputs[2]);links.new(glow.outputs[0],bs.inputs['Emission Color'])
 # Preserve the ivory pigment on the frills while baking a separate emission
 # mask into the same atlas. Position masks select deep faces, not leading lips.
 mat=B.mats['rib-tissue'];nodes=mat.node_tree.nodes;links=mat.node_tree.links;nodes.clear()
 output=nodes.new('ShaderNodeOutputMaterial');bs=nodes.new('ShaderNodeBsdfPrincipled');links.new(bs.outputs[0],output.inputs['Surface']);bs.inputs['Roughness'].default_value=.84
 coord=nodes.new('ShaderNodeTexCoord');sep=nodes.new('ShaderNodeSeparateXYZ');links.new(coord.outputs['Object'],sep.inputs[0])
 noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=9;noise.inputs['Detail'].default_value=3;links.new(coord.outputs['Object'],noise.inputs['Vector'])
 pale=nodes.new('ShaderNodeValToRGB');pale.color_ramp.elements[0].color=(.11,.088,.0605,1);pale.color_ramp.elements[1].color=(.396,.3575,.264,1);links.new(noise.outputs['Fac'],pale.inputs[0]);links.new(pale.outputs[0],bs.inputs['Base Color'])
 def operation(op,source,value):
  n=nodes.new('ShaderNodeMath');n.operation=op;links.new(source,n.inputs[0]);n.inputs[1].default_value=value;return n.outputs[0]
 xn=operation('DIVIDE',sep.outputs['X'],.34);zc=operation('SUBTRACT',sep.outputs['Z'],1.81);zn=operation('DIVIDE',zc,.34)
 combine=nodes.new('ShaderNodeCombineXYZ');links.new(xn,combine.inputs['X']);links.new(zn,combine.inputs['Z']);rad=nodes.new('ShaderNodeVectorMath');rad.operation='LENGTH';links.new(combine.outputs[0],rad.inputs[0])
 central=nodes.new('ShaderNodeMapRange');central.clamp=True;central.interpolation_type='SMOOTHSTEP';central.inputs['From Min'].default_value=.12;central.inputs['From Max'].default_value=.90;central.inputs['To Min'].default_value=1;central.inputs['To Max'].default_value=0;links.new(rad.outputs['Value'],central.inputs['Value'])
 plane=operation('MULTIPLY',sep.outputs['Z'],math.tan(math.radians(12)));plane=operation('SUBTRACT',plane,.195)
 recess=nodes.new('ShaderNodeMath');recess.operation='SUBTRACT';links.new(plane,recess.inputs[0]);links.new(sep.outputs['Y'],recess.inputs[1])
 deep=nodes.new('ShaderNodeMapRange');deep.clamp=True;deep.interpolation_type='SMOOTHSTEP';deep.inputs['From Min'].default_value=.030;deep.inputs['From Max'].default_value=.090;links.new(recess.outputs[0],deep.inputs['Value'])
 mask=nodes.new('ShaderNodeMath');mask.operation='MULTIPLY';links.new(central.outputs[0],mask.inputs[0]);links.new(deep.outputs[0],mask.inputs[1])
 tint=nodes.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1;tint.inputs[1].default_value=(1,.62,.065,1);links.new(mask.outputs[0],tint.inputs[2]);links.new(tint.outputs[0],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=1
