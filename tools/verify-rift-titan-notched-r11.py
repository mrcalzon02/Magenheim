"""Run support, cap, hierarchy and compact export checks on the saved r11 mesh."""
from pathlib import Path
import sys,json
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from maw_contact_validation import validation_source
_r11_contacts=(ROOT/'tools/verify-blackwater-maw-contacts.py').read_text(encoding='utf-8').replace('blackwater-maw-refinement-r3','rift-titan-notched-r11').replace('creature-models/2026-10-04','creature-models/2026-10-14')
exec(compile(validation_source(_r11_contacts),str(__file__),'exec'))
_r11_torso=(ROOT/'tools/verify-rift-titan-wrap-r6.py').read_text(encoding='utf-8');_r11_torso=_r11_torso[_r11_torso.index('import json'):].replace('rift-titan-wrap-r6','rift-titan-notched-r11').replace('creature-models/2026-10-13','creature-models/2026-10-14')
exec(compile(_r11_torso,str(__file__),'exec'))
_r11_proportions=(ROOT/'tools/verify-rift-titan-silhouette-r7.py').read_text(encoding='utf-8');_r11_proportions=_r11_proportions[_r11_proportions.index('import json'):].replace('rift-titan-silhouette-r7','rift-titan-notched-r11').replace('creature-models/2026-10-13','creature-models/2026-10-14')
exec(compile(_r11_proportions,str(__file__),'exec'))
_r11_joint=(ROOT/'tools/verify-rift-titan-joints-r8.py').read_text(encoding='utf-8');_r11_joint=_r11_joint[_r11_joint.index("records=json.loads(scene['tissue_attachment_contract'])"):].replace('len(records)==58','len(records)==76').replace('full 58-contact','full 76-contact').replace('contacts_across_actions=58','contacts_across_actions=76')
exec(compile(_r11_joint,str(__file__),'exec'))
_r9_joint_verifier_source=_r11_joint
_r11_crystals=(ROOT/'tools/verify-rift-titan-crystal-limbs-r9.py').read_text(encoding='utf-8');_r11_crystals=_r11_crystals[_r11_crystals.index('# Independently audit'):]
exec(compile(_r11_crystals,str(__file__),'exec'))
_r11_thorax=(ROOT/'tools/verify-rift-titan-layered-r10.py').read_text(encoding='utf-8');_r11_thorax=_r11_thorax[_r11_thorax.index("roots=[r for r in records if r['end']=='thorax-shoulder-root']"):]
exec(compile(_r11_thorax,str(__file__),'exec'))
author=json.loads((out/'notch-authoring.json').read_text());assert len(author['changes'])==19 and author['net_triangle_change']==0 and author['triangles_transferred']==32
me=obj.data;components=me.attributes['attachment_component'];actual={}
for item in author['changes']:
 component=parts[item['name']];faces=[f for f in me.polygons if components.data[f.index].value==component];ids={i for f in faces for i in f.vertices}
 assert len(faces)==item['after'] and len(ids)==item['vertices'],item
 edges={}
 for f in faces:
  for a,b in zip(f.vertices,(*f.vertices[1:],f.vertices[0])):
   e=tuple(sorted((a,b)));edges[e]=edges.get(e,0)+1
 assert all(n==2 for n in edges.values()) and len(ids)-len(edges)+len(faces)==2,item
 actual[item['name']]=dict(triangles=len(faces),vertices=len(ids),closed_edges=True,euler_characteristic=2)
(out/'notch-verification.json').write_text(json.dumps(dict(result='PASS',remodeled_components=actual,triangles_transferred=32,net_triangle_change=0,contacts=76,scope='saved individual closed shells and triangle redistribution; all original supported endpoint and basal-face checks retained'),indent=2)+'\n',encoding='utf-8')
print('VERIFIED 19 remodeled closed stone shells and 32 redistributed triangles at unchanged total')
