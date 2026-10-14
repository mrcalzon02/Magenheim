"""Direct standard atlas/export recipe; preserve preceding studies unchanged."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/refine-first-bloom.py').read_text(encoding='utf-8').replace('first-bloom-refinement','rift-titan-notched-r11').replace('creature-models/2026-10-04','creature-models/2026-10-14').replace('crowncap-brute','rift-colossus').replace('6206','1200')
start=s.index('# One uninterrupted hip-to-shoulder trunk');end=s.index('meshes=[o for o in bpy.context.scene.objects',start)
s=s[:start]+"import rift_titan_notched_stone as sculpt\nsculpt.OUT=OUT\ntrunk=sculpt.build(B,arm,record,profile,tube,cap)\n\n"+s[end:]
start=s.index('# Add fine radial gill normals');end=s.index("bpy.ops.object.select_all(action='DESELECT')",start);s=s[:start]+s[end:]
s=s.replace('FirstBloom','RiftTitan').replace('First Bloom','Rift Titan').replace('factor=4.65/height','factor=7.0/height').replace("scene['boss_target_metres']=4.65","scene['boss_target_metres']=7.0").replace("scene['refinement_head_focus']=[0,.20*factor,2.76*factor]","scene['refinement_head_focus']=[0,.14*factor,2.3*factor]")
s=s.replace('width=512,height=512','width=1024,height=1024').replace('path=TEX/(ID+','im.scale(512,512)\n path=TEX/(ID+')
exec(compile(s,str(__file__),'exec'))
out=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-notched-r11';p=out/'refinement-metrics.json';row=json.loads(p.read_text());row.update(previous_id='underworld-boss-rift-titan-layered-r10',previous_triangles=1800,reduction_percent=0,study_height_metres=7.0,rig='original eight-bone hierarchy and seven retained actions; inherited rest fit and arm root blends',changes='stepped head and shoulder contours, actual forearm/shin notches; 32 triangles redistributed from simpler surfaces at unchanged total',texture_bake='retained r10 owned spatial shaders, re-baked at 1024 and condensed to four 512px maps for the new topology',status='unaccepted continued concept match candidate; no 85% claim');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
p=out/'concept-pass.json';row=json.loads(p.read_text());row.update(scope='individual head/shoulder/limb contour modeling and local topology redistribution',remaining='finer stone fragmentation, crest debris, fuller core, directional texture refinement and native acceptance',status='continued unaccepted candidate');p.write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
