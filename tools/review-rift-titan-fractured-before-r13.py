"""Render saved r12 from exactly the same cameras as the r13 candidate."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
out=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-fractured-head-r13/before';out.mkdir(exist_ok=True)
# Use the completed r13 review's camera positions, without modifying either
# source study or any original reference image.
s=(ROOT/'tools/review-first-bloom-refinement.py').read_text(encoding='utf-8').replace('first-bloom-refinement','rift-titan-fractured-head-r13/before').replace('creature-models/2026-10-04','creature-models/2026-10-14').replace("row.get('study_height_metres',4.65)","row.get('study_height_metres',7.0)")
s=s.replace("row=json.loads((OUT/'refinement-metrics.json').read_text())", "row=json.loads((ROOT/'docs/art/creature-models/2026-10-14/rift-titan-cracked-head-r12/refinement-metrics.json').read_text())\nlayout=json.loads((OUT.parent/'camera-layout.json').read_text())")
# Derive the r13 review's exact actor frame from its recorded full-front
# camera, so lights also use the current frame if head depth changed bounds.
s=s.replace('center=(hi+lo)/2;size=max(hi-lo)', "size=layout['front']['ortho_scale']/1.18;center=Vector(layout['front']['location'])-Vector((0,2,.06))*size*2")
start=s.index('for view,direction in [');end=s.index(' target=center;span=size',start)
s=s[:start]+"for view,direction in [('crown-front',(0,2,0)),('crown-detail',(1,2,.15)),('crown-side',(-2,.65,.05)),('crown-front-clay',(0,2,0)),('crown-side-clay',(-2,.65,.05))]:\n"+s[end:]
s=s.replace("view=='clay'","view.endswith('-clay')")
s=s.replace(" scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))", " cfg=layout[view];cam.location=cfg['location'];cam.rotation_euler=cfg['rotation'];cd.ortho_scale=cfg['ortho_scale'];scene.render.resolution_x,scene.render.resolution_y=cfg['resolution']\n if 'side' in view:assert cam.rotation_euler.to_matrix().col[0].dot(Vector((0,1,0)))<0\n scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))")
s=s[:s.index('data=(OUT/(ID+')]
exec(compile(s,str(__file__),'exec'))
(OUT/'provenance.json').write_text(json.dumps(dict(source=row['source'],source_sha256=hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest(),cameras='exact r13 camera-layout.json transforms and ortho scales',scope='actual saved r12 source; neither source file modified'),indent=2)+'\n',encoding='utf-8')
