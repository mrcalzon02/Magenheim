"""Use the established single-atlas bake/export path for individual Maw anatomy."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom.py').read_text().replace('first-bloom-refinement','blackwater-maw-refinement-r3').replace('crowncap-brute','deep-hunter').replace('6206','4872')
start=source.index('# One uninterrupted hip-to-shoulder trunk');end=source.index('meshes=[o for o in bpy.context.scene.objects',start)
source=source[:start]+"from blackwater_maw_refinement import build\ntrunk=build(B,arm,record,profile,tube,cap)\n\n"+source[end:]
start=source.index('# Add fine radial gill normals');end=source.index("bpy.ops.object.select_all(action='DESELECT')",start);source=source[:start]+source[end:]
source=source.replace('FirstBloom','BlackwaterMaw').replace('First Bloom','Blackwater Maw')
source=source.replace('factor=4.65/height','factor=10.0/(max(p.y for p in points)-min(p.y for p in points))')
source=source.replace("scene['boss_target_metres']=4.65", "scene['boss_target_metres']=10.0")
source=source.replace("scene['refinement_head_focus']=[0,.20*factor,2.76*factor]", "scene['refinement_head_focus']=[0,2.80*factor,.10*factor]")
source=source.replace('previous_triangles=91516','previous_triangles=31412').replace('(1-triangles/91516)','(1-triangles/31412)')
source=source.replace("texture_bake='self material bake from owned r2 maps plus radial gill normals; no high-to-low geometry transfer'", "texture_bake='owned procedural scales, cracked cheek armor, fleshy mouth folds and cyan light organs baked to one 512px atlas'")
source=source.replace('island_margin=.012','island_margin=.004').replace('scene.render.bake.margin=4','scene.render.bake.margin=2')
source=source.replace("bs.inputs['Emission Strength'].default_value=.65","bs.inputs['Emission Strength'].default_value=1.4")
source=source.replace('width=512,height=512','width=1024,height=1024')
source=source.replace('scene.render.bake.margin=2','scene.render.bake.margin=4')
source=source.replace('path=TEX/(ID+','im.scale(512,512)\n path=TEX/(ID+')
exec(compile(source,str(__file__),'exec'))
