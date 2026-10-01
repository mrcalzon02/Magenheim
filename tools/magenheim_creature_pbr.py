"""Bind authored creature anatomy to its committed species-specific PBR source maps."""
from pathlib import Path

def bind(bpy,material,species,stem):
    root=Path(__file__).resolve().parents[1]/'assets/textures/underworld/creatures'/species
    nodes=material.node_tree.nodes;links=material.node_tree.links
    bs=nodes.get('Principled BSDF');uv=nodes.new('ShaderNodeTexCoord')
    for kind,socket in (('albedo','Base Color'),('roughness','Roughness'),('normal',None),('emission','Emission Color')):
        path=root/(stem+'-'+kind+'.png')
        if kind=='emission' and not path.is_file():continue
        if not path.is_file():raise RuntimeError('Missing creature PBR source: '+str(path))
        image=bpy.data.images.load(str(path),check_existing=True);image.pack()
        image.colorspace_settings.name='sRGB' if kind in ('albedo','emission') else 'Non-Color'
        tex=nodes.new('ShaderNodeTexImage');tex.image=image;links.new(uv.outputs['UV'],tex.inputs['Vector'])
        if kind=='normal':
            normal=nodes.new('ShaderNodeNormalMap');links.new(tex.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],bs.inputs['Normal'])
        else:links.new(tex.outputs['Color'],bs.inputs[socket])
        if kind=='emission':bs.inputs['Emission Strength'].default_value=.45
