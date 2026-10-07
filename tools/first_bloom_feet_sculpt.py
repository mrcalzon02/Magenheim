"""Reshape the existing foot loops into crooked, grounded root fans."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector
import first_bloom_membrane_detail as previous
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree,inside
materials=previous.materials
unwrap_atlas=previous.unwrap_atlas
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-feet-r15-raw'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 trunk=previous.build(B,arm,record,profile,tube,cap)
 records=json.loads(bpy.context.scene['tissue_attachment_contract'])
 changes=[]
 for side,s in [('L',1),('R',-1)]:
  foot=arm.data.bones['Foot_'+side].head_local.copy()
  # Keep the existing three rings and six-sided cross sections. The middle
  # ring supplies a crooked knuckle rather than five identical radial rods.
  spreads=[-.48,-.27,-.035,.24,.48]
  reaches=[.30,.50,.62,.53,.32]
  for k in range(5):
   o=bpy.data.objects['GroundButtress'+side+str(k)]
   shift=.025 if side=='L' else -.025
   pts=[Vector((foot.x,foot.y,foot.z+.30)),
        Vector((foot.x+s*(spreads[k]*.42+shift),foot.y+.18+.055*(k%2),.135+.025*(k%2))),
        Vector((foot.x+s*spreads[k],foot.y+reaches[k]+(.035 if side=='R' and k%2 else 0),.033))]
   radii=[.105,.115 if k in (0,4) else .10,.018 if k in (0,4) else .014]
   for j,(p,r) in enumerate(zip(pts,radii)):
    tangent=(pts[min(j+1,2)]-pts[max(j-1,0)]).normalized();q=tangent.to_track_quat('Z','Y')
    for n in range(6):
     a=math.tau*n/6;v=p+q@Vector((r*math.cos(a),r*.72*math.sin(a),0))
     # Flatten the bottom of the middle knuckles into a ground contact pad.
     if j>0:v.z=max(.020,v.z)
     o.data.vertices[j*6+n].co=v
   o.data.update()
   host=bpy.data.objects['RootedThigh'+side]
   marker=len(records)+1
   for index in range(6):
    point,weights=embed(o.matrix_world@o.data.vertices[index].co,host,.004)
    assign(o,index,point,weights)
    o.data.attributes['attachment_endpoint'].data[index].value=marker
   assert all(inside(o.matrix_world@o.data.vertices[index].co,tree(host)) for index in range(6))
   records.append(dict(id=marker,tissue=o.name,end='ankle_root',support=host.name,support_component=host.data.attributes['attachment_component'].data[0].value,endpoint_seating='all six existing root-base vertices embedded in the ankle with matching host weights'))
   changes.append(o.name)
  for k in range(3):
   o=bpy.data.objects['RootSkirt'+side+str(k)]
   for v in o.data.vertices:
    p=o.matrix_world@v.co
    # Narrow the stacked discs and stagger them into ankle-hugging brackets;
    # lower brackets fan out, upper brackets climb back into the ankle.
    old=Vector((foot.x+s*.10,.18+k*.14,.10+k*.13))
    d=p-old
    p=Vector((foot.x+s*(.07+.055*k),foot.y+.11+.075*k,.095+.105*k))
    p.x+=d.x*(1.17-.16*k)
    p.y+=d.y*(.90-.10*k)+s*d.x*.16
    p.z+=d.z*.66-.10*max(0,d.y)+.025*math.sin(d.x*13+k)
    p.z=max(.024,p.z)
    v.co=o.matrix_world.inverted()@p
   o.data.update();changes.append(o.name)
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(groups=len(set(i['tissue'] for i in records)),endpoints=len(records),records=records),indent=2)+'\n')
 (OUT/'foot-refinement.json').write_text(json.dumps(dict(changed_components=changes,triangles_added=0,method='existing loops reshaped into unequal crooked root fans, flattened knuckles and staggered low fungal brackets',rig='original Foot_L and Foot_R weights retained except ten ankle-root rings seated with matching host weights',chest='unchanged r13 anatomy; hidden faces reaudited before final trim'),indent=2)+'\n')
 return trunk
