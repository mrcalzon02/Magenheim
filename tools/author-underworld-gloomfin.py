#!/usr/bin/env python3
"""Author the Blackwater Deep Gloomfin production source model in Blender.

Art-only source authoring: muscular 1.28 m pack predator on HOST-AQUATIC-FISH.
Valheim remains authoritative for swimming/navigation/gameplay movement.
"""
from pathlib import Path
import math
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'assets/models/source/underworld-creature-gloomfin.blend'
LENGTH = 1.28

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

def mat(name, color, rough=.7):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough
    return m
SKIN=mat('Gloomfin_DorsalHide',(.035,.055,.060),.76)
BELLY=mat('Gloomfin_VentralHide',(.105,.125,.120),.63)
FIN=mat('Gloomfin_FinTissue',(.045,.075,.078),.69)
MOUTH=mat('Gloomfin_MouthTissue',(.19,.075,.065),.58)
TOOTH=mat('Gloomfin_Tooth',(.56,.54,.43),.48)
parts={}

def uv(o):
    layer=o.data.uv_layers.new(name='GloomfinUV')
    xs=[v.co.x for v in o.data.vertices]; ys=[v.co.y for v in o.data.vertices]
    xmin,xmax=min(xs),max(xs); ymin,ymax=min(ys),max(ys); dx=max(xmax-xmin,.001); dy=max(ymax-ymin,.001)
    for p in o.data.polygons:
        for li in p.loop_indices:
            co=o.data.vertices[o.data.loops[li].vertex_index].co
            layer.data[li].uv=((co.x-xmin)/dx,(co.y-ymin)/dy)

def ico(name,loc,scale,material,bone,sub=3):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); o.data.materials.append(material); uv(o); parts[name]=bone; return o

def mesh(name,verts,faces,material,bone):
    me=bpy.data.meshes.new(name+'Mesh'); me.from_pydata(verts,[],faces); me.update(); o=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(o)
    me.materials.append(material); uv(o); parts[name]=bone; return o

def cone(name,loc,radius,depth,material,bone,rot=(0,0,0)):
    bpy.ops.mesh.primitive_cone_add(vertices=10,radius1=radius,radius2=0,depth=depth,location=loc,rotation=rot); o=bpy.context.object; o.name=name
    o.data.materials.append(material); uv(o); parts[name]=bone; return o

# Primary masses: deep-chested front, narrow caudal peduncle, oversized propulsion tail.
ico('Body',(0,0,.01),(.255,.47,.245),SKIN,'Spine_2',4)
ico('VentralBody',(0,.02,-.105),(.22,.39,.13),BELLY,'Spine_2',3)
ico('Head',(0,.405,.025),(.245,.27,.215),SKIN,'Head',3)
ico('TailMuscle',(0,-.405,.015),(.18,.30,.17),SKIN,'Spine_4',3)
# Physically separate lower jaw gives the predator a readable attack silhouette.
ico('LowerJaw',(0,.555,-.105),(.185,.20,.075),MOUTH,'Jaw',3)
# Pack-readable dorsal profile: three unequal fins, largest immediately behind head.
for i,(y,h,w) in enumerate([(.18,.235,.17),(-.08,.18,.14),(-.31,.125,.11)],1):
    mesh(f'DorsalFin_{i}',[(-w/2,y,.16),(w/2,y,.16),(0,y-.12,.16),(0,y-.04,.16+h)],[(0,1,3),(1,2,3),(2,0,3)],FIN,f'Spine_{min(i+1,4)}')
# Paired pectorals and ventral fins are silhouette geometry, not painted marks.
for side,s in [('L',-1),('R',1)]:
    mesh(f'PectoralFin_{side}',[(.16*s,.20,.00),(.42*s,.03,-.04),(.18*s,-.08,-.03)],[(0,1,2)],FIN,f'Fin_{side}')
    mesh(f'PelvicFin_{side}',[(.12*s,-.18,-.09),(.27*s,-.31,-.12),(.10*s,-.36,-.08)],[(0,1,2)],FIN,'Spine_3')
