from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];T=ROOT/'tools'
for source,target in [('refine-blackwater-maw-face-r12','refine-blackwater-maw-teeth-eyes-r13'),('verify-blackwater-maw-face-r12-raw','verify-blackwater-maw-teeth-eyes-r13-raw'),('process-blackwater-maw-face-r12','process-blackwater-maw-teeth-eyes-r13'),('review-blackwater-maw-face-r12','review-blackwater-maw-teeth-eyes-r13'),('render-maw-face-closeups','render-maw-teeth-eyes-closeups')]:
 s=(T/(source+'.py')).read_text().replace('blackwater-maw-face-r12','blackwater-maw-teeth-eyes-r13').replace('blackwater_maw_face_r12','blackwater_maw_teeth_eyes_r13')
 (T/(target+'.py')).write_text(s)
print('Prepared teeth and eyes r13')
