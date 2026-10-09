"""Closed heat-fractured dorsal scutes for the established Sulfurous Ashmite.

The five existing dorsal plates retain their authoring anchors, material and
abdomen/thorax bone assignments. Coordinates are mesh-local.
"""
from math import cos, exp, isfinite, pi, sin

RINGS = 16
SIDES = 64
CONTRACT = 'ashmite-five-heat-fractured-scutella-16x64'
PROFILE = ((.09,.94),(.18,.94),(.28,.93),(.38,.90),(.48,.87),
           (.58,.81),(.68,.75),(.78,.64),(.87,.54),(.94,.39),
           (1.,.08),(.98,-.16),(.85,-.35),(.65,-.46),(.40,-.44),(.20,-.34))


def scute_mesh_data(rx, ry, rz, plate):
    """Generate a five-identity, UV-safe, outward-wound solid carapace plate."""
    if not (1 <= plate <= 5):
        raise ValueError('Ashmite scute plate must be 1..5')
    if not all(isfinite(v) and v > 0 for v in (rx,ry,rz)):
        raise ValueError('Ashmite scute radii must be positive finite numbers')
    vertices = [(0.,0.,rz*.94), (0.,0.,-rz*.34)]
    for ring,(rad,level) in enumerate(PROFILE):
        for j in range(SIDES):
            a=2*pi*j/SIDES
            rim=(.040*cos(7*a+plate*.31)+.016*cos(11*a-plate*.43)) * min(1.,rad/.55)
            x=rx*rad*(1+rim)*cos(a)
            y=ry*rad*(1+rim)*sin(a)
            dorsal=max(0.,sin(pi*rad))**2 if ring < 11 else 0.
            crest=.12*exp(-((x/(rx*.32))**2))*dorsal
            crack=max(0.,cos(6*a+plate*.51+rad*.80))**18
            fracture=.095*crack*dorsal
            # Rear overlap and lateral load-bearing ridges are physical geometry,
            # not painted marks. Keep the approved five-scute footprint.
            trailing=max(0.,-sin(a))**8
            flank=abs(cos(a))**10
            rear_lip=.13*trailing*exp(-((rad-.91)/.12)**2)
            flank_buttress=.075*flank*exp(-((rad-.69)/.19)**2)
            z=rz*(level+crest-fracture+rear_lip+flank_buttress)
            vertices.append((x,y,z))
    faces=[]
    for j in range(SIDES):
        faces.append((0,2+j,2+(j+1)%SIDES))
    for ring in range(RINGS-1):
        first=2+ring*SIDES; nxt=first+SIDES
        for j in range(SIDES):
            k=(j+1)%SIDES
            faces.append((first+j,nxt+j,nxt+k,first+k))
    last=2+(RINGS-1)*SIDES
    for j in range(SIDES):
        faces.append((1,last+(j+1)%SIDES,last+j))
    return vertices, faces


def scute_uv(vertex, rx, ry):
    """Continuous plate-space mapping for existing heat-chitin PBR textures."""
    x,y,_=vertex
    return (.5+x/(2.30*rx), .5+y/(2.30*ry))


def sculpted_scute(name, loc, scale, material, plate):
    """Blender adapter, preserving author-side rigging and material assignment."""
    import bpy
    rx,ry,rz=scale
    vertices,faces=scute_mesh_data(rx,ry,rz,plate)
    mesh=bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(vertices,[],faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    obj.location=loc
    mesh.materials.append(material)
    obj['magenheim_ashmite_scute_contract']=CONTRACT
    obj['magenheim_ashmite_scute_index']=plate
    layer=mesh.uv_layers.new(name='AshmiteUV')
    for poly in mesh.polygons:
        poly.use_smooth=len(poly.vertices)==4
        for li in poly.loop_indices:
            vertex=mesh.vertices[mesh.loops[li].vertex_index].co
            layer.data[li].uv=scute_uv(vertex,rx,ry)
    return obj
