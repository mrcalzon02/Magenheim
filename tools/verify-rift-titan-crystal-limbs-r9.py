from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
_r9_joint_verifier_source=(ROOT/'tools/verify-rift-titan-joints-r8.py').read_text(encoding='utf-8').replace('rift-titan-joints-r8','rift-titan-crystal-limbs-r9').replace('len(records)==58','len(records)==74').replace('full 58-contact','full 74-contact').replace('contacts_across_actions=58','contacts_across_actions=74')
exec(compile(_r9_joint_verifier_source,str(__file__),'exec'))
# Independently audit the new crystal basal faces in the saved animated mesh.
roots=[r for r in records if r['end']=='limb-crystal-root'];assert len(roots)==16
groups={g.index:g.name for g in obj.vertex_groups}
for r in roots:
 vertices=[v for v in obj.data.vertices if obj.data.attributes['attachment_endpoint'].data[v.index].value==r['id']]
 expected=r['support'].replace('LongFracture','')[:-1]
 assert len(vertices)==(6 if r['tissue'].startswith('FloatingPrism') else 4)
 assert all({groups[w.group]:round(w.weight,6) for w in v.groups if w.weight>1e-7}=={expected:1.0} for v in vertices)
closing=_r9_joint_verifier_source[_r9_joint_verifier_source.index('from mathutils.bvhtree import BVHTree'):]
closing=closing.replace('assert len(caps)==4,r','assert len(caps)==len(ids)-1,r')
closing=closing.replace("'closing-face-verification.json'","'crystal-face-verification.json'").replace('root_groups=8,closing_triangles_per_group=4','root_groups=16,closing_triangles_total=56')
closing=closing.replace("'VERIFIED closing-face interiors: 32 triangles,'","'VERIFIED crystal basal-face interiors: 56 triangles,'")
exec(compile(closing,str(__file__),'exec'))
crystal_report=json.loads((out/'crystal-face-verification.json').read_text());assert crystal_report['result']=='PASS' and crystal_report['sampled_points']==14112 and crystal_report['poses']==36
author=json.loads((out/'limb-authoring.json').read_text());assert author['budget']==dict(binding_before=120,binding_after=48,freed_triangles=72,new_crystals=72,total=1800)
counts=json.loads((out/'refinement-metrics.json').read_text())['category_triangles'];assert counts['bindings']==48 and counts['binding-crystals']==172
(out/'limb-verification.json').write_text(json.dumps(dict(result='PASS',new_shards=12,repurposed_shards=4,attached_limb_growths=16,contacts=74,budget=author['budget'],weights='sixteen complete basal sections share their respective Arm, Forearm or Leg host weights',basal_faces=56,crystal_point_samples=14112,poses=36,scope='saved topology, rig ownership and crystal seatings; native every-frame acceptance separate'),indent=2)+'\n',encoding='utf-8')
print('VERIFIED limb crystals, condensed bindings and unchanged 1800 triangle budget')
