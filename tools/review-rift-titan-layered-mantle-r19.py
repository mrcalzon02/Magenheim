from pathlib import Path
import builtins
ROOT=Path(__file__).resolve().parents[1]
def compile(source,filename,mode):
    source=source.replace("('front',(0,2,.06)),('side',(-2,0,.06))", "('front',(0,2,.06)),('front-clay',(0,2,.06)),('side',(-2,0,.06)),('side-clay',(-2,0,.06)),('three-quarter',(1,2,.15))")
    if 'cd=bpy.data.cameras.new' in source:
        source=source.replace('cd=bpy.data.cameras.new',"bpy.ops.mesh.primitive_plane_add(size=size*4,location=(center.x,center.y,lo.z-.002))\nground=bpy.context.object;ground.name='Review ground only';gm=bpy.data.materials.new('Ground charcoal');gm.use_nodes=True;gm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.025,.03,.035,1);ground.data.materials.append(gm)\ncd=bpy.data.cameras.new")
    return builtins.compile(source,filename,mode)
s=(ROOT/'tools/review-rift-titan-fractured-head-r13.py').read_text().replace('rift-titan-fractured-head-r13','rift-titan-layered-mantle-r19').replace('2026-10-14','2026-10-25')
exec(compile(s,str(__file__),'exec'))
