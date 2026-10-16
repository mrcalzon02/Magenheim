from pathlib import Path
import sys,json
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from maw_contact_validation import validation_source
s=(ROOT/'tools/verify-blackwater-maw-contacts.py').read_text().replace('blackwater-maw-refinement-r3','carrion-crown-anatomy-r3').replace('creature-models/2026-10-04','creature-models/2026-10-16')
exec(compile(validation_source(s),str(__file__),'exec'))
# Reuse the independent saved closing-face sampler, not the authoring helper.
s=(ROOT/'tools/verify-rift-titan-concept-body-r17.py').read_text()
start=s.index('samples=');end=s.index('report=dict(result=',start)
obj=o;parts=json.loads(scene['silhouette_components']);records=json.loads(scene['tissue_attachment_contract'])
from mathutils.bvhtree import BVHTree
from mathutils import Vector
from maw_contact_validation import contains
import numpy as np
exec(compile(s[start:end],str(__file__),'exec'))
assert len(arm.data.bones)==7 and len(str(scene['magenheim_authored_actions']).split(','))==5
assert len([n for n in parts if n.startswith('GraftedFlower')])==4
assert len([n for n in parts if n.startswith('HangingAmberPod')])==6
report=dict(result='PASS' if not gaps else 'FAIL',contacts=len(records),closing_face_samples=total,closing_faces_by_contact=counts,poses=len(samples),outside_samples=gaps,bones=7,actions=5,scope='saved source support, cap interiors and retained actions sampled offline; art and native animation acceptance pending')
(OUT/'anatomy-verification.json').write_text(json.dumps(report,indent=2)+'\n');assert not gaps,gaps[:5]
print('VERIFIED Carrion Crown saved roots, cup teeth and pod supports')
