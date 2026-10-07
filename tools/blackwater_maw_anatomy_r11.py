"""Individual concept corrections: direct front fin, muscular middle fin and face."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
from first_bloom_frill_sculpt import assign
from first_bloom_deeper_chest import embed
from first_bloom_attachment_repair import tree,inside
import blackwater_maw_left_facing as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-anatomy-r11-raw'

def build(B,arm,record,profile,tube,cap):
 previous.OUT=OUT
 # Use the existing procedure with corrected fin-root anatomy, before baking.
 source=(ROOT/'tools/blackwater_maw_left_facing.py').read_text()
 source=source.replace("lower=make_joint('LowerFinJoint'+side,[(s*.72,1.36,-.43),(s*.98,1.20,-.56),(s*1.10,1.00,-.68),(s*1.18,.82,-.76)])","lower=body")
 source=source.replace("[.14,.17,.14,.11]","[.30,.29,.23,.15]")
 source=source.replace("(s*1.10,1.00,-.68),(s*1.36,.64,-.91)","(s*.66,1.36,-.43),(s*1.04,.91,-.78)")
 source=source.replace("[.05,.20,.36,.34,.18,.003]","[.25,.31,.36,.34,.18,.003]")
 ns=dict(__name__='maw_integrated_fin_recipe',__file__=str(ROOT/'tools/blackwater_maw_left_facing.py'))
 exec(compile(source,ns['__file__'],'exec'),ns);ns['OUT']=OUT
 body=ns['build'](B,arm,record,profile,tube,cap)
 records=json.loads(bpy.context.scene['tissue_attachment_contract'])
 # Straighten fang shafts in the front plane, keeping gum bases in place.
 for o in list(bpy.context.scene.objects):
  if o.type!='MESH' or not o.name.startswith(('UpperFang','UpperInner','LowerFang','LowerInner')):continue
  root=sum((v.co for v in o.data.vertices[:6]),Vector())/6
  for j in range(1,4):
   ring=list(o.data.vertices[j*6:(j+1)*6]);c=sum((v.co for v in ring),Vector())/6
   target=Vector((root.x*.99,root.y-.04*j,c.z))
   for v in ring:v.co+=target-c
  o.data.update()
 # Compress the upper head to the body skyline, preserving the open lower jaw.
 for o in list(bpy.context.scene.objects):
  if o.type!='MESH':continue
  for v in o.data.vertices:
   t=max(0,min(1,(v.co.y-1.70)/.55));t=t*t*(3-2*t)
   v.co.x*=1-.10*t;v.co.z*=1-(.32 if v.co.z>0 else .12)*t
   v.co.y-=.10*t*(v.co.y-1.70)
  o.data.update()
 head=bpy.data.objects['VaultedSkull']
 # An uneven cluster on the cheek plate, rather than a line along the snout.
 for side in (-1,1):
  for k,(y,z,size) in enumerate(((2.50,.39,1.25),(2.34,.23,1.50),(2.55,.10,1.0),(2.18,.37,.9))):
   o=bpy.data.objects['Sensory'+str(side)+'_'+str(k)]
   hit,normal,face,d=tree(head).ray_cast(Vector((side*3,y,z)),Vector((-side,0,0)),6)
   assert hit is not None,('eye socket',side,k)
   old=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
   for v in o.data.vertices:v.co=hit+normal*.022+(v.co-old)*size
   o.data.update()
 next_component=max(a.value for o in bpy.context.scene.objects if o.type=='MESH' for a in o.data.attributes['attachment_component'].data)+1
 def register(o,host,ids,label):
  nonlocal next_component
  a=o.data.attributes.new('attachment_component','INT','FACE')
  for v in a.data:v.value=next_component
  next_component+=1;o.data.attributes.new('attachment_endpoint','INT','POINT')
  marker=len(records)+1
  for i in ids:
   p,w=embed(o.data.vertices[i].co,host,.025);assign(o,i,p,w);o.data.attributes['attachment_endpoint'].data[i].value=marker
  records.append(dict(id=marker,tissue=o.name,end=label,support=host.name,support_component=host.data.attributes['attachment_component'].data[0].value,endpoint_seating='complete embedded root section'))
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.uv.smart_project(island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT');o.data.uv_layers.active.name='BossConstructionUV';o.select_set(False)
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED',ngon_method='EAR_CLIP');bm.to_mesh(o.data);bm.free()
 def crest(name,host,ys,widths,heights,bone,xs=None):
  vertices=[];faces=[];ids=[]
  for j,(y,w,h) in enumerate(zip(ys,widths,heights)):
   x=0 if xs is None else xs[j]
   hit,normal,face,d=tree(host).ray_cast(Vector((x,y,4)),Vector((0,0,-1)),8)
   assert hit is not None,(name,y)
   for x,z in ((-w,-.04),(w,-.04),(w,.02),(0,h),(-w,.02)):
    vertices.append(hit+Vector((x,0,z)))
   ids.extend((j*5,j*5+1))
  for j in range(len(ys)-1):
   for k in range(5):a=j*5+k;b=j*5+(k+1)%5;faces.append((a,b,b+5,a+5))
  faces.extend((tuple(reversed(range(5))),tuple((len(ys)-1)*5+k for k in range(5))))
  me=bpy.data.meshes.new(name);me.from_pydata(vertices,[],faces);me.materials.append(B.mats['bone-armor']);o=bpy.data.objects.new(name,me);bpy.context.scene.collection.objects.link(o);record(o,bone,'armor')
  for v in me.vertices:assign(o,v.index,v.co,{bone:1})
  register(o,host,ids,'integrated_armor_ridge');return o
 for s in (-1,1):
  crest('SnoutToEyeArmorRidge'+str(s),head,[3.17,2.98,2.73,2.50,2.29],[.035,.07,.09,.085,.035],[.025,.055,.075,.055,.015],'Head',[0,s*.18,s*.40,s*.62,s*.72])
 crest('BodyCrownArmorRidge',body,[1.38,1.13,.88,.64,.39],[.055,.18,.23,.20,.055],[.025,.17,.25,.18,.025],'Spine_1')
 # Three feelers on each side from the crown/neck, swept over the main body.
 for side in (-1,1):
  for k in range(3):
   y=1.14-.23*k;z=.66-.16*k;x=side*(.58+.10*k)
   o=tube('CrownFeeler'+str(side)+'_'+str(k),[(x,y,z),(side*(.99+.12*k),y-.30,z-.05),(side*(1.24+.18*k),y-.65,z-.22),(side*(1.38+.22*k),y-.83,z-.48)],[.030,.024,.015,.027],'bone-armor','Spine_1',6,'sensory')
   register(o,body,list(range(5)),'crown_feeler_root')
   # Light tips use existing geometry and no additional shader/material slot.
   for p in o.data.polygons:
    if any(i>=18 for i in p.vertices):
     if len(o.data.materials)==1:o.data.materials.append(B.mats['light-organ'])
     p.material_index=1
 # Re-seat moved eye sockets and any nonlinear-warp contacts against the final hosts.
 for item in records:
  o=bpy.data.objects[item['tissue']];host=bpy.data.objects[item['support']];bvh=tree(host)
  for i,a in enumerate(o.data.attributes['attachment_endpoint'].data):
   if a.value!=item['id']:continue
   if not inside(o.data.vertices[i].co,bvh) or o.name.startswith(('Sensory','ForwardPectoral')):
    p,w=embed(o.data.vertices[i].co,host,.070 if o.name.startswith('ForwardPectoral') else .018);assign(o,i,p,w)
  o.data.update()
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'concept-corrections.json').write_text(json.dumps(dict(forward_fin='direct attachment to body, no shoulder or arm',middle_fin='thick tapered muscular member integrated at trunk',head='upper jaw compressed to the body skyline, narrower and shorter head; lower jaw remains open below',fangs='straighter frontal shafts, rearward rake in profile; gum positions retained',eyes='varied posterior cheek cluster',armor='snout-to-eye crests and separate body crown crest',feelers='three curved crown-rooted rear-swept feelers per side with luminous ends',status='art candidate, requires concept review'),indent=2)+'\n')
 return body