# Forked caudal fin attached to the final tail bone.
mesh('CaudalFin',[(0,-.64,.02),(-.30,-.83,.17),(0,-.75,.02),(-.25,-.86,-.14),(0,-.72,.02),(.30,-.83,.17),(.25,-.86,-.14)],[(0,1,2),(2,3,4),(0,2,5),(2,4,6)],FIN,'Tail_2')
# Teeth are real geometry around the openable jaw; restrained count keeps Valheim readability.
for side,s in [('L',-1),('R',1)]:
    for i in range(4):
        y=.515+i*.038
        cone(f'ToothUpper_{side}_{i+1}',(.065*s,y,-.035),.014,.055,TOOTH,'Head',rot=(math.pi,0,0))
        cone(f'ToothLower_{side}_{i+1}',(.060*s,y+.012,-.115),.013,.050,TOOTH,'Jaw')

armdata=bpy.data.armatures.new('HOST-AQUATIC-FISH'); arm=bpy.data.objects.new('GloomfinRig',armdata); bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=armdata.edit_bones.new(name); b.head=head; b.tail=tail; b.parent=parent; return b
root=bone('Root',(0,0,-.03),(0,0,.13)); s1=bone('Spine_1',(0,.34,.02),(0,.16,.02),root); s2=bone('Spine_2',(0,.16,.02),(0,-.08,.02),s1); s3=bone('Spine_3',(0,-.08,.02),(0,-.30,.02),s2); s4=bone('Spine_4',(0,-.30,.02),(0,-.50,.02),s3); t1=bone('Tail_1',(0,-.50,.02),(0,-.66,.02),s4); t2=bone('Tail_2',(0,-.66,.02),(0,-.84,.02),t1)
head=bone('Head',(0,.16,.02),(0,.58,.02),s1); jaw=bone('Jaw',(0,.42,-.08),(0,.64,-.08),head)
for side,s in [('L',-1),('R',1)]: bone(f'Fin_{side}',(.12*s,.16,0),(.40*s,.01,-.03),s1)
bone('AttackOrigin',(0,.52,-.04),(0,.73,-.04),head); bone('MouthFX',(0,.50,-.06),(0,.68,-.06),head); bone('HitCenter',(0,-.04,0),(0,.20,0),root); bone('TailTip',(0,-.66,.02),(0,-.91,.02),t2)
bpy.ops.object.mode_set(mode='OBJECT'); arm.show_in_front=True
for name,bname in parts.items():
    o=bpy.data.objects[name]; mod=o.modifiers.new('GloomfinArmature','ARMATURE'); mod.object=arm; vg=o.vertex_groups.new(name=bname); vg.add(range(len(o.data.vertices)),1.0,'REPLACE'); o.parent=arm
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in meshes: o.data.calc_loop_triangles()
tris=sum(len(o.data.loop_triangles) for o in meshes)
if not 1.0 <= LENGTH <= 1.5: raise RuntimeError('Gloomfin length outside production band')
if len(meshes) < 25: raise RuntimeError(f'Gloomfin anatomy regression: {len(meshes)} mesh parts')
if tris < 8000: raise RuntimeError(f'Gloomfin source triangle floor regression: {tris}')
for o in meshes:
    if not o.data.uv_layers.get('GloomfinUV'): raise RuntimeError(f'Missing UVs: {o.name}')
for required in ['Jaw','Fin_L','Fin_R','Tail_1','Tail_2','AttackOrigin','MouthFX','TailTip','HitCenter']:
    if required not in arm.data.bones: raise RuntimeError(f'Missing production bone/socket: {required}')
arm['magenheim_asset']='gloomfin'; arm['production_contract']='production-creature-r1'; arm['host_rig']='HOST-AQUATIC-FISH'; arm['length_m']=LENGTH
arm['role']='blackwater-common-pack-predator'; arm['dorsal_fins']=3; arm['modeled_teeth']=16; arm['animation_contract']='swim-idle,cruise,sprint,bank-left,bank-right,bite,hit,death'
OUT.parent.mkdir(parents=True,exist_ok=True); bpy.ops.wm.save_as_mainfile(filepath=str(OUT)); print(f'Wrote {OUT} meshes={len(meshes)} tris={tris}')
