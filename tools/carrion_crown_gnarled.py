"""Carrion Crown r5: narrower gnarled timber and genuinely fluted flower mouths.

Reference: the adopted sheet, especially its front and side anatomy. This
replaces the old flat flower mask and solid barrel, rather than decimating them.
"""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector
from first_bloom_frill_sculpt import assign
from first_bloom_deeper_chest import embed
from first_bloom_attachment_repair import tree
from maw_contact_validation import contains
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-25/carrion-crown-gnarled-r5'

def materials(B):
    palettes={
        'decay-root':((.018,.021,.015),(.060,.057,.040),(.145,.126,.084)),
        'carrion-sepal':((.014,.006,.002),(.075,.025,.007),(.19,.082,.022)),
        'sepal-edge':((.025,.023,.016),(.072,.065,.044),(.15,.13,.085)),
        'ivory':((.065,.056,.035),(.20,.18,.12),(.36,.31,.20)),
        'amber-organ':((.06,.018,.001),(.29,.105,.005),(.51,.25,.018)),
        'cavity':((.004,.004,.002),(.014,.010,.004),(.035,.023,.006)),
        'lichen':((.018,.023,.008),(.055,.060,.018),(.105,.105,.026))}
    for key,colors in palettes.items():
        mat=bpy.data.materials.new('Carrion '+key);mat.use_nodes=True;n=mat.node_tree.nodes;l=mat.node_tree.links
        bs=n.get('Principled BSDF');bs.inputs['Roughness'].default_value=.88 if key!='amber-organ' else .47
        geo=n.new('ShaderNodeNewGeometry');coord=n.new('ShaderNodeAttribute');coord.attribute_name='CrownGrain'
        sc=n.new('ShaderNodeVectorMath');sc.operation='MULTIPLY';sc.inputs[1].default_value=(4.5,3.5,1.25);l.new(coord.outputs['Vector'],sc.inputs[0])
        cell=n.new('ShaderNodeTexVoronoi');cell.distance='MANHATTAN';cell.inputs['Scale'].default_value=1.7;l.new(sc.outputs[0],cell.inputs['Vector'])
        noise=n.new('ShaderNodeRGBToBW');l.new(cell.outputs['Color'],noise.inputs[0]);signal=noise.outputs[0]
        ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.interpolation='CONSTANT'
        ramp.color_ramp.elements[0].position=.27;ramp.color_ramp.elements[0].color=(*colors[0],1)
        ramp.color_ramp.elements[1].position=.72;ramp.color_ramp.elements[1].color=(*colors[2],1)
        e=ramp.color_ramp.elements.new(.48);e.color=(*colors[1],1);l.new(signal,ramp.inputs[0])
        ao=n.new('ShaderNodeAmbientOcclusion');ao.inputs['Distance'].default_value=.065
        shade=n.new('ShaderNodeMixRGB');shade.blend_type='MULTIPLY';shade.inputs[0].default_value=.60;l.new(ramp.outputs[0],shade.inputs[1]);l.new(ao.outputs['Color'],shade.inputs[2]);l.new(shade.outputs[0],bs.inputs['Base Color'])
        seam=n.new('ShaderNodeTexVoronoi');seam.feature='DISTANCE_TO_EDGE';seam.inputs['Scale'].default_value=2.8;l.new(sc.outputs[0],seam.inputs['Vector'])
        bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.24;bump.inputs['Distance'].default_value=.008;l.new(seam.outputs['Distance'],bump.inputs['Height']);l.new(bump.outputs[0],bs.inputs['Normal'])
        if key=='amber-organ':
            pores=n.new('ShaderNodeTexVoronoi');pores.inputs['Scale'].default_value=25;l.new(geo.outputs['Position'],pores.inputs['Vector'])
            mask=n.new('ShaderNodeValToRGB');mask.color_ramp.elements[0].position=.15;mask.color_ramp.elements[0].color=(.008,.004,.001,1);mask.color_ramp.elements[1].position=.29;l.new(pores.outputs['Distance'],mask.inputs[0])
            tint=n.new('ShaderNodeVertexColor');tint.layer_name='CrownOrganTint'
            color=n.new('ShaderNodeMixRGB');color.blend_type='MULTIPLY';color.inputs[0].default_value=1;l.new(tint.outputs[0],color.inputs[1]);l.new(mask.outputs[0],color.inputs[2]);l.new(color.outputs[0],bs.inputs['Base Color']);l.new(color.outputs[0],bs.inputs['Emission Color']);bs.inputs['Emission Strength'].default_value=.18
            l.new(mask.outputs[0],bump.inputs['Height']);bump.inputs['Distance'].default_value=.012;bump.inputs['Strength'].default_value=.35
        B.mats[key]=mat

