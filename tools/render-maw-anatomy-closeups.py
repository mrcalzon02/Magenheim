import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-anatomy-r11'
s=(ROOT/'tools/review-first-bloom-refinement.py').read_text().replace('first-bloom-refinement','blackwater-maw-anatomy-r11')
s=s[:s.index('deformation={}')]
s=s.replace("assert all(e.is_manifold for e in bm.edges),'open/nonmanifold edge'","assert not any(e.is_wire or len(e.link_faces)>2 for e in bm.edges)")
s=s.replace("assert abs(hi.z-lo.z-row.get('study_height_metres',4.65))<.005","assert abs(hi.y-lo.y-row.get('study_length_metres',10))<.005")
exec(compile(s,str(__file__),'exec'))
scale=scene['refinement_head_focus'][1]/2.8
for label,target,direction,span in [('fin-roots',(-1.12,.35,-.40),(-2,.4,.8),3.35),('armor-and-feelers',(-.22,1.75,.45),(-2,1,.8),4.8)]:
 target=Vector(target)*scale;cd.ortho_scale=span*scale;cam.location=target+Vector(direction)*span*scale*2;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
 scene.render.filepath=str(OUT/(ID+'--'+label+'.png'));bpy.ops.render.render(write_still=True)
 print('RENDERED Maw closeup',label,flush=True)
