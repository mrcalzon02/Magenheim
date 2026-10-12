from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/refine-first-bloom.py').read_text(encoding='utf-8').replace('first-bloom-refinement','rift-titan-primary-r3').replace('creature-models/2026-10-04','creature-models/2026-10-12').replace('crowncap-brute','rift-colossus').replace('6206','1200')
start=s.index('# One uninterrupted hip-to-shoulder trunk');end=s.index('meshes=[o for o in bpy.context.scene.objects',start)
s=s[:start]+"from rift_titan_primary_refinement import build\ntrunk=build(B,arm,record,profile,tube,cap)\n\n"+s[end:]
start=s.index('# Add fine radial gill normals');end=s.index("bpy.ops.object.select_all(action='DESELECT')",start);s=s[:start]+s[end:]
s=s.replace('FirstBloom','RiftTitan').replace('First Bloom','Rift Titan').replace('factor=4.65/height','factor=7.0/height').replace("scene['boss_target_metres']=4.65","scene['boss_target_metres']=7.0").replace("scene['refinement_head_focus']=[0,.20*factor,2.76*factor]","scene['refinement_head_focus']=[0,.14*factor,2.3*factor]")
s=s.replace('previous_triangles=91516','previous_triangles=16376').replace('(1-triangles/91516)','(1-triangles/16376)')
s=s.replace("rig='retained crowncap-brute baseline; mixed weights at trunk and limb roots'","rig='retained Rift Colossus hierarchy and actions; rigid fragments'").replace("texture_bake='self material bake from owned r2 maps plus radial gill normals; no high-to-low geometry transfer'","texture_bake='owned r2 strata and crystal materials baked to four 512px maps; primary-form review'")
s=s.replace('width=512,height=512','width=1024,height=1024').replace('path=TEX/(ID+','im.scale(512,512)\n path=TEX/(ID+')
exec(compile(s,str(__file__),'exec'))
