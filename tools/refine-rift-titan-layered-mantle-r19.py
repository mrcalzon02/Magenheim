from pathlib import Path
import json,builtins
ROOT=Path(__file__).resolve().parents[1]
def compile(source,filename,mode):
    # The inherited atlas recipe shrank the global UV layout with the head
    # still included. Repack only body faces into their reserved lower strip.
    source=source.replace("component=json.loads(bpy.context.scene['silhouette_components'])['UnifiedCrystalHead']", "component=json.loads(bpy.context.scene['silhouette_components'])['UnifiedCrystalHead']\nlabels=o.data.attributes['attachment_component']\nbpy.context.tool_settings.mesh_select_mode=(False,False,True)\nfor f in o.data.polygons:f.select=labels.data[f.index].value!=component\nbpy.ops.object.mode_set(mode='EDIT');bpy.ops.uv.smart_project(angle_limit=math.radians(60),island_margin=.012);bpy.ops.object.mode_set(mode='OBJECT')")
    return builtins.compile(source,filename,mode)
s=(ROOT/'tools/refine-rift-titan-fracture-planes-r16.py').read_text().replace('rift-titan-fracture-planes-r16','rift-titan-layered-mantle-r19').replace('rift_titan_fracture_planes','rift_titan_layered_mantle').replace('2026-10-16','2026-10-25')
exec(compile(s,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-25/rift-titan-layered-mantle-r19'
for name in ('refinement-metrics','concept-pass'):
    p=out/(name+'.json');row=json.loads(p.read_text());row.pop('reduction_percent',None);row.update(previous_id='underworld-boss-rift-titan-broken-strata-r18',previous_triangles=1800,triangle_delta=0,changes='four leaner staggered outer mantle plates with long front ridges and reduced rear depth, finer discrete stone chips, dark long seams and sparse upward olive lichen',status='intermediate concept refinement; art and native acceptance pending');p.write_text(json.dumps(row,indent=2)+'\n')
