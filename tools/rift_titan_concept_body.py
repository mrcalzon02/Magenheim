"""R17: reference-led crest, rounded cage, hanging hands and planted foot groups.

Works on the separate authored components before the standard atlas/rig export.
The head and neck from r16 are preserved; budget comes from rear face ridges,
compact digit sections and lower-value pelvic/core topology.
"""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector,Matrix,geometry
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree
from maw_contact_validation import contains
import numpy as np
import rift_titan_fracture_planes as previous

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-16/rift-titan-concept-body-r17'

def build(B,arm,record,profile,tube,cap):
    OUT.mkdir(parents=True,exist_ok=True);previous.OUT=OUT
    trunk=previous.build(B,arm,record,profile,tube,cap)
    components=json.loads(bpy.context.scene['silhouette_components'])
    contacts=json.loads(bpy.context.scene['tissue_attachment_contract'])
    changes=[]
    objects=lambda:[o for o in bpy.context.scene.objects if o.type=='MESH']
    floor=min(v.co.z for o in objects() for v in o.data.vertices)
    def finish(o,unwrap=True):
        bm=bmesh.new();bm.from_mesh(o.data)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED',ngon_method='EAR_CLIP')
        bm.to_mesh(o.data);bm.free();o.data.update();o.data.calc_loop_triangles()
        if unwrap:
            bpy.context.view_layer.objects.active=o;o.select_set(True)
            bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
            bpy.ops.uv.smart_project(angle_limit=1.05,island_margin=.008)
            bpy.ops.object.mode_set(mode='OBJECT');o.data.uv_layers.active.name='BossConstructionUV';o.select_set(False)
    def replace(o,verts,faces,key,bone,root_ids=(),marker=0):
        old_count=len(o.data.loop_triangles)
        me=bpy.data.meshes.new(o.name+' concept surface');me.from_pydata(verts,[],faces)
        me.materials.append(B.mats[key]);o.data=me
        for g in list(o.vertex_groups):o.vertex_groups.remove(g)
        o.vertex_groups.new(name=bone).add(list(range(len(verts))),1,'REPLACE')
        a=me.attributes.new('attachment_component','INT','FACE')
        for d in a.data:d.value=components[o.name]
        a=me.attributes.new('attachment_endpoint','INT','POINT')
        for i in root_ids:a.data[i].value=marker
        finish(o);changes.append(dict(name=o.name,before=old_count,after=len(o.data.loop_triangles)))
        return o
    def new(name,verts,faces,key,bone,category):
        me=bpy.data.meshes.new(name);o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
        record(o,bone,category);components[name]=max(components.values())+1
        return replace(o,verts,faces,key,bone)
    def shell(front,back):
        n=len(front);pts=[Vector((p.x,p.z,0)) for p in front]
        caps=[tuple(v if isinstance(v,int) else pts.index(v) for v in t) for t in geometry.tessellate_polygon([pts])]
        return [*front,*back],[tuple(reversed(t)) for t in caps]+[tuple(n+i for i in t) for t in caps]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    # Lower the wrist along the reference's long forearm, keeping the elbow and
    # its pivot. Transform the authored bone tail and its plates together.
    arm_transforms={}
    for side,sign in (('L',-1),('R',1)):
        for j in (0,2):
            o=bpy.data.objects['ShoulderStratum'+side+str(j)];c=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
            for v in o.data.vertices:
                v.co.x=c.x+(v.co.x-c.x)*(.84 if j==0 else .88)
                v.co.z=c.z+(v.co.z-c.z)*(1.14 if j==0 else 2.0)-(.08 if j==2 else 0)
                v.co.y+=.045 if j==0 else -.015
            finish(o)
    for side,sign in (('L',-1),('R',1)):
        b=arm.data.bones['Forearm'+side];elbow=b.head_local.z;wrist=b.tail_local.z
        def fit(p,elbow=elbow,wrist=wrist):
            q=p.copy();t=max(0,min(1,(elbow-q.z)/(elbow-wrist)));q.z-=.35*t;return q
        arm_transforms[side]=fit
        for o in objects():
            if o.name.startswith(('LongFractureForearm'+side,'JointBindingForearm'+side,'Palm'+side,'FingerStratum'+side)):
                for v in o.data.vertices:v.co=fit(v.co)
            if any(r['tissue']==o.name and r['support'].startswith('LongFractureForearm'+side) for r in contacts):
                for v in o.data.vertices:v.co=fit(v.co)
    bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
    for side in ('L','R'):arm.data.edit_bones['Forearm'+side].tail.z-=.35
    bpy.ops.object.mode_set(mode='OBJECT');arm.select_set(False)
    # Compact digit surfaces retain all six root-section vertices. Individual
    # ends reach close to the foot plane, with unequal length and slight splay.
    for side,sign in (('L',-1),('R',1)):
        for j in range(3):
            o=bpy.data.objects['FingerStratum'+side+str(j)]
            p=[v.co.copy() for v in o.data.vertices]
            front=[p[i] for i in (0,1,2,3,4)];back=[p[i] for i in (6,7,8,9,10)]
            target=floor+(.095,.012,.14)[j]
            for chain in (front,back):
                for k in (0,4):
                    chain[k].z=target+(.03 if k==4 else 0)
                    center_x=sum(p.x for p in chain)/len(chain)
                    chain[k].x=center_x+(chain[k].x-center_x)*.73+sign*((j-1)*.035)
                    chain[k].y+=.025*(j-1)+(.08 if chain is front else -.035)
            marker=next(r['id'] for r in contacts if r['tissue']==o.name)
            pts,ff=shell(front,back);replace(o,pts,ff,'rift-strata','Forearm'+side,(1,2,3,6,7,8),marker)
            # A knuckle/fracture ridge breaks the long flat digit face at its
            # middle. Transfer the remaining twelve triangles into six real
            # front-face ridges, rather than drawing false cracks in texture.
            me=o.data
            candidates=[f for f in me.polygons if all(i<5 for i in f.vertices)]
            chosen=max(candidates,key=lambda f:f.area)
            verts=[v.co.copy() for v in me.vertices]
            ridged=sum((me.vertices[i].co for i in chosen.vertices),Vector())/3+Vector((sign*.015,.065,.02*(j-1)))
            faces=[tuple(f.vertices) for f in me.polygons if f.index!=chosen.index]
            a,b,c=chosen.vertices;idx=len(verts);verts.append(ridged);faces.extend(((a,b,idx),(b,c,idx),(c,a,idx)))
            replace(o,verts,faces,'rift-strata','Forearm'+side,(1,2,3,6,7,8),marker)
    # Remove rear-only face hubs from plates without endpoint/blend markers.
    # Perimeters and front relief stay intact, freeing two triangles per plate.
    for o in list(objects()):
        if not o.name.startswith('LongFracture'):continue
        if len(o.data.vertices)!=14 or any(v.value for v in o.data.attributes['attachment_endpoint'].data):continue
        if 'Forearm' in o.name and o.name.endswith('1') or 'Leg' in o.name and o.name.endswith('1'):continue
        me=o.data;bm=bmesh.new();bm.from_mesh(me);bm.verts.ensure_lookup_table()
        bmesh.ops.dissolve_verts(bm,verts=[bm.verts[12]],use_face_split=False,use_boundary_tear=False)
        bm.to_mesh(me);bm.free();finish(o)
        changes.append(dict(name=o.name,change='rear face hub dissolved; visible front and perimeter retained'))
    # Narrow hanging pelvic rocks instead of a broad horizontal bridge.
    for name in ('FloatingStratum2','FloatingStratum3','PelvisKeystone1'):
        o=bpy.data.objects[name];p=[v.co.copy() for v in o.data.vertices]
        front=[p[i] for i in ((0,2,3,5) if name.startswith('Floating') else (0,1,2,3,4))]
        back=[p[i+6] for i in ((0,2,3,5) if name.startswith('Floating') else (0,1,2,3,4))]
        c=sum(front+back,Vector())/len(front+back)
        for p in front+back:p.x=c.x+(p.x-c.x)*.66;p.z=c.z+(p.z-c.z)*1.18
        pts,ff=shell(front,back);replace(o,pts,ff,'rift-strata','Body')
    # Rounded upper/lower cage envelope, still using the continuous twelve ribs
    # and thin full-circumference stone wrap. Their supported ends are fixed.
    def bowl(p):
        q=p.copy();t=max(0,min(1,(p.z-1.65)/(2.76-1.65)));s=math.sin(math.pi*t)
        q.x*=1+.18*s;q.y=.14+(q.y-.14)*(1+.26*s);return q
    for o in objects():
        if o.name.startswith('ContinuousTorsoRib') or o.name=='WrappedStoneBand':
            marker=o.data.attributes['attachment_endpoint']
            for v in o.data.vertices:
                if o.name=='WrappedStoneBand' or not marker.data[v.index].value:v.co=bowl(v.co)
            if o.name.startswith('ContinuousTorsoRib'):
                j=int(o.name.removeprefix('ContinuousTorsoRib'));rows=len(o.data.vertices)//3
                for row in range(1,rows-1):
                    ids=list(range(row*3,row*3+3));c=sum((o.data.vertices[i].co for i in ids),Vector())/3
                    amount=.09 if row==1 else -.045
                    offset=Vector((amount*math.cos(j*math.tau/12),-.03*math.sin(j*math.tau/12),0))
                    for i in ids:
                        if not marker.data[i].value:o.data.vertices[i].co=c+(o.data.vertices[i].co-c)*1.45+offset
            finish(o)
    # Six interlocking unequal core crystals replace four isolated upright
    # stakes. Their facets use deliberate dark/bright violet, not uniform glow.
    crystal=B.mats['binding-prism'];n=crystal.node_tree.nodes;l=crystal.node_tree.links;n.clear()
    geo=n.new('ShaderNodeNewGeometry');dot=n.new('ShaderNodeVectorMath');dot.operation='DOT_PRODUCT';dot.inputs[1].default_value=(.38,.65,.65);l.new(geo.outputs['Normal'],dot.inputs[0])
    ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=-.8;ramp.color_ramp.elements[0].color=(.012,.002,.025,1);ramp.color_ramp.elements[1].position=.9;ramp.color_ramp.elements[1].color=(.29,.11,.40,1)
    e=ramp.color_ramp.elements.new(.3);e.color=(.075,.012,.17,1);l.new(dot.outputs['Value'],ramp.inputs[0])
    bs=n.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=.42;bs.inputs['Emission Strength'].default_value=.28;l.new(ramp.outputs[0],bs.inputs['Base Color']);l.new(ramp.outputs[0],bs.inputs['Emission Color'])
    output=n.new('ShaderNodeOutputMaterial');l.new(bs.outputs[0],output.inputs[0])
    core_specs=[((-.03,.14,2.38),(.24,.18,.36),(.18,-.28,.13)),((-.24,.18,2.20),(.16,.13,.43),(-.21,.32,-.18)),((.24,.20,2.27),(.17,.13,.37),(.19,-.35,.2)),((.05,-.01,2.15),(.22,.17,.40),(-.20,.42,-.1)),((-.12,.22,1.94),(.15,.10,.24),(.45,-.5,.13)),((.13,.25,2.42),(.15,.13,.24),(-.5,.35,-.2))]
    for j,(center,dim,angles) in enumerate(core_specs):
        c=Vector(center);rot=Matrix.Rotation(angles[0],3,'X')@Matrix.Rotation(angles[1],3,'Y')@Matrix.Rotation(angles[2],3,'Z')
        pts=[c+rot@Vector(v) for v in ((0,0,dim[2]),(0,0,-dim[2]),(dim[0],0,0),(0,dim[1],0),(-dim[0],0,0),(0,-dim[1],0))]
        ff=[(a,2+k,2+(k+1)%4) if a==0 else (a,2+(k+1)%4,2+k) for a in (0,1) for k in range(4)]
        if j<4:replace(bpy.data.objects['SuspendedCore'+str(j)],pts,ff,'binding-prism','Body')
        else:new('CoreFracture'+str(j),pts,ff,'binding-prism','Body','binding-crystals')
    # A broken raised architecture BEHIND the shoulders, plainly visible in
    # the supplied side sheet; these fragments are deliberately suspended.
    for j,(c,w,h,d,tilt) in enumerate([((-.72,-.50,3.70),.42,.075,.11,-.08),((.68,-.64,3.59),.37,.082,.12,.13),((.02,-.73,3.24),1.02,.09,.13,-.08)]):
        c=Vector(c);front=[];back=[]
        for y,chain in ((d,front),(-d,back)):
            for x,z in ((-w,-h),(-w*.94,h*.7),(w,h),(w*.91,-h*.8)):
                chain.append(c+Vector((x,y,z+x*tilt)))
        pts,ff=shell(front,back);new('BrokenCrestBeam'+str(j),pts,ff,'rift-strata','Body','crest')
    for j,c in enumerate(((-.96,-.44,3.98),(.87,-.58,3.92),(.40,-.75,4.12))):
        c=Vector(c);pts=[c+Vector(v) for v in ((-.06,0,-.11),(.055,.04,-.06),(.035,-.04,.13),(-.04,-.045,.07))]
        new('RaisedCrestFragment'+str(j),pts,[(0,1,2),(0,3,1),(0,2,3),(1,3,2)],'rift-strata','Body','crest')
    for j,c in enumerate(((-.72,.04,3.93),(.65,-.04,3.87),(-1.03,-.08,3.50))):
        c=Vector(c);r=.045+j*.009;pts=[c+Vector((0,0,.13)),c-Vector((0,0,.10))]+[c+Vector((r*math.cos(k*math.tau/3),r*math.sin(k*math.tau/3),0)) for k in range(3)]
        ff=[(0,2+k,2+(k+1)%3) for k in range(3)]+[(1,2+(k+1)%3,2+k) for k in range(3)]
        new('CrestVioletMote'+str(j),pts,ff,'binding-prism','Body','crest')
    # Irregular planted footprints with a modeled cleft, above the original
    # rest model's lowest plane. Keep that plane so normalization cannot move
    # the accepted head landmarks.
    for side,sign in (('L',-1),('R',1)):
        foot=bpy.data.objects['Foot'+side];c=sum((v.co for v in foot.data.vertices),Vector())/len(foot.data.vertices)
        footprint=[(-.28,-.12),(-.25,.28),(-.06,.42),(.015,.31),(.075,.43),(.27,.34),(.29,-.02),(.14,-.19)]
        pts=[]
        for upper in (False,True):
            for j,(x,y) in enumerate(footprint):pts.append((c.x+x*(.90 if upper else 1),c.y+y, floor+(.27+(.035*math.sin(j*2)) if upper else 0)))
        ff=[tuple(reversed(range(8))),tuple(range(8,16))]+[(j,(j+1)%8,(j+1)%8+8,j+8) for j in range(8)]
        replace(foot,pts,ff,'rift-strata','Leg'+side)
        def shin_fit(p):
            q=p.copy()
            if q.z<.24:q.z=floor+.17+(.24-floor-.17)*max(0,(q.z-floor)/(.24-floor))
            return q
        for o in objects():
            if o.name=='LongFractureLeg'+side+'2' or any(r['tissue']==o.name and r['support']=='LongFractureLeg'+side+'2' for r in contacts):
                for v in o.data.vertices:v.co=shin_fit(v.co)
                finish(o)
    # Replace cloudy global stone with the restrained directional head palette;
    # per-component depth and real facets establish the large fractures.
    stone=previous.stone_material();stone.name='Rift cool fractured stone';B.mats['rift-strata']=stone
    nodes=stone.node_tree.nodes;links=stone.node_tree.links;nodes.clear()
    bs=nodes.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=.86
    pigment=nodes.new('ShaderNodeVertexColor');pigment.layer_name='RiftStonePigment'
    ao=nodes.new('ShaderNodeAmbientOcclusion');ao.inputs['Distance'].default_value=.13
    shade=nodes.new('ShaderNodeMixRGB');shade.blend_type='MULTIPLY';shade.inputs[0].default_value=.72
    links.new(pigment.outputs['Color'],shade.inputs[1]);links.new(ao.outputs['Color'],shade.inputs[2]);links.new(shade.outputs[0],bs.inputs['Base Color'])
    bevel=nodes.new('ShaderNodeBevel');bevel.inputs['Radius'].default_value=.008;bevel.samples=4;links.new(bevel.outputs['Normal'],bs.inputs['Normal'])
    output=nodes.new('ShaderNodeOutputMaterial');links.new(bs.outputs[0],output.inputs[0])
    for o in objects():
        if o.name=='UnifiedCrystalHead':continue
        if o.name.startswith(('ShoulderStratum','LongFracture','Palm','FingerStratum','Foot','Pelvis','FloatingStratum','StoneCollar','WrappedStone','LowerRib','BrokenCrest','RaisedCrest')):
            for i in range(len(o.data.materials)):o.data.materials[i]=stone
            attr=o.data.color_attributes.new(name='RiftStonePigment',type='FLOAT_COLOR',domain='CORNER')
            seed=sum(ord(c) for c in o.name)
            for f in o.data.polygons:
                # Pigment groups follow the actual fracture-plane normals;
                # long faces remain coherent instead of becoming marble noise.
                top=max(0,f.normal.z);front=max(0,f.normal.y)
                tint=.86+.12*math.sin(seed*1.7)
                color=((.040+.075*top)*tint,(.046+.064*top)*tint,(.057+.044*top)*tint,1)
                for li in f.loop_indices:attr.data[li].color=color
    # Re-seat complete marked roots after final support shapes, then audit the
    # actual cap interiors. Nothing is admitted by merely testing its centroid.
    for r in contacts:
        o=bpy.data.objects[r['tissue']];host=bpy.data.objects[r['support']]
        ids=[v.index for v in o.data.vertices if o.data.attributes['attachment_endpoint'].data[v.index].value==r['id']]
        if r['tissue'].startswith('NeckMetalRib'):continue
        for i in ids:
            p,w=embed(o.data.vertices[i].co,host,.014);assign(o,i,p,w)
        finish(o,False)
        bvh=tree(host);tri=np.array([[list(host.data.vertices[i].co) for i in t.vertices] for t in host.data.loop_triangles]);marked=set(ids)
        caps=[tuple(t.vertices) for t in o.data.loop_triangles if set(t.vertices)<=marked]
        def enclosed():
            points=[o.data.vertices[i].co for i in ids]
            for a,b,c in caps:
                a,b,c=(o.data.vertices[i].co for i in (a,b,c));points.extend((a+b+c)/3 for _ in (0,));points.extend(((a+b)/2,(b+c)/2,(c+a)/2,a*.5+b*.25+c*.25,a*.25+b*.5+c*.25,a*.25+b*.25+c*.5))
            return all(contains(p,bvh,tri) for p in points)
        if enclosed():continue
        original=[o.data.vertices[i].co.copy() for i in ids];anchor,w=embed(sum(original,Vector())/len(ids),host,.035)
        for amount in (.85,.65,.45,.25,.1):
            for i,p in zip(ids,original):assign(o,i,anchor+(p-anchor)*amount,w)
            if enclosed():break
        else:raise ValueError('root remains exposed: '+o.name)
        finish(o,False)
    bpy.context.scene['silhouette_components']=json.dumps(components);bpy.context.scene['tissue_attachment_contract']=json.dumps(contacts)
    count=sum(len(o.data.loop_triangles) for o in objects());assert count<=1800,count
    (OUT/'body-authoring.json').write_text(json.dumps(dict(changes=changes,actor_triangles=count,foot_plane_authored=floor,reference='unaltered adopted front and left-facing side; long hands, raised rear crest, rounded densely occupied core cage, hanging pelvic rocks and cleft foot masses',preserved='r16 head and neck; eight bone identities/hierarchy, seven actions; forearm tails fitted to longer reference arms',acceptance='candidate; no visual or runtime acceptance inferred'),indent=2)+'\n')
    return trunk
