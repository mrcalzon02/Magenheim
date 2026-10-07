"""Two distinct dorsal fins and painted skin instead of raised flank blocks."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
recipe=(ROOT/'tools/blackwater_maw_jaw_revision.py').read_text().replace('blackwater-maw-jaws-r5-raw','blackwater-maw-fins-r6-raw')
adjustments='''
start=source.index(' # Purpose-sized flank scales');end=source.index(' # Name-stable face and endpoint',start)
source=source[:start]+source[end:]
source=source.replace('one continuous spinal surface with 48 rooted flank scales','one continuous spinal surface; painted scale detail with no raised flank blocks')
source=source.replace('closed pleated pectorals, continuous ragged dorsal sail and forked caudal fan','closed pleated pectorals, separate anterior and posterior dorsal membranes, and forked caudal fan')
start=source.index(' # One skinned dorsal sail');end=source.index(' # Forked caudal fan',start)
dorsals=""" # Two separate dorsal membranes, each rooted in the continuous body.
 def top_z(y):
  for (a,rx,rz,z),(b,brx,brz,bz) in zip(rings,rings[1:]):
   if b<=y<=a:t=(a-y)/(a-b);return (z+rz)*(1-t)+(bz+brz)*t
  return .2
 for name,ys,heights in [('AnteriorDorsalFin',[1.12,.90,.62,.34,.05,-.16,-.34],[.10,.55,1.05,1.20,.97,.40,.06]),('PosteriorDorsalFin',[-.55,-.72,-.94,-1.16,-1.38,-1.60,-1.80],[.08,.45,.82,.96,.72,.35,.06])]:
  vertices=[];fn=len(ys);count=fn*4
  for layer in range(2):
   for j,t in enumerate((0,.33,.70,1)):
    for k,(y,h) in enumerate(zip(ys,heights)):
     pleat=.009*math.sin(k*2.7)*t*(1-t)
     vertices.append(((-1 if layer else 1)*(.020+pleat)*(1-.45*t),y+.16*t,top_z(y)-.04+h*t))
  faces=[]
  for layer in range(2):
   for j in range(3):
    for k in range(fn-1):
     a=layer*count+j*fn+k;f=(a,a+1,a+fn+1,a+fn);faces.append(f if layer==0 else tuple(reversed(f)))
  boundary=list(range(fn))+[j*fn+fn-1 for j in range(1,4)]+list(reversed(range(3*fn,4*fn-1)))+[j*fn for j in reversed(range(1,3))]
  for a,b in zip(boundary,boundary[1:]+boundary[:1]):faces.append((a,b,b+count,a+count))
  fin=mesh(name,vertices,faces,'fin-membrane','Spine_1','fins')
  for v in fin.data.vertices:assign(fin,v.index,v.co,body_weights(v.co.y))
  pending.append((fin,list(range(fn))+list(range(count,count+fn)),body,'dorsal_root',.065))
"""
source=source[:start]+dorsals+source[end:]
source=source.replace("(.09,.13,.13,1)","(.025,.052,.061,1)").replace("(.16,.17,.13,1)","(.06,.09,.073,1)")
source=source.replace("(.19,.21,.19,1)","(.10,.13,.115,1)").replace("(.32,.32,.27,1)","(.21,.23,.19,1)")
paint=""" # Owned generated albedo tile; fine relief is derived into the atlas.
 mat=B.mats['wet-hide'];nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes['Principled BSDF']
 image=bpy.data.images.load(str(ROOT/'assets/textures/underworld/bosses/blackwater-maw-skin-study/blackwater-maw-scales-albedo.png'),check_existing=True);image.colorspace_settings.name='sRGB';image.pack()
 uv=nodes.new('ShaderNodeUVMap');uv.uv_map='BossConstructionUV';tex=nodes.new('ShaderNodeTexImage');tex.image=image;tex.extension='REPEAT';links.new(uv.outputs[0],tex.inputs['Vector']);links.new(tex.outputs['Color'],bs.inputs['Base Color'])
 gray=nodes.new('ShaderNodeRGBToBW');links.new(tex.outputs['Color'],gray.inputs[0])
 height=nodes.new('ShaderNodeMapRange');height.clamp=True;height.inputs['From Min'].default_value=.003;height.inputs['From Max'].default_value=.10;links.new(gray.outputs[0],height.inputs[0])
 bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.28;bump.inputs['Distance'].default_value=.010;links.new(height.outputs[0],bump.inputs['Height']);links.new(bump.outputs[0],bs.inputs['Normal'])
 rough=nodes.new('ShaderNodeMapRange');rough.clamp=True;rough.inputs['From Min'].default_value=.003;rough.inputs['From Max'].default_value=.10;rough.inputs['To Min'].default_value=.82;rough.inputs['To Max'].default_value=.65;links.new(gray.outputs[0],rough.inputs[0]);links.new(rough.outputs[0],bs.inputs['Roughness'])
 # Put worn cracks in the bone albedo as well as its normal detail.
 mat=B.mats['bone-armor'];nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes['Principled BSDF'];base=bs.inputs['Base Color'].links[0].from_socket
 edge=next(node for node in nodes if node.type=='TEX_VORONOI');cracks=nodes.new('ShaderNodeValToRGB');cracks.color_ramp.elements[0].position=.006;cracks.color_ramp.elements[0].color=(.13,.16,.145,1);cracks.color_ramp.elements[1].position=.050;cracks.color_ramp.elements[1].color=(1,.98,.91,1);links.new(edge.outputs['Distance'],cracks.inputs[0])
 blend=nodes.new('ShaderNodeMixRGB');blend.blend_type='MULTIPLY';blend.inputs[0].default_value=.65;links.new(base,blend.inputs[1]);links.new(cracks.outputs[0],blend.inputs[2]);links.new(blend.outputs[0],bs.inputs['Base Color'])
"""
source=source.replace(' materials(B);pending=[];parts={}', ' materials(B);pending=[];parts={}'+chr(10)+paint)
cyl_uv=""" # Longitudinal skin rows, with the circumferential seam on the belly.
 for poly in body.data.polygons:
  keys=[(i%24+6)%24 for i in poly.vertices];seam=0 in keys and 23 in keys
  for li in poly.loop_indices:
   index=body.data.loops[li].vertex_index;k=(index%24+6)%24
   body.data.uv_layers['BossConstructionUV'].data[li].uv=((1 if seam and k==0 else k/24)*2,-body.data.vertices[index].co.y*.34)
"""
source=source.replace(" body.data.materials.append(B.mats['oral-tissue']);",cyl_uv+" body.data.materials.append(B.mats['oral-tissue']);")
'''
marker="exec(compile(source,str(__file__),'exec'))"
assert recipe.count(marker)==1;recipe=recipe.replace(marker,adjustments+'\n'+marker)
exec(compile(recipe,str(__file__),'exec'))