def build(B,arm,record,profile,tube,cap):
    OUT.mkdir(parents=True,exist_ok=True);materials(B)
    components={};contacts=[]
    def finish(o):
        bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED');bm.to_mesh(o.data);bm.free();o.data.update();o.data.calc_loop_triangles()
        bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.0,island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT');o.data.uv_layers.active.name='BossConstructionUV';o.select_set(False)
    def solid(name,points,faces,key,bone,category):
        me=bpy.data.meshes.new(name);me.from_pydata(points,[],faces);me.materials.append(B.mats[key]);o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);record(o,bone,category)
        components[name]=len(components)+1;a=me.attributes.new('attachment_component','INT','FACE')
        for d in a.data:d.value=components[name]
        me.attributes.new('attachment_endpoint','INT','POINT');finish(o);return o
    def branch(name,pts,radii,bone='Stalk_2',sides=4,key='decay-root',category='timber'):
        # Transport a polygon section along the branch; all ends are closed.
        pts=[Vector(p) for p in pts];verts=[];normal=Vector((1,0,0));last=None
        for row,(p,r) in enumerate(zip(pts,radii)):
            tangent=(pts[min(row+1,len(pts)-1)]-pts[max(0,row-1)]).normalized()
            if last is not None:normal=last.rotation_difference(tangent)@normal
            if abs(normal.dot(tangent))>.99:normal=Vector((0,1,0))
            normal=(normal-tangent*normal.dot(tangent)).normalized();cross=tangent.cross(normal)
            for k in range(sides):
                a=k*math.tau/sides;verts.append(p+r*(normal*math.cos(a)+cross*.78*math.sin(a)))
            last=tangent
        faces=[]
        for row in range(len(pts)-1):
            for k in range(sides):a=row*sides+k;b=row*sides+(k+1)%sides;faces.append((a,b,b+sides,a+sides))
        faces.extend((tuple(reversed(range(sides))),tuple((len(pts)-1)*sides+k for k in range(sides))))
        return solid(name,verts,faces,key,bone,category)
    def seat(o,ids,host,label):
        marker=len(contacts)+1
        for i in ids:
            p,w=embed(o.data.vertices[i].co,host,.012);assign(o,i,p,w);o.data.attributes['attachment_endpoint'].data[i].value=marker
        finish(o);bvh=tree(host);tris=np.array([[list(host.data.vertices[i].co) for i in t.vertices] for t in host.data.loop_triangles]);marked=set(ids)
        caps=[tuple(t.vertices) for t in o.data.loop_triangles if set(t.vertices)<=marked]
        def enclosed():
            pts=[o.data.vertices[i].co for i in ids]
            for face in caps:
                a,b,c=[o.data.vertices[i].co for i in face];pts.extend(((a+b+c)/3,(a+b)/2,(b+c)/2,(c+a)/2,a*.5+b*.25+c*.25,a*.25+b*.5+c*.25,a*.25+b*.25+c*.5))
            return all(contains(p,bvh,tris) for p in pts)
        if not enclosed():
            old=[o.data.vertices[i].co.copy() for i in ids];anchor,w=embed(sum(old,Vector())/len(old),host,.03)
            for t in (.8,.6,.4,.2,.08,.02,.005):
                for i,p in zip(ids,old):assign(o,i,anchor+(p-anchor)*t,w)
                if enclosed():break
            else:
                from first_bloom_attachment_repair import bone_weights
                candidates=[]
                for ti,tri in enumerate(host.data.loop_triangles):
                    a,b,c=[host.data.vertices[i].co for i in tri.vertices];center=(a+b+c)/3;normal=(b-a).cross(c-a).normalized()
                    candidates.append(((center-anchor).length,ti,center,normal))
                fixed=False
                for _,ti,center,normal in sorted(candidates,key=lambda row:row[0])[:32]:
                    for depth in (.003,.006,.012):
                        for sign in (-1,1):
                            target=center+normal*depth*sign
                            if not contains(target,bvh,tris):continue
                            weights=bone_weights(host,ti,center)
                            for i,p in zip(ids,old):assign(o,i,target+(p-anchor)*.005,weights)
                            if enclosed():fixed=True;break
                        if fixed:break
                    if fixed:break
                if not fixed:raise ValueError('unseated '+o.name)
        contacts.append(dict(id=marker,tissue=o.name,end=label,support=host.name,support_component=components[host.name],endpoint_seating='complete named root section, cap interiors checked, matching support weights'))
    # Closed thin-walled timber shell with an actual frontal abdominal opening.
    stations=((.04,.34,.25),(.36,.27,.24),(.76,.20,.18),(1.12,.23,.19),(1.44,.20,.18))
    verts=[];n=12
    for inner in (False,True):
        for row,(z,rx,ry) in enumerate(stations):
            for k in range(n):
                a=k*math.tau/n+.045*math.sin(row*1.7);inset=.045 if inner else 0;r=1+.20*math.sin(k*2.3+row*.8)
                dz=.055*math.sin(k*1.7+row*.8) if row in (1,2,3) else 0
                verts.append(((rx-inset)*math.sin(a)*r+.028*math.sin(row*1.6),-.04+(ry-inset)*math.cos(a)*r,z+dz))
    faces=[];face_roles=[]
    omitted={(1,k) for k in (10,11,0,1)}|{(2,k) for k in (11,0)}
    for inner in (False,True):
        off=int(inner)*60
        for row in range(4):
            for k in range(n):
                if (row,k) in omitted:continue
                a=off+row*n+k;b=off+row*n+(k+1)%n
                faces.append((a,b,b+n,a+n) if not inner else (a+n,b+n,b,a));face_roles.append(int(inner))
    # Bridge every boundary of the omitted patch as well as the shell's ends.
    edges={}
    for f in faces[:42]:
        for a,b in zip(f,(*f[1:],f[0])):key=tuple(sorted((a,b)));edges[key]=edges.get(key,0)+1
    for (a,b),count in edges.items():
        if count==1:faces.append((a,b,b+60,a+60));face_roles.append(0)
    trunk=solid('HollowTimberTrunk',verts,faces,'decay-root','Stalk_2','trunk')
    trunk.data.materials.append(B.mats['cavity'])
    # The inside wall stays much darker than bark; this is a real shell cavity.
    for f in trunk.data.polygons:
        if all(i>=60 for i in f.vertices):f.material_index=1
    for v in trunk.data.vertices:assign(trunk,v.index,v.co,{'Stalk_2':1})
    # Broad buttress roots with upper sections buried in that actual shell.
    roots=[]
    for j in range(8):
        a=j*math.tau/8+.16;s=math.sin(a);c=math.cos(a);r=1.02+.15*math.sin(j*2.1)
        o=branch('ButtressRoot'+str(j),[(s*.23,c*.16-.04,.59),(math.sin(a+.10)*.31,math.cos(a+.10)*.24,.30),(math.sin(a-.11)*.53,math.cos(a-.11)*.41,.10),(s*r*.74,c*.60,.025)],[.075,.12,.09,.015],'Root',5)
        # Upper root and middle section blend into the moving central timber.
        for v in o.data.vertices:
            t=v.index//5;w=(1,.6,0,0)[t];assign(o,v.index,v.co,{'Root':1-w,'Stalk_2':w})
        seat(o,list(range(5)),trunk,'buttress-to-trunk');roots.append(o)
    # Gnarled paired upper/lower branches; four different stalk bones retain
    # the original animation identities, with support-matched proximal rings.
    arms=[]
    for j,(s,z,bone) in enumerate(((-1,1.19,'Stalk_1'),(1,1.17,'Stalk_4'),(-1,.89,'Stalk_3'),(1,.82,'Stalk_4'))):
        endx=s*(.91 if j<2 else 1.02)
        o=branch('GraftedBranch'+str(j),[(s*.18,-.03,z),(s*.40,-.045,z+.17-.02*j),(s*.72,.07,z+.11-.01*j),(endx,.18,z-.12)],[.060,.050,.032,.012],bone,5)
        seat(o,list(range(5)),trunk,'grafted-branch-root');arms.append(o)
    def cup(name,c,r,bone,n=10):
        c=Vector(c);verts=[]
        # Five-ring cup: rear support, torn primary sepals, offset overlapping
        # secondary sepals, thick inner lip and deeply recessed floor. The
        # overlap ring spends the remaining donor budget on the concept's
        # broken crown silhouette instead of tertiary surface noise.
        phase=(sum(ord(ch) for ch in name)%17)*.13
        rings=((.43,-.15),(1,.115),(.94,.055),(.77,.09),(.20,-.22))
        for ring,(scale,depth) in enumerate(rings):
            for k in range(n):
                a=k*math.tau/n
                aa=a+(.075*math.sin(k*1.91+phase) if ring==2 else 0)
                irregular=1+.10*math.sin(k*2.2+.7)
                if ring==1:
                    torn=1+.07*math.sin(k*1.7+phase)+(.12 if math.sin(a)>.3 else 0)
                elif ring==2:
                    torn=1+.055*math.sin(k*1.7+phase+1.1)
                else:
                    torn=1
                flare=(1.04+.07*math.sin(k*2.7)) if ring==1 else 1
                rr=r*scale*irregular*torn*flare
                x=math.cos(aa)*rr;z=math.sin(aa)*rr
                if ring in (1,2):
                    z*=1.12 if z>0 else .82
                    if z>0:x+=r*.08*math.sin(phase)
                fold=(.035*math.sin(k*2.1) if ring==1 else 0)+(.028*math.cos(k*2.4+phase) if ring==2 else 0)
                p=Vector((x,depth-z*.85+fold,z));yaw=-math.copysign(.42,c.x) if abs(c.x)>.1 else .055
                p.x,p.y=p.x*math.cos(yaw)-p.y*math.sin(yaw),p.x*math.sin(yaw)+p.y*math.cos(yaw)
                verts.append(c+p)
        ff=[]
        for row in range(4):
            for k in range(n):a=row*n+k;b=row*n+(k+1)%n;ff.append((a,b,b+n,a+n))
        ff.extend((tuple(reversed(range(n))),tuple(4*n+k for k in range(n))))
        o=solid(name,verts,ff,'carrion-sepal',bone,'flower-cups');o.data.materials.append(B.mats['cavity']);o.data.materials.append(B.mats['sepal-edge']);o.data.materials.append(B.mats['decay-root'])
        for f in o.data.polygons:
            if all(i>=4*n for i in f.vertices):f.material_index=1
            elif all(n<=i<3*n for i in f.vertices):f.material_index=2
            elif all(i<2*n for i in f.vertices):f.material_index=3
        return o
    main=cup('CentralCrownCup',(0,.30,1.60),.33,'Stalk_2',12)
    seat(main,[8,9,10],trunk,'central-crown-root')
    flowers=[(main,Vector((0,.30,1.60)),.33,'Stalk_2',12)]
    for j,(s,z,r,host) in enumerate(((-1,1.36,.17,arms[0]),(1,1.33,.18,arms[1]),(-1,.91,.12,arms[2]),(1,.83,.13,arms[3]))):
        bone=('Stalk_1','Stalk_4','Stalk_3','Stalk_4')[j]
        c=Vector((s*(.60 if j<2 else .70),.14,z));flower=cup('GraftedFlower'+str(j),c,r,bone,8 if j<2 else 6)
        count=8 if j<2 else 6;ids=[count//2+count//4-1,count//2+count//4,count//2+count//4+1]
        anchor=sum((host.data.vertices[i].co for i in range(10,15)),Vector())/5
        center=sum((flower.data.vertices[i].co for i in ids),Vector())/3
        for i in ids:flower.data.vertices[i].co=anchor+(flower.data.vertices[i].co-center)*.2
        seat(flower,ids,host,'flower-branch-root');flowers.append((flower,c,r,bone,count))
    # Unequal timber teeth rise inward from each cup's actual rim. A complete
    # triangular root is embedded; the spike is a thick closed taper.
    for j,(flower,c,r,bone,count) in enumerate(flowers):
        teeth=14 if j==0 else 7 if j<3 else 5
        for k in range(teeth):
            a=k*math.tau/teeth+.13*j;x=math.cos(a);z=math.sin(a);p=c+Vector((x*r*.80,.115-z*r*.68,z*r*.80))
            tip=p+Vector((-x*r*.33,.06,-z*r*.32+(.055 if j==0 else .025)))*(1+.25*math.sin(k*2))
            axis=(tip-p).normalized();right=axis.cross(Vector((0,0,1)))
            if right.length<.01:right=axis.cross(Vector((1,0,0)))
            right.normalize();up=axis.cross(right);radius=r*.065
            points=[p+radius*(right*math.cos(t*math.tau/3)+up*math.sin(t*math.tau/3)) for t in range(3)]+[tip]
            o=solid('TimberTooth'+str(j)+'_'+str(k),points,[(2,1,0),(0,1,3),(1,2,3),(2,0,3)],'ivory',bone,'teeth');seat(o,[0,1,2],flower,'tooth-rim-root')
    # Angular backing sepals and long ragged crown spires, rooted into timber.
    spires=[]
    for j in range(5):
        a=(.10+j/4*.80)*math.pi;x=math.cos(a);z=math.sin(a)
        p=Vector((x*.29,-.12,1.50+z*.16));tip=p+Vector((x*.12,-.07,.30+z*.20))
        o=branch('CrownTimberSpire'+str(j),[p,p.lerp(tip,.60)+Vector((.025,0,0)),tip],[.065,.045,.002],'Stalk_2',3,'decay-root','crown-spires');seat(o,[0,1,2],trunk,'crown-spire-root');spires.append(o)
    for j,sign in enumerate((-1,1)):
        host=spires[0 if sign>0 else 4];anchor=sum((host.data.vertices[i].co for i in range(3,6)),Vector())/3
        o=branch('ForkedCrownTimber'+str(j),[anchor,anchor+Vector((sign*.10,-.02,.08)),anchor+Vector((sign*.17,.02,.20))],[.027,.02,.002],'Stalk_2',3,'decay-root','crown-spires');seat(o,[0,1,2],host,'crown-fork-root')
    # Amber organs and pods have polygonal raised ribs and recessed dark pores
    # in their owned surface maps; six branches carry individually hung pods.
    def pod(name,c,rx,rz,bone,sides=5):
        c=Vector(c);verts=[]
        for row,(z,r) in enumerate(((-1,.10),(-.52,.82),(.16,1),(.84,.46))):
            for k in range(sides):
                a=k*math.tau/sides;verts.append(c+Vector((rx*r*math.cos(a),rx*r*.78*math.sin(a),rz*z)))
        ff=[]
        for row in range(3):
            for k in range(sides):a=row*sides+k;b=row*sides+(k+1)%sides;ff.append((a,b,b+sides,a+sides))
        ff.extend((tuple(reversed(range(sides))),tuple(3*sides+k for k in range(sides))))
        return solid(name,verts,ff,'amber-organ',bone,'amber-pods')
    organ=pod('CentralAmberSeed',(0,.23,1.56),.12,.14,'Stalk_2',6);seat(organ,[0,1,2,3,4,5],main,'central-seed-root')
    for j in range(6):
        host=arms[j%4];bone=('Stalk_1','Stalk_4','Stalk_3','Stalk_4')[j%4];s=-1 if j%2==0 else 1
        top=Vector((s*(.73+.12*(j//2)),.16,1.08-.16*(j%4//2)));bottom=top+Vector((s*.015,.02,-.23-.045*(j%3)))
        stem=branch('PodStem'+str(j),[top,bottom],[.013,.009],bone,3,category='pod-stems');seat(stem,[0,1,2],host,'pod-stem-branch-root')
        o=pod('HangingAmberPod'+str(j),bottom-Vector((0,0,.105)),.06+.009*(j%3),.115+.02*(j%2),bone)
        seat(o,list(range(15,20)),stem,'pod-stem-seat')
    # Diagonal ivory ribs bridge the real abdominal opening and leave irregular
    # dark gaps. They seat at their outer end in the hollow timber wall.
    for s in (-1,1):
        for j in range(4):
            z=((.42,.58,.78,.94) if s<0 else (.35,.55,.70,.90))[j];p=Vector((s*(.20+.025*math.sin(j*1.9)),.14,z+.11));tip=Vector((-s*(.035+.075*math.cos(j*1.3)),.25-.012*j,z-.015*j))
            o=branch('AbdominalRib'+str(s)+'_'+str(j),[p,p.lerp(tip,.55)+Vector((0,.04,.03)),tip],[.024,.030,.006],'Stalk_2',3,'ivory','ivory-ribs');seat(o,[0,1,2],trunk,'rib-timber-root')
    # Subordinate split roots repeat the trunk's radial flow at varied lengths.
    for j in range(6):
        a=j*math.tau/8+.16;s=math.sin(a);c=math.cos(a);host=roots[j]
        anchor=sum((host.data.vertices[i].co for i in range(10,15)),Vector())/5
        o=branch('RootSplinter'+str(j),[anchor,(s*.82+.08,c*.58,.07),(s*(.96+.12*(j%2))+.06,c*.77,.022)],[.04,.032,.002],'Root',3)
        seat(o,[0,1,2],host,'split-root-seat')
    # Gnarled rear timber rises behind the open crown in the side reference.
    # Broad folded cup lips above are part of the cup itself, not paddle props.
    for j,s in enumerate((-1,0,1)):
        o=branch('DorsalCrownTimber'+str(j),[(s*.16,-.12,1.10),(s*.24,-.23,1.43),(s*.30+.035,-.25,1.73),(s*.26-.035,-.29,1.95+(.10 if j==1 else -.05))],[.075,.065,.045,.002],'Stalk_2',4)
        seat(o,[0,1,2,3],trunk,'dorsal-timber-root')
    for j in range(6):
        s=-1 if j%2==0 else 1;x=s*(.18+.025*(j//2));z=1.15-.17*(j//2)
        o=branch('HangingLichen'+str(j),[(x,.20,z),(x+s*.035,.23,z-.14),(x+s*.01,.25,z-.25)],[.025,.018,.001],'Stalk_2',3,'lichen','lichen');seat(o,[0,1,2],trunk,'lichen-timber-root')
    # Twenty-two rooted growths use the 152 triangles recovered from tooth caps.
    for j,host in enumerate(arms):
        bone=('Stalk_1','Stalk_4','Stalk_3','Stalk_4')[j];s=-1 if j%2==0 else 1
        anchor=sum((host.data.vertices[i].co for i in range(10,15)),Vector())/5
        o=branch('CrookedBranchFork'+str(j),[anchor,anchor+Vector((s*.15,-.04,.17)),anchor+Vector((s*.27,.07,.07))],[.032,.026,.003],bone,3)
        seat(o,[0,1,2],host,'crooked-branch-fork')
    for j in range(6):
        s=-1 if j%2==0 else 1;z=1.30-.17*(j//2);p=Vector((s*.16,.12,z));tip=p+Vector((-s*.10,.10,-.34+.04*j))
        o=branch('TornTrunkBlade'+str(j),[p,tip],[.042,.004],'Stalk_2',3,'decay-root','bark-blades');seat(o,[0,1,2],trunk,'splintered-trunk-blade')
    for j in range(12):
        host=roots[j%8];anchor=sum((host.data.vertices[i].co for i in range(10,15)),Vector())/5;axis=Vector((math.sin(j*1.7)*.05,.025,.07+.025*(j%3)))
        points=[anchor+Vector(v) for v in ((-.032,0,-.018),(.035,.015,-.012),(0,-.025,.015))]+[anchor+axis]
        o=solid('BrokenRootKnot'+str(j),points,[(2,1,0),(0,1,3),(1,2,3),(2,0,3)],'decay-root','Root','root-knots');seat(o,[0,1,2],host,'broken-root-knot')
    # Directional coordinates follow each real timber member. Cup grain follows
    # radial petal growth; source noise no longer floats over the whole creature.
    for o in [o for o in bpy.context.scene.objects if o.type=='MESH']:
        points=np.array([list(v.co) for v in o.data.vertices]);center=points.mean(axis=0);_,_,basis=np.linalg.svd(points-center,full_matrices=False)
        projected=(points-center)@basis.T;lo=projected.min(axis=0);span=np.maximum(projected.max(axis=0)-lo,.001);coord=(projected-lo)/span
        coord=coord[:,[1,2,0]]
        if o.name.startswith(('CentralCrownCup','GraftedFlower')):
            xz=points[:,[0,2]]-center[[0,2]];coord[:,0]=(np.arctan2(xz[:,1],xz[:,0])+math.pi)/math.tau*3;coord[:,2]=np.linalg.norm(xz,axis=1)/max(np.linalg.norm(xz,axis=1).max(),.001)
        attr=o.data.attributes.new('CrownGrain','FLOAT_VECTOR','CORNER')
        for loop in o.data.loops:attr.data[loop.index].vector=coord[loop.vertex_index]
        tint=o.data.color_attributes.new(name='CrownOrganTint',type='FLOAT_COLOR',domain='CORNER')
        color=(.40,.17,.012,1) if o.name=='CentralAmberSeed' else (.16,.055,.006,1)
        for value in tint.data:value.color=color
    bpy.context.scene['silhouette_components']=json.dumps(components);bpy.context.scene['tissue_attachment_contract']=json.dumps(contacts)
    total=sum(len(o.data.loop_triangles) for o in bpy.context.scene.objects if o.type=='MESH')
    (OUT/'anatomy-authoring.json').write_text(json.dumps(dict(triangles=total,donor='Greydwarf_Shaman',donor_triangles=1422,soft_cap=2133,anatomy=['hollow timber abdomen with real front opening','eight broad attached buttress roots','central deep crown cup and four grafted secondary cups','rooted unequal timber teeth and crown spires','four animated grafted branch arms','six individually stem-supported amber pods','diagonal ribs across the hollow front'],supports=len(contacts),rig_bones=[b.name for b in arm.data.bones],actions=str(bpy.context.scene['magenheim_authored_actions']),remaining='reference-led refinement of crooked timber/tissue, porous amber, branch/rib asymmetry, bark/lichen and native animation',status='torn overlapping sepal candidate; no art acceptance'),indent=2)+'\n')
    assert total<=2133,total
    return trunk
