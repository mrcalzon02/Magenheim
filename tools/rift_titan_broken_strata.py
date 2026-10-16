"""R18: spend the existing budget on broken crest, stone digits and toe masses."""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector,geometry
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree
from maw_contact_validation import contains
from rift_titan_joint_silhouette import frame
import numpy as np
import rift_titan_concept_body as previous
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-16/rift-titan-broken-strata-r18'

def build(B,arm,record,profile,tube,cap):
    OUT.mkdir(parents=True,exist_ok=True);previous.OUT=OUT
    trunk=previous.build(B,arm,record,profile,tube,cap)
    scene=bpy.context.scene;parts=json.loads(scene['silhouette_components']);contacts=json.loads(scene['tissue_attachment_contract']);changes=[]
    meshes=lambda:[o for o in scene.objects if o.type=='MESH']
    floor=min(v.co.z for o in meshes() for v in o.data.vertices)
    def finish(o):
        bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED',ngon_method='EAR_CLIP');bm.to_mesh(o.data);bm.free();o.data.update();o.data.calc_loop_triangles()
        bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.05,island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT');o.data.uv_layers.active.name='BossConstructionUV';o.select_set(False)
    def replace(o,verts,faces,bone,key='rift-strata',root=(),marker=0):
        old=len(o.data.loop_triangles);me=bpy.data.meshes.new(o.name+' broken stone');me.from_pydata(verts,[],faces);me.materials.append(B.mats[key]);o.data=me
        for g in list(o.vertex_groups):o.vertex_groups.remove(g)
        o.vertex_groups.new(name=bone).add(list(range(len(verts))),1,'REPLACE')
        a=me.attributes.new('attachment_component','INT','FACE')
        for d in a.data:d.value=parts[o.name]
        a=me.attributes.new('attachment_endpoint','INT','POINT')
        for i in root:a.data[i].value=marker
        finish(o);changes.append(dict(name=o.name,before=old,after=len(me.loop_triangles)))
    def tess(points,axes=(0,2)):
        flat=[Vector((p[axes[0]],p[axes[1]],0)) for p in points]
        return [tuple(v if isinstance(v,int) else flat.index(v) for v in t) for t in geometry.tessellate_polygon([flat])]
    def bridge(a,b):
        # Parameterized cyclic zipper supports unequal section counts.
        faces=[];i=j=0
        while i<len(a) or j<len(b):
            if j==len(b) or (i<len(a) and (i+1)/len(a)<(j+1)/len(b)-1e-8):faces.append((a[i%len(a)],a[(i+1)%len(a)],b[j%len(b)]));i+=1
            elif i==len(a) or (j+1)/len(b)<(i+1)/len(a)-1e-8:faces.append((a[i%len(a)],b[(j+1)%len(b)],b[j%len(b)]));j+=1
            else:faces.append((a[i%len(a)],a[(i+1)%len(a)],b[(j+1)%len(b)],b[j%len(b)]));i+=1;j+=1
        return faces
    # Six visible mantle faces keep their chipped eight-point outlines. The
    # less visible rear loops lose two points; inner front ridges and thicker
    # digit sections use the recovered triangles. No support marker is removed.
    for side in ('L','R'):
        for j in range(3):
            o=bpy.data.objects['ShoulderStratum'+side+str(j)];p=[v.co.copy() for v in o.data.vertices];assert len(p)==16
            c=sum(p,Vector())/16
            # Inner mantle is the established collar support. Its envelope
            # stays fixed; outer/rear slabs gain the longer stone silhouette.
            if j:
                for q in p:q.x=c.x+(q.x-c.x)*.92;q.z=c.z+(q.z-c.z)*1.10
            front=p[8:16];keep=list(range(8))
            while len(keep)>6:
                k=min(range(1,len(keep)),key=lambda k:(p[keep[k]]-p[keep[k-1]]).cross(p[keep[(k+1)%len(keep)]]-p[keep[k]]).length)
                keep.pop(k)
            verts=front+[p[k] for k in keep];faces=tess(front)+[tuple(8+i for i in reversed(t)) for t in tess([p[k] for k in keep])]
            # Match the retained original perimeter stations, including the
            # small front-only chip, without crossing bridge diagonals.
            for k,a in enumerate(keep):
                b=keep[(k+1)%len(keep)];chain=[];i=a
                while i!=b:chain.append(i);i=(i+1)%8
                chain.append(b)
                faces.extend((chain[t],chain[t+1],8+k) for t in range(len(chain)-1))
                faces.append((b,8+(k+1)%len(keep),8+k))
            if j==0:
                chosen=max(range(len(tess(front))),key=lambda i:(verts[faces[i][1]]-verts[faces[i][0]]).cross(verts[faces[i][2]]-verts[faces[i][0]]).length)
                tri=faces.pop(chosen);ridge=sum((verts[i] for i in tri),Vector())/3+Vector((0,.065,0));n=len(verts);verts.append(ridge);faces.extend((tri[k],tri[(k+1)%3],n) for k in range(3))
            replace(o,verts,faces,'Body')
    # Existing limb plates become longer overlapping strata in each bone's
    # actual 3D direction. Topology, endpoint IDs and blend ownership survive.
    for o in meshes():
        if not o.name.startswith('LongFracture'):continue
        bone=next(n for n in ('ForearmL','ForearmR','ArmL','ArmR','LegL','LegR') if n in o.name)
        basis=frame(arm.data.bones[bone].head_local-arm.data.bones[bone].tail_local);inv=basis.transposed();c=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
        for v in o.data.vertices:
            q=inv@(v.co-c);q.x*=.88;q.y*=.90;q.z*=1.13;v.co=c+basis@q
        finish(o)
        if any(a.value for a in o.data.attributes['attachment_endpoint'].data):continue
        # Four interior points, paired into two constrained long fracture
        # edges, replace a single radial face hub. Rear section simplification
        # pays for these edges at exactly the existing component cost.
        count=len(o.data.vertices);stations=7 if o.name.endswith('1') and ('Forearm' in o.name or 'Leg' in o.name) else 6
        p=[inv@(v.co-c) for v in o.data.vertices];front=p[stations:2*stations];rear=p[:stations];rear_count=count-stations-4
        assert rear_count in (3,4),(o.name,count,stations)
        keep=[round(k*stations/rear_count)%stations for k in range(rear_count)]
        low=Vector((min(q.x for q in front),min(q.z for q in front)));high=Vector((max(q.x for q in front),max(q.z for q in front)));mid=(low+high)/2;span=high-low
        flat=[Vector((q.x,q.z)) for q in front];cap_tri=tess(front)
        def inside_face(q):return any(geometry.intersect_point_tri_2d(q,flat[a],flat[b],flat[d]) for a,b,d in cap_tri)
        inner=[]
        for track in (-1,1):
            for end in (-1,1):
                q=mid+Vector((span.x*.16*track,span.y*.22*end))
                while not inside_face(q):q=q.lerp(mid,.25)
                inner.append(Vector((q.x,max(v.y for v in front)+(.04 if track<0 else -.025)+end*.012,q.y)))
        coords=[Vector((q.x,q.z)) for q in front+inner];area=sum(a.x*b.y-b.x*a.y for a,b in zip(flat,flat[1:]+flat[:1]));outline=list(range(stations))
        if area<0:outline.reverse()
        result=geometry.delaunay_2d_cdt(coords,[(stations,stations+1),(stations+2,stations+3)],[outline],1,1e-7)
        assert len(result[0])==len(coords) and all(len(ids)==1 for ids in result[3]),o.name
        mapping=[ids[0] for ids in result[3]];faces=[tuple(mapping[i] for i in f) for f in result[2]]
        start=stations+4;verts=front+inner+[rear[k] for k in keep];faces.extend(tuple(start+i for i in reversed(t)) for t in tess([rear[k] for k in keep]))
        for k,a in enumerate(keep):
            b=keep[(k+1)%len(keep)];chain=[];i=a
            while i!=b:chain.append(i);i=(i+1)%stations
            chain.append(b);faces.extend((chain[t],chain[t+1],start+k) for t in range(len(chain)-1));faces.append((b,start+(k+1)%len(keep),start+k))
        replace(o,[c+basis@q for q in verts],faces,bone)
        assert len(o.data.vertices)==count,(o.name,count,len(o.data.vertices))
    # Bent stone digits: a supported four-point root, five-corner knuckle with
    # an actual forward ridge and a thick four-corner unequal lower section.
    for side,sign in (('L',-1),('R',1)):
        for j in range(3):
            o=bpy.data.objects['FingerStratum'+side+str(j)];old=[v.co.copy() for v in o.data.vertices]
            c=sum((old[i] for i in (1,2,3,8,7,6)),Vector())/6;target=floor+(.075,.012,.12)[j];height=c.z-target
            middle=c+Vector((sign*(j-1)*.035,.055,-height*.54));tip=c+Vector((sign*(j-1)*.085,.015,target-c.z));width=(.115,.13,.105)[j];depth=(.13,.15,.12)[j]
            roots=[c+Vector(v) for v in ((-width*.7,.065,0),(width*.7,.065,.015),(width*.65,-.065,0),(-width*.65,-.065,-.01))]
            verts=roots+[middle+Vector(v) for v in ((-width,depth*.55,.035),(-width*.08,depth*1.32,.055),(width,depth*.6,-.025),(width*.78,-depth,-.01),(-width*.82,-depth,.015))]+[tip+Vector(v) for v in ((-width*.70,depth*.72,.025),(width*.80,depth*.65,0),(width*.65,-depth*.75,.07),(-width*.68,-depth*.85,.06))]
            faces=[(3,2,1,0)]+bridge([0,1,2,3],[4,5,6,7,8])+bridge([4,5,6,7,8],[9,10,11,12])+[(9,10,11,12)]
            marker=next(r['id'] for r in contacts if r['tissue']==o.name);replace(o,verts,faces,'Forearm'+side,root=range(4),marker=marker)
            next(r for r in contacts if r['id']==marker)['endpoint_seating']='four-point complete root section embedded into palm with matching Forearm weights'
    # Three separately fractured toe/heel masses per foot; all three soles
    # share the preserved ground plane and fit beneath the lower shin.
    for side in ('L','R'):
        o=bpy.data.objects['Foot'+side];c=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices);verts=[];faces=[]
        for j,(x,y,w,d,h) in enumerate(((-.18,.10,.145,.25,.25),(.14,.12,.16,.29,.22),(.015,-.16,.20,.16,.32))):
            base=len(verts);cx=c.x+x;cy=c.y+y
            if j<2:
                pts=[(-w,-d,0),(-w*.9,d*.9,0),(w*.9,d,0),(w,-d*.85,0),(-w*.72,-d*.68,h*.78),(-w*.76,d*.78,h),(w*.70,d*.82,h*.88),(w*.79,-d*.6,h*.69)]
                ff=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
            else:
                pts=[(-w,-d,0),(-w*.6,d,0),(w,d*.2,0),(w*.1,-d*.05,h)]
                ff=[(0,2,1),(0,1,3),(1,2,3),(2,0,3)]
            verts.extend((cx+a,cy+b,floor+z) for a,b,z in pts)
            faces.extend(tuple(base+i for i in f) for f in ff)
        replace(o,verts,faces,'Leg'+side)
    # The two raised crest fragments have broken, sloping five-corner profiles.
    # The lower rear bar becomes a three-section bent architectural arch.
    for j in (0,1):
        o=bpy.data.objects['BrokenCrestBeam'+str(j)];p=[v.co.copy() for v in o.data.vertices];c=sum(p,Vector())/8;w=(.30,.28)[j];h=(.11,.15)[j];d=.12
        outline=[(-w,-h*.45),(-w*.92,h*.7),(-w*.12,h),(w,h*.25),(w*.74,-h)]
        verts=[c+Vector((x,y*(.75,1.08,.8,1.12,.7)[k],z+x*(.16 if j else -.14))) for y in (d,-d) for k,(x,z) in enumerate(outline)];ff=tess(verts[:5])+[tuple(5+i for i in reversed(t)) for t in tess(verts[5:])]+[(k,(k+1)%5,(k+1)%5+5,k+5) for k in range(5)]
        replace(o,verts,ff,'Body')
    o=bpy.data.objects['BrokenCrestBeam2'];verts=[]
    for x,y,z,w,h in ((-1.00,-.48,3.40,.11,.07),(.02,-1.05,3.26,.15,.09),(.99,-.48,3.34,.10,.065)):
        verts.extend((x,y+dy*w,z+dz*h) for dy,dz in ((1,-1),(1,1),(-1,1),(-1,-1)))
    ff=[(3,2,1,0),(8,9,10,11)]+[(4*k+i,4*k+(i+1)%4,4*(k+1)+(i+1)%4,4*(k+1)+i) for k in range(2) for i in range(4)]
    replace(o,verts,ff,'Body')
    for j in range(3):
        o=bpy.data.objects['RaisedCrestFragment'+str(j)];c=sum((v.co for v in o.data.vertices),Vector())/4
        for v in o.data.vertices:v.co=c+Vector(((v.co.x-c.x)*1.5,(v.co.y-c.y)*2.5,(v.co.z-c.z)*1.2))
        finish(o)
        o=bpy.data.objects['CrestVioletMote'+str(j)];c=sum((v.co for v in o.data.vertices),Vector())/5;r=.05+j*.008
        verts=[c+Vector(v) for v in ((-r,0,-.08),(r,.045,-.04),(r*.2,-.045,.10),(-r*.4,-.035,.025))];replace(o,verts,[(0,1,2),(0,3,1),(0,2,3),(1,3,2)],'Body','binding-prism')
    # Smaller secondary core splinters fund visible silhouette improvements;
    # the three main uneven crystals retain their full eight-face volumes.
    for name in ('SuspendedCore1','CoreFracture4','CoreFracture5'):
        o=bpy.data.objects[name];p=[v.co.copy() for v in o.data.vertices];c=sum(p,Vector())/6;verts=[c+(p[i]-c)*.82 for i in (0,1,2,4)]
        # Distinct back vertex prevents a planar shard along the X/Z section.
        verts[-1].y-=.12
        replace(o,verts,[(0,1,2),(0,3,1),(0,2,3),(1,3,2)],'Body','binding-prism')
    # Local rock coordinates make long, restrained fracture marks follow each
    # individual slab. These authoring attributes are baked, never exported.
    stone=bpy.data.materials.new('Rift long gray strata with sparse warm mineral');stone.use_nodes=True;n=stone.node_tree.nodes;l=stone.node_tree.links;n.clear()
    coord=n.new('ShaderNodeAttribute');coord.attribute_name='RiftStoneGrain'
    cell=n.new('ShaderNodeTexVoronoi');cell.distance='MANHATTAN';cell.inputs['Scale'].default_value=1;l.new(coord.outputs['Vector'],cell.inputs['Vector'])
    bw=n.new('ShaderNodeRGBToBW');l.new(cell.outputs['Color'],bw.inputs[0]);ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.interpolation='CONSTANT'
    for i,(p,color) in enumerate(((.12,(.032,.039,.049)),(.30,(.052,.061,.073)),(.49,(.075,.082,.090)),(.66,(.094,.091,.079)),(.78,(.068,.073,.041)),(.89,(.125,.111,.078)))):
        e=ramp.color_ramp.elements[i] if i<2 else ramp.color_ramp.elements.new(p);e.position=p;e.color=(*color,1)
    l.new(bw.outputs[0],ramp.inputs[0]);seam=n.new('ShaderNodeTexVoronoi');seam.feature='DISTANCE_TO_EDGE';seam.inputs['Scale'].default_value=1.7;l.new(coord.outputs['Vector'],seam.inputs['Vector'])
    edge=n.new('ShaderNodeValToRGB');edge.color_ramp.elements[0].position=.008;edge.color_ramp.elements[0].color=(.35,.35,.35,1);edge.color_ramp.elements[1].position=.026;l.new(seam.outputs['Distance'],edge.inputs[0])
    mul=n.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=.45;l.new(ramp.outputs[0],mul.inputs[1]);l.new(edge.outputs[0],mul.inputs[2]);ao=n.new('ShaderNodeAmbientOcclusion');ao.inputs['Distance'].default_value=.11
    shade=n.new('ShaderNodeMixRGB');shade.blend_type='MULTIPLY';shade.inputs[0].default_value=.65;l.new(mul.outputs[0],shade.inputs[1]);l.new(ao.outputs['Color'],shade.inputs[2])
    bs=n.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=.88;l.new(shade.outputs[0],bs.inputs['Base Color']);bevel=n.new('ShaderNodeBevel');bevel.inputs['Radius'].default_value=.007;bevel.samples=4
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.18;bump.inputs['Distance'].default_value=.004;l.new(edge.outputs[0],bump.inputs['Height']);l.new(bevel.outputs['Normal'],bump.inputs['Normal']);l.new(bump.outputs[0],bs.inputs['Normal']);output=n.new('ShaderNodeOutputMaterial');l.new(bs.outputs[0],output.inputs[0])
    for o in meshes():
        slots=[i for i,m in enumerate(o.data.materials) if m and ('stone' in m.name.lower() or m==B.mats['rift-strata'] or m==B.mats.get('head-fracture-stone'))]
        if not slots:continue
        for i in slots:o.data.materials[i]=stone
        bone=max(o.vertex_groups,key=lambda g:sum(x.weight for v in o.data.vertices for x in v.groups if x.group==g.index)).name
        basis=frame(arm.data.bones[bone].head_local-arm.data.bones[bone].tail_local);inv=basis.transposed();p=[inv@v.co for v in o.data.vertices];lo=Vector(tuple(min(q[k] for q in p) for k in range(3)));span=Vector(tuple(max(q[k] for q in p)-lo[k] for k in range(3)));seed=sum(ord(c) for c in o.name)
        a=o.data.attributes.new('RiftStoneGrain','FLOAT_VECTOR','CORNER')
        for loop in o.data.loops:
            q=p[loop.vertex_index];a.data[loop.index].vector=tuple((q[k]-lo[k])/max(span[k],.001)*(3.2,3.5,1.8)[k]+math.sin(seed*(k+1)*.7)*5 for k in range(3))
    # Reseating is restricted to supports actually changed in this revision.
    # Head, neck and the two torso rings retain exact previous coordinates.
    modified={r['name'] for r in changes}|{o.name for o in meshes() if o.name.startswith('LongFracture')}
    checked=[]
    for r in contacts:
        if r['tissue'].startswith('NeckMetalRib') or r['tissue'] in ('StoneCollarbone','WrappedStoneBand'):continue
        o=bpy.data.objects[r['tissue']];host=bpy.data.objects[r['support']];ids=[v.index for v in o.data.vertices if o.data.attributes['attachment_endpoint'].data[v.index].value==r['id']]
        if r['support'] in modified or r['tissue'] in modified:
            for i in ids:p,w=embed(o.data.vertices[i].co,host,.018);assign(o,i,p,w)
        finish(o);bvh=tree(host);tri=np.array([[list(host.data.vertices[i].co) for i in t.vertices] for t in host.data.loop_triangles]);marked=set(ids);caps=[tuple(t.vertices) for t in o.data.loop_triangles if set(t.vertices)<=marked]
        def enclosed():
            points=[o.data.vertices[i].co for i in ids]
            for a,b,c in caps:
                a,b,c=(o.data.vertices[i].co for i in (a,b,c));points.extend(((a+b+c)/3,(a+b)/2,(b+c)/2,(c+a)/2,a*.5+b*.25+c*.25,a*.25+b*.5+c*.25,a*.25+b*.25+c*.5))
            return all(contains(p,bvh,tri) for p in points)
        if not enclosed():
            original=[o.data.vertices[i].co.copy() for i in ids];anchor,w=embed(sum(original,Vector())/len(ids),host,.04)
            for amount in (.85,.65,.45,.25,.1):
                for i,p in zip(ids,original):assign(o,i,anchor+(p-anchor)*amount,w)
                if enclosed():break
            else:raise ValueError('Exposed root '+o.name)
            finish(o)
        checked.append(r['id'])
    scene['silhouette_components']=json.dumps(parts);scene['tissue_attachment_contract']=json.dumps(contacts)
    count=sum(len(o.data.loop_triangles) for o in meshes());assert count<=1800,count
    (OUT/'strata-authoring.json').write_text(json.dumps(dict(actor_triangles=count,changes=changes,rechecked_contacts=checked,preserved='exact head/neck geometry, torso ring geometry, eight bone transforms/hierarchy and seven actions from r17',reference='committed unaltered front and left-facing side concept; thick bent rocky fingers, low broken toe groups, long mantle/limb strata and bent rear architecture',surface='locally directed long gray fracture patches, sparse warm/olive mineral, thin recessed seams; baked into existing four 512 maps',acceptance='candidate; user art and native acceptance separate'),indent=2)+'\n')
    return trunk
