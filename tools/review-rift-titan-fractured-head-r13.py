"""Matched head front/oblique/left-side material and clay plus full silhouettes."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/review-first-bloom-refinement.py').read_text(encoding='utf-8').replace('first-bloom-refinement','rift-titan-fractured-head-r13').replace('creature-models/2026-10-04','creature-models/2026-10-14').replace("row.get('study_height_metres',4.65)","row.get('study_height_metres',7.0)")
start=s.index('for view,direction in [');end=s.index(' target=center;span=size',start)
s=s[:start]+"for view,direction in [('front',(0,2,.06)),('side',(-2,0,.06)),('crown-front',(0,2,0)),('crown-detail',(1,2,.15)),('crown-side',(-2,.65,.05)),('crown-front-clay',(0,2,0)),('crown-side-clay',(-2,.65,.05)),('attack',(1,2,.55))]:\n"+s[end:]
s=s.replace(' target=center;span=size',' target=center;span=size\n if view.startswith("crown-"):target=center+Vector((0,0,size*.30));span=size*.48')
s=s.replace("view=='clay'","view.endswith('-clay')")
s=s.replace(" scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))"," if view in ('side','crown-side','crown-side-clay'):assert cam.rotation_euler.to_matrix().col[0].dot(Vector((0,1,0)))<0, 'front +Y must project screen-left'\n scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))")
s=s.replace('deformation={}','deformation={};layout={}')
s=s.replace(" scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))", " layout[view]=dict(location=list(cam.location),rotation=list(cam.rotation_euler),ortho_scale=cd.ortho_scale,resolution=[scene.render.resolution_x,scene.render.resolution_y])\n scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))")
s+="\n(OUT/'camera-layout.json').write_text(json.dumps(layout,indent=2)+'\\n',encoding='utf-8')\n"
exec(compile(s,str(__file__),'exec'))
