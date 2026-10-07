import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-spined-fins-r8'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-blackwater-maw-spined-fins-r8.blend'))
o=next(o for o in bpy.context.scene.objects if o.type=='MESH');me=o.data;records=json.loads(bpy.context.scene['tissue_attachment_contract']);results=[]
for item in [r for r in records if r['end'] in ('dorsal_root','forward_pectoral_root','pelvic_root')]:
 roots={i for i,a in enumerate(me.attributes['attachment_endpoint'].data) if a.value==item['id']}
 components={me.attributes['attachment_component'].data[p.index].value for p in me.polygons if roots.intersection(p.vertices)};assert len(components)==1
 component=next(iter(components));ids={i for p in me.polygons if me.attributes['attachment_component'].data[p.index].value==component for i in p.vertices}
 tip=max((me.vertices[i].co for i in ids),key=lambda p:p.z);leading=max((me.vertices[i].co for i in roots),key=lambda p:p.y)
 r=dict(part=item['tissue'],kind=item['end'],root_mean_y=sum(me.vertices[i].co.y for i in roots)/len(roots))
 if item['end']=='dorsal_root':r['tip_y_relative_to_leading_root']=tip.y-leading.y;assert r['tip_y_relative_to_leading_root']<-.10
 results.append(r)
front=sum(r['root_mean_y'] for r in results if r['kind']=='forward_pectoral_root')/2;rear=sum(r['root_mean_y'] for r in results if r['kind']=='pelvic_root')/2
assert front-rear>2,'paired fins are still stacked in the same body region'
(OUT/'fin-direction-verification.json').write_text(json.dumps(dict(result='PASS dorsals sweep tailward; pectoral and pelvic roots are separate',paired_root_separation_metres=front-rear,parts=results),indent=2)+'\n')
print('VERIFIED tailward dorsal tips and distinct pelvic roots',front-rear,'metres',flush=True)
