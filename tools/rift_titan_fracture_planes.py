"""R16 head: solid ridged face, smaller broken flank sockets, crisp stone atlas.

The twenty accepted frontal X/Z landmarks are immutable. Twelve inner vertices
describe long stepped stone planes; five-corner pockets leave more stone intact.
"""
import bpy, bmesh, json, math
from pathlib import Path
from mathutils import Vector, geometry
import rift_titan_paired_recesses as previous

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'docs/art/creature-models/2026-10-16/rift-titan-fracture-planes-r16'
POCKET = ((.145,.38),(.285,.25),(.565,.48),(.425,.74),(.215,.73))
INSET = (.065,.105,.055,.095,.085)

def stone_material():
    mat = bpy.data.materials.new('Head layered cool stone and broken warm edges')
    mat.use_nodes = True
    n=mat.node_tree.nodes; l=mat.node_tree.links
    bs=n.get('Principled BSDF'); bs.inputs['Roughness'].default_value=.86
    geo=n.new('ShaderNodeNewGeometry')
    scale=n.new('ShaderNodeVectorMath'); scale.operation='MULTIPLY'
    scale.inputs[1].default_value=(2.8,3.8,1.35); l.new(geo.outputs['Position'],scale.inputs[0])
    cells=n.new('ShaderNodeTexVoronoi'); cells.inputs['Scale'].default_value=7
    cells.distance='MANHATTAN'; l.new(scale.outputs['Vector'],cells.inputs['Vector'])
    bw=n.new('ShaderNodeRGBToBW'); l.new(cells.outputs['Color'],bw.inputs[0])
    ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.interpolation='CONSTANT'
    stops=[(.10,(.020,.026,.036)),(.27,(.035,.043,.057)),(.42,(.055,.063,.077)),(.62,(.075,.081,.087)),(.80,(.12,.115,.10)),(.92,(.12,.105,.065))]
    for i,(p,c) in enumerate(stops):
        e=ramp.color_ramp.elements[i] if i<2 else ramp.color_ramp.elements.new(p)
        e.position=p; e.color=(*c,1)
    l.new(bw.outputs[0],ramp.inputs[0])
    # Sparse longitudinal seams: sharp cells, no cloudy colour noise.
    seam=n.new('ShaderNodeTexVoronoi'); seam.feature='DISTANCE_TO_EDGE'
    seam.inputs['Scale'].default_value=5.5; l.new(scale.outputs['Vector'],seam.inputs['Vector'])
    edge=n.new('ShaderNodeValToRGB'); edge.color_ramp.elements[0].position=.012
    edge.color_ramp.elements[0].color=(.22,.22,.22,1)
    edge.color_ramp.elements[1].position=.035; l.new(seam.outputs['Distance'],edge.inputs[0])
    mul=n.new('ShaderNodeMixRGB'); mul.blend_type='MULTIPLY'; mul.inputs[0].default_value=.65
    l.new(ramp.outputs[0],mul.inputs[1]); l.new(edge.outputs[0],mul.inputs[2])
    l.new(mul.outputs[0],bs.inputs['Base Color'])
    bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.16
    bump.inputs['Distance'].default_value=.007
    l.new(edge.outputs[0],bump.inputs['Height']); l.new(bump.outputs[0],bs.inputs['Normal'])
    return mat

