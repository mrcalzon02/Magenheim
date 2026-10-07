from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
T=ROOT/'tools'
s=(T/'blackwater_maw_blade_fins.py').read_text().replace('blackwater-maw-blades-r9','blackwater-maw-left-facing-r10')
s=s.replace("records=json.loads", "# Lower, tailward dorsal silhouette; preserve seated roots.\n for name in ('AnteriorDorsalFin','MainSecondDorsalFin','AftDorsalFin','CaudalDorsalFin'):\n  fin=bpy.data.objects[name]\n  for v in fin.data.vertices:\n   row=(v.index%52)//13;k=v.index%13;t=(0,.32,.70,1)[row];u=k/12\n   if row:\n    v.co.y-=.82*(1-u)**.8*t\n    rootz=fin.data.vertices[(v.index//52)*52+k].co.z\n    v.co.z=rootz+(v.co.z-rootz)*.72\n  fin.data.update()\n records=json.loads",1)
# Two separate body attachments: upper rear-swept blade and lower forward blade.
start=s.index("  joint=tube(");end=s.index("  paths=",start)
block=s[start:end]
block=block.replace("  joint=tube", "  def make_joint(name,points):\n   nonlocal next_component\n   joint=tube",1)
block=block.replace("'FleshyFinJoint'+side,[(s*.80,1.10,-.13),(s*1.08,.96,-.23),(s*1.19,.68,-.34),(s*1.26,.52,-.44)]", "name,points")
lines=block.splitlines()
# The first three lines already have helper indentation; indent the remaining body.
block='\n'.join(lines[:3]+[' '+line for line in lines[3:]])+'\n   return joint\n'
block+="  upper=make_joint('UpperFinJoint'+side,[(s*.80,.90,-.13),(s*1.08,.72,-.23),(s*1.26,.38,-.34),(s*1.40,.16,-.44)])\n  lower=make_joint('LowerFinJoint'+side,[(s*.72,1.36,-.43),(s*.98,1.20,-.56),(s*1.10,1.00,-.68),(s*1.18,.82,-.76)])\n"
s=s[:start]+block+s[end:]
ps=s.index('  paths=');pe=s.index('  for name,path',ps)
s=s[:ps]+"  paths=[([(s*1.10,1.00,-.68),(s*1.36,.64,-.91),(s*1.62,.21,-1.14),(s*1.85,-.27,-1.27),(s*2.03,-.66,-1.30),(s*2.12,-.88,-1.29)],[.05,.20,.36,.34,.18,.003],Vector((0,0,1))),([(s*1.26,.38,-.34),(s*1.47,-.05,-.48),(s*1.70,-.54,-.60),(s*1.89,-1.05,-.74),(s*1.98,-1.51,-.87),(s*1.98,-1.74,-.92)],[.04,.15,.22,.21,.11,.003],Vector((0,0,1)))]\n"+s[pe:]
s=s.replace("   o=bpy.data.objects[name];", "   joint=lower if name.startswith('Forward') else upper\n   o=bpy.data.objects[name];")
s=s.replace("joint=joint.name,blades=", "joints=[lower.name,upper.name],blades=")
s=s.replace('shared fleshy base','separate lower-forward and upper-rear body attachments')
(T/'blackwater_maw_left_facing.py').write_text(s)
for source,target in [('refine-blackwater-maw-blade-fins','refine-blackwater-maw-left-facing'),('verify-blackwater-maw-blade-fins-raw','verify-blackwater-maw-left-facing-raw'),('process-blackwater-maw-blade-fins','process-blackwater-maw-left-facing')]:
 s=(T/(source+'.py')).read_text().replace('blackwater-maw-blades-r9','blackwater-maw-left-facing-r10').replace('blackwater_maw_blade_fins','blackwater_maw_left_facing').replace('7480','7600')
 (T/(target+'.py')).write_text(s)
# Flatten the render wrapper so all review angles consistently show the left-facing fish.
s=(T/'review-first-bloom-trim.py').read_text().replace('first-bloom-trim-r14','blackwater-maw-left-facing-r10')
s=s.replace("source=source.replace(\"assert all(e.is_manifold", "source=source.replace(\"assert abs(hi.z-lo.z-row.get('study_height_metres',4.65))<.005\",\"assert abs(hi.y-lo.y-row.get('study_length_metres',10))<.005\")\nsource=source.replace('(1,2,.55)','(-1,2,.55)').replace('(2,0,.06)','(-2,0,.06)').replace('(1,2,.35)','(-1,2,.35)')\nsource=source.replace(\"assert all(e.is_manifold")
(T/'review-blackwater-maw-left-facing.py').write_text(s)
print('Prepared left-facing review and separate fin attachments')
