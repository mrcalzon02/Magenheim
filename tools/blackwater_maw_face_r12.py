"""Smaller mouth mass, low armor and independently spread sensory feelers."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-face-r12-raw'
source=(ROOT/'tools/blackwater_maw_anatomy_r11.py').read_text().replace('blackwater-maw-anatomy-r11-raw','blackwater-maw-face-r12-raw')
source=source.replace("v.co.x*=1-.10*t;v.co.z*=1-(.32 if v.co.z>0 else .12)*t\n   v.co.y-=.10*t*(v.co.y-1.70)","v.co.x*=1-.22*t\n   oldz=v.co.z*(1-(.32 if v.co.z>0 else .12)*t)\n   v.co.z=oldz*(1-t)+(.65+(oldz-.65)*.76)*t\n   if v.co.z>.55 and v.co.y>1.65:v.co.z=.55+.16*math.tanh((v.co.z-.55)/.16)\n   v.co.y-=.26*t*(v.co.y-1.70)")
# Socket coordinates follow the reduced head, rather than leaving eyes behind.
source=source.replace("((2.50,.39,1.25),(2.34,.23,1.50),(2.55,.10,1.0),(2.18,.37,.9))","((2.34,.45,1.15),(2.19,.31,1.35),(2.37,.20,.95),(2.04,.43,.85))")
# Keep the mouth brow low, with no raised sail at the rear skull/crown.
source=source.replace("[3.17,2.98,2.73,2.50,2.29]","[2.94,2.76,2.52,2.33,2.14]")
source=source.replace("[.025,.055,.075,.055,.015]","[.012,.025,.030,.025,.012]")
source=source.replace("[0,s*.18,s*.40,s*.62,s*.72]","[0,s*.14,s*.31,s*.50,s*.59]")
source=source.replace("[.025,.17,.25,.18,.025]","[.015,.035,.050,.035,.010]")
old="y=1.14-.23*k;z=.66-.16*k;x=side*(.58+.10*k)\n   o=tube('CrownFeeler'+str(side)+'_'+str(k),[(x,y,z),(side*(.99+.12*k),y-.30,z-.05),(side*(1.24+.18*k),y-.65,z-.22),(side*(1.38+.22*k),y-.83,z-.48)],[.030,.024,.015,.027],'bone-armor','Spine_1',6,'sensory')"
new="y,z,x,ty,tz=((1.12,.65,.72,-.15,.62),(.87,.36,.87,-.55,.28),(.62,.02,.93,-.95,-.06))[k];x*=side\n   tip=Vector((side*(1.46+.14*k),ty,tz))\n   start=Vector((x,y,z))\n   points=[start,start.lerp(tip,.30)+Vector((side*.08,0,.10)),start.lerp(tip,.68)+Vector((side*.05,.03,.06)),tip]\n   o=tube('CrownFeeler'+str(side)+'_'+str(k),points,[.026,.021,.014,.027],'bone-armor','Spine_1',6,'sensory')"
assert old in source;source=source.replace(old,new).replace("register(o,body,list(range(5)),'crown_feeler_root')","register(o,body,list(range(6)),'crown_feeler_root')")
# The top mouth armor is a front brow strip, not an entire armored roof.
marker="head=bpy.data.objects['VaultedSkull']"
extra="""
 head=bpy.data.objects['VaultedSkull']
 # Narrow flesh strip is baked into the roof texture, without a raised band.
 transition=B.mats['wet-hide'].copy();transition.name='Maw narrow flesh transition'
 nodes=transition.node_tree.nodes;links=transition.node_tree.links
 bs=nodes.get('Principled BSDF') or next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
 oldcolor=bs.inputs['Base Color'].links[0].from_socket
 coord=nodes.new('ShaderNodeNewGeometry');sep=nodes.new('ShaderNodeSeparateXYZ');links.new(coord.outputs['Position'],sep.inputs[0])
 lower=nodes.new('ShaderNodeMath');lower.operation='GREATER_THAN';lower.inputs[1].default_value=2.00;links.new(sep.outputs['Y'],lower.inputs[0])
 upper=nodes.new('ShaderNodeMath');upper.operation='LESS_THAN';upper.inputs[1].default_value=2.07;links.new(sep.outputs['Y'],upper.inputs[0])
 mask=nodes.new('ShaderNodeMath');mask.operation='MULTIPLY';links.new(lower.outputs[0],mask.inputs[0]);links.new(upper.outputs[0],mask.inputs[1])
 top=nodes.new('ShaderNodeMath');top.operation='GREATER_THAN';top.inputs[1].default_value=.55;links.new(sep.outputs['Z'],top.inputs[0])
 roofmask=nodes.new('ShaderNodeMath');roofmask.operation='MULTIPLY';links.new(mask.outputs[0],roofmask.inputs[0]);links.new(top.outputs[0],roofmask.inputs[1])
 mix=nodes.new('ShaderNodeMixRGB');links.new(roofmask.outputs[0],mix.inputs[0]);links.new(oldcolor,mix.inputs[1]);mix.inputs[2].default_value=(.065,.044,.036,1);links.new(mix.outputs[0],bs.inputs['Base Color'])
 head.data.materials.append(transition);transition_slot=len(head.data.materials)-1
 for p in head.data.polygons:
  if not all(i<114 for i in p.vertices):continue
  row=min(i//19 for i in p.vertices)
  if row==1:p.material_index=transition_slot
  elif row>=3:p.material_index=2
  else:p.material_index=0
  p.use_smooth=True
"""
source=source.replace(marker,extra)
source=source.replace("head='upper jaw compressed to the body skyline, narrower and shorter head; lower jaw remains open below'","head='further shortened and narrowed; gape height compressed around upper body line'")
source=source.replace("armor='snout-to-eye crests and separate body crown crest'","armor='low mouth brow; rear crown reduced to small nub; exposed fleshy band behind brow'")
source=source.replace("feelers='three curved crown-rooted rear-swept feelers per side with luminous ends'","feelers='three independently spread arcs per side with separate roots and well separated blue tips'")
exec(compile(source,str(__file__),'exec'))
_build=build
def build(B,arm,record,profile,tube,cap):
 body=_build(B,arm,record,profile,tube,cap)
 tips={}
 for side in (-1,1):
  pts=[]
  for k in range(3):
   o=bpy.data.objects['CrownFeeler'+str(side)+'_'+str(k)]
   pts.append(sum((v.co for v in o.data.vertices[18:24]),Vector())/6)
  distance=min((pts[a]-pts[b]).length for a,b in ((0,1),(0,2),(1,2)))
  assert distance>.45,('stacked feeler tips',side,distance)
  tips[str(side)]=dict(tip_positions=[list(p) for p in pts],minimum_separation_author_units=distance)
 head=bpy.data.objects['VaultedSkull'];jaw=bpy.data.objects['HingedGumBowl']
 headpoints=[v.co for o in (head,jaw) for v in o.data.vertices]
 trunk=[v.co for v in body.data.vertices if 0<=v.co.y<=1.2]
 dims=lambda points:[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
 (OUT/'face-and-feeler-measurements.json').write_text(json.dumps(dict(feelers=tips,head_dimensions_author_units=dims(headpoints),front_trunk_dimensions_author_units=dims(trunk),notes='separation is measured on authored rest geometry; same left-facing renders required for visual acceptance'),indent=2)+'\n')
 return body
