"""Widen the joined lower panel and medallion, then add baked membrane detail."""
import bpy,json
from pathlib import Path
from mathutils import Vector
import first_bloom_medallion_arch as previous
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree,inside
from first_bloom_deeper_chest import embed
unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-membranes-r13'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 trunk=previous.build(B,arm,record,profile,tube,cap)
 core=bpy.data.objects['AmberChamberHeart'];center=sum(v.co.x for v in core.data.vertices[8:16])/8
 before_width=max(v.co.x for v in core.data.vertices[8:])-min(v.co.x for v in core.data.vertices[8:])
 for v in core.data.vertices:v.co.x=center+(v.co.x-center)*1.5
 pair=[bpy.data.objects['ChamberRib'+str(s)+'_0'] for s in (-1,1)]
 before_panel=max(v.co.x for o in pair for v in o.data.vertices[5:15])-min(v.co.x for o in pair for v in o.data.vertices[5:15])
 for o in pair:
  for v in o.data.vertices:v.co.x*=3
 records=json.loads(bpy.context.scene['tissue_attachment_contract']);changed={core.name}|{o.name for o in pair}
 byname={o.name:o for o in bpy.context.scene.objects if o.type=='MESH'}
 # Seat the medallion base before all contacts that depend on its new surface.
 ordered=sorted(records,key=lambda r:0 if r['end']=='medallion_recessed_base' else 1)
 for item in ordered:
  if item['tissue'] not in changed and item['support'] not in changed:continue
  o=byname[item['tissue']];host=byname[item['support']]
  ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']]
  for ordinal,index in enumerate(ids):
   point=o.matrix_world@o.data.vertices[index].co
   if host==core:
    interior=core.matrix_world@sum((v.co for v in core.data.vertices[8:]),Vector())/16
    bvh=tree(core)
    for amount in (.04,.08,.16,.32):
     seated=point.lerp(interior,amount)
     if inside(seated,bvh):
      assign(o,index,seated,{'Chest':1.0});break
    else:raise ValueError('expanded join has no seated root')
    continue
   if inside(point,tree(host)):continue
   point,weights=embed(point,host,.003 if ordinal<len(ids)//2 else .006);assign(o,index,point,weights)
 for item in records:
  o=byname[item['tissue']];host=byname[item['support']];bvh=tree(host)
  ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']]
  assert all(inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 overlap=tree(pair[0]).overlap(tree(pair[1]));assert overlap
 join_width=max(v.co.x for v in core.data.vertices[8:])-min(v.co.x for v in core.data.vertices[8:])
 panel_width=max(v.co.x for o in pair for v in o.data.vertices[5:15])-min(v.co.x for o in pair for v in o.data.vertices[5:15])
 assert abs(join_width/before_width-1.5)<1e-5 and abs(panel_width/before_panel-3)<1e-5
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'membrane-detail.json').write_text(json.dumps(dict(join_width_scale=join_width/before_width,lower_panel_width_scale=panel_width/before_panel,lower_panel_increase_percent=200,height_and_depth='retained',arch_intersecting_triangle_pairs=len(overlap),triangles_added=0,texture_detail='shallow tissue folds, fine pores and subtle fibrous pigmentation baked into the existing 512px atlas'),indent=2)+'\n')
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(groups=len(set(i['tissue'] for i in records)),endpoints=len(records),records=records),indent=2)+'\n')
 return trunk

def materials(B):
 previous.materials(B)
 for key in ('ivory','rib-tissue'):
  mat=B.mats[key];nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes['Principled BSDF']
  coord=nodes.new('ShaderNodeTexCoord');wave=nodes.new('ShaderNodeTexWave');wave.wave_type='BANDS';wave.bands_direction='Z';wave.inputs['Scale'].default_value=18;wave.inputs['Distortion'].default_value=5;wave.inputs['Detail Scale'].default_value=1.2;links.new(coord.outputs['Object'],wave.inputs['Vector'])
  folds=nodes.new('ShaderNodeBump');folds.inputs['Strength'].default_value=.30;folds.inputs['Distance'].default_value=.010;links.new(wave.outputs['Color'],folds.inputs['Height'])
  if bs.inputs['Normal'].links:links.new(bs.inputs['Normal'].links[0].from_socket,folds.inputs['Normal'])
  noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=55;noise.inputs['Detail'].default_value=2;links.new(coord.outputs['Object'],noise.inputs['Vector'])
  pores=nodes.new('ShaderNodeBump');pores.inputs['Strength'].default_value=.18;pores.inputs['Distance'].default_value=.0015;links.new(noise.outputs['Fac'],pores.inputs['Height']);links.new(folds.outputs['Normal'],pores.inputs['Normal']);links.new(pores.outputs['Normal'],bs.inputs['Normal'])
  pigment=nodes.new('ShaderNodeValToRGB');pigment.color_ramp.elements[0].color=(.78,.76,.72,1);pigment.color_ramp.elements[1].color=(1,1,.97,1);links.new(wave.outputs['Color'],pigment.inputs[0])
  base=bs.inputs['Base Color'].links[0].from_socket;mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;links.new(base,mix.inputs[1]);links.new(pigment.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],bs.inputs['Base Color'])
