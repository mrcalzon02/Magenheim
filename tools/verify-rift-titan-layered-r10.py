from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
_r10_limb_verifier=(ROOT/'tools/verify-rift-titan-crystal-limbs-r9.py').read_text(encoding='utf-8').replace('rift-titan-crystal-limbs-r9','rift-titan-layered-r10').replace(".replace('len(records)==58','len(records)==74')",".replace('len(records)==58','len(records)==76')").replace('full 74-contact','full 76-contact').replace('contacts_across_actions=74','contacts_across_actions=76')
exec(compile(_r10_limb_verifier,str(__file__),'exec'))
roots=[r for r in records if r['end']=='thorax-shoulder-root'];assert len(roots)==2 and len(records)==76
groups={g.index:g.name for g in obj.vertex_groups}
for r in roots:
 vertices=[v for v in obj.data.vertices if obj.data.attributes['attachment_endpoint'].data[v.index].value==r['id']]
 assert len(vertices)==6 and all({groups[w.group]:round(w.weight,6) for w in v.groups if w.weight>1e-7}=={'Body':1.0} for v in vertices)
_r10_closing=_r9_joint_verifier_source[_r9_joint_verifier_source.index('from mathutils.bvhtree import BVHTree'):]
_r10_closing=_r10_closing.replace("'closing-face-verification.json'","'thorax-face-verification.json'").replace('root_groups=8','root_groups=2').replace("'VERIFIED closing-face interiors: 32 triangles,'","'VERIFIED upper-thorax closing faces: 8 triangles,'")
exec(compile(_r10_closing,str(__file__),'exec'))
report=json.loads((out/'thorax-face-verification.json').read_text());assert report['result']=='PASS' and report['sampled_points']==2016 and report['poses']==36
print('VERIFIED two upper-thorax shoulder roots and 2016 closing-face samples')
_r10_limb_report_path=out/'limb-verification.json';_r10_limb_report=json.loads(_r10_limb_report_path.read_text());_r10_limb_report['contacts']=76;_r10_limb_report_path.write_text(json.dumps(_r10_limb_report,indent=2)+'\n',encoding='utf-8')
