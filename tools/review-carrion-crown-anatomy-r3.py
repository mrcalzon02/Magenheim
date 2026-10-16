from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/review-first-bloom-refinement.py').read_text().replace('first-bloom-refinement','carrion-crown-anatomy-r3').replace('creature-models/2026-10-04','creature-models/2026-10-16').replace("row.get('study_height_metres',4.65)","row.get('study_height_metres',6.8)")
s=s.replace("('side',(2,0,.06))","('side',(-2,0,.06)),('front-clay',(0,2,.06)),('side-clay',(-2,0,.06))")
s=s.replace("view=='clay'","(view=='clay' or view.endswith('-clay'))")
s=s.replace('deformation={}','deformation={};layout={}')
s=s.replace(" scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))", " layout[view]=dict(location=list(cam.location),rotation=list(cam.rotation_euler),ortho_scale=cd.ortho_scale,resolution=[scene.render.resolution_x,scene.render.resolution_y])\n scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))")
s+="\n(OUT/'camera-layout.json').write_text(json.dumps(layout,indent=2)+'\\n')\n"
exec(compile(s,str(__file__),'exec'))
