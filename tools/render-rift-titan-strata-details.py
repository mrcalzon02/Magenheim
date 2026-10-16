"""Saved-source close-ups using the established review lights and material."""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];slug='rift-titan-broken-strata-r18';ID='underworld-boss-'+slug
out=ROOT/'docs/art/creature-models/2026-10-16'/slug;source='assets/models/studies/'+ID+'.blend'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/source));scene=bpy.context.scene;o=next(o for o in scene.objects if o.type=='MESH');parts=json.loads(scene['silhouette_components']);tag=o.data.attributes['attachment_component'];layout=json.loads((out/'camera-layout.json').read_text())
groups=[('hand-detail',[n for n in parts if n=='PalmL' or n.startswith('FingerStratumL')],(1.2,2,.35),1.45),('foot-detail',['FootL'],(1.1,2,.7),1.9),('crest-detail',[n for n in parts if n.startswith(('BrokenCrest','RaisedCrest','CrestViolet'))],(-2,.35,.15),1.2)]
for name,names,direction,pad in groups:
    labels={parts[n] for n in names};ids={i for f in o.data.polygons if tag.data[f.index].value in labels for i in f.vertices};p=[o.matrix_world@o.data.vertices[i].co for i in ids];lo=Vector(tuple(min(q[k] for q in p) for k in range(3)));hi=Vector(tuple(max(q[k] for q in p) for k in range(3)));center=(lo+hi)/2;span=max(hi-lo)*pad;location=center+Vector(direction)*span*2;rotation=(center-location).to_track_quat('-Z','Y').to_euler()
    layout[name]=dict(location=list(location),rotation=list(rotation),ortho_scale=span,resolution=[900,900])
(out/'camera-layout.json').write_text(json.dumps(layout,indent=2)+'\n')
sys.argv=['blender','--','--study',str(out.relative_to(ROOT)),'--source',source,'--label',ID,'--views',','.join(r[0] for r in groups),'--ground']
s=(ROOT/'tools/render-creature-study-history.py').read_text().replace("out=study/'before'","out=study")
exec(compile(s,str(__file__),'exec'))
