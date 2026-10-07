"""Use the pilot's validated bake/export path with individualized concept anatomy."""
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools'))
source=(ROOT/'tools/refine-first-bloom.py').read_text()
source=source.replace('first-bloom-refinement','first-bloom-concept-r3')
start=source.index('# One uninterrupted hip-to-shoulder trunk')
end=source.index('meshes=[o for o in bpy.context.scene.objects',start)
source=source[:start]+"from first_bloom_concept_sculpt import build,materials,unwrap_atlas\ntrunk=build(B,arm,record,profile,tube,cap)\nmaterials(B)\n\n"+source[end:]
# Fine gill function from the pilot is irrelevant to the amber portal. Its
# closed ribs are sculpted explicitly; preserve the bespoke shader normals.
start=source.index('# Add fine radial gill normals')
end=source.index("bpy.ops.object.select_all(action='DESELECT')",start)
source=source[:start]+source[end:]
source=source.replace("bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(angle_limit=math.radians(70),island_margin=.012);bpy.ops.object.mode_set(mode='OBJECT')","unwrap_atlas(o)")
source=source.replace('scene.render.bake.margin=4','scene.render.bake.margin=2')
source=source.replace("scene['refinement_head_focus']=[0,.20*factor,2.76*factor]","scene['refinement_head_focus']=[0,.20*factor,2.22*factor]")
source=source.replace("bs.inputs['Emission Strength'].default_value=.65","bs.inputs['Emission Strength'].default_value=2.5")
source=source.replace("previous_triangles=91516","previous_triangles=7940")
source=source.replace("(1-triangles/91516)*100","(1-triangles/7940)*100")
source=source.replace("texture_bake='self material bake from owned r2 maps plus radial gill normals; no high-to-low geometry transfer'","texture_bake='owned procedural bark, moss, ivory and amber material bake; no concept pixels or high-to-low geometry transfer'")
exec(compile(source,str(ROOT/'tools/refine-first-bloom.py'),'exec'),{'__file__':str(ROOT/'tools/refine-first-bloom.py'),'__name__':'__main__'})
