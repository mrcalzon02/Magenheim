"""Pale curved lamellae radiating from a fleshy join over amber tissue."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
import first_bloom_attachment_repair as prior
from first_bloom_frill_sculpt import embed,assign
unwrap_atlas=prior.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-radiating-r7'

def materials(B):
 prior.materials(B)
 mat=B.mats['oral-tissue'];nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes['Principled BSDF']
 coord=nodes.new('ShaderNodeTexCoord');sep=nodes.new('ShaderNodeSeparateXYZ');links.new(coord.outputs['Object'],sep.inputs[0])
 noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=19;noise.inputs['Detail'].default_value=3;links.new(coord.outputs['Object'],noise.inputs['Vector'])
 ramp=nodes.new('ShaderNodeValToRGB');links.new(noise.outputs['Fac'],ramp.inputs[0])
 ramp.color_ramp.elements[0].position=.35;ramp.color_ramp.elements[0].color=(.003,.0004,0,1)
 ramp.color_ramp.elements[1].position=.65;ramp.color_ramp.elements[1].color=(1,.31,.018,1)
 def mathnode(op,source,value):
  node=nodes.new('ShaderNodeMath');node.operation=op;links.new(source,node.inputs[0]);node.inputs[1].default_value=value;return node.outputs[0]
 ax=mathnode('ABSOLUTE',sep.outputs['X'],0)
 lateral=mathnode('GREATER_THAN',ax,.10)
 below=mathnode('LESS_THAN',sep.outputs['Z'],1.78)
 region=nodes.new('ShaderNodeMath');region.operation='MAXIMUM';links.new(lateral,region.inputs[0]);links.new(below,region.inputs[1])
 upper=mathnode('LESS_THAN',sep.outputs['Z'],2.26)
 mask=nodes.new('ShaderNodeMath');mask.operation='MULTIPLY';links.new(region.outputs[0],mask.inputs[0]);links.new(upper,mask.inputs[1])
 mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;links.new(ramp.outputs['Color'],mix.inputs[1]);links.new(mask.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=1

def build(B,arm,record,profile,tube,cap):
 prior.OUT=OUT
 trunk=prior.build(B,arm,record,profile,tube,cap)
 wall=bpy.data.objects['RecessedChamber'];core=bpy.data.objects['AmberChamberHeart']
 # A short fleshy central join, supported at the recessed wall; no long rod.
 core.data.materials[0]=B.material('ivory')
 for v in core.data.vertices:
  j,k=divmod(v.index,8);a=math.tau*k/8
  x,y,z=[(.015,-.027,1.67),(.090,.115,1.69),(.045,.190,1.72)][j]
  v.co=(x*math.cos(a),y, z+.095*math.sin(a))
 for index in range(8):
  point,weights=embed(core.matrix_world@core.data.vertices[index].co,wall)
  assign(core,index,point,weights)
 for o in list(bpy.context.scene.objects):
  if o.name.startswith('InnerGlow'):bpy.data.objects.remove(o,do_unlink=True)
 # Amber tissue is behind the pale fins, distributed around the cavity floor.
 for p in wall.data.polygons:
  p.material_index=0
 old=json.loads(bpy.context.scene['tissue_attachment_contract'])
 fins=[]
 for o in list(bpy.context.scene.objects):
  if not o.name.startswith('ChamberRib'):continue
  s=-1 if '-' in o.name else 1;k=int(o.name.rsplit('_',1)[1]);a=-1.15+k*2.50/10
  centers=[Vector((s*.027,.145,1.65+.010*k)),Vector((s*(.105+.007*k),.19,1.62+.037*k)),Vector((s*(.235+.009*k),.215,1.63+.061*k)),Vector((s*.43*math.cos(a),.20,1.91+.40*math.sin(a)))]
  verts=[];faces=[]
  for j,c in enumerate(centers):
   direction=centers[min(3,j+1)]-centers[max(0,j-1)]
   tangent=Vector((-direction.z,0,direction.x)).normalized()
   w=[.006,.009,.014,.010][j];depth=[.028,.052,.067,.022][j]
   # Narrow visible edges, deep lamellar faces, and a folded lower lip.
   for u,d in [(-w,-depth),(w,-depth),(w,depth),(-w*.2,depth*1.08),(-w,depth)]:verts.append(c+tangent*u+Vector((0,d,0)))
  for j in range(3):
   for k2 in range(5):
    a2=j*5+k2;b=j*5+(k2+1)%5;faces.append((a2,b,b+5,a2+5))
  faces.extend([tuple(reversed(range(5))),tuple(range(15,20))])
  me=bpy.data.meshes.new(o.name+'Radiating');me.from_pydata(verts,[],faces);me.update();me.materials.append(B.material('ivory'))
  component=o.data.attributes['attachment_component'].data[0].value
  o.data=me
  attr=me.attributes.new('attachment_component','INT','FACE')
  for item in attr.data:item.value=component
  ends=me.attributes.new('attachment_endpoint','INT','POINT')
  o.vertex_groups.clear()
  group=o.vertex_groups.get('Chest') or o.vertex_groups.new(name='Chest');group.add(list(range(20)),1,'REPLACE')
  for item in [r for r in old if r['tissue']==o.name]:
   ids=range(5) if item['end']=='start' else range(15,20)
   host=core if item['end']=='start' else wall
   item['support']=host.name;item['support_component']=host.data.attributes['attachment_component'].data[0].value
   item['anatomy']='pale lamella radiating from central fleshy join into cavity wall'
   for index in ids:
    point,weights=embed(o.matrix_world@me.vertices[index].co,host,.002)
    assign(o,index,point,weights);me.attributes['attachment_endpoint'].data[index].value=item['id']
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False)
  me.uv_layers.active.name='BossConstructionUV'
  for p in me.polygons:p.use_smooth=False
  fins.append(o.name)
 changed=set(fins+[core.name]);byname={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'}
 for item in old:
  if item['tissue'] in fins:continue
  if item['support'] not in changed:continue
  o=byname[item['tissue']];host=byname[item['support']]
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  for index in ids:
   point,weights=embed(o.matrix_world@o.data.vertices[index].co,host,.001)
   assign(o,index,point,weights)
 for item in old:
  o=byname[item['tissue']];host=byname[item['support']];bvh=prior.tree(host)
  ids=[i for i,v in enumerate(o.data.attributes['attachment_endpoint'].data) if v.value==item['id']]
  assert all(prior.inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 bpy.context.scene['tissue_attachment_contract']=json.dumps(old)
 OUT.mkdir(parents=True,exist_ok=True)
 (OUT/'frill-anatomy.json').write_text(json.dumps(dict(frills=22,attachment_ends=len(old),central_join_roots=22,wall_roots=22,anatomy='pale deep thin curved lamellae radiating outward and down from a short fleshy join; amber tissue inside and underneath',removed_glowing_struts=6),indent=2)+'\n')
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(groups=60,endpoints=len(old),records=old),indent=2)+'\n')
 return trunk
