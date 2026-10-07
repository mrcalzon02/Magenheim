"""Individual anatomy and compact surface construction for the Blackwater Maw."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,geometry
from first_bloom_frill_sculpt import assign
from first_bloom_deeper_chest import embed
from first_bloom_attachment_repair import tree,inside
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-refinement-r3'

def materials(B):
 palettes={
 'wet-hide':[(.10,(.003,.006,.009,1)),(.38,(.008,.018,.025,1)),(.57,(.022,.042,.047,1)),(.70,(.058,.070,.065,1)),(.88,(.013,.019,.012,1))],
 'bone-armor':[(.15,(.050,.063,.064,1)),(.40,(.19,.21,.19,1)),(.62,(.32,.32,.27,1)),(.83,(.13,.15,.11,1))],
 'fin-membrane':[(.15,(.012,.023,.03,1)),(.42,(.026,.064,.083,1)),(.72,(.09,.13,.13,1)),(.90,(.16,.17,.13,1))],
 'oral-tissue':[(.15,(.018,.008,.010,1)),(.40,(.085,.036,.036,1)),(.65,(.20,.103,.089,1)),(.85,(.30,.19,.14,1))],
 'ivory':[(.10,(.085,.062,.030,1)),(.45,(.32,.27,.16,1)),(.80,(.62,.57,.41,1))],
 'light-organ':[(.10,(.008,.035,.04,1)),(.42,(.015,.18,.21,1)),(.85,(.05,.45,.47,1))]}
 for key,palette in palettes.items():
  mat=bpy.data.materials.new('Maw_'+key);mat.use_nodes=True;B.mats[key]=mat
  nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes['Principled BSDF']
  coord=nodes.new('ShaderNodeTexCoord');noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=5;noise.inputs['Detail'].default_value=4;noise.inputs['Roughness'].default_value=.8;links.new(coord.outputs['Object'],noise.inputs['Vector'])
  ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements.remove(ramp.color_ramp.elements[-1]);ramp.color_ramp.elements[0].position=palette[0][0];ramp.color_ramp.elements[0].color=palette[0][1]
  for position,color in palette[1:]:ramp.color_ramp.elements.new(position).color=color
  links.new(noise.outputs['Fac'],ramp.inputs[0]);links.new(ramp.outputs[0],bs.inputs['Base Color'])
  bs.inputs['Roughness'].default_value=.48 if key in ('wet-hide','oral-tissue','light-organ') else .73
  fine=nodes.new('ShaderNodeTexVoronoi');fine.feature='DISTANCE_TO_EDGE';fine.inputs['Scale'].default_value=22 if key in ('wet-hide','bone-armor') else 40;links.new(coord.outputs['Object'],fine.inputs['Vector'])
  bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.45;bump.inputs['Distance'].default_value=.023 if key in ('wet-hide','bone-armor') else .006;links.new(fine.outputs['Distance'],bump.inputs['Height']);links.new(bump.outputs[0],bs.inputs['Normal'])
  if key in ('oral-tissue','fin-membrane'):
   wave=nodes.new('ShaderNodeTexWave');wave.bands_direction='Y';wave.inputs['Scale'].default_value=15;wave.inputs['Distortion'].default_value=6;links.new(coord.outputs['Object'],wave.inputs['Vector'])
   folds=nodes.new('ShaderNodeBump');folds.inputs['Strength'].default_value=.3;folds.inputs['Distance'].default_value=.018;links.new(wave.outputs[0],folds.inputs['Height']);links.new(bump.outputs[0],folds.inputs['Normal']);links.new(folds.outputs[0],bs.inputs['Normal'])
  if key=='light-organ':links.new(ramp.outputs[0],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=1.4

def build(B,arm,record,profile,tube,cap):
 materials(B);pending=[];parts={}
 def mesh(name,vertices,faces,key,bone,category,slots=None):
  me=bpy.data.meshes.new(name);me.from_pydata(vertices,[],faces);me.update();me.materials.append(B.mats[key])
  o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);record(o,bone,category);parts[name]=o
  for p in me.polygons:p.use_smooth=True
  if slots:
   me.materials.append(B.mats['oral-tissue'])
   for p,slot in zip(me.polygons,slots):p.material_index=slot
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT');me.uv_layers.active.name='BossConstructionUV';o.select_set(False)
  return o
 anchors=[(1.85,'Head'),(1.40,'Head'),(.95,'Spine_1'),(.40,'Spine_1'),(.0,'Spine_2'),(-.55,'Spine_3'),(-1.15,'Spine_4'),(-1.65,'Spine_5'),(-2.25,'Tail_1'),(-2.9,'Tail_2'),(-3.7,'Tail_2')]
 def body_weights(y):
  for (a,ba),(b,bb) in zip(anchors,anchors[1:]):
   if b<=y<=a:
    t=(a-y)/(a-b)
    return {ba:1} if ba==bb else {ba:1-t,bb:t}
  return {anchors[0 if y>1.85 else -1][1]:1}
 rings=[(1.85,.43,.43,.05),(1.58,.72,.65,.06),(1.25,.91,.76,.07),(.95,1.00,.80,.09),(.55,.99,.77,.07),(.10,.90,.68,.04),(-.40,.78,.58,.02),(-.9,.65,.47,.01),(-1.4,.50,.37,.02),(-1.9,.36,.27,.03),(-2.35,.24,.20,.04),(-2.80,.16,.16,.05),(-3.2,.115,.14,.06),(-3.58,.075,.10,.07)]
 labels=['Head','Head','Spine_1','Spine_1','Spine_1','Spine_2','Spine_3','Spine_4','Spine_5','Tail_1','Tail_1','Tail_2','Tail_2','Tail_2']
 anchors=[(r[0],bone) for r,bone in zip(rings,labels)]
 # Shorter bend panels and one interpolated loop between rigid bone sections
 # keep every support triangle on at most two bones.
 expanded=[]
 for a,b in zip(rings,rings[1:]):expanded.extend([a,tuple((x+y)*.5 for x,y in zip(a,b))])
 expanded.append(rings[-1]);rings=expanded
 vertices=[];faces=[];n=24
 for j,(y,rx,rz,z) in enumerate(rings):
  for k in range(n):
   a=math.tau*k/n;flute=1+.025*math.cos(a*8+j*.4)
   vertices.append((math.cos(a)*rx*flute,y,z+math.sin(a)*rz*flute))
 for j in range(len(rings)-1):
  for k in range(n):a=j*n+k;b=j*n+(k+1)%n;faces.append((a,b,b+n,a+n))
 faces.extend([tuple(reversed(range(n))),tuple((len(rings)-1)*n+k for k in range(n))])
 body=mesh('ContinuousScaledBody',vertices,faces,'wet-hide','Spine_1','body')
 body.data.materials.append(B.mats['oral-tissue']);body.data.polygons[-2].material_index=1
 for v in body.data.vertices:assign(body,v.index,v.co,body_weights(v.co.y))

 def shell(name,rows,upper,bone):
  verts=[];faces=[];slots=[];n=18;count=len(rows)*n
  for layer in range(2):
   for y,rx,rz,cz in rows:
    for k in range(n):
     a=-.22+(math.pi+.44)*k/(n-1)
     x=(rx-layer*.065)*math.cos(a);z=cz+(1 if upper else -1)*(rz-layer*.065)*math.sin(a)
     verts.append((x,y,z))
  for layer in range(2):
   for j in range(len(rows)-1):
    for k in range(n-1):
     a=layer*count+j*n+k;f=(a,a+1,a+n+1,a+n)
     outward=f if not upper else tuple(reversed(f));faces.append(outward if layer==0 else tuple(reversed(outward)));slots.append(layer)
  for j in range(len(rows)-1):
   for k in (0,n-1):a=j*n+k;b=(j+1)*n+k;faces.append((a,b,b+count,a+count));slots.append(1)
  for j in (0,len(rows)-1):
   for k in range(n-1):a=j*n+k;b=a+1;faces.append((a,b,b+count,a+count));slots.append(1)
  return mesh(name,verts,faces,'wet-hide',bone,'skull' if upper else 'jaw',slots),n,count
 head,n,hcount=shell('VaultedSkull',[(1.52,.56,.43,.12),(1.96,.83,.67,.16),(2.40,.99,.84,.16),(2.78,1.02,.94,.17),(3.12,1.06,1.01,.18),(3.44,1.08,1.04,.18)],True,'Head')
 head.data.materials.append(B.mats['bone-armor'])
 for p in head.data.polygons:
  if p.index<5*(n-1):
   j,k=divmod(p.index,n-1)
   if 3<=k<=13 and j>=1:p.material_index=2;p.use_smooth=False
 for v in head.data.vertices[:hcount]:
  j,k=divmod(v.index,n)
  if j>=1 and 3<=k<=14:v.co.z+=.037*math.sin(k*2.5+j*1.7)+.018*math.cos(k*4+j)
 head.data.update()
 def upper_surface(point):
  head.data.calc_loop_triangles();candidates=[]
  for tri in head.data.loop_triangles:
   if tri.polygon_index>=5*(n-1):continue
   coords=[head.data.vertices[i].co for i in tri.vertices];hit=geometry.closest_point_on_tri(point,*coords)
   normal=(coords[1]-coords[0]).cross(coords[2]-coords[0]).normalized()
   if normal.dot(Vector((hit.x,0,hit.z-.17)))<0:normal=-normal
   candidates.append(((hit-point).length,hit,normal))
  return min(candidates,key=lambda v:v[0])[1:]
 jaw,jn,jcount=shell('HingedGumBowl',[(1.58,.40,.29,-.13),(2.06,.68,.48,-.20),(2.50,.88,.70,-.24),(2.97,1.00,.85,-.25),(3.46,1.06,.91,-.22)],False,'Jaw')
 pending.extend([(head,list(range(n))+list(range(hcount,hcount+n)),body,'skull_neck',.025),(jaw,list(range(jn))+list(range(jcount,jcount+jn)),body,'jaw_hinge',.025)])
 # Elastic cheek commissures close the lateral mouth cavity and articulate
 # between the upper skull and jaw without independent floating strips.
 for side,s in [('L',-1),('R',1)]:
  ys=[2.16,2.49,2.82,3.12];verts=[];count=12
  for layer in range(2):
   for j,t in enumerate((0,.5,1)):
    for k,y in enumerate(ys):
     upper=Vector((s*(.85+.055*k),y,.44+.025*k));lower=Vector((s*(.72+.08*k),y,-.44-.035*k))
     p=upper.lerp(lower,t);p.x+=s*(.035*math.sin(t*math.pi)+(.015 if layer==0 else -.015));verts.append(p)
  fs=[]
  for layer in range(2):
   for j in range(2):
    for k in range(3):a=layer*count+j*4+k;f=(a,a+1,a+5,a+4);fs.append(f if layer==0 else tuple(reversed(f)))
  boundary=[0,1,2,3,7,11,10,9,8,4]
  for a,b in zip(boundary,boundary[1:]+boundary[:1]):fs.append((a,b,b+count,a+count))
  cheek=mesh('FleshyCommissure'+side,verts,fs,'oral-tissue','Head','mouth-folds')
  for v in cheek.data.vertices:
   t=(v.index%count)//4/2;assign(cheek,v.index,v.co,{'Head':1-t,'Jaw':t})
  pending.extend([(cheek,list(range(4))+list(range(count,count+4)),head,'upper_cheek',.016),(cheek,list(range(8,12))+list(range(count+8,count+12)),jaw,'lower_cheek',.016)])
 # The rear lining carries relief folds and a restrained inner light organ.
 for k in range(5):
  z=-.27+k*.14
  o=tube('ThroatRuga'+str(k),[(-.33,1.89,z),(0,1.94,z-.055),(.33,1.89,z)],[.027,.04,.014],'oral-tissue','Head',6,'mouth-folds');parts[o.name]=o
  pending.extend([(o,list(range(6)),body,'fold_left',.02),(o,list(range(12,18)),body,'fold_right',.02)])
 throat=B.mass('InnerThroatOrgan',(0,1.94,.04),(.11,.085,.11),'light-organ','Head',6);record(throat,'Head','sensory');parts[throat.name]=throat
 pending.append((throat,list(range(6)),body,'inner_organ_root',.015))
 # Unequal, curved outer and inner fang rows. Their bases are embedded in gum.
 for upper,host,bone in [(True,head,'Head'),(False,jaw,'Jaw')]:
  for inner,total in [(False,16),(True,6)]:
   for k in range(total):
    a=.12+(math.pi-.24)*k/(total-1);rx=1.065 if not inner else .99;rz=1.025 if upper else .89
    y=3.40 if not inner else 3.08;z=(.18+rz*math.sin(a)) if upper else (-.22-rz*math.sin(a));x=rx*math.cos(a)
    length=(.47+.22*((k*7)%5)/4) if not inner else .24+.07*(k%2)
    direction=-1 if upper else 1
    o=tube(('Upper' if upper else 'Lower')+('Inner' if inner else 'Fang')+str(k),[(x,y,z),(x*.97,y+.06,z+direction*length*.42),(x*.90,y+.12,z+direction*length*.82),(x*.85,y+.10,z+direction*length)],[.061 if not inner else .037,.043 if not inner else .024,.018,.0025],'ivory',bone,6,'fangs');parts[o.name]=o
    pending.append((o,list(range(6)),host,'gum_root',.018))

 # Angular overlapping cheek/brow plates, with roots in the skull wall.
 for s in (-1,1):
  for k in range(8):
   a=.16+.095*(k%3);y=2.20+.15*k;rx=.96;rz=.87+.012*k
   center=Vector((s*rx*math.cos(a),y,.17+rz*math.sin(a)))
   hit,normal=upper_surface(center);center=hit+normal*.030
   tangent=Vector((0,1,0));up=normal.cross(tangent).normalized()
   corners=[(-.18,-.10),(.12,-.11),(.20,.015),(.035,.15),(-.15,.105)]
   verts=[tuple(center+tangent*dy+up*z) for dy,z in corners]+[tuple(center+tangent*dy*.84+up*z*.84-normal*.050) for dy,z in corners]+[tuple(center+normal*.025)]
   fs=[(10,i,(i+1)%5) for i in range(5)]+[tuple(reversed(range(5,10)))]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)]
   o=mesh('CheekPlate'+str(s)+'_'+str(k),verts,fs,'bone-armor','Head','armor')
   for p in o.data.polygons:p.use_smooth=False
   pending.append((o,list(range(5,10)),head,'armor_base',.018))
  for k in range(4):
   a=.28+.12*(k%2);y=2.32+.23*k
   point=Vector((s*.99*math.cos(a),y,.17+.92*math.sin(a)));hit,normal=upper_surface(point);p=hit+normal*.025
   o=B.mass('Sensory'+str(s)+'_'+str(k),p,(.067,.045,.052),'light-organ','Head',6);record(o,'Head','sensory');parts[o.name]=o
   q=normal.to_track_quat('Z','Y')
   for v in o.data.vertices:v.co=p+q@(v.co-p)
   o.data.update()
   # Closed ocular bulbs intersect their skull socket; the bottom cap seats.
   pending.append((o,list(range(6)),head,'sensory_socket',.018))

 def pectoral(s,side):
  verts=[];rows=4;n=13;count=rows*n
  for layer in range(2):
   for j,t in enumerate((0,.33,.70,1)):
    for k in range(n):
     u=k/(n-1);root=Vector((s*(.89+.055*math.sin(u*math.pi)),.92-u*.46,-.18))
     tip=Vector((s*(2.02-.33*u+.08*math.sin(k*1.7)),.52-u*1.65,-.43-.11*math.sin(u*math.pi)))
     p=root.lerp(tip,t);p.z+=.045*math.sin(k*math.pi/2)*t*(1-t)+(.014 if layer==0 else -.014)
     verts.append(p)
  faces=[]
  for layer in range(2):
   for j in range(rows-1):
    for k in range(n-1):a=layer*count+j*n+k;f=(a,a+1,a+n+1,a+n);faces.append(f if layer==0 else tuple(reversed(f)))
  boundary=list(range(n))+[j*n+n-1 for j in range(1,rows)]+list(reversed(range((rows-1)*n,(rows-1)*n+n-1)))+[j*n for j in reversed(range(1,rows-1))]
  for a,b in zip(boundary,boundary[1:]+boundary[:1]):faces.append((a,b,b+count,a+count))
  o=mesh('PleatedPectoral'+side,verts,faces,'fin-membrane','Fin_'+side,'fins')
  pending.append((o,list(range(n))+list(range(count,count+n)),body,'pectoral_root',.035))
  return o
 pectoral(-1,'L');pectoral(1,'R')

 # One skinned dorsal sail carries uneven spines and recessed membrane bays.
 ys=[1.20,.91,.63,.40,.12,-.15,-.42,-.70,-.98,-1.24,-1.50,-1.78,-2.05,-2.32,-2.58]
 heights=[.13,.80,1.22,.68,.35,.95,1.10,.29,.18,.65,.78,.21,.11,.34,.13]
 def top_z(y):
  for (a,rx,rz,z),(b,brx,brz,bz) in zip(rings,rings[1:]):
   if b<=y<=a:t=(a-y)/(a-b);return (z+rz)*(1-t)+(bz+brz)*t
  return .2
 verts=[];n=len(ys);count=n*3
 for layer in range(2):
  for j,t in enumerate((0,.50,1)):
   for k,(y,h) in enumerate(zip(ys,heights)):
    verts.append(((-1 if layer else 1)*(.018+.010*math.sin(k*1.3)**2)*(1-.5*t),y-.18*t,top_z(y)-.035+h*t))
 faces=[]
 for layer in range(2):
  for j in range(2):
   for k in range(n-1):a=layer*count+j*n+k;f=(a,a+1,a+n+1,a+n);faces.append(f if layer==0 else tuple(reversed(f)))
 boundary=list(range(n))+[2*n-1,3*n-1]+list(reversed(range(2*n,3*n-1)))+[n]
 for a,b in zip(boundary,boundary[1:]+boundary[:1]):faces.append((a,b,b+count,a+count))
 dorsal=mesh('RaggedDorsalSail',verts,faces,'fin-membrane','Spine_1','fins')
 for v in dorsal.data.vertices:assign(dorsal,v.index,v.co,body_weights(v.co.y))
 pending.append((dorsal,list(range(n))+list(range(count,count+n)),body,'dorsal_root',.065))
 # Forked caudal fan, closed thickness and real central notch.
 outline=[(-3.35,.12),(-3.64,.40),(-4.20,1.30),(-4.02,.40),(-3.78,.06),(-4.02,-.37),(-4.22,-1.12),(-3.60,-.28),(-3.35,-.04)]
 n=len(outline);verts=[(x,y,z) for x in (-.025,.025) for y,z in outline]+[(-.025,-3.58,.04),(.025,-3.58,.04)];faces=[]
 for k in range(n):
  b=(k+1)%n;faces.extend([(2*n,k,b),(2*n+1,n+b,n+k),(k,n+k,n+b,b)])
 tail=mesh('ForkedCaudalFan',verts,faces,'fin-membrane','Tail_2','fins');pending.append((tail,[0,n,8,n+8],body,'tail_root',.025))
 for s in (-1,1):
  for k in range(4):
   y=.99-.11*k
   o=tube('RecessedGill'+str(s)+'_'+str(k),[(s*.83,y,.30),(s*.99,y-.055,.02),(s*.87,y-.07,-.28)],[.025,.039,.012],'oral-tissue','Spine_1',5,'gills');parts[o.name]=o
   pending.extend([(o,list(range(5)),body,'gill_upper',.035),(o,list(range(10,15)),body,'gill_lower',.035)])
 # Purpose-sized flank scales spend the remaining budget on real overlaps,
 # while texture supplies the smaller scales and cracks.
 from first_bloom_attachment_repair import bone_weights
 for s in (-1,1):
  for j,y in enumerate((.88,.42,-.05,-.52,-1.0,-1.46)):
   for k,a in enumerate((.15,.49,.82,1.13)):
    for ra,rb in zip(rings,rings[1:]):
     if rb[0]<=y<=ra[0]:
      t=(ra[0]-y)/(ra[0]-rb[0]);rx=ra[1]*(1-t)+rb[1]*t;rz=ra[2]*(1-t)+rb[2]*t;z=ra[3]*(1-t)+rb[3]*t;break
    point=Vector((s*rx*math.cos(a),y,z+rz*math.sin(a)));bvh=tree(body);hit,normal,face,distance=bvh.find_nearest(point)
    if normal.dot(Vector((hit.x,0,hit.z-z)))<0:normal=-normal
    tangent=Vector((0,1,0));up=normal.cross(tangent).normalized();center=hit+normal*.022
    corners=[(-.16,-.08),(.105,-.085),(.18,.055),(-.11,.10)]
    vertices=[center+tangent*dy+up*dz for dy,dz in corners]+[center+tangent*dy*.85+up*dz*.85-normal*.04 for dy,dz in corners]+[center+normal*.035]
    faces=[(8,i,(i+1)%4) for i in range(4)]+[(7,6,5,4)]+[(i,(i+1)%4,(i+1)%4+4,i+4) for i in range(4)]
    o=mesh('FlankScale'+str(s)+'_'+str(j)+'_'+str(k),vertices,faces,'wet-hide','Spine_1','scales')
    for p in o.data.polygons:p.use_smooth=False
    weights=bone_weights(body,face,hit)
    for v in o.data.vertices:assign(o,v.index,v.co,weights)
    pending.append((o,list(range(4,8)),body,'scale_root',.065))
 # Name-stable face and endpoint attributes allow independent saved-mesh checks.
 ordered=sorted(parts.values(),key=lambda o:o.name);components={o.name:k+1 for k,o in enumerate(ordered)}
 for o in ordered:
  attr=o.data.attributes.new('attachment_component','INT','FACE')
  for item in attr.data:item.value=components[o.name]
  o.data.attributes.new('attachment_endpoint','INT','POINT')
 records=[]
 for o,indices,host,label,depth in pending:
  marker=len(records)+1
  if label=='scale_root':
   # Root small scales in a rigid body cross-section. A surface-normal offset
   # on a mixed-bone triangle can drift during the strongest spine bends.
   center=sum((o.data.vertices[i].co for i in indices),Vector())/len(indices)
   rows=[]
   for j,r in enumerate(rings):
    weights={host.vertex_groups[g.group].name:g.weight for g in host.data.vertices[j*24].groups if g.weight>1e-7}
    if len(weights)==1:rows.append((abs(r[0]-center.y),j,r,weights))
   distance,j,r,weights=min(rows,key=lambda q:q[0]);y,rx,rz,cz=r
   a=math.atan2((center.z-cz)/rz,center.x/rx)
   anchor=Vector((.55*rx*math.cos(a),y,cz+.55*rz*math.sin(a)))
   tangent=Vector((-math.sin(a),0,math.cos(a)));radial=Vector((math.cos(a),0,math.sin(a)))
   for v in o.data.vertices:assign(o,v.index,v.co,weights)
   for ordinal,index in enumerate(indices):
    phase=math.tau*ordinal/len(indices);point=anchor+tangent*(.018*math.cos(phase))+radial*(.010*math.sin(phase))
    assign(o,index,point,weights);o.data.attributes['attachment_endpoint'].data[index].value=marker
  else:
   for index in indices:
    p,w=embed(o.matrix_world@o.data.vertices[index].co,host,depth);assign(o,index,p,w)
    o.data.attributes['attachment_endpoint'].data[index].value=marker
  o.data.update();records.append(dict(id=marker,tissue=o.name,end=label,support=host.name,support_component=components[host.name],endpoint_seating='all root ring vertices embedded with actual support weights'))
 for item in records:
  o=parts[item['tissue']];host=parts[item['support']];bvh=tree(host)
  ids=[i for i,a in enumerate(o.data.attributes['attachment_endpoint'].data) if a.value==item['id']]
  assert ids and all(inside(o.matrix_world@o.data.vertices[i].co,bvh) for i in ids),item
 bpy.context.scene['tissue_attachment_contract']=json.dumps(records)
 (OUT/'attachment-authoring.json').write_text(json.dumps(dict(endpoints=len(records),groups=len(set(r['tissue'] for r in records)),records=records),indent=2)+'\n')
 (OUT/'anatomy.json').write_text(json.dumps(dict(body='one continuous spinal surface with 48 rooted flank scales',mouth='armored upper vault, articulated thick lower gum bowl, lateral cheek commissures and deep rugae-lined open gape',fangs=44,cheek_plates=16,sensory_organs=9,fins='closed pleated pectorals, continuous ragged dorsal sail and forked caudal fan',rig='retained Deep Hunter skeleton and actions; max two weights per vertex',quality_bar='native Serpent plus concept anatomy; art and native acceptance pending'),indent=2)+'\n')
 return body