def build(B,arm,record,profile,tube,cap):
    OUT.mkdir(parents=True,exist_ok=True)
    previous.OUT=OUT
    trunk=previous.build(B,arm,record,profile,tube,cap)
    head=bpy.data.objects['UnifiedCrystalHead']; old=head.data
    stone=stone_material(); B.mats['head-fracture-stone']=stone
    base=min(v.co.z for v in old.vertices); height=max(v.co.z for v in old.vertices)-base
    # Front landmarks remain byte-for-byte; the back retains the main turns.
    retained=(20,22,23,25,26,28,29,31)
    vertices=[tuple(v.co) for v in list(old.vertices)[:20]]+[tuple(old.vertices[i].co) for i in retained]
    # Correct the depth profile against the supplied left-facing crop. The
    # front plane is a long backward-raking edge, not a bulging lower shield.
    def profile_at(t):return .80-.45*t, .035+.29*math.sin(math.pi*t)**.75
    for i,p in enumerate(vertices):
        x,y,z=p;t=(z-base)/height;front,depth=profile_at(t)
        vertices[i]=(x,front-(depth if i>=20 else 0)-(.025 if x<0 else .018)+x*(.30 if i>=20 else .55),z)
    # Three four-point chains define long stepped stone planes. Their joins
    # stay solid stone; the only violet remains inside the flank recesses.
    for track in range(3):
        for j,t in enumerate((.16,.39,.65,.86)):
            x=((- .13,-.11,-.08,-.075),(-.03,-.04,-.025,-.045),(.10,.105,.10,.065))[track][j]
            relief=((.025,.040,.030,.020),(-.010,-.014,-.008,-.005),(.035,.045,.025,.015))[track][j]
            y,_=profile_at(t);vertices.append((x,y-.025+x*.55+relief,base+height*t))
    faces=[]; roles=[]; lined=[]; sides=[]; plates=[]
    def face(ids,role=1,crystal=False,side=0,plate=False):
        faces.append(tuple(ids)); roles.append(role); lined.append(crystal); sides.append(side); plates.append(plate)
    def poly(ids,axes=(0,2),role=1,side=0,plate=False):
        pts=[Vector((vertices[i][axes[0]],vertices[i][axes[1]],0)) for i in ids]
        tris=geometry.tessellate_polygon([pts]); assert len(tris)==len(ids)-2
        for tri in tris:face([ids[v if isinstance(v,int) else pts.index(v)] for v in tri],role,role==3,side,plate)
    def bridge(a,b,side):
        def progress(chain):
            d=[0.]
            for i,j in zip(chain,chain[1:]):d.append(d[-1]+(Vector(vertices[i])-Vector(vertices[j])).length)
            return [v/d[-1] for v in d]
        pa,pb=progress(a),progress(b); i=j=0
        while i<len(a)-1 or j<len(b)-1:
            if j==len(b)-1 or (i<len(a)-1 and pa[i+1]<=pb[j+1]):face((a[i],a[i+1],b[j]),side=side);i+=1
            else:face((a[i],b[j+1],b[j]),side=side);j+=1
    poly(list(range(10))+[31,30,29,28],plate=True)
    poly([0,28,29,30,31,9,35,34,33,32],plate=True)
    poly([0,32,33,34,35,9,39,38,37,36],plate=True)
    poly(list(range(9,20))+[0,36,37,38,39],plate=True)
    poly(list(range(20,28)))
    face((9,10,24,23));face((19,0,20,27))
    def at_height(ids,t):
        z=base+height*t; pts=sorted([Vector(vertices[i]) for i in ids],key=lambda p:p.z)
        for a,b in zip(pts,pts[1:]):
            if a.z<=z<=b.z:return a.lerp(b,(z-a.z)/(b.z-a.z))
        raise ValueError(t)
    rows=[]
    for side,sign,front,back in ((1,-1,list(range(10)),[23,22,21,20]),(2,1,list(range(19,9,-1)),[24,25,26,27])):
        lip=[]; floor=[]
        for j,(t,s) in enumerate(POCKET):
            if side==2:t=t*.975+.008;s+=.01
            p=at_height(front,t).lerp(at_height(back,t),s)
            lip.append(len(vertices));vertices.append(tuple(p))
        for j,i in enumerate(lip):
            p=Vector(vertices[i]);p.x-=sign*INSET[j]*(1 if side==1 else .9)
            p.y+=(.008,-.009,.012,-.004,.008,-.012,.003)[j]
            floor.append(len(vertices));vertices.append(tuple(p))
        bridge(front,[lip[0],lip[1],lip[2]],side)
        poly([front[-1],back[0],lip[3],lip[2]],(1,2),side=side)
        bridge(back,[lip[3],lip[4]],side)
        poly([back[-1],front[0],lip[0],lip[4]],(1,2),side=side)
        for j in range(5):face((lip[j],lip[(j+1)%5],floor[(j+1)%5],floor[j]),2,j in (0,3,4),side)
        poly(floor,(1,2),3,side)
        rows.append(dict(side=side,lip=lip,floor=floor))
    me=bpy.data.meshes.new('RidgedFrontPairedBrokenRecessHead');me.from_pydata(vertices,[],faces)
    me.materials.append(stone);me.materials.append(old.materials[1]);head.data=me
    attrs=[('attachment_component','FACE'),('head_surface_role','FACE'),('head_crystal_lining','FACE'),('head_socket_side','FACE'),('head_front_plate','FACE'),('attachment_endpoint','POINT'),('head_outline_vertex','POINT'),('head_socket_ring','POINT'),('head_socket_corner','POINT')]
    for name,domain in attrs:me.attributes.new(name,'INT',domain)
    component=json.loads(bpy.context.scene['silhouette_components'])[head.name]
    for f,role,crystal,side,plate in zip(me.polygons,roles,lined,sides,plates):
        f.material_index=int(crystal)
        for name,value in [('attachment_component',component),('head_surface_role',role),('head_crystal_lining',int(crystal)),('head_socket_side',side),('head_front_plate',int(plate))]:me.attributes[name].data[f.index].value=value
    for v in me.vertices:
        i=v.index;me.attributes['head_outline_vertex'].data[i].value=i+1 if i<20 else 0
        if i>=40:
            me.attributes['head_socket_ring'].data[i].value=(i-40)//5+1
            me.attributes['head_socket_corner'].data[i].value=(i-40)%5+1
    group=head.vertex_groups.get('Body') or head.vertex_groups.new(name='Body')
    group.add(list(range(60)),1,'REPLACE')
    previous.previous.normals(head)
    assert len(me.vertices)==60 and len(me.loop_triangles)==116,(len(me.vertices),len(me.loop_triangles))
    bpy.context.view_layer.objects.active=head;head.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=1.1,island_margin=.006);bpy.ops.object.mode_set(mode='OBJECT')
    me.uv_layers.active.name='BossConstructionUV';head.select_set(False)
    # The changed rear profile requires re-seating complete neck cap sections.
    from first_bloom_deeper_chest import embed
    from first_bloom_frill_sculpt import assign
    from first_bloom_attachment_repair import tree
    from maw_contact_validation import contains
    import numpy as np
    bvh=tree(head);tris=np.array([[list(me.vertices[i].co) for i in t.vertices] for t in me.loop_triangles])
    for r in json.loads(bpy.context.scene['tissue_attachment_contract']):
        if r['support']!=head.name:continue
        neck=bpy.data.objects[r['tissue']];ids=[v.index for v in neck.data.vertices if neck.data.attributes['attachment_endpoint'].data[v.index].value==r['id']]
        def enclosed():
            a,b,c=[neck.data.vertices[i].co for i in ids]
            return all(contains(p,bvh,tris) for p in (a,b,c,(a+b+c)/3,(a+b)/2,(b+c)/2,(c+a)/2,a*.5+b*.25+c*.25,a*.25+b*.5+c*.25,a*.25+b*.25+c*.5))
        if enclosed():continue
        anchor,weights=embed(sum((neck.data.vertices[i].co for i in ids),Vector())/3,head,.035)
        original=[neck.data.vertices[i].co.copy() for i in ids]
        for contraction in (1,.85,.65,.45,.25,.1):
            for i,p in zip(ids,original):assign(neck,i,anchor+(p-anchor)*contraction,weights)
            if enclosed():break
        else:raise ValueError('unseated neck cap')
        previous.previous.normals(neck)
    (OUT/'fracture-plane-authoring.json').write_text(json.dumps(dict(front_landmarks='twenty r13/r14/r15 frontal X/Z landmarks unchanged; side depth/rake corrected',front_plate='twelve inner vertices form three chains of long stepped stone planes; no central opening',budget='eight rear perimeter vertices and five-corner paired pockets; 60 vertices / 116 triangles',pockets=rows,scope='head and necessary head-end seating only; body, non-end neck geometry and rig preserved',art_acceptance=False),indent=2)+'\n')
    return trunk
