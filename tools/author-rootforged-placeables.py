"""Author editable Rootforged construction; geometry and UVs are baked before runtime.

Revision 3 is the end-game fidelity pass. Stable catalog IDs, meter envelopes, recipe authority and
snap/collider ownership remain unchanged; this file only raises the authored visual language from
prototype geometry to deliberate late-game construction.
"""
import bpy,bmesh,math,json,random
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1]
entries=json.loads((R/'default-data/foundation.json').read_text())['underworldArchitecture']['pieces']
DETAIL_REVISION=3
DETAIL_FLOORS={0:24,1:24,2:18,3:18,4:36,5:36,6:28,7:28,8:28,9:24,10:32}


def material(name,color,metal=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.use_backface_culling=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(1,1,1,1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=.8 if not metal else .45
 im=bpy.data.images.new(name+'-grain',256,256);pixels=[];rng=random.Random(47)
 for y in range(256):
  for x in range(256):
   grain=.87+.07*math.sin(x*.36+1.8*math.sin(y*.035))+.035*math.sin(x*1.37+y*.025)+rng.uniform(-.045,.045)
   if 'stone' in name:grain=.86+.09*math.sin(x*.1+y*.12)+rng.uniform(-.06,.06)
   pixels.extend([min(1,max(0,c*grain)) for c in color]+[1])
 im.pixels[:]=pixels;im.pack();tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=im;m.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
 return m


def mesh(name,vs,fs,uvs,mat,collision=True):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob);me.materials.append(mat)
 uv=me.uv_layers.new(name='RootUV')
 for poly,faceuv in zip(me.polygons,uvs):
  for li,co in zip(poly.loop_indices,faceuv):uv.data[li].uv=co
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 ob['game_node_path']=name;ob['game_collision']=collision;ob['game_crystal']='null'
 return ob


def tube(name,points,radii,mat,sides=12,collision=True):
 vs=[];fs=[];uvs=[]
 for k,p in enumerate(points):
  tangent=(points[min(k+1,len(points)-1)]-points[max(0,k-1)]).normalized()
  side=Vector((0,1,0));up=tangent.cross(side).normalized()
  for j in range(sides):
   angle=math.tau*j/sides;r=radii[k]*(1+.055*math.sin(j*3.1+k*.43))
   vs.append(p+r*(math.cos(angle)*side+math.sin(angle)*up))
 for k in range(len(points)-1):
  for j in range(sides):
   fs.append((k*sides+j,k*sides+(j+1)%sides,(k+1)*sides+(j+1)%sides,(k+1)*sides+j))
   u=j/sides;v=k/(len(points)-1);uvs.append([(u,v),((j+1)/sides,v),((j+1)/sides,(k+1)/(len(points)-1)),(u,(k+1)/(len(points)-1))])
 for k,rev in [(0,True),(len(points)-1,False)]:
  ids=list(range(k*sides,(k+1)*sides));ids=ids[::-1] if rev else ids
  fs.append(tuple(ids));uvs.append([(.5+.45*math.cos(math.tau*(i%sides)/sides),.5+.45*math.sin(math.tau*(i%sides)/sides)) for i in ids])
 return mesh(name,vs,fs,uvs,mat,collision)


def box(name,center,size,mat,collision=True,bevel=.035):
 x,y,z=center;a,b,c=[v/2 for v in size]
 vs=[(x+dx*a,y+dy*b,z+dz*c) for dx,dy,dz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
 fs=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
 ob=mesh(name,vs,fs,[[(0,0),(1,0),(1,1),(0,1)]]*6,mat,collision)
 if bevel>0:
  mod=ob.modifiers.new('Worn edges','BEVEL');mod.width=bevel;mod.segments=2
 return ob


def detail_tube(name,points,radius,mat,sides=8):
 radii=[radius if not isinstance(radius,(list,tuple)) else radius[i] for i in range(len(points))]
 return tube(name,points,radii,mat,sides,False)


def path_axis(kind,w,h,t):
 if kind==5:return Vector(((w-.8)*(t-.5),0,.36+(h-.72)*math.sin(math.pi*t)))
 if kind==3:return Vector((0,0,h*t))
 return Vector((w*(t-.5),0,.5))


def add_root_flares(kind,w,h,wood,pale):
 # Grown structural pieces widen into visible load paths instead of ending as clean cylinders.
 for end,t in enumerate((.035,.965)):
  anchor=path_axis(kind,w,h,t)
  tangent=(path_axis(kind,w,h,min(.999,t+.01))-path_axis(kind,w,h,max(.001,t-.01))).normalized()
  side=Vector((0,1,0));up=tangent.cross(side).normalized()
  for branch in range(3):
   a=-.55+branch*.55
   lateral=side*math.sin(a)*.30+up*math.cos(a)*.30
   inward=-tangent*(.22 if end==0 else -.22)
   pts=[anchor+inward,anchor+lateral*.45,anchor+lateral]
   detail_tube(f'grown-foot-{end}-{branch}',pts,[.10,.075,.035],wood if branch!=1 else pale,8)


def add_root_surface_detail(kind,w,h,wood,pale):
 # Secondary bark ridges and crossing tendrils add the scale change missing from the prototype set.
 for ridge in range(4):
  pts=[];phase=ridge*math.tau/4
  for i in range(29):
   t=.06+.88*i/28;p=path_axis(kind,w,h,t);tw=phase+t*math.tau*1.15
   if kind==3:offset=Vector((math.sin(tw)*.305,math.cos(tw)*.305,0))
   else:offset=Vector((0,math.cos(tw)*.305,math.sin(tw)*.305))
   pts.append(p+offset)
  detail_tube('bark-ridge-'+str(ridge),pts,[.030+.018*math.sin(math.pi*i/28) for i in range(29)],wood if ridge%2==0 else pale,7)
 for knot,t in enumerate((.22,.39,.63,.79)):
  p=path_axis(kind,w,h,t);p2=path_axis(kind,w,h,min(.99,t+.04));tangent=(p2-p).normalized()
  side=Vector((0,1,0));up=tangent.cross(side).normalized();direction=(side if knot%2==0 else up)*(1 if knot<2 else -1)
  detail_tube('root-knot-'+str(knot),[p,p+direction*.16+tangent*.02,p+direction*.24], [.055,.075,.025], pale,8)
 add_root_flares(kind,w,h,wood,pale)


def add_ironwork(kind,w,h,iron):
 # Reinforcement now explains the mechanical tier: collars, longitudinal straps, gussets and rivets.
 for number,t in enumerate((.12,.5,.88)):
  p=path_axis(kind,w,h,t)
  tangent=(path_axis(kind,w,h,min(.999,t+.001))-path_axis(kind,w,h,max(.001,t-.001))).normalized()
  bpy.ops.mesh.primitive_torus_add(major_radius=.35,minor_radius=.065,major_segments=24,minor_segments=8,location=p)
  ring=bpy.context.object;ring.name='iron-collar-'+str(number);ring.rotation_mode='QUATERNION';ring.rotation_quaternion=Vector((0,0,1)).rotation_difference(tangent);ring.data.materials.append(iron)
  ring['game_node_path']=ring.name;ring['game_collision']=False;ring['game_crystal']='null'
  side=Vector((0,1,0));up=tangent.cross(side).normalized()
  for rivet in range(4):
   angle=rivet*math.pi*.5;rp=p+(side*math.cos(angle)+up*math.sin(angle))*.40
   box(f'collar-rivet-{number}-{rivet}',rp,(.08,.08,.08),iron,False,.018)
 for strap in range(4):
  phase=strap*math.pi*.5;pts=[]
  for i in range(33):
   t=.08+.84*i/32;p=path_axis(kind,w,h,t);p2=path_axis(kind,w,h,min(.99,t+.01));tangent=(p2-p).normalized()
   side=Vector((0,1,0));up=tangent.cross(side).normalized();pts.append(p+(side*math.cos(phase)+up*math.sin(phase))*.33)
  detail_tube('forged-longitudinal-strap-'+str(strap),pts,.028,iron,6)
 for gusset,t in enumerate((.12,.5,.88)):
  p=path_axis(kind,w,h,t)
  box('forged-gusset-'+str(gusset),(p.x,p.y,p.z),(.18,.58,.18),iron,False,.025)


def add_understone_detail(w,h,d,stone,wood,pale):
 box('lower-plinth',(0,0,h*.15),(w,d,h*.3),stone)
 box('recessed-core',(0,0,h*.6),(w*.88,d*.88,h*.6),stone)
 box('upper-seat',(0,0,h*.925),(w*.96,d*.96,h*.15),stone)
 for sx in (-1,1):
  for sy in (-1,1):
   box(f'corner-buttress-{sx}-{sy}',(sx*w*.40,sy*d*.40,h*.46),(w*.13,d*.13,h*.70),stone,True,.045)
 # Recessed relief panels create readable faces at player distance.
 for side in (-1,1):
  for tier,z in enumerate((h*.40,h*.67)):
   box(f'front-relief-{side}-{tier}',(0,side*d*.445,z),(w*.52,.035,h*.16),stone,False,.018)
   box(f'side-relief-{side}-{tier}',(side*w*.445,0,z),(.035,d*.52,h*.16),stone,False,.018)
 # Root channels wrap all four faces and converge on the upper socket.
 for side in (-1,1):
  for branch in (-1,1):
   pts=[Vector((branch*w*.29,side*d*.452,h*t)) for t in (.16,.32,.50,.69,.84)]
   pts[-1]=Vector((branch*w*.15,side*d*.452,h*.86))
   detail_tube(f'front-root-inlay-{side}-{branch}',pts,[.045,.042,.038,.032,.024],wood if branch<0 else pale,7)
   pts=[Vector((side*w*.452,branch*d*.29,h*t)) for t in (.16,.32,.50,.69,.84)]
   pts[-1]=Vector((side*w*.452,branch*d*.15,h*.86))
   detail_tube(f'side-root-inlay-{side}-{branch}',pts,[.045,.042,.038,.032,.024],wood if branch<0 else pale,7)
 for side in (-1,1):
  for axis in (-1,1):
   box(f'upper-socket-stone-{side}-{axis}',(side*w*.29,axis*d*.29,h*.91),(w*.12,d*.12,h*.10),stone,False,.025)


def add_floor_detail(w,h,d,wood,pale):
 for i in range(8):
  x=-w*.5+(i+.5)*w/8
  box('split-root-board-'+str(i),(x,0,.78),(w/8-.012,d,.44),pale if i%3==0 else wood)
 for i in range(3):
  y=(i-1)*d*.35;pts=[Vector((-w*.5+w*j/24,y,.25)) for j in range(25)]
  tube('floor-joist-'+str(i),pts,[.24]*25,wood)
 for edge,y in enumerate((-d*.44,d*.44)):
  pts=[Vector((-w*.46+w*j/24,y,.66)) for j in range(25)]
  detail_tube('edge-binding-'+str(edge),pts,.055,wood,9)
 for edge,x in enumerate((-w*.44,w*.44)):
  pts=[Vector((x,-d*.46+d*j/24,.66)) for j in range(25)]
  detail_tube('end-binding-'+str(edge),pts,.055,pale,9)
 for row,y in enumerate((-d*.30,d*.30)):
  for col,x in enumerate((-w*.38,-w*.13,w*.13,w*.38)):
   box(f'root-peg-{row}-{col}',(x,y,.98),(.09,.09,.10),pale,False,.025)
 for brace in range(4):
  sx=-1 if brace<2 else 1;sy=-1 if brace%2==0 else 1
  detail_tube('underside-diagonal-'+str(brace),[Vector((sx*w*.42,sy*d*.35,.18)),Vector((0,0,.24)),Vector((-sx*w*.20,-sy*d*.16,.20))],[.055,.07,.035],wood,8)


def add_stair_detail(w,h,d,stone,wood,pale):
 for i in range(8):
  rise=h*(i+1)/8;y=d*.5-(i+.5)*d/8
  box('stone-tread-'+str(i),(0,y,rise*.5),(w,d/8,rise),stone)
 for side in (-1,1):
  for i in range(8):
   y=d*.5-(i+.5)*d/8;z=h*(i+1)/8-.1
   box('stair-root-inlay-'+str(side)+'-'+str(i),(side*(w*.5-.07),y,z),(.07,d/8*.7,.08),wood,False,.015)
  cheek=[]
  for i in range(17):
   t=i/16;cheek.append(Vector((side*(w*.5-.12),d*.45-d*.90*t,.12+h*.82*t)))
  detail_tube('grown-stair-cheek-'+str(side),cheek,[.11-.035*t for t in [i/16 for i in range(17)]],wood if side<0 else pale,9)
  for marker,t in enumerate((.12,.36,.64,.88)):
   y=d*.45-d*.90*t;z=.14+h*.82*t
   box(f'stair-carved-cheek-{side}-{marker}',(side*(w*.5-.025),y,z),(.045,d*.14,h*.10),stone,False,.018)
 for landing,t in enumerate((.08,.92)):
  y=d*.45-d*.90*t;z=.12+h*.82*t
  detail_tube('landing-root-crown-'+str(landing),[Vector((-w*.38,y,z)),Vector((0,y,z+.10)),Vector((w*.38,y,z))],[.055,.075,.055],pale,8)


def add_brace_detail(kind,w,h,wood,pale):
 junction=h*.5 if kind==6 else h*.7 if kind==8 else h-.38
 paths=[('trunk',Vector((0,0,.3)),Vector((0,0,junction)))]
 for sign in (-1,1):paths.append(('arm-'+str(sign),Vector((0,0,junction)),Vector((sign*(w*.5-.3),0,h-.3))))
 for label,start,end in paths:
  tangent=(end-start).normalized();normal=tangent.cross(Vector((0,1,0))).normalized()
  for strand in range(3):
   phase=strand*math.tau/3;pts=[];rs=[]
   for i in range(33):
    t=i/32;angle=phase+t*math.pi;p=start.lerp(end,t);p+=(normal*math.sin(angle)+Vector((0,math.cos(angle),0)))*.12*math.sin(math.pi*t)
    pts.append(p);rs.append(.23*(1+.07*math.sin(t*14+phase)))
   tube(label+'-root-'+str(strand),pts,rs,wood if strand!=1 else pale)
  for vein in range(2):
   pts=[]
   for i in range(25):
    t=.06+.88*i/24;angle=t*math.pi+vein*math.pi;pts.append(start.lerp(end,t)+normal*(.25*math.sin(angle))+Vector((0,.25*math.cos(angle),0)))
   detail_tube(label+'-vein-'+str(vein),pts,.035,pale,8)
  for ridge in range(3):
   pts=[];phase=ridge*math.tau/3
   for i in range(23):
    t=.08+.84*i/22;angle=phase+t*math.pi*1.6;pts.append(start.lerp(end,t)+normal*(.28*math.sin(angle))+Vector((0,.28*math.cos(angle),0)))
   detail_tube(label+'-ridge-'+str(ridge),pts,.028,wood if ridge!=1 else pale,7)
 # A visible grown knot binds the split rather than letting three limbs simply intersect.
 for binding in range(4):
  a=binding*math.pi*.5;center=Vector((0,0,junction));rad=.31
  pts=[center+Vector((math.cos(a+t*.35)*rad,math.sin(a+t*.35)*rad,.04*math.sin(t*math.pi))) for t in (-1,0,1)]
  detail_tube('junction-binding-'+str(binding),pts,[.055,.072,.055],pale if binding%2 else wood,8)
 for branch in range(4):
  sx=-1 if branch<2 else 1;sy=-1 if branch%2==0 else 1
  anchor=Vector((0,0,.24));detail_tube('brace-foot-'+str(branch),[anchor,Vector((sx*.18,sy*.10,.12)),Vector((sx*.32,sy*.16,.03))],[.09,.065,.03],wood,8)


for e in entries:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 wood=material('worldroot-bark',(.38,.235,.11));pale=material('worldroot-heartwood',(.52,.36,.18));iron=material('forged-iron',(.13,.15,.16),.7);stone=material('understone',(.28,.3,.28))
 kind=e['Kind'];w,h,d=[e['Dimensions'][k] for k in ['WidthMeters','HeightMeters','DepthMeters']]
 mid='rootforged-'+e['Id'].split('.')[-1].replace('_','-')
 if kind in (0,1):
  add_understone_detail(w,h,d,stone,wood,pale)
 elif kind==9:
  add_floor_detail(w,h,d,wood,pale)
 elif kind==10:
  add_stair_detail(w,h,d,stone,wood,pale)
 elif kind in (6,7,8):
  add_brace_detail(kind,w,h,wood,pale)
 else:
  def axis(t):return path_axis(kind,w,h,t)
  n=max(24,int(max(w,h)*10))
  for strand in range(3):
   phase=strand*math.tau/3;pts=[];rs=[]
   for i in range(n+1):
    t=i/n;p=axis(t);tw=phase+t*math.pi*2
    offset=Vector((math.sin(tw)*.13,math.cos(tw)*.14,0)) if kind==3 else Vector((0,math.cos(tw)*.14,math.sin(tw)*.13))
    p+=offset*math.sin(math.pi*t);pts.append(p);rs.append(.22*(1+.06*math.sin(t*19+phase)))
   tube('braided-root-'+str(strand),pts,rs,wood if strand!=1 else pale)
  for branch in range(2):
   pts=[]
   for i in range(25):
    t=.08+.84*i/24;p=axis(t);angle=t*math.tau*1.4+branch*math.pi
    offset=Vector((math.sin(angle)*.3,math.cos(angle)*.3,0)) if kind==3 else Vector((0,math.cos(angle)*.3,math.sin(angle)*.3))
    pts.append(p+offset)
   detail_tube('surface-root-vein-'+str(branch),pts,[.035+.025*math.sin(math.pi*i/24) for i in range(25)],pale,8)
  add_root_surface_detail(kind,w,h,wood,pale)
  if kind in (4,5):add_ironwork(kind,w,h,iron)
 # Fit the authored envelope to catalog meters; detail remains inside stable snap planes.
 objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
 floor=DETAIL_FLOORS.get(kind,12)
 if len(objects)<floor:raise RuntimeError(f'{mid}: Rootforged detail regression: {len(objects)} mesh parts, expected at least {floor}')
 points=[o.matrix_world @ v.co for o in objects for v in o.data.vertices]
 lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
 target=Vector((w,d,h))
 for ob in objects:
  world=ob.matrix_world.copy()
  for vertex in ob.data.vertices:
   pos=world@vertex.co
   vertex.co=Vector([(pos[i]-lo[i])/(hi[i]-lo[i])*target[i] for i in range(3)])-Vector((w*.5,d*.5,0))
  ob.matrix_world.identity()
 bpy.context.scene['model_id']=mid;bpy.context.scene['runtime_lights']='[]'
 bpy.context.scene['magenheim_family']='underworld_rootforged_placeable'
 bpy.context.scene['magenheim_detail_revision']=DETAIL_REVISION
 bpy.context.scene['magenheim_fidelity']='endgame-placeable-r3'
 bpy.context.scene['magenheim_detail_parts']=len(objects)
 bpy.context.scene['magenheim_material_language']='understone+worldroot+bark-heartwood+forged-iron'
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(R/'assets/models/source'/f'{mid}.blend'),compress=True)
 print('AUTHORED',mid,'detail-r'+str(DETAIL_REVISION),len(objects),'parts',flush=True)
