from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];T=ROOT/'tools'
for source,target in [('refine-blackwater-maw-anatomy-r11','refine-blackwater-maw-face-r12'),('verify-blackwater-maw-anatomy-r11-raw','verify-blackwater-maw-face-r12-raw'),('process-blackwater-maw-anatomy-r11','process-blackwater-maw-face-r12'),('review-blackwater-maw-anatomy-r11','review-blackwater-maw-face-r12'),('render-maw-anatomy-closeups','render-maw-face-closeups')]:
 s=(T/(source+'.py')).read_text().replace('blackwater-maw-anatomy-r11','blackwater-maw-face-r12').replace('blackwater_maw_anatomy_r11','blackwater_maw_face_r12')
 (T/(target+'.py')).write_text(s)
print('Prepared face r12')
