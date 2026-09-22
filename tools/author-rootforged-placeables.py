"""Author editable Rootforged construction; geometry and UVs are baked before runtime."""
import bpy,bmesh,math,json,random
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[1]
entries=json.loads((R/'default-data/foundation.json').read_text())['underworldArchitecture']['pieces']

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

def tube(name,points,radii,mat,sides=12):
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
 return mesh(name,vs,fs,uvs,mat)

def box(name,center,size,mat):
 x,y,z=center;a,b,c=[v/2 for v in size]
 vs=[(x+dx*a,y+dy*b,z+dz*c) for dx,dy,dz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
 fs=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
 ob=mesh(name,vs,fs,[[(0,0),(1,0),(1,1),(0,1)]]*6,mat)
 bevel=ob.modifiers.new('Worn edges','BEVEL');bevel.width=.035;bevel.segments=2
 return ob

for e in entries:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 wood=material('worldroot-bark',(.38,.235,.11));pale=material('worldroot-heartwood',(.52,.36,.18));iron=material('forged-iron',(.13,.15,.16),.7);stone=material('understone',(.28,.3,.28))
 kind=e['Kind'];w,h,d=[e['Dimensions'][k] for k in ['WidthMeters','HeightMeters','DepthMeters']]
 mid='rootforged-'+e['Id'].split('.')[-1].replace('_','-')
 if kind in (0,1):
  box('lower-plinth',(0,0,h*.15),(w,d,h*.3),stone)
  box('recessed-core',(0,0,h*.6),(w*.88,d*.88,h*.6),stone)
  box('upper-seat',(0,0,h*.925),(w*.96,d*.96,h*.15),stone)
  for side in [-1,1]:
   pts=[Vector((side*w*.31+.035*math.sin(t*4),0,h*t)) for t in [i/16 for i in range(17)]]
   tube('root-inlay-'+str(side),pts,[.07]*17,wood,8)
 else:
  def axis(t):
   if kind==5:return Vector(((w-.8)*(t-.5),0,.36+(h-.72)*math.sin(math.pi*t)))
   if kind==3:return Vector((0,0,h*t))
   return Vector((w*(t-.5),0,.5))
  n=max(24,int(max(w,h)*10))
  for strand in range(3):
   phase=strand*math.tau/3;pts=[];rs=[]
   for i in range(n+1):
    t=i/n;p=axis(t);tw=phase+t*math.pi*2
    offset=Vector((math.sin(tw)*.13,math.cos(tw)*.14,0)) if kind==3 else Vector((0,math.cos(tw)*.14,math.sin(tw)*.13))
    # Clean terminal cuts stay on the declared snap planes.
    p+=offset*math.sin(math.pi*t);pts.append(p);rs.append(.22*(1+.06*math.sin(t*19+phase)))
   tube('braided-root-'+str(strand),pts,rs,wood if strand!=1 else pale)
  if kind in (4,5):
   for number,t in enumerate([.12,.5,.88]):
    p=axis(t)
    # Collars follow the local tangent, with four forged straps framing the root.
    tangent=(axis(min(.999,t+.001))-axis(max(.001,t-.001))).normalized()
    bpy.ops.mesh.primitive_torus_add(major_radius=.35,minor_radius=.065,major_segments=24,minor_segments=8,location=p)
    ring=bpy.context.object;ring.name='iron-collar-'+str(number);ring.rotation_mode='QUATERNION';ring.rotation_quaternion=Vector((0,0,1)).rotation_difference(tangent);ring.data.materials.append(iron)
    ring['game_node_path']=ring.name;ring['game_collision']=False;ring['game_crystal']='null'
  # Slender raised root veins give a second detail scale without changing the structural core.
  for branch in range(2):
   pts=[]
   for i in range(25):
    t=.08+.84*i/24;p=axis(t);angle=t*math.tau*1.4+branch*math.pi
    offset=Vector((math.sin(angle)*.3,math.cos(angle)*.3,0)) if kind==3 else Vector((0,math.cos(angle)*.3,math.sin(angle)*.3))
    pts.append(p+offset)
   tube('surface-root-vein-'+str(branch),pts,[.035+.025*math.sin(math.pi*i/24) for i in range(25)],pale,8)
 # Fit the authored envelope to catalog meters; roots retain local detail inside snap planes.
 objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
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
 bpy.context.preferences.filepaths.save_version=0
 bpy.ops.wm.save_as_mainfile(filepath=str(R/'assets/models/source'/f'{mid}.blend'),compress=True)
 print('AUTHORED',mid,flush=True)
