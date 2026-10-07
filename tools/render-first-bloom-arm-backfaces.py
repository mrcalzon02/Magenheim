"""Highlight any visible backfaces in the repaired shoulder close-ups."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/inspect-first-bloom-arm-joins.py').read_text().replace("[('before','first-bloom-bundles-r21'),('untrimmed','first-bloom-bundles-r21-raw')]", "[('backfaces','first-bloom-arm-seams-r22')]")
source=source.replace('o.data.materials[0]=mat', "o.data.materials[0]=mat\n nodes=mat.node_tree.nodes;links=mat.node_tree.links;geo=nodes.new('ShaderNodeNewGeometry');mix=nodes.new('ShaderNodeMixRGB');mix.inputs[1].default_value=(.30,.32,.34,1);mix.inputs[2].default_value=(1,0,0,1);links.new(geo.outputs['Backfacing'],mix.inputs[0]);links.new(mix.outputs[0],nodes['Principled BSDF'].inputs['Base Color'])")
exec(compile(source,str(__file__),'exec'))
