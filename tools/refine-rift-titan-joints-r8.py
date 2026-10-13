from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-rift-titan-silhouette-r7.py').read_text(encoding='utf-8').replace('rift-titan-silhouette-r7','rift-titan-joints-r8').replace('rift_titan_silhouette_fit','rift_titan_joint_silhouette')
exec(compile(source,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-13/rift-titan-joints-r8';p=out/'refinement-metrics.json';row=json.loads(p.read_text())
row.update(previous_id='underworld-boss-rift-titan-silhouette-r7',changes='sloped shoulder mantle, 3D-aligned arm plates, elbow/wrist profile, closer leg stance and attached shorter fingers',rig='original eight-bone hierarchy and seven actions; continued rest fit; sparse blended proximal arm roots',status='continued unaccepted front/left-side silhouette candidate; texture detail deferred')
p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
concept=json.loads((out/'concept-pass.json').read_text());concept.update(scope='continue frontal and left-facing silhouette, with shoulder/arm/digit support refinement; surface detail deferred',anatomy='steeper layered shoulder stones, 3D-aligned arm armor, backward elbows and forward wrists, closer leg stance, attached shorter digits and thicker instep',preserved='1800 triangles, one material, four 512px maps, original eight-bone hierarchy and seven actions; both torso rings and all body supports retained',status='continued silhouette candidate; user says still far off, not adequately reworked')
(out/'concept-pass.json').write_text(json.dumps(concept,indent=2)+'\n',encoding='utf-8')
torso=json.loads((out/'torso-authoring.json').read_text());torso['scope']='r7 torso ring/cage fit retained; body roots reseated into reshaped shoulders; eight additional arm and finger roots'
(out/'torso-authoring.json').write_text(json.dumps(torso,indent=2)+'\n',encoding='utf-8')
