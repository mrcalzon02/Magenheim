"""R19: lean overlapping mantle plates and baked, directional stone fractures.

No head, neck, collar, band, bone or action is moved. The two supported
upper-arm roots are reseated into the revised outer mantle.
The four outer mantle plates exchange two rear vertices for a front ridge.
"""
import bpy,bmesh,json,math
from pathlib import Path
from mathutils import Vector,geometry
import rift_titan_broken_strata as previous
from rift_titan_joint_silhouette import frame
from first_bloom_deeper_chest import embed
from first_bloom_frill_sculpt import assign
from first_bloom_attachment_repair import tree
from maw_contact_validation import contains
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/art/creature-models/2026-10-25/rift-titan-layered-mantle-r19'

def build(B,arm,record,profile,tube,cap):
    OUT.mkdir(parents=True,exist_ok=True);previous.OUT=OUT
    trunk=previous.build(B,arm,record,profile,tube,cap)
    scene=bpy.context.scene;parts=json.loads(scene['silhouette_components']);changes=[]
    def finish(o):
        bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED',ngon_method='EAR_CLIP');bm.to_mesh(o.data);bm.free();o.data.update();o.data.calc_loop_triangles()
        bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.05,island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT');o.data.uv_layers.active.name='BossConstructionUV';o.select_set(False)
    for side,sign in (('L',-1),('R',1)):
        for j in (1,2):
            o=bpy.data.objects['ShoulderStratum'+side+str(j)];p=[v.co.copy() for v in o.data.vertices];assert len(p)==14
            c=sum(p,Vector())/14
            for q in p:
                q.x=c.x+(q.x-c.x)*(.84 if j==1 else .94)
                q.y=c.y+(q.y-c.y)*.70
                q.z=c.z+(q.z-c.z)*(1.06 if j==1 else 1.10)+(.045 if j==2 else -.025)
            front=p[:8];rear=p[8:];low=Vector((min(q.x for q in front),min(q.z for q in front)));high=Vector((max(q.x for q in front),max(q.z for q in front)));mid=(low+high)/2;span=high-low
            flat=[Vector((q.x,q.z)) for q in front];outline=list(range(8))
            if sum(a.x*b.y-b.x*a.y for a,b in zip(flat,flat[1:]+flat[:1]))<0:outline.reverse()
            # A displaced long ridge follows the mantle's own vertical plane.
            inner=[Vector((mid.x+sign*span.x*.035,mid.y+span.y*t)) for t in (-.22,.22)]
            result=geometry.delaunay_2d_cdt(flat+inner,[(8,9)],[outline],1,1e-7)
            assert len(result[0])==10 and all(len(x)==1 for x in result[3])
            mapping=[x[0] for x in result[3]];faces=[tuple(mapping[i] for i in f) for f in result[2]]
            yy=sum(q.y for q in front)/8
            verts=front+[Vector((q.x,yy+.045+(.015 if k else 0),q.y)) for k,q in enumerate(inner)]
            # The back is a four-corner plane; perimeter zipper stays closed.
            keep=(0,1,3,5);verts.extend(rear[k] for k in keep)
            backflat=[Vector((rear[k].x,rear[k].z,0)) for k in keep]
            faces.extend(tuple(10+(v if isinstance(v,int) else backflat.index(v)) for v in reversed(t)) for t in geometry.tessellate_polygon([backflat]))
            # Project original rear stations to their corresponding outline
            # vertices. The nearest matching stations give a monotone zipper.
            matches=[]
            for k in keep:matches.append(min(range(8),key=lambda i:(Vector((front[i].x,front[i].z))-Vector((rear[k].x,rear[k].z))).length))
            assert len(set(matches))==4,(o.name,matches)
            for k,a in enumerate(matches):
                b=matches[(k+1)%4];chain=[];i=a
                while i!=b:chain.append(i);i=(i+1)%8
                chain.append(b);faces.extend((chain[t],chain[t+1],10+k) for t in range(len(chain)-1));faces.append((b,10+(k+1)%4,10+k))
            me=bpy.data.meshes.new(o.name+' layered fracture');me.from_pydata(verts,[],faces);me.materials.append(B.mats['rift-strata']);o.data=me
            for g in list(o.vertex_groups):o.vertex_groups.remove(g)
            o.vertex_groups.new(name='Body').add(list(range(14)),1,'REPLACE')
            a=me.attributes.new('attachment_component','INT','FACE')
            for v in a.data:v.value=parts[o.name]
            me.attributes.new('attachment_endpoint','INT','POINT');finish(o);assert len(me.loop_triangles)==24
            changes.append(dict(component=o.name,triangles=24,change='leaner depth, staggered overlapping outline and long front fracture ridge; four-corner rear'))
    # Stone detail has two distinct scales: long mineral planes and smaller
    # broken chips. Neither is a uniform cloudy overlay or painted lighting.
    stone=bpy.data.materials.new('Rift authored layered stone r19');stone.use_nodes=True;n=stone.node_tree.nodes;l=stone.node_tree.links;n.clear()
    coord=n.new('ShaderNodeAttribute');coord.attribute_name='RiftStoneGrain'
    def scale(x,factors):
        node=n.new('ShaderNodeVectorMath');node.operation='MULTIPLY';node.inputs[1].default_value=factors;l.new(x,node.inputs[0]);return node.outputs[0]
    def cells(x,feature='F1'):
        node=n.new('ShaderNodeTexVoronoi');node.feature=feature;node.inputs['Scale'].default_value=1;l.new(x,node.inputs['Vector']);return node
    def ramp(signal,stops,constant=False):
        node=n.new('ShaderNodeValToRGB');node.color_ramp.interpolation='CONSTANT' if constant else 'LINEAR'
        for i,(pos,col) in enumerate(stops):
            e=node.color_ramp.elements[i] if i<2 else node.color_ramp.elements.new(pos);e.position=pos;e.color=(*col,1)
        l.new(signal,node.inputs[0]);return node.outputs[0]
    def mix(a,b,factor,mode='MULTIPLY'):
        node=n.new('ShaderNodeMixRGB');node.blend_type=mode
        if isinstance(factor,(int,float)):node.inputs[0].default_value=factor
        else:l.new(factor,node.inputs[0])
        if isinstance(a,tuple):node.inputs[1].default_value=(*a,1)
        else:l.new(a,node.inputs[1])
        if isinstance(b,tuple):node.inputs[2].default_value=(*b,1)
        else:l.new(b,node.inputs[2])
        return node.outputs[0]
    base=cells(scale(coord.outputs['Vector'],(.85,.80,.55)));bw=n.new('ShaderNodeRGBToBW');l.new(base.outputs['Color'],bw.inputs[0])
    pigment=ramp(bw.outputs[0],[(.12,(.027,.032,.043)),(.32,(.055,.061,.073)),(.48,(.083,.083,.080)),(.66,(.115,.105,.084)),(.79,(.070,.076,.057)),(.91,(.15,.134,.103))],True)
    detail=cells(scale(coord.outputs['Vector'],(2.7,2.5,1.5)));bw2=n.new('ShaderNodeRGBToBW');l.new(detail.outputs['Color'],bw2.inputs[0]);chips=ramp(bw2.outputs[0],[(.22,(.25,.28,.33)),(.42,(.65,.67,.67)),(.72,(1.2,1.12,.98))],True)
    pigment=mix(pigment,chips,.58)
    edge=cells(scale(coord.outputs['Vector'],(2.7,2.5,1.5)),'DISTANCE_TO_EDGE');seam=ramp(edge.outputs['Distance'],[(.012,(.08,.09,.11)),(.045,(.38,.40,.42)),(.080,(1,1,1))]);pigment=mix(pigment,seam,.85)
    # Ochre chips and olive lichen occur sparsely along actual upward faces.
    geo=n.new('ShaderNodeNewGeometry');sep=n.new('ShaderNodeSeparateXYZ');l.new(geo.outputs['Normal'],sep.inputs[0]);up=n.new('ShaderNodeMath');up.operation='MAXIMUM';l.new(sep.outputs['Z'],up.inputs[0]);up.inputs[1].default_value=0
    patch=cells(scale(coord.outputs['Vector'],(4,4,2.5)));patchmask=n.new('ShaderNodeMath');patchmask.operation='LESS_THAN';l.new(patch.outputs['Distance'],patchmask.inputs[0]);patchmask.inputs[1].default_value=.24
    amount=n.new('ShaderNodeMath');amount.operation='MULTIPLY';l.new(patchmask.outputs[0],amount.inputs[0]);l.new(up.outputs[0],amount.inputs[1]);pigment=mix(pigment,(.070,.075,.029),amount.outputs[0],'MIX')
    ao=n.new('ShaderNodeAmbientOcclusion');ao.inputs['Distance'].default_value=.065;pigment=mix(pigment,ao.outputs['Color'],.55)
    bs=n.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=.90;l.new(pigment,bs.inputs['Base Color']);bevel=n.new('ShaderNodeBevel');bevel.inputs['Radius'].default_value=.006;bevel.samples=4
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.36;bump.inputs['Distance'].default_value=.007;l.new(seam,bump.inputs['Height']);l.new(bevel.outputs[0],bump.inputs['Normal']);l.new(bump.outputs[0],bs.inputs['Normal']);output=n.new('ShaderNodeOutputMaterial');l.new(bs.outputs[0],output.inputs[0])
    for o in [o for o in scene.objects if o.type=='MESH']:
        if o.name=='UnifiedCrystalHead':continue
        slots=[i for i,m in enumerate(o.data.materials) if m and ('stone' in m.name.lower() or m.name.startswith('Rift long gray strata') or m==B.mats['rift-strata'])]
        if not slots:continue
        for i in slots:o.data.materials[i]=stone
        if not o.data.attributes.get('RiftStoneGrain'):
            bone=max(o.vertex_groups,key=lambda g:sum(x.weight for v in o.data.vertices for x in v.groups if x.group==g.index)).name;basis=frame(arm.data.bones[bone].head_local-arm.data.bones[bone].tail_local);p=[basis.transposed()@v.co for v in o.data.vertices];lo=Vector(tuple(min(q[k] for q in p) for k in range(3)));span=Vector(tuple(max(q[k] for q in p)-lo[k] for k in range(3)));seed=sum(ord(c) for c in o.name);a=o.data.attributes.new('RiftStoneGrain','FLOAT_VECTOR','CORNER')
            for loop in o.data.loops:a.data[loop.index].vector=tuple((p[loop.vertex_index][k]-lo[k])/max(span[k],.001)*(3.2,3.5,1.8)[k]+math.sin(seed*(k+1)*.7)*5 for k in range(3))
    reseated=[]
    for r in json.loads(scene['tissue_attachment_contract']):
        if r['support'] not in ('ShoulderStratumL1','ShoulderStratumR1'):continue
        o=bpy.data.objects[r['tissue']];host=bpy.data.objects[r['support']];ids=[v.index for v in o.data.vertices if o.data.attributes['attachment_endpoint'].data[v.index].value==r['id']]
        for i in ids:p,w=embed(o.data.vertices[i].co,host,.018);assign(o,i,p,w)
        finish(o);bvh=tree(host);tri=np.array([[list(host.data.vertices[i].co) for i in t.vertices] for t in host.data.loop_triangles]);caps=[tuple(t.vertices) for t in o.data.loop_triangles if set(t.vertices)<=set(ids)]
        def enclosed():
            points=[o.data.vertices[i].co for i in ids]
            for a,b,c in caps:
                a,b,c=(o.data.vertices[i].co for i in (a,b,c));points.extend(((a+b+c)/3,(a+b)/2,(b+c)/2,(c+a)/2,a*.5+b*.25+c*.25,a*.25+b*.5+c*.25,a*.25+b*.25+c*.5))
            return all(contains(p,bvh,tri) for p in points)
        if not enclosed():
            old=[o.data.vertices[i].co.copy() for i in ids];anchor,w=embed(sum(old,Vector())/len(ids),host,.04)
            for scale in (.8,.6,.4,.2,.05):
                for i,p in zip(ids,old):assign(o,i,anchor+(p-anchor)*scale,w)
                if enclosed():break
            else:raise ValueError('exposed mantle root '+o.name)
            finish(o)
        reseated.append(r['id'])
    total=sum(len(o.data.loop_triangles) for o in scene.objects if o.type=='MESH');assert total==1800,total
    (OUT/'mantle-authoring.json').write_text(json.dumps(dict(triangles=total,changes=changes,reseated_contacts=reseated,preserved='all r18 head, neck, collar, wrap, inner shoulders, bone matrices and actions; only the two upper-arm roots reseated',surface='directional large gray fracture fields, smaller discrete stone chips, dark seams and sparse upward olive lichen; four 512px baked maps',status='candidate, requires visual comparison and native checks'),indent=2)+'\n')
    return trunk
