from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/refine-first-bloom.py').read_text().replace('first-bloom-refinement','carrion-crown-anatomy-r3').replace('creature-models/2026-10-04','creature-models/2026-10-16').replace('crowncap-brute','corpse-orchard').replace('6206','1422')
start=s.index('# One uninterrupted hip-to-shoulder trunk');end=s.index('meshes=[o for o in bpy.context.scene.objects',start)
s=s[:start]+"import carrion_crown_anatomy as sculpt\ntrunk=sculpt.build(B,arm,record,profile,tube,cap)\n\n"+s[end:]
start=s.index('# Add fine radial gill normals');end=s.index("bpy.ops.object.select_all(action='DESELECT')",start);s=s[:start]+s[end:]
s=s.replace('FirstBloom','CarrionCrown').replace('First Bloom','Carrion Crown').replace('factor=4.65/height','factor=6.8/height').replace("scene['boss_target_metres']=4.65","scene['boss_target_metres']=6.8").replace("[0,.20*factor,2.76*factor]","[0,.1*factor,1.65*factor]")
s=s.replace('width=512,height=512','width=1024,height=1024').replace('path=TEX/(ID+','im.scale(512,512)\n path=TEX/(ID+')
exec(compile(s,str(__file__),'exec'))
p=OUT/'refinement-metrics.json';row=json.loads(p.read_text());row.update(previous_id='underworld-boss-carrion-crown-detail-r2',previous_triangles=29212,reduction_percent=(1-row['triangles']/29212)*100,study_height_metres=6.8,rig='original seven-bone Corpse Orchard hierarchy and five retained actions; support-matched branch/root sections',texture_bake='owned directional bark, dark cavity, ivory and amber; supersampled into four packed 512px maps',status='new anatomy candidate; art and native acceptance pending');p.write_text(json.dumps(row,indent=2)+'\n')
