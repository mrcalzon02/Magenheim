"""Check actual saved fin components and compare tail fan area with revision 5."""
import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-restored-fins-r7'
def measurements(revision):
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies'/('underworld-boss-'+revision+'.blend')))
 o=next(o for o in bpy.context.scene.objects if o.type=='MESH');me=o.data;me.calc_loop_triangles();records=json.loads(bpy.context.scene['tissue_attachment_contract']);results={}
 for item in [r for r in records if r['end'] in ('dorsal_root','pectoral_root','forward_pectoral_root','tail_root')]:
  roots={i for i,a in enumerate(me.attributes['attachment_endpoint'].data) if a.value==item['id']};assert roots
  component_ids={me.attributes['attachment_component'].data[p.index].value for p in me.polygons if roots.intersection(p.vertices)};assert len(component_ids)==1
  component=next(iter(component_ids));triangles=[t for t in me.loop_triangles if me.attributes['attachment_component'].data[t.polygon_index].value==component]
  ids=set(i for t in triangles for i in t.vertices);coords=[o.matrix_world@me.vertices[i].co for i in ids]
  lo=[min(p[a] for p in coords) for a in range(3)];hi=[max(p[a] for p in coords) for a in range(3)];area=0
  for t in triangles:
   a,b,c=[o.matrix_world@me.vertices[i].co for i in t.vertices];area+=abs((b.y-a.y)*(c.z-a.z)-(b.z-a.z)*(c.y-a.y))*.5
  results[item['tissue']]=dict(kind=item['end'],dimensions=[b-a for a,b in zip(lo,hi)],projected_yz_membrane_area=area*.5,triangles=len(triangles))
 return results
old=measurements('blackwater-maw-jaws-r5');new=measurements('blackwater-maw-restored-fins-r7')
assert len([v for v in new.values() if v['kind']=='dorsal_root'])==4
assert len([v for v in new.values() if v['kind']=='forward_pectoral_root'])==2
assert len([v for v in new.values() if v['kind']=='pectoral_root'])==2
assert 'BroadCaudalFan' in new
ratio=new['BroadCaudalFan']['projected_yz_membrane_area']/old['ForkedCaudalFan']['projected_yz_membrane_area']
assert ratio>3,'tail still reads as a narrow fork'
report=dict(result='PASS saved fin inventory and broad tail area',dorsals=4,lateral_pairs=2,tail_fan_area_ratio_to_revision5=ratio,previous_tail=old['ForkedCaudalFan'],components=new,scope='actual saved mesh components and projected membrane area; native animation acceptance pending')
(OUT/'fin-inventory-verification.json').write_text(json.dumps(report,indent=2)+'\n')
print('VERIFIED fin inventory: four dorsals, two lateral pairs, broad tail area ratio',ratio,flush=True)
