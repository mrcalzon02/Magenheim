from pathlib import Path
import bpy,json
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'assets/models/studies/underworld-boss-blackwater-maw-jaws-r5.blend'))
body_scale=bpy.context.scene['refinement_head_focus'][1]/2.80
source=(ROOT/'tools/refine-blackwater-maw.py').read_text().replace('blackwater-maw-refinement-r3','blackwater-maw-restored-fins-r7-raw').replace('blackwater_maw_refinement','blackwater_maw_fin_restoration')
source=source.replace("source=source.replace('factor=4.65/height','factor=10.0/(max(p.y for p in points)-min(p.y for p in points))')", "source=source.replace('factor=4.65/height','factor='+str(body_scale))")
exec(compile(source,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-restored-fins-r7-raw'
row=json.loads((out/'refinement-metrics.json').read_text());o=next(o for o in bpy.context.scene.objects if o.type=='MESH')
row['study_length_metres']=max(v.co.y for v in o.data.vertices)-min(v.co.y for v in o.data.vertices)
(out/'refinement-metrics.json').write_text(json.dumps(row,indent=2)+'\n')
