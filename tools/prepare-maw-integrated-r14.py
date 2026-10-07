from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];T=ROOT/'tools'
for source,target in [('refine-blackwater-maw-teeth-eyes-r13','refine-blackwater-maw-integrated-r14'),('verify-blackwater-maw-teeth-eyes-r13-raw','verify-blackwater-maw-integrated-r14-raw'),('process-blackwater-maw-teeth-eyes-r13','process-blackwater-maw-integrated-r14'),('review-blackwater-maw-teeth-eyes-r13','review-blackwater-maw-integrated-r14'),('render-maw-teeth-eyes-closeups','render-maw-integrated-closeups')]:
 s=(T/(source+'.py')).read_text().replace('blackwater-maw-teeth-eyes-r13','blackwater-maw-integrated-r14').replace('blackwater_maw_teeth_eyes_r13','blackwater_maw_integrated_r14')
 if source.startswith('render'):
  start=s.index('for label,target,direction,span in ');end=s.index(':\n target=',start)
  s=s[:start]+"for label,target,direction,span in [('fin-assembly',(-1.35,.35,-.65),(-2,.6,.8),3.8),('armor-and-feelers',(-.22,1.75,.45),(-2,1,.8),4.8)]"+s[end:]
 (T/(target+'.py')).write_text(s)
print('Prepared continuous fin assembly revision')
